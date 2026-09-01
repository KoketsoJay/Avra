using Microsoft.EntityFrameworkCore;
using CartService.Data;
using CartService.Models;

namespace CartService.Services;

/// <summary>
/// Service for managing shopping carts
/// </summary>
public interface ICartService
{
    Task<CartDto?> GetCartByUserIdAsync(string userId);
    Task<CartDto> AddToCartAsync(string userId, AddToCartDto addDto);
    Task<CartDto> UpdateCartItemAsync(string userId, int productId, int quantity);
    Task<CartDto> RemoveFromCartAsync(string userId, int productId);
    Task<bool> ClearCartAsync(string userId);
}

/// <summary>
/// Service for managing orders
/// </summary>
public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderDto createDto);
    Task<OrderDto?> GetOrderByIdAsync(int orderId);
    Task<IEnumerable<OrderDto>> GetOrdersByUserIdAsync(string userId);
    Task<OrderDto> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
}

/// <summary>
/// Implementation of ICartService
/// </summary>
public class CartService : ICartService
{
    private readonly CartDbContext _context;
    private readonly IProductCatalogClient _catalog;
    private readonly ILogger<CartService> _logger;

    public CartService(CartDbContext context, IProductCatalogClient catalog, ILogger<CartService> logger)
    {
        _context = context;
        _catalog = catalog;
        _logger = logger;
    }

    public async Task<CartDto?> GetCartByUserIdAsync(string userId)
    {
        _logger.LogInformation("Fetching cart for user: {UserId}", userId);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            return null;
        }

        return MapToDto(cart);
    }

    public async Task<CartDto> AddToCartAsync(string userId, AddToCartDto addDto)
    {
        _logger.LogInformation("Adding product to cart for user: {UserId}, Product: {ProductId}", userId, addDto.ProductId);

        // Name and price are taken from the catalogue; the client only chooses what and how many.
        var product = await _catalog.GetProductAsync(addDto.ProductId)
            ?? throw new KeyNotFoundException($"Product {addDto.ProductId} was not found in the catalogue");

        if (!product.IsActive)
        {
            throw new InvalidOperationException($"Product {addDto.ProductId} is no longer available");
        }

        if (addDto.Quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero");
        }

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            _context.Carts.Add(cart);
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == addDto.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += addDto.Quantity;
            existingItem.Price = product.Price;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = addDto.Quantity,
                Image = product.Image1 ?? addDto.Image
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product added to cart successfully for user: {UserId}", userId);

        return MapToDto(cart);
    }

    public async Task<CartDto> UpdateCartItemAsync(string userId, int productId, int quantity)
    {
        _logger.LogInformation("Updating cart item for user: {UserId}, Product: {ProductId}", userId, productId);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            throw new KeyNotFoundException($"Cart not found for user {userId}");
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
        {
            throw new KeyNotFoundException($"Product {productId} not found in cart");
        }

        if (quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cart item updated successfully for user: {UserId}", userId);

        return MapToDto(cart);
    }

    public async Task<CartDto> RemoveFromCartAsync(string userId, int productId)
    {
        _logger.LogInformation("Removing product from cart for user: {UserId}, Product: {ProductId}", userId, productId);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            throw new KeyNotFoundException($"Cart not found for user {userId}");
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Product removed from cart successfully for user: {UserId}", userId);

        return MapToDto(cart);
    }

    public async Task<bool> ClearCartAsync(string userId)
    {
        _logger.LogInformation("Clearing cart for user: {UserId}", userId);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            return false;
        }

        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cart cleared successfully for user: {UserId}", userId);

        return true;
    }

    private static CartDto MapToDto(Cart cart)
    {
        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = cart.Items.Select(i => new CartItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Price = i.Price,
                Quantity = i.Quantity,
                Image = i.Image,
                Subtotal = i.GetSubtotal()
            }).ToList(),
            Total = cart.GetTotal()
        };
    }
}

/// <summary>
/// Implementation of IOrderService
/// </summary>
public class OrderService : IOrderService
{
    private readonly CartDbContext _context;
    private readonly ICartService _cartService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(CartDbContext context, ICartService cartService, ILogger<OrderService> logger)
    {
        _context = context;
        _cartService = cartService;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createDto)
    {
        _logger.LogInformation("Creating order for user: {UserId}", createDto.UserId);

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == createDto.UserId);

        if (cart == null || cart.Items.Count == 0)
        {
            throw new InvalidOperationException($"No items in cart for user {createDto.UserId}");
        }

        var order = new Order
        {
            UserId = createDto.UserId,
            TotalAmount = cart.GetTotal(),
            ShippingAddress = createDto.ShippingAddress,
            BillingAddress = createDto.BillingAddress,
            Status = OrderStatus.Pending
        };

        foreach (var item in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Price = item.Price,
                Quantity = item.Quantity
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Clear cart after order creation
        await _cartService.ClearCartAsync(createDto.UserId);

        _logger.LogInformation("Order created successfully with id: {OrderId}", order.Id);

        return MapToDto(order);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
    {
        _logger.LogInformation("Fetching order with id: {OrderId}", orderId);

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        return order == null ? null : MapToDto(order);
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByUserIdAsync(string userId)
    {
        _logger.LogInformation("Fetching orders for user: {UserId}", userId);

        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto);
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        _logger.LogInformation("Updating order status for id: {OrderId}, Status: {Status}", orderId, newStatus);

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order with id {orderId} not found");
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Order status updated successfully for id: {OrderId}", orderId);

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList(),
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            ShippingAddress = order.ShippingAddress,
            BillingAddress = order.BillingAddress,
            CreatedAt = order.CreatedAt
        };
    }
}
