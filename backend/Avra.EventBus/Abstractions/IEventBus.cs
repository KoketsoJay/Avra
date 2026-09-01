namespace Avra.EventBus.Abstractions;

/// <summary>
/// Transport-agnostic publish side of the bus. Services depend on this, never on the SQL broker.
/// </summary>
public interface IEventBus
{
    /// <summary>Publishes an integration event to every subscribed service.</summary>
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
