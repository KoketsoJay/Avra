using MediatR;
using Microsoft.Extensions.Logging;
using OrderingService.Infrastructure.Idempotency;

namespace OrderingService.Application.Commands;

/// <summary>
/// Wraps a command with the client-supplied request id that makes it safe to retry.
/// </summary>
/// <typeparam name="T">The wrapped command type.</typeparam>
/// <typeparam name="TResult">The command result.</typeparam>
public class IdentifiedCommand<T, TResult>(T command, Guid id) : IRequest<TResult>
    where T : IRequest<TResult>
{
    public T Command { get; } = command;

    public Guid Id { get; } = id;
}

/// <summary>
/// Runs the inner command only the first time a given request id is seen. A duplicate returns
/// the "already handled" result instead of creating a second order.
/// </summary>
public abstract class IdentifiedCommandHandler<T, TResult>(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<IdentifiedCommandHandler<T, TResult>> logger) : IRequestHandler<IdentifiedCommand<T, TResult>, TResult>
    where T : IRequest<TResult>
{
    /// <summary>The value returned when the request id has already been processed.</summary>
    protected abstract TResult CreateResultForDuplicateRequest();

    public async Task<TResult> Handle(IdentifiedCommand<T, TResult> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await requestManager.ExistAsync(request.Id))
        {
            logger.LogInformation("Duplicate request {RequestId} ignored", request.Id);
            return CreateResultForDuplicateRequest();
        }

        await requestManager.CreateRequestForCommandAsync<T>(request.Id);
        var result = await mediator.Send(request.Command, cancellationToken);

        logger.LogInformation(
            "Command {CommandName} handled for request {RequestId} with result {Result}",
            typeof(T).Name,
            request.Id,
            result);

        return result;
    }
}

/// <summary>
/// Idempotent wrapper around <see cref="CreateOrderCommand"/>.
/// </summary>
public class CreateOrderIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<IdentifiedCommandHandler<CreateOrderCommand, bool>> logger)
    : IdentifiedCommandHandler<CreateOrderCommand, bool>(mediator, requestManager, logger)
{
    // A repeat of an accepted checkout is a success from the caller's point of view.
    protected override bool CreateResultForDuplicateRequest() => true;
}

/// <summary>Idempotent wrapper around <see cref="CancelOrderCommand"/>.</summary>
public class CancelOrderIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<IdentifiedCommandHandler<CancelOrderCommand, bool>> logger)
    : IdentifiedCommandHandler<CancelOrderCommand, bool>(mediator, requestManager, logger)
{
    protected override bool CreateResultForDuplicateRequest() => true;
}

/// <summary>Idempotent wrapper around <see cref="ShipOrderCommand"/>.</summary>
public class ShipOrderIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<IdentifiedCommandHandler<ShipOrderCommand, bool>> logger)
    : IdentifiedCommandHandler<ShipOrderCommand, bool>(mediator, requestManager, logger)
{
    protected override bool CreateResultForDuplicateRequest() => true;
}
