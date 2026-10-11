namespace RaceDay.Core.Repositories;
using RaceDay.Core.Models;

/// <summary>
/// Interface for accessing product information. Returned lists are read-only snapshots of the catalogue.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets all available products
    /// </summary>
    /// <exception cref="RaceDay.Core.Exceptions.CatalogueLoadException">The embedded catalogue could not be loaded.</exception>
    Task<IReadOnlyList<ProductInfo>> GetAllProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products filtered by type (case-insensitive)
    /// </summary>
    Task<IReadOnlyList<ProductInfo>> GetProductsByTypeAsync(string productType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific product by ID (case-insensitive)
    /// </summary>
    Task<ProductInfo?> GetProductByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name, brand, or ID (case-insensitive, culture-independent).
    /// A null or blank query returns an empty list.
    /// </summary>
    Task<IReadOnlyList<ProductInfo>> SearchProductsAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get filtered products by brand and excluded types
    /// </summary>
    /// <param name="filter">Filter criteria (brand, excluded types)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered list of products</returns>
    Task<IReadOnlyList<ProductInfo>> GetFilteredProductsAsync(ProductFilter? filter, CancellationToken cancellationToken = default);
}
