using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RaceDay.Core.Exceptions;
using RaceDay.Core.Models;

namespace RaceDay.Core.Repositories;

/// <summary>
/// Repository for loading and accessing nutrition product information from embedded JSON files
/// </summary>
public class ProductRepository : IProductRepository
{
    private const string CatalogueName = "product";
    private const string ResourcePrefix = "RaceDay.Core.Data.";
    private const string ResourceSuffix = "-products.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // PublicationOnly: a failed load is not cached, so a later call can retry.
    private readonly Lazy<IReadOnlyList<ProductInfo>> _productsLazy;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(ILogger<ProductRepository>? logger = null)
        : this(LoadProductsFromEmbeddedResources, logger)
    {
    }

    /// <summary>Test seam: supply the raw catalogue loader.</summary>
    internal ProductRepository(Func<IEnumerable<ProductInfo>> loader, ILogger<ProductRepository>? logger = null)
    {
        _logger = logger ?? NullLogger<ProductRepository>.Instance;
        _productsLazy = new Lazy<IReadOnlyList<ProductInfo>>(
            () => LoadCatalogue(loader, _logger),
            LazyThreadSafetyMode.PublicationOnly);
    }

    public Task<IReadOnlyList<ProductInfo>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_productsLazy.Value);
    }

    public async Task<IReadOnlyList<ProductInfo>> GetProductsByTypeAsync(string productType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productType))
            return Array.Empty<ProductInfo>();

        var all = await GetAllProductsAsync(cancellationToken);
        return all.Where(p => p.ProductType.Equals(productType.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<ProductInfo?> GetProductByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var all = await GetAllProductsAsync(cancellationToken);
        return all.FirstOrDefault(p => p.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<ProductInfo>();

        var term = query.Trim();
        var all = await GetAllProductsAsync(cancellationToken);
        return all.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Brand.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductInfo>> GetFilteredProductsAsync(ProductFilter? filter, CancellationToken cancellationToken = default)
    {
        var all = await GetAllProductsAsync(cancellationToken);
        if (filter == null)
            return all;

        IEnumerable<ProductInfo> filtered = all;

        if (!string.IsNullOrWhiteSpace(filter.Brand))
            filtered = filtered.Where(p => p.Brand.Equals(filter.Brand.Trim(), StringComparison.OrdinalIgnoreCase));

        if (filter.ExcludeTypes is { Count: > 0 })
        {
            var excluded = new HashSet<string>(
                filter.ExcludeTypes.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()),
                StringComparer.OrdinalIgnoreCase);
            filtered = filtered.Where(p => !excluded.Contains(p.ProductType));
        }

        return filtered.ToList();
    }

    private static ReadOnlyCollection<ProductInfo> LoadCatalogue(Func<IEnumerable<ProductInfo>> loader, ILogger logger)
    {
        try
        {
            var products = loader().ToList();
            if (products.Count == 0)
            {
                logger.LogError("Product catalogue is empty");
                throw new CatalogueLoadException(CatalogueName, "no products were found in the embedded resources");
            }
            return products.AsReadOnly();
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidOperationException or NotSupportedException)
        {
            logger.LogError(ex, "Failed to load products from embedded resources");
            throw new CatalogueLoadException(CatalogueName, ex.Message, ex);
        }
    }

    private static IEnumerable<ProductInfo> LoadProductsFromEmbeddedResources()
    {
        var assembly = typeof(ProductRepository).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                           && name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var products = new List<ProductInfo>();
        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new CatalogueLoadException(CatalogueName, $"embedded resource '{resourceName}' could not be opened");
            var brandProducts = JsonSerializer.Deserialize<List<ProductInfo>>(stream, JsonOptions)
                ?? throw new CatalogueLoadException(CatalogueName, $"embedded resource '{resourceName}' is empty");
            products.AddRange(brandProducts);
        }
        return products;
    }
}
