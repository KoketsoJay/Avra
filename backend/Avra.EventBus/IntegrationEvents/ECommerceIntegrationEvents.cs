using Avra.EventBus.Abstractions;

namespace Avra.EventBus.IntegrationEvents;

/// <summary>
/// Basket → Ordering. The shopper confirmed checkout; Ordering may now start an order.
/// </summary>
public record UserCheckoutAcceptedIntegrationEvent : IntegrationEvent
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;

    /// <summary>Card, BankTransfer or CashOnDelivery. Card fields are only meaningful for Card.</summary>
    public string PaymentMethod { get; init; } = "Card";

    public string CardNumber { get; init; } = string.Empty;
    public string CardHolderName { get; init; } = string.Empty;
    public DateTime CardExpiration { get; init; }
    public string CardSecurityNumber { get; init; } = string.Empty;
    public int CardTypeId { get; init; }
    public string? Buyer { get; init; }

    /// <summary>Correlates the checkout with the idempotent command that created the order.</summary>
    public Guid RequestId { get; init; }

    public CustomerBasket Basket { get; init; } = new(string.Empty, []);
}

/// <summary>
/// Ordering → Basket. The order exists, so the basket that produced it can be discarded.
/// </summary>
public record OrderStartedIntegrationEvent(string UserId) : IntegrationEvent;

/// <summary>
/// Ordering → clients. Emitted on every order state transition for read-model projection.
/// </summary>
public record OrderStatusChangedToSubmittedIntegrationEvent(
    int OrderId,
    string OrderStatus,
    string BuyerName,
    string BuyerIdentityGuid) : IntegrationEvent;

/// <summary>
/// Ordering → Catalog. Asks the catalogue whether every line can be fulfilled.
/// </summary>
public record OrderStatusChangedToAwaitingValidationIntegrationEvent(
    int OrderId,
    string OrderStatus,
    string BuyerName,
    string BuyerIdentityGuid,
    IReadOnlyCollection<OrderStockItem> OrderStockItems) : IntegrationEvent;

/// <summary>
/// Catalog → Ordering. Every requested line is available.
/// </summary>
public record OrderStockConfirmedIntegrationEvent(int OrderId) : IntegrationEvent;

/// <summary>
/// Catalog → Ordering. At least one line cannot be fulfilled.
/// </summary>
public record OrderStockRejectedIntegrationEvent(
    int OrderId,
    IReadOnlyCollection<ConfirmedOrderStockItem> OrderStockItems) : IntegrationEvent;

/// <summary>
/// Ordering → Payment. Stock is reserved; take the money.
/// </summary>
public record OrderStatusChangedToStockConfirmedIntegrationEvent(
    int OrderId,
    string OrderStatus,
    string BuyerName,
    string BuyerIdentityGuid) : IntegrationEvent;

/// <summary>Payment → Ordering. The charge succeeded.</summary>
public record OrderPaymentSucceededIntegrationEvent(int OrderId) : IntegrationEvent;

/// <summary>Payment → Ordering. The charge failed; the order is cancelled.</summary>
public record OrderPaymentFailedIntegrationEvent(int OrderId, string Reason) : IntegrationEvent;

/// <summary>
/// Ordering → Catalog. Payment cleared, so reserved units leave the catalogue for good.
/// </summary>
public record OrderStatusChangedToPaidIntegrationEvent(
    int OrderId,
    string OrderStatus,
    string BuyerName,
    string BuyerIdentityGuid,
    IReadOnlyCollection<OrderStockItem> OrderStockItems) : IntegrationEvent;

/// <summary>Ordering → clients. The order left the warehouse.</summary>
public record OrderStatusChangedToShippedIntegrationEvent(
    int OrderId,
    string OrderStatus,
    string BuyerName,
    string BuyerIdentityGuid) : IntegrationEvent;

/// <summary>Ordering → clients. The order was cancelled.</summary>
public record OrderStatusChangedToCancelledIntegrationEvent(
    int OrderId,
    string OrderStatus,
    string BuyerName,
    string BuyerIdentityGuid) : IntegrationEvent;

/// <summary>
/// Background tasks → Ordering. The buyer's cancellation window has closed.
/// </summary>
public record GracePeriodConfirmedIntegrationEvent(int OrderId) : IntegrationEvent;

/// <summary>
/// Catalog → Basket. Re-price the affected lines in every open basket.
/// </summary>
public record ProductPriceChangedIntegrationEvent(
    int ProductId,
    decimal NewPrice,
    decimal OldPrice) : IntegrationEvent;
