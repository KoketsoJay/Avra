using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Avra.ServiceDefaults.Security;

/// <summary>
/// The caller behind the current request, resolved from the validated bearer token.
/// Controllers use this instead of trusting identifiers supplied in the route or body.
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);

    /// <summary>
    /// Returns true when the caller owns <paramref name="resourceUserId"/> or is an administrator.
    /// </summary>
    bool CanAccess(string resourceUserId);
}

internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId =>
        accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? accessor.HttpContext?.User.FindFirstValue("sub");

    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) ?? false;

    public bool CanAccess(string resourceUserId)
    {
        if (string.IsNullOrWhiteSpace(resourceUserId) || !IsAuthenticated)
        {
            return false;
        }

        return IsInRole(AvraRoles.Administrator)
            || string.Equals(UserId, resourceUserId, StringComparison.Ordinal);
    }
}

/// <summary>Role names recognised across AVRA services.</summary>
public static class AvraRoles
{
    public const string Administrator = "administrator";
    public const string Customer = "customer";
}

/// <summary>Authorization policy names recognised across AVRA services.</summary>
public static class AvraPolicies
{
    public const string AdministratorOnly = "AdministratorOnly";
}
