namespace RaceDay.Core.Repositories;
using RaceDay.Core.Models;

/// <summary>
/// Interface for accessing product information
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets all available products
    /// </summary>
    Task<List<ProductInfo>> GetAllProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products filtered by type
    /// </summary>
    Task<List<ProductInfo>> GetProductsByTypeAsync(string productType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific product by ID
    /// </summary>
    Task<ProductInfo?> GetProductByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name, brand, or ID
    /// </summary>
    Task<List<ProductInfo>> SearchProductsAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get filtered products by brand and excluded types
    /// </summary>
    /// <param name="filter">Filter criteria (brand, excluded types)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered list of products</returns>
    Task<List<ProductInfo>> GetFilteredProductsAsync(ProductFilter? filter, CancellationToken cancellationToken = default);
}
