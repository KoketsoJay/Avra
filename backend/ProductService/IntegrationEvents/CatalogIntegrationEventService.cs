using Avra.EventBus.Abstractions;
using Avra.EventBus.Outbox;
using ProductService.Data;

namespace ProductService.IntegrationEvents;

/// <summary>
/// Commits a catalogue change and the event announcing it in one transaction, then publishes.
/// </summary>
public interface ICatalogIntegrationEventService
{
    Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent integrationEvent);

    Task PublishThroughEventBusAsync(IntegrationEvent integrationEvent);
}

/// <summary>
/// Outbox-based publisher for the catalogue. Writing the event with the catalogue change means a
/// crash before publishing leaves a replayable record instead of an inconsistent catalogue.
/// </summary>
public class CatalogIntegrationEventService(
    IEventBus eventBus,
    ProductDbContext catalogContext,
    IIntegrationEventLogService eventLogService,
    ILogger<CatalogIntegrationEventService> logger) : ICatalogIntegrationEventService
{
    public async Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        await ResilientTransaction.New(catalogContext).ExecuteAsync(async transaction =>
        {
            await catalogContext.SaveChangesAsync();
            await eventLogService.SaveEventAsync(integrationEvent, transaction.TransactionId);
        });
    }

    public async Task PublishThroughEventBusAsync(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        try
        {
            await eventLogService.MarkEventAsInProgressAsync(integrationEvent.Id);
            await eventBus.PublishAsync(integrationEvent);
            await eventLogService.MarkEventAsPublishedAsync(integrationEvent.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing integration event {EventId}", integrationEvent.Id);
            await eventLogService.MarkEventAsFailedAsync(integrationEvent.Id);
        }
    }
}
