using MediatR;
using OrderingService.Domain.AggregatesModel.OrderAggregate;

namespace OrderingService.Application.Commands;

/// <summary>
/// Grace period elapsed — move the order into stock validation.
/// </summary>
public class SetAwaitingValidationOrderStatusCommandHandler(IOrderRepository orderRepository)
    : IRequestHandler<SetAwaitingValidationOrderStatusCommand, bool>
{
    public async Task<bool> Handle(SetAwaitingValidationOrderStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await orderRepository.GetAsync(command.OrderNumber);
        if (order is null)
        {
            return false;
        }

        order.SetAwaitingValidationStatus();
        return await orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}

/// <summary>
/// The catalogue reserved every line — the order can be paid.
/// </summary>
public class SetStockConfirmedOrderStatusCommandHandler(IOrderRepository orderRepository)
    : IRequestHandler<SetStockConfirmedOrderStatusCommand, bool>
{
    public async Task<bool> Handle(SetStockConfirmedOrderStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await orderRepository.GetAsync(command.OrderNumber);
        if (order is null)
        {
            return false;
        }

        order.SetStockConfirmedStatus();
        return await orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}

/// <summary>
/// The catalogue could not fulfil part of the order — cancel it and tell the buyer why.
/// </summary>
public class SetStockRejectedOrderStatusCommandHandler(IOrderRepository orderRepository)
    : IRequestHandler<SetStockRejectedOrderStatusCommand, bool>
{
    public async Task<bool> Handle(SetStockRejectedOrderStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await orderRepository.GetAsync(command.OrderNumber);
        if (order is null)
        {
            return false;
        }

        order.SetCancelledStatusWhenStockIsRejected(command.OrderStockItems);
        return await orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}

/// <summary>
/// Payment cleared.
/// </summary>
public class SetPaidOrderStatusCommandHandler(IOrderRepository orderRepository)
    : IRequestHandler<SetPaidOrderStatusCommand, bool>
{
    public async Task<bool> Handle(SetPaidOrderStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await orderRepository.GetAsync(command.OrderNumber);
        if (order is null)
        {
            return false;
        }

        order.SetPaidStatus();
        return await orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}

/// <summary>
/// Buyer-initiated cancellation. The aggregate refuses this once the order is paid or shipped.
/// </summary>
public class CancelOrderCommandHandler(IOrderRepository orderRepository)
    : IRequestHandler<CancelOrderCommand, bool>
{
    public async Task<bool> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await orderRepository.GetAsync(command.OrderNumber);
        if (order is null)
        {
            return false;
        }

        order.SetCancelledStatus();
        return await orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}

/// <summary>
/// Warehouse dispatched the order.
/// </summary>
public class ShipOrderCommandHandler(IOrderRepository orderRepository)
    : IRequestHandler<ShipOrderCommand, bool>
{
    public async Task<bool> Handle(ShipOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await orderRepository.GetAsync(command.OrderNumber);
        if (order is null)
        {
            return false;
        }

        order.SetShippedStatus(command.Carrier, command.TrackingNumber);
        return await orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}
