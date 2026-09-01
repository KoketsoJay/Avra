using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderingService.Application.IntegrationEvents;
using OrderingService.Infrastructure;

namespace OrderingService.Application.Behaviors;

/// <summary>
/// Gives each command its own transaction and publishes the integration events it produced only
/// after that transaction commits. This is what keeps state changes and the events announcing
/// them consistent without a distributed transaction.
/// </summary>
public class TransactionBehavior<TRequest, TResponse>(
    OrderingContext dbContext,
    IOrderingIntegrationEventService integrationEventService,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var typeName = typeof(TRequest).Name;

        // A nested command (for example the inner command of an IdentifiedCommand) joins the
        // transaction its caller already opened.
        if (dbContext.HasActiveTransaction)
        {
            return await next();
        }

        var response = default(TResponse)!;
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            Guid transactionId;

            await using var transaction = await dbContext.BeginTransactionAsync();
            using (logger.BeginScope(new List<KeyValuePair<string, object>>
            {
                new("TransactionContext", transaction!.TransactionId),
            }))
            {
                logger.LogInformation("Begin transaction {TransactionId} for {CommandName}", transaction.TransactionId, typeName);

                response = await next();

                logger.LogInformation("Commit transaction {TransactionId} for {CommandName}", transaction.TransactionId, typeName);

                await dbContext.CommitTransactionAsync(transaction);
                transactionId = transaction.TransactionId;
            }

            await integrationEventService.PublishEventsThroughEventBusAsync(transactionId);
        });

        return response;
    }
}
