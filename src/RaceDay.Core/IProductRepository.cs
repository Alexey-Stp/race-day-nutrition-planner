namespace RaceDay.Core;

/// <summary>
/// Interface for accessing product information
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets all available products
    /// </summary>
    Task<List<ProductInfo>> GetAllProductsAsync();

    /// <summary>
    /// Gets products filtered by type
    /// </summary>
    Task<List<ProductInfo>> GetProductsByTypeAsync(string productType);

    /// <summary>
    /// Gets a specific product by ID
    /// </summary>
    Task<ProductInfo?> GetProductByIdAsync(string id);

    /// <summary>
    /// Searches products by name, brand, or ID
    /// </summary>
    Task<List<ProductInfo>> SearchProductsAsync(string query);
}
