namespace Avra.EventBus.IntegrationEvents;

/// <summary>
/// A line captured from the shopper's basket at checkout time.
/// </summary>
public record BasketItem(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    decimal OldUnitPrice,
    int Quantity,
    string? PictureUrl);

/// <summary>
/// The basket snapshot handed to Ordering when checkout is accepted.
/// </summary>
public record CustomerBasket(string BuyerId, IReadOnlyCollection<BasketItem> Items);

/// <summary>
/// A quantity Ordering asks the catalogue to reserve.
/// </summary>
public record OrderStockItem(int ProductId, int Units);

/// <summary>
/// The catalogue's verdict on a single requested line.
/// </summary>
public record ConfirmedOrderStockItem(int ProductId, bool HasStock);
