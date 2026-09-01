using Avra.EventBus.IntegrationEvents;
using MediatR;
using OrderingService.Application.IntegrationEvents;
using OrderingService.Domain.AggregatesModel.BuyerAggregate;
using OrderingService.Domain.AggregatesModel.OrderAggregate;
using OrderingService.Domain.Events;

namespace OrderingService.Application.DomainEventHandlers;

/// <summary>
/// Translates order lifecycle domain events into the integration events that drive the rest of
/// the system. Everything is written to the outbox — the transaction behavior publishes it.
/// </summary>
public class OrderStatusChangedDomainEventHandlers(
    IOrderRepository orderRepository,
    IBuyerRepository buyerRepository,
    IOrderingIntegrationEventService integrationEventService) :
    INotificationHandler<OrderStatusChangedToAwaitingValidationDomainEvent>,
    INotificationHandler<OrderStatusChangedToStockConfirmedDomainEvent>,
    INotificationHandler<OrderStatusChangedToPaidDomainEvent>,
    INotificationHandler<OrderShippedDomainEvent>,
    INotificationHandler<OrderCancelledDomainEvent>
{
    public async Task Handle(OrderStatusChangedToAwaitingValidationDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var (order, buyer) = await LoadAsync(domainEvent.OrderId);
        if (order is null || buyer is null)
        {
            return;
        }

        var stockItems = domainEvent.OrderItems
            .Select(item => new OrderStockItem(item.ProductId, item.Units))
            .ToList();

        await integrationEventService.AddAndSaveEventAsync(
            new OrderStatusChangedToAwaitingValidationIntegrationEvent(
                order.Id, order.Status.ToString(), buyer.Name, buyer.IdentityGuid, stockItems));
    }

    public async Task Handle(OrderStatusChangedToStockConfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var (order, buyer) = await LoadAsync(domainEvent.OrderId);
        if (order is null || buyer is null)
        {
            return;
        }

        await integrationEventService.AddAndSaveEventAsync(
            new OrderStatusChangedToStockConfirmedIntegrationEvent(
                order.Id, order.Status.ToString(), buyer.Name, buyer.IdentityGuid));
    }

    public async Task Handle(OrderStatusChangedToPaidDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var (order, buyer) = await LoadAsync(domainEvent.OrderId);
        if (order is null || buyer is null)
        {
            return;
        }

        var stockItems = domainEvent.OrderItems
            .Select(item => new OrderStockItem(item.ProductId, item.Units))
            .ToList();

        await integrationEventService.AddAndSaveEventAsync(
            new OrderStatusChangedToPaidIntegrationEvent(
                order.Id, order.Status.ToString(), buyer.Name, buyer.IdentityGuid, stockItems));
    }

    public async Task Handle(OrderShippedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var (order, buyer) = await LoadAsync(domainEvent.Order.Id);
        if (order is null || buyer is null)
        {
            return;
        }

        await integrationEventService.AddAndSaveEventAsync(
            new OrderStatusChangedToShippedIntegrationEvent(
                order.Id, order.Status.ToString(), buyer.Name, buyer.IdentityGuid));
    }

    public async Task Handle(OrderCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var (order, buyer) = await LoadAsync(domainEvent.Order.Id);
        if (order is null || buyer is null)
        {
            return;
        }

        await integrationEventService.AddAndSaveEventAsync(
            new OrderStatusChangedToCancelledIntegrationEvent(
                order.Id, order.Status.ToString(), buyer.Name, buyer.IdentityGuid));
    }

    private async Task<(Order? Order, Buyer? Buyer)> LoadAsync(int orderId)
    {
        var order = await orderRepository.GetAsync(orderId);
        if (order?.GetBuyerId is null)
        {
            return (order, null);
        }

        return (order, await buyerRepository.FindByIdAsync(order.GetBuyerId.Value));
    }
}
