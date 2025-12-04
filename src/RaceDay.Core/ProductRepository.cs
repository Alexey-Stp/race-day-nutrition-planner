using System.Reflection;
using System.Text.Json;

namespace RaceDay.Core;

/// <summary>
/// Repository for loading and accessing nutrition product information from embedded JSON files
/// </summary>
public class ProductRepository : IProductRepository
{
    private static List<ProductInfo>? _products;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public async Task<List<ProductInfo>> GetAllProductsAsync()
    {
        if (_products != null)
            return _products;

        await _semaphore.WaitAsync();
        try
        {
            if (_products != null)
                return _products;

            _products = await Task.Run(() => LoadProductsFromJsonFiles());
        }
        finally
        {
            _semaphore.Release();
        }

        return _products;
    }

    public async Task<List<ProductInfo>> GetProductsByTypeAsync(string productType)
    {
        var all = await GetAllProductsAsync();
        return all.Where(p => p.ProductType.Equals(productType, StringComparison.OrdinalIgnoreCase))
                  .ToList();
    }

    public async Task<ProductInfo?> GetProductByIdAsync(string id)
    {
        var all = await GetAllProductsAsync();
        return all.FirstOrDefault(p => p.Id == id);
    }

    public async Task<List<ProductInfo>> SearchProductsAsync(string query)
    {
        var all = await GetAllProductsAsync();
        var lowerQuery = query.ToLower();
        
        return all.Where(p => 
            p.Name.ToLower().Contains(lowerQuery) ||
            p.Brand.ToLower().Contains(lowerQuery) ||
            p.Id.ToLower().Contains(lowerQuery))
            .ToList();
    }

    private static List<ProductInfo> LoadProductsFromJsonFiles()
    {
        try
        {
            var assembly = typeof(ProductRepository).Assembly;
            var products = new List<ProductInfo>();
            
            // Get all embedded resource names that match the pattern
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith("RaceDay.Core.Data.") && name.EndsWith("-products.json"))
                .OrderBy(name => name)
                .ToList();

            foreach (var resourceName in resourceNames)
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        continue;

                    using (var reader = new StreamReader(stream))
                    {
                        var json = reader.ReadToEnd();
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var brandProducts = JsonSerializer.Deserialize<List<ProductInfo>>(json, options);
                        
                        if (brandProducts != null)
                            products.AddRange(brandProducts);
                    }
                }
            }

            return products;
        }
        catch
        {
            return new List<ProductInfo>();
        }
    }
}
