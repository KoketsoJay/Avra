namespace OrderingService.Domain.AggregatesModel.OrderAggregate;

/// <summary>
/// The order lifecycle. Transitions are enforced by the <see cref="Order"/> aggregate and
/// each one publishes an integration event that drives the next step of the saga.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order created; the buyer can still cancel during the grace period.</summary>
    Submitted = 1,

    /// <summary>Grace period elapsed; the catalogue is checking stock.</summary>
    AwaitingValidation = 2,

    /// <summary>Catalogue reserved every line; payment can proceed.</summary>
    StockConfirmed = 3,

    /// <summary>Payment succeeded; the warehouse can pick and pack.</summary>
    Paid = 4,

    /// <summary>Order dispatched. Terminal state.</summary>
    Shipped = 5,

    /// <summary>Order cancelled by the buyer, by stock rejection, or by a failed payment.</summary>
    Cancelled = 6,
}
