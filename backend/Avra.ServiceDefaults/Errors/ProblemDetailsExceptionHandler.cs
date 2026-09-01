using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Avra.ServiceDefaults.Errors;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 responses. Internal detail is logged but never
/// returned to the caller outside Development.
/// </summary>
internal sealed class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Request could not be completed"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            OperationCanceledException => (ClientClosedRequest, "Request cancelled"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request failed with {Status} for {Method} {Path}", status, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Exception messages can carry internal detail, so only echo them in Development.
                Detail = environment.IsDevelopment() ? exception.Message : null,
                Instance = httpContext.Request.Path,
            },
        });
    }
}
