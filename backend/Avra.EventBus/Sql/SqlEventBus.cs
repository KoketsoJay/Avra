using System.Text.Json;
using Avra.EventBus.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avra.EventBus.Sql;

/// <summary>
/// Publishes integration events by appending them to the shared Azure SQL broker table.
/// </summary>
public sealed class SqlEventBus(
    IDbContextFactory<EventBusDbContext> contextFactory,
    IOptions<EventBusSubscriptionInfo> subscriptionInfo,
    ILogger<SqlEventBus> logger) : IEventBus
{
    private readonly JsonSerializerOptions _jsonOptions = subscriptionInfo.Value.JsonSerializerOptions;

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventName = integrationEvent.GetType().Name;
        var message = new EventBusMessage
        {
            MessageId = integrationEvent.Id,
            EventName = eventName,
            // Serialise against the concrete type so derived members are not dropped.
            Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), _jsonOptions),
            EnqueuedAt = DateTime.UtcNow,
        };

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Messages.Add(message);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Published integration event {EventName} ({EventId})", eventName, integrationEvent.Id);
    }
}
