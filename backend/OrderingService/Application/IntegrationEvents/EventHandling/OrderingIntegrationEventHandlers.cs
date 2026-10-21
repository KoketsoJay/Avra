using Avra.EventBus.Abstractions;
using Avra.EventBus.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderingService.Application.Commands;

namespace OrderingService.Application.IntegrationEvents.EventHandling;

/// <summary>
/// Turns an accepted checkout into an order. The request id carried by the event makes the
/// handler safe to run more than once — at-least-once delivery cannot create duplicate orders.
/// </summary>
public class UserCheckoutAcceptedIntegrationEventHandler(
    IMediator mediator,
    ILogger<UserCheckoutAcceptedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<UserCheckoutAcceptedIntegrationEvent>
{
    public async Task HandleAsync(UserCheckoutAcceptedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (integrationEvent.RequestId == Guid.Empty)
        {
            logger.LogWarning("Checkout for {UserId} arrived without a request id and was ignored", integrationEvent.UserId);
            return;
        }

        var items = integrationEvent.Basket.Items
            .Select(item => new OrderItemDTO
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Discount = Math.Max(item.OldUnitPrice - item.UnitPrice, 0m),
                Units = item.Quantity,
                PictureUrl = item.PictureUrl,
            })
            .ToList();

        var createOrderCommand = new CreateOrderCommand(
            items,
            integrationEvent.UserId,
            integrationEvent.UserName,
            integrationEvent.City,
            integrationEvent.Street,
            integrationEvent.State,
            integrationEvent.Country,
            integrationEvent.ZipCode,
            integrationEvent.PaymentMethod,
            integrationEvent.CardLast4,
            integrationEvent.CardHolderName,
            integrationEvent.CardExpiration,
            string.Empty,
            integrationEvent.CardTypeId);

        var requestCreateOrder = new IdentifiedCommand<CreateOrderCommand, bool>(createOrderCommand, integrationEvent.RequestId);
        var result = await mediator.Send(requestCreateOrder, cancellationToken);

        logger.LogInformation(
            "CreateOrderCommand for request {RequestId} completed with result {Result}",
            integrationEvent.RequestId,
            result);
    }
}

/// <summary>
/// The buyer's cancellation window closed: start stock validation.
/// </summary>
public class GracePeriodConfirmedIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<GracePeriodConfirmedIntegrationEvent>
{
    public Task HandleAsync(GracePeriodConfirmedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return mediator.Send(new SetAwaitingValidationOrderStatusCommand(integrationEvent.OrderId), cancellationToken);
    }
}

/// <summary>Catalogue confirmed stock for every line.</summary>
public class OrderStockConfirmedIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<OrderStockConfirmedIntegrationEvent>
{
    public Task HandleAsync(OrderStockConfirmedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return mediator.Send(new SetStockConfirmedOrderStatusCommand(integrationEvent.OrderId), cancellationToken);
    }
}

/// <summary>Catalogue rejected at least one line; cancel the order.</summary>
public class OrderStockRejectedIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<OrderStockRejectedIntegrationEvent>
{
    public Task HandleAsync(OrderStockRejectedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var rejected = integrationEvent.OrderStockItems
            .Where(item => !item.HasStock)
            .Select(item => item.ProductId)
            .ToList();

        return mediator.Send(new SetStockRejectedOrderStatusCommand(integrationEvent.OrderId, rejected), cancellationToken);
    }
}

/// <summary>Payment cleared.</summary>
public class OrderPaymentSucceededIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<OrderPaymentSucceededIntegrationEvent>
{
    public Task HandleAsync(OrderPaymentSucceededIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return mediator.Send(new SetPaidOrderStatusCommand(integrationEvent.OrderId), cancellationToken);
    }
}

/// <summary>Payment was declined; the order cannot proceed.</summary>
public class OrderPaymentFailedIntegrationEventHandler(IMediator mediator)
    : IIntegrationEventHandler<OrderPaymentFailedIntegrationEvent>
{
    public Task HandleAsync(OrderPaymentFailedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return mediator.Send(new CancelOrderCommand(integrationEvent.OrderId), cancellationToken);
    }
}
