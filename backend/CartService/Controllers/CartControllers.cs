using Microsoft.AspNetCore.Mvc;
using CartService.Models;
using CartService.Services;

namespace CartService.Controllers;

/// <summary>
/// API controller for cart operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CartsController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartsController> _logger;

    public CartsController(ICartService cartService, ILogger<CartsController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Get cart by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Cart details or null if not found</returns>
    [HttpGet("{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> GetCart(string userId)
    {
        try
        {
            var cart = await _cartService.GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                return NotFound(new { message = $"Cart not found for user {userId}" });
            }
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cart for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching cart" });
        }
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="addDto">Item to add</param>
    /// <returns>Updated cart</returns>
    [HttpPost("{userId}/items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> AddToCart(string userId, AddToCartDto addDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var cart = await _cartService.AddToCartAsync(userId, addDto);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error adding item to cart" });
        }
    }

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="productId">Product ID</param>
    /// <param name="quantity">New quantity</param>
    /// <returns>Updated cart</returns>
    [HttpPut("{userId}/items/{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CartDto>> UpdateCartItem(string userId, int productId, [FromQuery] int quantity)
    {
        try
        {
            if (quantity < 0)
            {
                return BadRequest(new { message = "Quantity cannot be negative" });
            }

            var cart = await _cartService.UpdateCartItemAsync(userId, productId, quantity);
            return Ok(cart);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Item not found in cart for user {UserId}", userId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating cart item" });
        }
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="productId">Product ID</param>
    /// <returns>Updated cart</returns>
    [HttpDelete("{userId}/items/{productId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RemoveFromCart(string userId, int productId)
    {
        try
        {
            var cart = await _cartService.RemoveFromCartAsync(userId, productId);
            return Ok(cart);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Error removing item from cart for user {UserId}", userId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error removing item from cart" });
        }
    }

    /// <summary>
    /// Clear entire cart
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearCart(string userId)
    {
        try
        {
            var cleared = await _cartService.ClearCartAsync(userId);
            if (!cleared)
            {
                return NotFound(new { message = $"Cart not found for user {userId}" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error clearing cart" });
        }
    }
}

/// <summary>
/// API controller for order operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <returns>Order details</returns>
    [HttpGet("{orderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(int orderId)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = $"Order with id {orderId} not found" });
            }
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order with id {OrderId}", orderId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching order" });
        }
    }

    /// <summary>
    /// Get orders by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of user's orders</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetUserOrders(string userId)
    {
        try
        {
            var orders = await _orderService.GetOrdersByUserIdAsync(userId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching orders" });
        }
    }

    /// <summary>
    /// Create new order
    /// </summary>
    /// <param name="createDto">Order creation data</param>
    /// <returns>Created order</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var order = await _orderService.CreateOrderAsync(createDto);
            return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error creating order");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating order" });
        }
    }

    /// <summary>
    /// Update order status
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="newStatus">New status</param>
    /// <returns>Updated order</returns>
    [HttpPut("{orderId}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(int orderId, [FromQuery] OrderStatus newStatus)
    {
        try
        {
            var order = await _orderService.UpdateOrderStatusAsync(orderId, newStatus);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order not found with id {OrderId}", orderId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for id {OrderId}", orderId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating order status" });
        }
    }
}
