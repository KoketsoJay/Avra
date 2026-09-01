using Avra.EventBus.Abstractions;
using Avra.EventBus.IntegrationEvents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PaymentService.IntegrationEvents.EventHandling;

/// <summary>
/// Takes payment once the catalogue has reserved stock, then reports the outcome back to
/// Ordering. Payment is a separate service precisely so a slow or failing gateway cannot block
/// the rest of the checkout pipeline.
/// </summary>
public class OrderStatusChangedToStockConfirmedIntegrationEventHandler(
    IEventBus eventBus,
    IOptions<PaymentSettings> settings,
    ILogger<OrderStatusChangedToStockConfirmedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<OrderStatusChangedToStockConfirmedIntegrationEvent>
{
    private readonly PaymentSettings _settings = settings.Value;

    public async Task HandleAsync(
        OrderStatusChangedToStockConfirmedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        logger.LogInformation("Processing payment for order {OrderId}", integrationEvent.OrderId);

        IntegrationEvent outcome = _settings.PaymentSucceeded
            ? new OrderPaymentSucceededIntegrationEvent(integrationEvent.OrderId)
            : new OrderPaymentFailedIntegrationEvent(integrationEvent.OrderId, _settings.DeclineReason);

        await eventBus.PublishAsync(outcome, cancellationToken);

        logger.LogInformation(
            "Payment for order {OrderId} {Outcome}",
            integrationEvent.OrderId,
            _settings.PaymentSucceeded ? "succeeded" : "failed");
    }
}
