namespace ProductService.Models;

/// <summary>
/// Represents a product in the AVRA catalog
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;

    /// <summary>Units currently available to sell.</summary>
    public int Stock { get; set; }

    /// <summary>Level at which the item is flagged for reordering.</summary>
    public int RestockThreshold { get; set; }

    /// <summary>Maximum stock the warehouse will hold for this item.</summary>
    public int MaxStockThreshold { get; set; } = 100;

    /// <summary>True once stock has fallen below <see cref="RestockThreshold"/>.</summary>
    public bool OnReorder { get; set; }

    public string? Tag { get; set; }
    public string? Details { get; set; }
    public string? Image1 { get; set; }
    public string? Image2 { get; set; }
    public string? ImageGalleryJson { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Takes units out of stock, returning how many were actually removed. A partial removal is
    /// deliberate: it lets the caller decide whether to accept a short shipment or reject the line.
    /// </summary>
    /// <param name="quantityDesired">Units requested.</param>
    public int RemoveStock(int quantityDesired)
    {
        if (Stock == 0)
        {
            throw new InvalidOperationException($"Empty stock, product item {Name} is sold out");
        }

        if (quantityDesired <= 0)
        {
            throw new InvalidOperationException("Item units desired should be greater than zero");
        }

        var removed = Math.Min(quantityDesired, Stock);
        Stock -= removed;
        OnReorder = Stock <= RestockThreshold;
        UpdatedAt = DateTime.UtcNow;

        return removed;
    }

    /// <summary>
    /// Returns units to stock, capped at <see cref="MaxStockThreshold"/>, and returns the new level.
    /// </summary>
    /// <param name="quantity">Units to return.</param>
    public int AddStock(int quantity)
    {
        var original = Stock;

        if (Stock + quantity > MaxStockThreshold)
        {
            Stock += MaxStockThreshold - Stock;
        }
        else
        {
            Stock += quantity;
        }

        OnReorder = false;
        UpdatedAt = DateTime.UtcNow;

        return Stock - original;
    }
}

/// <summary>
/// Represents a product category
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>
/// DTO for creating/updating products
/// </summary>
public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string? Tag { get; set; }
    public string? Image1 { get; set; }
    public string? Image2 { get; set; }
    public List<string>? ImageUrls { get; set; }
}

/// <summary>
/// DTO for product responses
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Details { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string? Tag { get; set; }
    public string? Image1 { get; set; }
    public string? Image2 { get; set; }
    public IReadOnlyList<string> ImageUrls { get; set; } = [];
    public bool IsActive { get; set; }
}

/// <summary>
/// Result of uploading a product image: the stored reference to persist and a signed URL to preview it.
/// </summary>
public class ImageUploadResultDto
{
    /// <summary>The stored blob reference to save against the product.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>A short-lived signed URL for previewing the freshly uploaded image.</summary>
    public string? Url { get; set; }
}

/// <summary>
/// Warehouse view of a product. Unlike <see cref="ProductDto"/> it carries the reorder settings
/// and includes de-listed items, because stock still has to be counted for those.
/// </summary>
public class InventoryItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int RestockThreshold { get; set; }
    public int MaxStockThreshold { get; set; }
    public bool OnReorder { get; set; }
    public bool IsActive { get; set; }
    public string? Image1 { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// A stock movement. Positive quantities are receipts, negative ones are write-offs.
/// </summary>
public class StockAdjustmentDto
{
    public int Quantity { get; set; }

    /// <summary>Why the count changed — recorded in the service log for auditing.</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Reorder settings for a product.</summary>
public class StockThresholdsDto
{
    public int RestockThreshold { get; set; }
    public int MaxStockThreshold { get; set; }
}
