using Microsoft.AspNetCore.Http;

namespace Avra.ServiceDefaults.Middleware;

/// <summary>
/// Applies the baseline response headers expected of a JSON API surface.
/// </summary>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        context.Response.OnStarting(() =>
        {
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Cross-Origin-Resource-Policy"] = "same-site";
            // These endpoints only ever return JSON, so nothing needs to be loadable from them.
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            return Task.CompletedTask;
        });

        await next(context);
    }
}
