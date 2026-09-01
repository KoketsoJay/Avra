using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Avra.ServiceDefaults.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserService.Models;

namespace UserService.Services;

/// <summary>
/// Issues the access tokens every other AVRA service validates.
/// </summary>
public interface ITokenService
{
    /// <summary>Issues a signed access token and reports how long it is valid for.</summary>
    (string AccessToken, int ExpiresInSeconds) CreateAccessToken(User user);
}

/// <summary>
/// Signs tokens with the shared <see cref="JwtOptions"/>. Reading the key from configuration is
/// what allows the other services to validate what this one issues.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string AccessToken, int ExpiresInSeconds) CreateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), _options.AccessTokenMinutes * 60);
    }
}
