using MediatR;
using Microsoft.Extensions.Logging;

namespace OrderingService.Application.Behaviors;

/// <summary>
/// Traces every command through the pipeline. Kept first so failures in later behaviors are
/// still attributed to a named command.
/// </summary>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling command {CommandName}", requestName);

        var response = await next();

        logger.LogInformation("Command {CommandName} handled", requestName);
        return response;
    }
}
