using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace Avra.ServiceDefaults.Middleware;

/// <summary>
/// Echoes (or mints) a correlation id so a single customer journey can be traced across services.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out StringValues incoming)
            && !StringValues.IsNullOrEmpty(incoming)
                ? Sanitize(incoming.ToString())
                : context.TraceIdentifier;

        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    /// <summary>Caller-supplied values reach the logs, so restrict them to a safe shape and length.</summary>
    private static string Sanitize(string value)
    {
        var trimmed = value.Length > 64 ? value[..64] : value;
        return string.Create(trimmed.Length, trimmed, (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                span[i] = char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-';
            }
        });
    }
}
