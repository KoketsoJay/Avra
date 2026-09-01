using Avra.EventBus.Abstractions;
using Avra.EventBus.Outbox;

namespace OrderingService.Application.IntegrationEvents;

/// <summary>
/// Records integration events in the outbox during a business transaction, then publishes them
/// once that transaction has committed.
/// </summary>
public interface IOrderingIntegrationEventService
{
    Task PublishEventsThroughEventBusAsync(Guid transactionId);

    Task AddAndSaveEventAsync(IntegrationEvent integrationEvent);
}
