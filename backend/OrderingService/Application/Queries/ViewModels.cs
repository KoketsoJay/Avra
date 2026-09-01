namespace OrderingService.Application.Queries;

/// <summary>Read model for a single order.</summary>
public record OrderViewModel
{
    public int OrderNumber { get; init; }
    public DateTime Date { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string? Carrier { get; init; }
    public string? TrackingNumber { get; init; }
    public DateTime? ShippedAt { get; init; }
    public IReadOnlyCollection<OrderItemViewModel> OrderItems { get; init; } = [];
}

/// <summary>Read model for an order line.</summary>
public record OrderItemViewModel
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Units { get; init; }
    public decimal UnitPrice { get; init; }
    public string? PictureUrl { get; init; }
}

/// <summary>Read model for the buyer's order list.</summary>
public record OrderSummaryViewModel
{
    public int OrderNumber { get; init; }
    public DateTime Date { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal Total { get; init; }
}

/// <summary>Read model for the administrator's fulfilment queue.</summary>
public record AdminOrderSummaryViewModel
{
    public int OrderNumber { get; init; }
    public DateTime Date { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public int ItemCount { get; init; }
    public string BuyerName { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? Carrier { get; init; }
    public string? TrackingNumber { get; init; }
    public DateTime? ShippedAt { get; init; }

    /// <summary>True once the order is paid and still waiting to leave the warehouse.</summary>
    public bool AwaitingDispatch { get; init; }
}

/// <summary>Read model for the card type lookup.</summary>
public record CardTypeViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
