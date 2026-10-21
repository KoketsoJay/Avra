using Microsoft.EntityFrameworkCore;

namespace ProductService.Data;

/// <summary>
/// Applies forward-compatible schema updates for local product databases.
/// </summary>
public static class ProductSchemaBootstrapper
{
    public static async Task EnsureLatestProductSchemaAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProductDbContext>>();

        const string addDetailsColumnSql = """
IF OBJECT_ID(N'[Products]', N'U') IS NOT NULL AND COL_LENGTH('Products', 'Details') IS NULL
BEGIN
    ALTER TABLE [Products] ADD [Details] nvarchar(max) NULL;
END;
""";

        const string addImageGalleryColumnSql = """
IF OBJECT_ID(N'[Products]', N'U') IS NOT NULL AND COL_LENGTH('Products', 'ImageGalleryJson') IS NULL
BEGIN
    ALTER TABLE [Products] ADD [ImageGalleryJson] nvarchar(max) NULL;
END;
""";

        const string seedDetailsSql = """
IF OBJECT_ID(N'[Products]', N'U') IS NOT NULL
BEGIN
    UPDATE [Products]
    SET [Details] = [Description]
    WHERE [Details] IS NULL;
END;
""";

        const string seedImageGallerySql = """
IF OBJECT_ID(N'[Products]', N'U') IS NOT NULL
BEGIN
    UPDATE [Products]
    SET [ImageGalleryJson] =
        CASE
            WHEN NULLIF(LTRIM(RTRIM(ISNULL([Image1], ''))), '') IS NULL AND NULLIF(LTRIM(RTRIM(ISNULL([Image2], ''))), '') IS NULL THEN NULL
            WHEN NULLIF(LTRIM(RTRIM(ISNULL([Image1], ''))), '') IS NULL THEN CONCAT('["', REPLACE([Image2], '"', '\\"'), '"]')
            WHEN NULLIF(LTRIM(RTRIM(ISNULL([Image2], ''))), '') IS NULL OR [Image1] = [Image2] THEN CONCAT('["', REPLACE([Image1], '"', '\\"'), '"]')
            ELSE CONCAT('["', REPLACE([Image1], '"', '\\"'), '","', REPLACE([Image2], '"', '\\"'), '"]')
        END
    WHERE [ImageGalleryJson] IS NULL;
END;
""";

        try
        {
            await context.Database.ExecuteSqlRawAsync(addDetailsColumnSql, cancellationToken);
            await context.Database.ExecuteSqlRawAsync(addImageGalleryColumnSql, cancellationToken);
            await context.Database.ExecuteSqlRawAsync(seedDetailsSql, cancellationToken);
            await context.Database.ExecuteSqlRawAsync(seedImageGallerySql, cancellationToken);
            logger.LogInformation("Applied ProductService compatibility schema updates");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed applying ProductService compatibility schema updates");
        }
    }
}
