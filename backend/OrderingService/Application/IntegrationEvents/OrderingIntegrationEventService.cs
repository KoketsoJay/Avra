using Avra.EventBus.Abstractions;
using Avra.EventBus.Outbox;
using Microsoft.Extensions.Logging;
using OrderingService.Infrastructure;

namespace OrderingService.Application.IntegrationEvents;

/// <summary>
/// Implements the outbox handshake: events are written inside the order transaction, and only
/// after it commits are they pushed to the bus and marked published. A crash anywhere in
/// between leaves a replayable record rather than a lost event.
/// </summary>
public class OrderingIntegrationEventService(
    IEventBus eventBus,
    OrderingContext orderingContext,
    IIntegrationEventLogService eventLogService,
    ILogger<OrderingIntegrationEventService> logger) : IOrderingIntegrationEventService
{
    public async Task PublishEventsThroughEventBusAsync(Guid transactionId)
    {
        var pendingLogEvents = await eventLogService.RetrieveEventLogsPendingToPublishAsync(transactionId);

        foreach (var logEvt in pendingLogEvents)
        {
            logger.LogInformation("Publishing integration event {EventId} - {Event}", logEvt.EventId, logEvt.IntegrationEvent);

            try
            {
                await eventLogService.MarkEventAsInProgressAsync(logEvt.EventId);
                await eventBus.PublishAsync(logEvt.IntegrationEvent!);
                await eventLogService.MarkEventAsPublishedAsync(logEvt.EventId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error publishing integration event {EventId}", logEvt.EventId);
                await eventLogService.MarkEventAsFailedAsync(logEvt.EventId);
            }
        }
    }

    public async Task AddAndSaveEventAsync(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var transaction = orderingContext.GetCurrentTransaction()
            ?? throw new InvalidOperationException("An integration event must be recorded inside a business transaction.");

        logger.LogInformation("Enqueuing integration event {EventId} to outbox", integrationEvent.Id);
        await eventLogService.SaveEventAsync(integrationEvent, transaction.TransactionId);
    }
}
