using System.ComponentModel.DataAnnotations;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avra.ServiceDefaults.Storage;

/// <summary>Configuration for catalogue image storage in Azure Blob Storage.</summary>
public sealed class ImageStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Blob service endpoint, e.g. https://acct.blob.core.windows.net. Empty disables blob storage.</summary>
    public string BlobEndpoint { get; set; } = string.Empty;

    /// <summary>Container that holds catalogue images.</summary>
    public string Container { get; set; } = "catalogue-images";

    /// <summary>Lifetime of the read SAS URLs handed to the browser.</summary>
    [Range(1, 1440)]
    public int SasMinutes { get; set; } = 60;
}

/// <summary>
/// Stores catalogue images in a private blob container and hands out short-lived read SAS URLs.
/// Uploads and the user-delegation key are authenticated with the container app's managed identity.
/// </summary>
public interface IImageStorage
{
    /// <summary>True when a real blob backend is configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Uploads image bytes and returns the stored blob name to persist against the record.</summary>
    Task<string> UploadAsync(Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns a stored reference into something an &lt;img&gt; can load. Absolute URLs pass through;
    /// blob names become a time-limited read SAS URL. Returns the reference unchanged when blob
    /// storage is not configured (e.g. local development using bundled assets).
    /// </summary>
    string? ToDisplayUrl(string? reference);

    /// <summary>
    /// Inverse of <see cref="ToDisplayUrl"/>: collapses a signed URL for our own container back to the
    /// bare blob name so re-saving a record never persists an expiring SAS URL. Other values pass through.
    /// </summary>
    string? ToStorageReference(string? displayUrl);
}

/// <summary>No-op storage used when no blob endpoint is configured; references pass through untouched.</summary>
public sealed class NullImageStorage : IImageStorage
{
    public bool IsConfigured => false;

    public Task<string> UploadAsync(Stream content, string contentType, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Image storage is not configured. Set Storage:BlobEndpoint to enable uploads.");

    public string? ToDisplayUrl(string? reference) => string.IsNullOrWhiteSpace(reference) ? null : reference;

    public string? ToStorageReference(string? displayUrl) => string.IsNullOrWhiteSpace(displayUrl) ? displayUrl : displayUrl.Trim();
}

/// <summary>Azure Blob Storage implementation using managed identity and user-delegation SAS.</summary>
public sealed class BlobImageStorage : IImageStorage
{
    private static readonly IReadOnlyDictionary<string, string> ExtensionByContentType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    private readonly BlobServiceClient _service;
    private readonly BlobContainerClient _container;
    private readonly ImageStorageOptions _options;
    private readonly ILogger<BlobImageStorage> _logger;
    private readonly object _keyLock = new();
    private UserDelegationKey? _delegationKey;
    private DateTimeOffset _delegationKeyExpiresAt = DateTimeOffset.MinValue;

    public BlobImageStorage(ImageStorageOptions options, string? managedIdentityClientId, ILogger<BlobImageStorage> logger)
    {
        _options = options;
        _logger = logger;

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = string.IsNullOrWhiteSpace(managedIdentityClientId) ? null : managedIdentityClientId,
        });

        _service = new BlobServiceClient(new Uri(options.BlobEndpoint), credential);
        _container = _service.GetBlobContainerClient(options.Container);
    }

    public bool IsConfigured => true;

    public async Task<string> UploadAsync(Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!ExtensionByContentType.TryGetValue(contentType ?? string.Empty, out var extension))
        {
            throw new InvalidOperationException($"Unsupported image type '{contentType}'. Allowed: JPEG, PNG, WebP, GIF.");
        }

        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blobName = $"{Guid.NewGuid():N}{extension}";
        var blob = _container.GetBlobClient(blobName);

        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        _logger.LogInformation("Uploaded catalogue image {BlobName}", blobName);
        return blobName;
    }

    public string? ToDisplayUrl(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        // Absolute URLs and data URIs are already loadable; only blob names need signing.
        if (reference.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return reference;
        }

        var blobName = reference.Contains('/') ? reference[(reference.LastIndexOf('/') + 1)..] : reference;

        try
        {
            var key = GetDelegationKey();
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _options.Container,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_options.SasMinutes),
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sas = sasBuilder.ToSasQueryParameters(key, _service.AccountName).ToString();
            return $"{_container.GetBlobClient(blobName).Uri}?{sas}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign catalogue image {Reference}", reference);
            return null;
        }
    }

    public string? ToStorageReference(string? displayUrl)
    {
        if (string.IsNullOrWhiteSpace(displayUrl))
        {
            return displayUrl;
        }

        if (!Uri.TryCreate(displayUrl.Trim(), UriKind.Absolute, out var uri))
        {
            return displayUrl.Trim();
        }

        var prefix = $"/{_options.Container}/";
        if (string.Equals(uri.Host, _service.Uri.Host, StringComparison.OrdinalIgnoreCase) &&
            uri.AbsolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            // AbsolutePath excludes the query, so the SAS token is dropped here.
            return Uri.UnescapeDataString(uri.AbsolutePath[prefix.Length..]);
        }

        return displayUrl.Trim();
    }

    private UserDelegationKey GetDelegationKey()
    {
        lock (_keyLock)
        {
            if (_delegationKey is not null && DateTimeOffset.UtcNow < _delegationKeyExpiresAt.AddMinutes(-10))
            {
                return _delegationKey;
            }

            var expiry = DateTimeOffset.UtcNow.AddHours(6);
            _delegationKey = _service.GetUserDelegationKey(DateTimeOffset.UtcNow.AddMinutes(-5), expiry).Value;
            _delegationKeyExpiresAt = expiry;
            return _delegationKey;
        }
    }
}

/// <summary>Registers catalogue image storage.</summary>
public static class ImageStorageExtensions
{
    public static WebApplicationBuilder AddAvraImageStorage(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Configuration.GetSection(ImageStorageOptions.SectionName).Get<ImageStorageOptions>() ?? new ImageStorageOptions();

        if (string.IsNullOrWhiteSpace(options.BlobEndpoint))
        {
            builder.Services.AddSingleton<IImageStorage, NullImageStorage>();
            return builder;
        }

        var clientId = builder.Configuration["AZURE_CLIENT_ID"];
        builder.Services.AddSingleton<IImageStorage>(sp =>
            new BlobImageStorage(options, clientId, sp.GetRequiredService<ILogger<BlobImageStorage>>()));

        return builder;
    }
}
