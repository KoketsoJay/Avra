using MediatR;
using OrderingService.Domain.AggregatesModel.BuyerAggregate;
using OrderingService.Domain.AggregatesModel.OrderAggregate;

namespace OrderingService.Domain.Events;

/// <summary>
/// Raised the moment an order is created, before it has an identity. Handlers use it to make
/// sure a Buyer and payment method exist for the shopper.
/// </summary>
public class OrderStartedDomainEvent(
    Order order,
    string userId,
    string userName,
    string paymentMethod,
    int cardTypeId,
    string cardNumber,
    string cardSecurityNumber,
    string cardHolderName,
    DateTime cardExpiration) : INotification
{
    public Order Order { get; } = order;

    public string UserId { get; } = userId;

    public string UserName { get; } = userName;

    /// <summary>How the buyer chose to settle the order.</summary>
    public string PaymentMethod { get; } = paymentMethod;

    public int CardTypeId { get; } = cardTypeId;

    public string CardNumber { get; } = cardNumber;

    public string CardSecurityNumber { get; } = cardSecurityNumber;

    public string CardHolderName { get; } = cardHolderName;

    public DateTime CardExpiration { get; } = cardExpiration;
}

/// <summary>
/// Raised once the buyer behind an order has been persisted. <see cref="Payment"/> is null when
/// the order is settled without a card.
/// </summary>
public class BuyerAndPaymentMethodVerifiedDomainEvent(Buyer buyer, PaymentMethod? payment, int orderId) : INotification
{
    public Buyer Buyer { get; } = buyer;

    public PaymentMethod? Payment { get; } = payment;

    public int OrderId { get; } = orderId;
}

/// <summary>Grace period elapsed: ask the catalogue to validate stock.</summary>
public class OrderStatusChangedToAwaitingValidationDomainEvent(int orderId, IEnumerable<OrderItem> orderItems) : INotification
{
    public int OrderId { get; } = orderId;

    public IEnumerable<OrderItem> OrderItems { get; } = orderItems;
}

/// <summary>Catalogue reserved every line: ask Payment to charge the buyer.</summary>
public class OrderStatusChangedToStockConfirmedDomainEvent(int orderId) : INotification
{
    public int OrderId { get; } = orderId;
}

/// <summary>Payment cleared: tell the catalogue to release the reserved units.</summary>
public class OrderStatusChangedToPaidDomainEvent(int orderId, IEnumerable<OrderItem> orderItems) : INotification
{
    public int OrderId { get; } = orderId;

    public IEnumerable<OrderItem> OrderItems { get; } = orderItems;
}

/// <summary>Order dispatched.</summary>
public class OrderShippedDomainEvent(Order order) : INotification
{
    public Order Order { get; } = order;
}

/// <summary>Order cancelled by the buyer, by stock rejection, or by a failed payment.</summary>
public class OrderCancelledDomainEvent(Order order) : INotification
{
    public Order Order { get; } = order;
}
