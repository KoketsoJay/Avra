using System.Net.Http.Json;

namespace CartService.Services;

/// <summary>
/// Read-only view of a catalogue product, as the catalogue reports it.
/// </summary>
public record CatalogProduct(int Id, string Name, decimal Price, int Stock, bool IsActive, string? Image1);

/// <summary>
/// Looks products up in the catalogue service.
/// </summary>
public interface IProductCatalogClient
{
    Task<CatalogProduct?> GetProductAsync(int productId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed HTTP client for the catalogue. Basket lines are priced from here rather than from the
/// request body, so a tampered payload cannot change what a shopper pays.
/// </summary>
public class ProductCatalogClient(HttpClient httpClient, ILogger<ProductCatalogClient> logger) : IProductCatalogClient
{
    public async Task<CatalogProduct?> GetProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(new Uri($"api/products/{productId}", UriKind.Relative), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Catalogue returned {StatusCode} for product {ProductId}", response.StatusCode, productId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CatalogProduct>(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Catalogue lookup failed for product {ProductId}", productId);
            return null;
        }
    }
}

/// <summary>
/// Registration helper for the catalogue client.
/// </summary>
public static class ProductCatalogServiceCollectionExtensions
{
    public static IServiceCollection AddProductCatalog(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var baseUrl = configuration["Services:Catalog:BaseUrl"] ?? "http://localhost:5001";

        services.AddHttpClient<IProductCatalogClient, ProductCatalogClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
