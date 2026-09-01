namespace CartService.Models;

/// <summary>
/// Represents a shopping cart
/// </summary>
public class Cart
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<CartItem> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public decimal GetTotal() => Items.Sum(i => i.GetSubtotal());
}

/// <summary>
/// Represents an item in the cart
/// </summary>
public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? Image { get; set; }

    public decimal GetSubtotal() => Price * Quantity;
}

/// <summary>
/// Represents an order
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? ShippingAddress { get; set; }
    public string? BillingAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Represents an item in an order
/// </summary>
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Order status enumeration
/// </summary>
public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}

/// <summary>
/// DTO for cart responses
/// </summary>
public class CartDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<CartItemDto> Items { get; set; } = new();
    public decimal Total { get; set; }
}

/// <summary>
/// DTO for cart items
/// </summary>
public class CartItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? Image { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>
/// DTO for adding items to cart
/// </summary>
public class AddToCartDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? Image { get; set; }
}

/// <summary>
/// DTO for creating orders
/// </summary>
public class CreateOrderDto
{
    public string UserId { get; set; } = string.Empty;
    public string? ShippingAddress { get; set; }
    public string? BillingAddress { get; set; }
}

/// <summary>
/// DTO for order responses
/// </summary>
public class OrderDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string? ShippingAddress { get; set; }
    public string? BillingAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for order items
/// </summary>
public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
