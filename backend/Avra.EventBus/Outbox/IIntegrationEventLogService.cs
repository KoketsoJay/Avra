using Avra.EventBus.Abstractions;

namespace Avra.EventBus.Outbox;

/// <summary>
/// Outbox operations used by a service's integration event service.
/// </summary>
public interface IIntegrationEventLogService
{
    Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId);

    Task SaveEventAsync(IntegrationEvent integrationEvent, Guid transactionId);

    Task MarkEventAsPublishedAsync(Guid eventId);

    Task MarkEventAsInProgressAsync(Guid eventId);

    Task MarkEventAsFailedAsync(Guid eventId);
}
