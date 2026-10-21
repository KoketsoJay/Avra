using Avra.EventBus.Abstractions;
using Avra.EventBus.IntegrationEvents;
using Avra.ServiceDefaults.Security;
using CartService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CartService.Controllers;

/// <summary>
/// Accepts a checkout and hands it to Ordering as an integration event. The basket service never
/// creates orders itself — it only states that the shopper confirmed their basket.
/// </summary>
[ApiController]
[Route("api/basket")]
[Authorize]
[Produces("application/json")]
public class BasketCheckoutController(
    ICartService cartService,
    IEventBus eventBus,
    ICurrentUser currentUser,
    IMemberDiscountClient memberDiscountClient,
    ILogger<BasketCheckoutController> logger) : ControllerBase
{
    /// <summary>Confirms the signed-in shopper's basket and starts the ordering workflow.</summary>
    /// <param name="checkout">Shipping and payment details.</param>
    /// <param name="requestId">Client-generated id that makes the call safe to retry.</param>
    [HttpPost("checkout")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckoutAsync(
        [FromBody] BasketCheckoutRequest checkout,
        [FromHeader(Name = "x-requestid")] Guid requestId)
    {
        ArgumentNullException.ThrowIfNull(checkout);

        if (requestId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Title = "The x-requestid header is required." });
        }

        var userId = currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Forbid();
        }

        var cart = await cartService.GetCartByUserIdAsync(userId);
        if (cart is null || cart.Items.Count == 0)
        {
            return BadRequest(new ProblemDetails { Title = "The basket is empty." });
        }

        // Resolved here rather than taken from the request, so the shopper cannot set their own rate.
        var accessToken = Request.Headers.Authorization.ToString()
            .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
        var discount = await memberDiscountClient.GetDiscountForCurrentUserAsync(accessToken);

        var basket = new CustomerBasket(
            userId,
            cart.Items
                .Select(item => new BasketItem(
                    item.ProductId,
                    item.ProductName,
                    MemberPricing.Apply(item.Price, discount.Percentage),
                    item.Price,
                    item.Quantity,
                    item.Image))
                .ToList());

        var integrationEvent = new UserCheckoutAcceptedIntegrationEvent
        {
            UserId = userId,
            UserName = checkout.UserName,
            City = checkout.City,
            Street = checkout.Street,
            State = checkout.State,
            Country = checkout.Country,
            ZipCode = checkout.ZipCode,
            PaymentMethod = checkout.PaymentMethod,
            CardLast4 = LastFourOf(checkout.CardNumber),
            CardHolderName = checkout.CardHolderName,
            CardExpiration = checkout.CardExpiration,
            CardTypeId = checkout.CardTypeId,
            Buyer = checkout.Buyer,
            RequestId = requestId,
            Basket = basket,
        };

        logger.LogInformation(
            "Checkout accepted for user {UserId} with request {RequestId} at a {Percentage}% member discount",
            userId, requestId, discount.Percentage);
        await eventBus.PublishAsync(integrationEvent);

        return Accepted();
    }

    /// <summary>
    /// Reduces a card number to its last four digits. This is the only part of the card that is
    /// allowed past this controller; the CVV is discarded entirely and never persisted.
    /// </summary>
    private static string LastFourOf(string? cardNumber)
    {
        var digits = new string((cardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length <= 4 ? digits : digits[^4..];
    }
}

/// <summary>Shipping and payment details supplied at checkout.</summary>
public class BasketCheckoutRequest
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
    public string? Buyer { get; set; }
}
