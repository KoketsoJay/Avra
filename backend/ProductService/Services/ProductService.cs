using Avra.EventBus.IntegrationEvents;
using Avra.ServiceDefaults.Storage;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.IntegrationEvents;
using ProductService.Models;
using System.Text.Json;

namespace ProductService.Services;

/// <summary>
/// Service for managing products
/// </summary>
public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(string category);
    Task<ProductDto> CreateProductAsync(CreateProductDto createDto);
    Task<ProductDto> UpdateProductAsync(int id, CreateProductDto updateDto);
    Task<bool> DeleteProductAsync(int id);
    Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm);

    /// <summary>Warehouse view of every product, de-listed items included.</summary>
    Task<IEnumerable<InventoryItemDto>> GetInventoryAsync();

    /// <summary>Products at or below their reorder threshold.</summary>
    Task<IEnumerable<InventoryItemDto>> GetLowStockAsync();

    /// <summary>Applies a stock movement and returns the resulting inventory position.</summary>
    Task<InventoryItemDto> AdjustStockAsync(int id, StockAdjustmentDto adjustment);

    /// <summary>Updates the reorder settings for a product.</summary>
    Task<InventoryItemDto> UpdateThresholdsAsync(int id, StockThresholdsDto thresholds);
}

/// <summary>
/// Implementation of IProductService
/// </summary>
public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    private readonly ICatalogIntegrationEventService _integrationEventService;
    private readonly IImageStorage _imageStorage;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        ProductDbContext context,
        ICatalogIntegrationEventService integrationEventService,
        IImageStorage imageStorage,
        ILogger<ProductService> logger)
    {
        _context = context;
        _integrationEventService = integrationEventService;
        _imageStorage = imageStorage;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        _logger.LogInformation("Fetching all products");
        
        var products = await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        _logger.LogInformation("Fetching product with id: {ProductId}", id);
        
        var product = await _context.Products.FindAsync(id);
        
        if (product == null)
        {
            _logger.LogWarning("Product not found with id: {ProductId}", id);
            return null;
        }

        return MapToDto(product);
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(string category)
    {
        _logger.LogInformation("Fetching products in category: {Category}", category);
        
        var products = await _context.Products
            .Where(p => p.Category == category && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createDto)
    {
        _logger.LogInformation("Creating new product: {ProductName}", createDto.Name);
        var imageUrls = ToStorageReferences(NormalizeImageUrls(createDto.ImageUrls, createDto.Image1, createDto.Image2));
        
        var product = new Product
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Details = string.IsNullOrWhiteSpace(createDto.Details) ? createDto.Description : createDto.Details,
            Price = createDto.Price,
            Category = createDto.Category,
            Stock = createDto.Stock,
            Tag = createDto.Tag,
            Image1 = imageUrls.ElementAtOrDefault(0),
            Image2 = imageUrls.ElementAtOrDefault(1) ?? imageUrls.ElementAtOrDefault(0),
            ImageGalleryJson = SerializeImageUrls(imageUrls),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created successfully with id: {ProductId}", product.Id);
        
        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateProductAsync(int id, CreateProductDto updateDto)
    {
        _logger.LogInformation("Updating product with id: {ProductId}", id);
        
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with id {id} not found");
        }

        var oldPrice = product.Price;
        var imageUrls = ToStorageReferences(NormalizeImageUrls(updateDto.ImageUrls, updateDto.Image1, updateDto.Image2));

        product.Name = updateDto.Name;
        product.Description = updateDto.Description;
        product.Details = string.IsNullOrWhiteSpace(updateDto.Details) ? updateDto.Description : updateDto.Details;
        product.Price = updateDto.Price;
        product.Category = updateDto.Category;
        product.Stock = updateDto.Stock;
        product.Tag = updateDto.Tag;
        product.Image1 = imageUrls.ElementAtOrDefault(0);
        product.Image2 = imageUrls.ElementAtOrDefault(1) ?? imageUrls.ElementAtOrDefault(0);
        product.ImageGalleryJson = SerializeImageUrls(imageUrls);
        product.UpdatedAt = DateTime.UtcNow;

        if (oldPrice != product.Price)
        {
            // Open baskets must be re-priced, so the change and its announcement commit together.
            var priceChangedEvent = new ProductPriceChangedIntegrationEvent(product.Id, product.Price, oldPrice);
            await _integrationEventService.SaveEventAndCatalogContextChangesAsync(priceChangedEvent);
            await _integrationEventService.PublishThroughEventBusAsync(priceChangedEvent);
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Product updated successfully with id: {ProductId}", id);
        
        return MapToDto(product);
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        _logger.LogInformation("Deleting product with id: {ProductId}", id);
        
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            _logger.LogWarning("Product not found for deletion with id: {ProductId}", id);
            return false;
        }

        // Hard delete
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product deleted successfully with id: {ProductId}", id);
        
        return true;
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        _logger.LogInformation("Searching products with term: {SearchTerm}", searchTerm);
        
        var products = await _context.Products
            .Where(p => p.IsActive && 
                   (p.Name.Contains(searchTerm) || 
                    p.Description.Contains(searchTerm) ||
                    p.Category.Contains(searchTerm)))
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<InventoryItemDto>> GetInventoryAsync()
    {
        _logger.LogInformation("Fetching the full inventory position");

        var products = await _context.Products
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToInventoryDto);
    }

    public async Task<IEnumerable<InventoryItemDto>> GetLowStockAsync()
    {
        _logger.LogInformation("Fetching products at or below their reorder threshold");

        var products = await _context.Products
            .Where(p => p.IsActive && p.Stock <= p.RestockThreshold)
            .OrderBy(p => p.Stock)
            .ToListAsync();

        return products.Select(MapToInventoryDto);
    }

    public async Task<InventoryItemDto> AdjustStockAsync(int id, StockAdjustmentDto adjustment)
    {
        ArgumentNullException.ThrowIfNull(adjustment);

        if (adjustment.Quantity == 0)
        {
            throw new InvalidOperationException("A stock adjustment must move at least one unit");
        }

        var product = await _context.Products.FindAsync(id)
            ?? throw new KeyNotFoundException($"Product with id {id} not found");

        if (adjustment.Quantity > 0)
        {
            var added = product.AddStock(adjustment.Quantity);
            if (added < adjustment.Quantity)
            {
                _logger.LogWarning(
                    "Only {Added} of {Requested} units were received for product {ProductId}; the warehouse cap was reached",
                    added, adjustment.Quantity, id);
            }
        }
        else
        {
            var requested = -adjustment.Quantity;
            if (requested > product.Stock)
            {
                throw new InvalidOperationException($"Cannot remove {requested} units; only {product.Stock} are in stock");
            }

            product.RemoveStock(requested);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Stock for product {ProductId} adjusted by {Quantity} to {Stock}. Reason: {Reason}",
            id, adjustment.Quantity, product.Stock, string.IsNullOrWhiteSpace(adjustment.Reason) ? "not given" : adjustment.Reason);

        return MapToInventoryDto(product);
    }

    public async Task<InventoryItemDto> UpdateThresholdsAsync(int id, StockThresholdsDto thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        if (thresholds.RestockThreshold < 0 || thresholds.MaxStockThreshold <= 0)
        {
            throw new InvalidOperationException("Thresholds must be positive");
        }

        if (thresholds.RestockThreshold >= thresholds.MaxStockThreshold)
        {
            throw new InvalidOperationException("The reorder threshold must be below the warehouse cap");
        }

        var product = await _context.Products.FindAsync(id)
            ?? throw new KeyNotFoundException($"Product with id {id} not found");

        product.RestockThreshold = thresholds.RestockThreshold;
        product.MaxStockThreshold = thresholds.MaxStockThreshold;
        product.OnReorder = product.Stock <= product.RestockThreshold;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reorder settings updated for product {ProductId}", id);

        return MapToInventoryDto(product);
    }

    private InventoryItemDto MapToInventoryDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Category = product.Category,
        Price = product.Price,
        Stock = product.Stock,
        RestockThreshold = product.RestockThreshold,
        MaxStockThreshold = product.MaxStockThreshold,
        OnReorder = product.OnReorder,
        IsActive = product.IsActive,
        Image1 = _imageStorage.ToDisplayUrl(product.Image1),
        UpdatedAt = product.UpdatedAt,
    };

    private ProductDto MapToDto(Product product)
    {
        var imageUrls = DeserializeImageUrls(product.ImageGalleryJson, product.Image1, product.Image2)
            .Select(_imageStorage.ToDisplayUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url!)
            .ToList();

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Details = string.IsNullOrWhiteSpace(product.Details) ? product.Description : product.Details,
            Price = product.Price,
            Category = product.Category,
            Stock = product.Stock,
            Tag = product.Tag,
            Image1 = _imageStorage.ToDisplayUrl(product.Image1),
            Image2 = _imageStorage.ToDisplayUrl(product.Image2),
            ImageUrls = imageUrls,
            IsActive = product.IsActive
        };
    }

    private List<string> ToStorageReferences(IEnumerable<string> references) =>
        references
            .Select(_imageStorage.ToStorageReference)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<string> NormalizeImageUrls(IEnumerable<string>? imageUrls, params string?[] fallbackImages)
    {
        var normalized = (imageUrls ?? [])
            .Select(url => (url ?? string.Empty).Trim())
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count > 0)
        {
            return normalized;
        }

        return fallbackImages
            .Select(url => (url ?? string.Empty).Trim())
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? SerializeImageUrls(IReadOnlyList<string> imageUrls) =>
        imageUrls.Count == 0 ? null : JsonSerializer.Serialize(imageUrls);

    private static IReadOnlyList<string> DeserializeImageUrls(string? imageGalleryJson, params string?[] fallbackImages)
    {
        if (!string.IsNullOrWhiteSpace(imageGalleryJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(imageGalleryJson);
                var normalized = NormalizeImageUrls(parsed, fallbackImages);
                if (normalized.Count > 0)
                {
                    return normalized;
                }
            }
            catch (JsonException)
            {
                // Older data can still be consumed through image1/image2 fallback values.
            }
        }

        return NormalizeImageUrls(null, fallbackImages);
    }
}
