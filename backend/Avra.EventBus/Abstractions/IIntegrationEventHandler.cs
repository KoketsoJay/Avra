namespace Avra.EventBus.Abstractions;

/// <summary>
/// Non-generic entry point the dispatcher uses once it has deserialised an event.
/// </summary>
public interface IIntegrationEventHandler
{
    Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Strongly typed handler for a single integration event type.
/// </summary>
/// <typeparam name="TIntegrationEvent">The event this handler consumes.</typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    Task HandleAsync(TIntegrationEvent integrationEvent, CancellationToken cancellationToken);

    Task IIntegrationEventHandler.HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
        => HandleAsync((TIntegrationEvent)integrationEvent, cancellationToken);
}
