using System.Text.Json;
using Avra.EventBus.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avra.EventBus.Sql;

/// <summary>
/// Polls the broker table for events this service subscribes to, claims each one exactly once
/// per subscriber, and fans it out to the registered handlers.
/// </summary>
public sealed class SqlEventBusDispatcher(
    IDbContextFactory<EventBusDbContext> contextFactory,
    IServiceScopeFactory scopeFactory,
    IOptions<SqlEventBusOptions> options,
    IOptions<EventBusSubscriptionInfo> subscriptionInfo,
    ILogger<SqlEventBusDispatcher> logger) : BackgroundService
{
    private readonly SqlEventBusOptions _options = options.Value;
    private readonly EventBusSubscriptionInfo _subscriptions = subscriptionInfo.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_subscriptions.EventTypes.Count == 0)
        {
            logger.LogInformation("No integration event subscriptions registered; dispatcher is idle");
            return;
        }

        await EnsureBrokerAsync(stoppingToken);

        var cleanupDue = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var handled = await PumpAsync(stoppingToken);

                if (DateTime.UtcNow >= cleanupDue)
                {
                    await PurgeAsync(stoppingToken);
                    cleanupDue = DateTime.UtcNow.AddHours(1);
                }

                // Only back off when the broker was empty, so bursts drain quickly.
                if (handled == 0)
                {
                    await Task.Delay(_options.PollingInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Integration event dispatcher poll failed; retrying");

                // Services race to create the shared broker on first start, so re-attempt it here.
                await EnsureBrokerAsync(stoppingToken);
                await Task.Delay(_options.PollingInterval, stoppingToken);
            }
        }
    }

    private async Task EnsureBrokerAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Another service may be creating it concurrently; polling will recover.
            logger.LogWarning(ex, "Could not verify the event bus broker schema on start-up");
        }
    }

    private async Task<int> PumpAsync(CancellationToken cancellationToken)
    {
        // A List, not an array: on an array the compiler binds Contains to the ReadOnlySpan
        // overload, which EF Core cannot translate.
        var eventNames = _subscriptions.EventTypes.Keys.ToList();

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var pending = await context.Messages
            .Where(m => eventNames.Contains(m.EventName))
            .Where(m => !context.Deliveries.Any(d =>
                d.MessageSequence == m.Sequence &&
                d.Subscriber == _options.SubscriberName &&
                (d.State == DeliveryState.Completed || d.State == DeliveryState.Poisoned)))
            .OrderBy(m => m.Sequence)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        var handled = 0;

        foreach (var message in pending)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var delivery = await ClaimAsync(message, cancellationToken);
            if (delivery is null)
            {
                continue;
            }

            handled++;
            await DeliverAsync(message, delivery, cancellationToken);
        }

        return handled;
    }

    private async Task<EventBusDelivery?> ClaimAsync(EventBusMessage message, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var delivery = await context.Deliveries.FirstOrDefaultAsync(
            d => d.MessageSequence == message.Sequence && d.Subscriber == _options.SubscriberName,
            cancellationToken);

        if (delivery is null)
        {
            delivery = new EventBusDelivery
            {
                MessageSequence = message.Sequence,
                Subscriber = _options.SubscriberName,
                State = DeliveryState.InProgress,
                Attempts = 1,
                ClaimedAt = DateTime.UtcNow,
            };
            context.Deliveries.Add(delivery);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return delivery;
            }
            catch (DbUpdateException)
            {
                // The unique index rejected us: a competing instance owns this message.
                return null;
            }
        }

        if (delivery.State is DeliveryState.Completed or DeliveryState.Poisoned)
        {
            return null;
        }

        delivery.Attempts++;
        delivery.State = DeliveryState.InProgress;
        delivery.ClaimedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return delivery;
    }

    private async Task DeliverAsync(EventBusMessage message, EventBusDelivery delivery, CancellationToken cancellationToken)
    {
        try
        {
            if (!_subscriptions.EventTypes.TryGetValue(message.EventName, out var eventType))
            {
                await CompleteAsync(delivery, DeliveryState.Completed, null, cancellationToken);
                return;
            }

            if (JsonSerializer.Deserialize(message.Content, eventType, _subscriptions.JsonSerializerOptions)
                is not IntegrationEvent integrationEvent)
            {
                await CompleteAsync(delivery, DeliveryState.Poisoned, "Payload could not be deserialised", cancellationToken);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
            var handlers = scope.ServiceProvider.GetServices(handlerType).Cast<IIntegrationEventHandler>();

            foreach (var handler in handlers)
            {
                await handler.HandleAsync(integrationEvent, cancellationToken);
            }

            await CompleteAsync(delivery, DeliveryState.Completed, null, cancellationToken);
            logger.LogInformation("Handled integration event {EventName} ({EventId})", message.EventName, message.MessageId);
        }
        catch (Exception ex)
        {
            var exhausted = delivery.Attempts >= _options.MaxDeliveryAttempts;
            logger.Log(
                exhausted ? LogLevel.Error : LogLevel.Warning,
                ex,
                "Handling integration event {EventName} ({EventId}) failed on attempt {Attempt}",
                message.EventName,
                message.MessageId,
                delivery.Attempts);

            await CompleteAsync(
                delivery,
                exhausted ? DeliveryState.Poisoned : DeliveryState.Failed,
                ex.Message,
                cancellationToken);
        }
    }

    private async Task CompleteAsync(EventBusDelivery delivery, DeliveryState state, string? error, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var tracked = await context.Deliveries.FirstOrDefaultAsync(d => d.Id == delivery.Id, cancellationToken);
        if (tracked is null)
        {
            return;
        }

        tracked.State = state;
        tracked.Error = error?.Length > 2000 ? error[..2000] : error;
        tracked.CompletedAt = state == DeliveryState.Failed ? null : DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.Subtract(_options.Retention);

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var stale = await context.Messages
            .Where(m => m.EnqueuedAt < cutoff)
            .Select(m => m.Sequence)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return;
        }

        await context.Deliveries.Where(d => stale.Contains(d.MessageSequence)).ExecuteDeleteAsync(cancellationToken);
        await context.Messages.Where(m => stale.Contains(m.Sequence)).ExecuteDeleteAsync(cancellationToken);
    }
}
