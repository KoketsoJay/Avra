using Avra.EventBus.Abstractions;
using Avra.EventBus.IntegrationEvents;
using CartService.Data;
using Microsoft.EntityFrameworkCore;

namespace CartService.IntegrationEvents.EventHandling;

/// <summary>
/// Keeps open baskets in step with the catalogue. A shopper should never be charged a price they
/// were not shown, so a catalogue price change is applied to every basket holding that product.
/// </summary>
public class ProductPriceChangedIntegrationEventHandler(
    CartDbContext context,
    ILogger<ProductPriceChangedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<ProductPriceChangedIntegrationEvent>
{
    public async Task HandleAsync(ProductPriceChangedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var affectedItems = await context.CartItems
            .Where(item => item.ProductId == integrationEvent.ProductId && item.Price == integrationEvent.OldPrice)
            .ToListAsync(cancellationToken);

        if (affectedItems.Count == 0)
        {
            return;
        }

        foreach (var item in affectedItems)
        {
            item.Price = integrationEvent.NewPrice;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Re-priced {Count} basket lines for product {ProductId}",
            affectedItems.Count,
            integrationEvent.ProductId);
    }
}

/// <summary>
/// The order now owns the shopper's intent, so the basket that produced it is discarded.
/// </summary>
public class OrderStartedIntegrationEventHandler(
    CartDbContext context,
    ILogger<OrderStartedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<OrderStartedIntegrationEvent>
{
    public async Task HandleAsync(OrderStartedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var cart = await context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == integrationEvent.UserId, cancellationToken);

        if (cart is null)
        {
            return;
        }

        context.CartItems.RemoveRange(cart.Items);
        context.Carts.Remove(cart);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Basket cleared for user {UserId} after order started", integrationEvent.UserId);
    }
}
