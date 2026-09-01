using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CartService.Services;

/// <summary>
/// The discount that applies to a shopper, as the user service resolves it.
/// </summary>
public record MemberDiscount(decimal Percentage, string Label);

/// <summary>
/// Looks up the signed-in shopper's member discount.
/// </summary>
public interface IMemberDiscountClient
{
    Task<MemberDiscount> GetDiscountForCurrentUserAsync(string accessToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed HTTP client for the user service. The rate is resolved here at checkout rather than
/// trusted from the client, so a shopper cannot award themselves a discount.
/// </summary>
public class MemberDiscountClient(HttpClient httpClient, ILogger<MemberDiscountClient> logger) : IMemberDiscountClient
{
    private static readonly MemberDiscount None = new(0m, string.Empty);

    public async Task<MemberDiscount> GetDiscountForCurrentUserAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return None;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("api/memberships/discount/me", UriKind.Relative));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("User service returned {StatusCode} resolving the member discount", response.StatusCode);
                return None;
            }

            var discount = await response.Content.ReadFromJsonAsync<MemberDiscount>(cancellationToken);
            if (discount is null)
            {
                return None;
            }

            // Guard the boundary: a bad rate must never turn into a negative or inflated price.
            return discount with { Percentage = Math.Clamp(discount.Percentage, 0m, 100m) };
        }
        catch (HttpRequestException ex)
        {
            // Checkout must not fail because the user service is down; the shopper pays list price.
            logger.LogError(ex, "Member discount lookup failed; continuing at catalogue prices");
            return None;
        }
    }
}

/// <summary>
/// Registration helper for the member discount client.
/// </summary>
public static class MemberDiscountServiceCollectionExtensions
{
    public static IServiceCollection AddMemberDiscounts(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var baseUrl = configuration["Services:Users:BaseUrl"] ?? "http://localhost:5003";

        services.AddHttpClient<IMemberDiscountClient, MemberDiscountClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}

/// <summary>
/// Applies a member discount to a catalogue price.
/// </summary>
public static class MemberPricing
{
    /// <summary>
    /// Rounds to two decimals away from zero. The storefront mirrors this exactly, so the price a
    /// shopper is quoted is the price the order records.
    /// </summary>
    public static decimal Apply(decimal unitPrice, decimal discountPercentage)
    {
        if (discountPercentage <= 0m)
        {
            return unitPrice;
        }

        var discounted = unitPrice * (1m - Math.Clamp(discountPercentage, 0m, 100m) / 100m);
        return Math.Round(Math.Max(discounted, 0m), 2, MidpointRounding.AwayFromZero);
    }
}
