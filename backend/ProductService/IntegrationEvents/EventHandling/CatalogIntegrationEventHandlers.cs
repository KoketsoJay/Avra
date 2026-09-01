using Avra.EventBus.Abstractions;
using Avra.EventBus.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;

namespace ProductService.IntegrationEvents.EventHandling;

/// <summary>
/// Answers Ordering's stock question. The catalogue is the only place that knows whether a line
/// can be fulfilled, so it decides and replies with a confirmation or a rejection.
/// </summary>
public class OrderStatusChangedToAwaitingValidationIntegrationEventHandler(
    ProductDbContext catalogContext,
    ICatalogIntegrationEventService catalogIntegrationEventService,
    ILogger<OrderStatusChangedToAwaitingValidationIntegrationEventHandler> logger)
    : IIntegrationEventHandler<OrderStatusChangedToAwaitingValidationIntegrationEvent>
{
    public async Task HandleAsync(
        OrderStatusChangedToAwaitingValidationIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        logger.LogInformation("Validating stock for order {OrderId}", integrationEvent.OrderId);

        var confirmedItems = new List<ConfirmedOrderStockItem>();

        foreach (var stockItem in integrationEvent.OrderStockItems)
        {
            var product = await catalogContext.Products
                .FirstOrDefaultAsync(p => p.Id == stockItem.ProductId, cancellationToken);

            var hasStock = product is not null && product.IsActive && product.Stock >= stockItem.Units;
            confirmedItems.Add(new ConfirmedOrderStockItem(stockItem.ProductId, hasStock));
        }

        IntegrationEvent reply = confirmedItems.TrueForAll(item => item.HasStock)
            ? new OrderStockConfirmedIntegrationEvent(integrationEvent.OrderId)
            : new OrderStockRejectedIntegrationEvent(integrationEvent.OrderId, confirmedItems);

        await catalogIntegrationEventService.SaveEventAndCatalogContextChangesAsync(reply);
        await catalogIntegrationEventService.PublishThroughEventBusAsync(reply);
    }
}

/// <summary>
/// Payment cleared, so the reserved units leave the catalogue permanently.
/// </summary>
public class OrderStatusChangedToPaidIntegrationEventHandler(
    ProductDbContext catalogContext,
    ILogger<OrderStatusChangedToPaidIntegrationEventHandler> logger)
    : IIntegrationEventHandler<OrderStatusChangedToPaidIntegrationEvent>
{
    public async Task HandleAsync(
        OrderStatusChangedToPaidIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        logger.LogInformation("Removing stock for paid order {OrderId}", integrationEvent.OrderId);

        foreach (var stockItem in integrationEvent.OrderStockItems)
        {
            var product = await catalogContext.Products
                .FirstOrDefaultAsync(p => p.Id == stockItem.ProductId, cancellationToken);

            product?.RemoveStock(stockItem.Units);
        }

        await catalogContext.SaveChangesAsync(cancellationToken);
    }
}
