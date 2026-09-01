using Avra.EventBus.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderingService.Application.IntegrationEvents;
using OrderingService.Domain.AggregatesModel.OrderAggregate;

namespace OrderingService.Application.Commands;

/// <summary>
/// Creates the Order aggregate from an accepted checkout. The aggregate itself decides which
/// domain events to raise; this handler only assembles it and commits the unit of work.
/// </summary>
public class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrderingIntegrationEventService integrationEventService,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, bool>
{
    public async Task<bool> Handle(CreateOrderCommand message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Recorded in the outbox first so the basket is only emptied if the order really commits.
        await integrationEventService.AddAndSaveEventAsync(new OrderStartedIntegrationEvent(message.UserId));

        var address = new Address(message.Street, message.City, message.State, message.Country, message.ZipCode);
        var order = new Order(
            message.UserId,
            message.UserName,
            address,
            message.PaymentMethod,
            message.CardTypeId,
            message.CardNumber,
            message.CardSecurityNumber,
            message.CardHolderName,
            message.CardExpiration);

        foreach (var item in message.OrderItems)
        {
            order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
        }

        logger.LogInformation("Creating order for buyer {UserId}", message.UserId);

        orderRepository.Add(order);
        return await orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}
