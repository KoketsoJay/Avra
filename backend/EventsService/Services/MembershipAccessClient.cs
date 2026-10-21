using System.Net;
using System.Net.Http.Headers;

namespace EventsService.Services;

/// <summary>
/// Resolves whether the signed-in shopper currently has an active membership.
/// </summary>
public interface IMembershipAccessClient
{
    Task<bool> IsCurrentUserMemberAsync(string accessToken, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed HTTP client against UserService membership endpoints.
/// </summary>
public class MembershipAccessClient(HttpClient httpClient, ILogger<MembershipAccessClient> logger) : IMembershipAccessClient
{
    public async Task<bool> IsCurrentUserMemberAsync(string accessToken, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"api/memberships/user/{Uri.EscapeDataString(userId)}", UriKind.Relative));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            logger.LogWarning("UserService membership lookup returned {StatusCode}", response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Membership lookup failed; treating shopper as non-member");
            return false;
        }
    }
}

public static class MembershipAccessServiceCollectionExtensions
{
    public static IServiceCollection AddMembershipAccess(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var baseUrl = configuration["Services:Users:BaseUrl"] ?? "http://localhost:5003";

        services.AddHttpClient<IMembershipAccessClient, MembershipAccessClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}