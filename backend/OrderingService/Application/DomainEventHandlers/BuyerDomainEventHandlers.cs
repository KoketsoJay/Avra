using Avra.EventBus.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderingService.Application.IntegrationEvents;
using OrderingService.Domain.AggregatesModel.BuyerAggregate;
using OrderingService.Domain.AggregatesModel.OrderAggregate;
using OrderingService.Domain.Events;

namespace OrderingService.Application.DomainEventHandlers;

/// <summary>
/// Reacts to a started order by making sure the Buyer aggregate and the payment method exist.
/// Keeping this out of the Order aggregate is what stops the two aggregates from becoming one.
/// </summary>
public class ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler(
    IBuyerRepository buyerRepository,
    ILogger<ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler> logger)
    : INotificationHandler<OrderStartedDomainEvent>
{
    public async Task Handle(OrderStartedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var buyer = await buyerRepository.FindAsync(domainEvent.UserId)
            ?? new Buyer(domainEvent.UserId, domainEvent.UserName);

        if (PaymentMethods.RequiresCard(domainEvent.PaymentMethod))
        {
            var cardTypeId = domainEvent.CardTypeId != 0 ? domainEvent.CardTypeId : (int)CardType.Visa;

            // Only the last four digits are ever retained; the security number is never stored.
            var alias = domainEvent.CardNumber.Length >= 4
                ? $"XXXXXXXXXXXX{domainEvent.CardNumber[^4..]}"
                : "XXXXXXXXXXXX";

            buyer.VerifyOrAddPaymentMethod(
                cardTypeId,
                alias,
                domainEvent.CardNumber,
                domainEvent.CardHolderName,
                domainEvent.CardExpiration,
                domainEvent.Order.Id);
        }
        else
        {
            buyer.VerifyWithoutPaymentMethod(domainEvent.Order.Id);
        }

        var updatedBuyer = buyer.IsTransient() ? buyerRepository.Add(buyer) : buyerRepository.Update(buyer);
        await buyerRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("Buyer {BuyerId} verified for order {OrderId}", updatedBuyer.Id, domainEvent.Order.Id);
    }
}

/// <summary>
/// Links the freshly verified buyer and payment method back onto the order, and announces the
/// submitted order. The announcement lives here because this is the first point at which the
/// buyer's identity is known.
/// </summary>
public class UpdateOrderWhenBuyerAndPaymentMethodVerifiedDomainEventHandler(
    IOrderRepository orderRepository,
    IOrderingIntegrationEventService integrationEventService)
    : INotificationHandler<BuyerAndPaymentMethodVerifiedDomainEvent>
{
    public async Task Handle(BuyerAndPaymentMethodVerifiedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var order = await orderRepository.GetAsync(domainEvent.OrderId);
        if (order is null)
        {
            return;
        }

        order.SetBuyerId(domainEvent.Buyer.Id);

        if (domainEvent.Payment is not null)
        {
            order.SetPaymentMethodId(domainEvent.Payment.Id);
        }

        await integrationEventService.AddAndSaveEventAsync(
            new OrderStatusChangedToSubmittedIntegrationEvent(
                order.Id,
                order.Status.ToString(),
                domainEvent.Buyer.Name,
                domainEvent.Buyer.IdentityGuid));
    }
}
