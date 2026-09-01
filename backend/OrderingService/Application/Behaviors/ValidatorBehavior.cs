using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderingService.Domain.Exceptions;

namespace OrderingService.Application.Behaviors;

/// <summary>
/// Rejects invalid commands before they reach a handler, so the domain model never has to
/// defend against malformed input.
/// </summary>
public class ValidatorBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidatorBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var typeName = typeof(TRequest).Name;

        var failures = validators
            .Select(v => v.Validate(request))
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .ToList();

        if (failures.Count != 0)
        {
            logger.LogWarning("Validation errors for command {CommandName}: {Errors}", typeName, failures);
            throw new OrderingDomainException(
                $"Command validation failed for {typeName}",
                new ValidationException("Validation exception", failures));
        }

        return await next();
    }
}
