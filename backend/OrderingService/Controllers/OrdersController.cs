using Avra.ServiceDefaults.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingService.Application.Commands;
using OrderingService.Application.Queries;

namespace OrderingService.Controllers;

/// <summary>
/// The ordering write and read API. Writes go through MediatR commands; reads bypass the
/// domain model and use the Dapper-backed query side.
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(IMediator mediator, IOrderQueries orderQueries, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Places an order for the signed-in buyer.</summary>
    /// <param name="request">The checkout payload.</param>
    /// <param name="requestId">Client-generated id that makes the call safe to retry.</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrderAsync(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "x-requestid")] Guid requestId)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (requestId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Title = "The x-requestid header is required." });
        }

        var userId = currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        var command = new CreateOrderCommand(
            request.Items,
            userId,
            request.UserName,
            request.City,
            request.Street,
            request.State,
            request.Country,
            request.ZipCode,
            request.PaymentMethod,
            request.CardNumber,
            request.CardHolderName,
            request.CardExpiration,
            request.CardSecurityNumber,
            request.CardTypeId);

        var accepted = await mediator.Send(new IdentifiedCommand<CreateOrderCommand, bool>(command, requestId));
        return accepted ? Accepted() : BadRequest();
    }

    /// <summary>Cancels an order that has not yet been paid.</summary>
    [HttpPut("cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrderAsync(
        [FromBody] OrderNumberRequest request,
        [FromHeader(Name = "x-requestid")] Guid requestId)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (requestId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Title = "The x-requestid header is required." });
        }

        var command = new IdentifiedCommand<CancelOrderCommand, bool>(new CancelOrderCommand(request.OrderNumber), requestId);
        return await mediator.Send(command) ? Ok() : BadRequest();
    }

    /// <summary>Marks a paid order as shipped. Administrators only.</summary>
    [HttpPut("ship")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ShipOrderAsync(
        [FromBody] ShipOrderRequest request,
        [FromHeader(Name = "x-requestid")] Guid requestId)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (requestId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Title = "The x-requestid header is required." });
        }

        var command = new IdentifiedCommand<ShipOrderCommand, bool>(
            new ShipOrderCommand(request.OrderNumber, request.Carrier, request.TrackingNumber),
            requestId);

        return await mediator.Send(command) ? Ok() : BadRequest();
    }

    /// <summary>The fulfilment queue across every buyer. Administrators only.</summary>
    /// <param name="status">Optional order status to filter on, for example Paid or Shipped.</param>
    [HttpGet("admin")]
    [Authorize(Policy = AvraPolicies.AdministratorOnly)]
    [ProducesResponseType<IEnumerable<AdminOrderSummaryViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminOrderSummaryViewModel>>> GetAllOrdersAsync([FromQuery] string? status) =>
        Ok(await orderQueries.GetAllOrdersAsync(status));

    /// <summary>Returns a single order, provided it belongs to the caller.</summary>
    [HttpGet("{orderId:int}")]
    [ProducesResponseType<OrderViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderViewModel>> GetOrderAsync(int orderId)
    {
        var order = await orderQueries.GetOrderAsync(orderId);
        if (order is null)
        {
            return NotFound();
        }

        // Ownership is checked against the buyer's own order list, not a client-supplied id.
        var owned = await orderQueries.GetOrdersFromUserAsync(currentUser.UserId ?? string.Empty);
        return owned.Any(o => o.OrderNumber == orderId) || currentUser.IsInRole(AvraRoles.Administrator)
            ? Ok(order)
            : Forbid();
    }

    /// <summary>Returns the signed-in buyer's orders.</summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<OrderSummaryViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderSummaryViewModel>>> GetOrdersAsync()
    {
        var userId = currentUser.UserId;
        return string.IsNullOrEmpty(userId)
            ? Forbid()
            : Ok(await orderQueries.GetOrdersFromUserAsync(userId));
    }

    /// <summary>Returns the accepted card types.</summary>
    [HttpGet("cardtypes")]
    [AllowAnonymous]
    [ProducesResponseType<IEnumerable<CardTypeViewModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CardTypeViewModel>>> GetCardTypesAsync() =>
        Ok(await orderQueries.GetCardTypesAsync());
}

/// <summary>Checkout payload posted by the storefront.</summary>
public class CreateOrderRequest
{
    public string UserName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>Card, BankTransfer or CashOnDelivery.</summary>
    public string PaymentMethod { get; set; } = "Card";

    public string CardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public DateTime CardExpiration { get; set; }
    public string CardSecurityNumber { get; set; } = string.Empty;
    public int CardTypeId { get; set; }
    public List<OrderItemDTO> Items { get; set; } = [];
}

/// <summary>Identifies an existing order.</summary>
public class OrderNumberRequest
{
    public int OrderNumber { get; set; }
}

/// <summary>Dispatch details recorded when the warehouse hands a parcel to the carrier.</summary>
public class ShipOrderRequest
{
    public int OrderNumber { get; set; }

    /// <summary>Carrier that collected the parcel.</summary>
    public string Carrier { get; set; } = string.Empty;

    /// <summary>Carrier tracking reference the buyer can follow.</summary>
    public string TrackingNumber { get; set; } = string.Empty;
}
