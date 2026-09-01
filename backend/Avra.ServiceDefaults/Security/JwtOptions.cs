using System.ComponentModel.DataAnnotations;

namespace Avra.ServiceDefaults.Security;

/// <summary>
/// Bearer token settings shared by every AVRA service. Supplied through configuration
/// (environment variables, user-secrets or a secret store) — never checked into source.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "avra-auth-service";

    [Required]
    public string Audience { get; set; } = "avra-api";

    /// <summary>HMAC-SHA256 signing key. Must be at least 32 characters (256 bits).</summary>
    [Required]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters. Supply it via the Jwt__SigningKey environment variable.")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 60;
}
