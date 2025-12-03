using System.Reflection;
using System.Text.Json;

namespace RaceDay.Core;

public class ProductRepository
{
    private static List<ProductInfo>? _products;
    private static readonly object _lock = new object();

    public static async Task<List<ProductInfo>> GetAllProductsAsync()
    {
        if (_products != null)
            return _products;

        lock (_lock)
        {
            if (_products != null)
                return _products;

            _products = LoadProductsFromJson();
        }

        return await Task.FromResult(_products);
    }

    public static async Task<List<ProductInfo>> GetProductsByTypeAsync(string productType)
    {
        var all = await GetAllProductsAsync();
        return all.Where(p => p.ProductType.Equals(productType, StringComparison.OrdinalIgnoreCase))
                  .ToList();
    }

    public static async Task<ProductInfo?> GetProductByIdAsync(string id)
    {
        var all = await GetAllProductsAsync();
        return all.FirstOrDefault(p => p.Id == id);
    }

    public static async Task<List<ProductInfo>> SearchProductsAsync(string query)
    {
        var all = await GetAllProductsAsync();
        var lowerQuery = query.ToLower();
        
        return all.Where(p => 
            p.Name.ToLower().Contains(lowerQuery) ||
            p.Brand.ToLower().Contains(lowerQuery) ||
            p.Id.ToLower().Contains(lowerQuery))
            .ToList();
    }

    private static List<ProductInfo> LoadProductsFromJson()
    {
        try
        {
            var assembly = typeof(ProductRepository).Assembly;
            var resourceName = "RaceDay.Core.Data.products.json";
            
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return new List<ProductInfo>();

                using (var reader = new StreamReader(stream))
                {
                    var json = reader.ReadToEnd();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var products = JsonSerializer.Deserialize<List<ProductInfo>>(json, options);
                    return products ?? new List<ProductInfo>();
                }
            }
        }
        catch
        {
            return new List<ProductInfo>();
        }
    }
}
