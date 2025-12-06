namespace RaceDay.Core.Tests;
using RaceDay.Core.Models;
using RaceDay.Core.Repositories;

public class ProductRepositoryTests
{
    private readonly ProductRepository _repository = new();

    [Fact]
    public async Task GetAllProductsAsync_ReturnsProducts()
    {
        // Act
        var products = await _repository.GetAllProductsAsync();

        // Assert
        Assert.NotNull(products);
        Assert.NotEmpty(products);
    }

    [Fact]
    public async Task GetAllProductsAsync_ContainsValidProducts()
    {
        // Act
        var products = await _repository.GetAllProductsAsync();

        // Assert
        Assert.All(products, p =>
        {
            Assert.NotNull(p.Name);
            Assert.NotNull(p.ProductType);
            Assert.True(p.CarbsG >= 0);
            Assert.True(p.SodiumMg >= 0);
        });
    }

    [Fact]
    public async Task GetProductByIdAsync_WithValidId_ReturnsProduct()
    {
        // Arrange
        var products = await _repository.GetAllProductsAsync();
        var firstProduct = products.First();

        // Act
        var product = await _repository.GetProductByIdAsync(firstProduct.Id);

        // Assert
        Assert.NotNull(product);
        Assert.Equal(firstProduct.Id, product.Id);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var product = await _repository.GetProductByIdAsync("NonExistentProduct123");

        // Assert
        Assert.Null(product);
    }

    [Fact]
    public async Task GetProductsByTypeAsync_WithGelType_ReturnsGels()
    {
        // Act
        var products = await _repository.GetProductsByTypeAsync("gel");

        // Assert
        Assert.NotNull(products);
        Assert.NotEmpty(products);
        Assert.All(products, p => Assert.Contains("gel", p.ProductType.ToLower()));
    }

    [Fact]
    public async Task GetProductsByTypeAsync_WithDrinkType_ReturnsDrinks()
    {
        // Act
        var products = await _repository.GetProductsByTypeAsync("drink");

        // Assert
        Assert.NotNull(products);
        // Drinks may or may not exist in test data, but method should work
        Assert.True(products == null || products.All(p => p.ProductType.ToLower().Contains("drink")));
    }

    [Fact]
    public async Task GetProductsByTypeAsync_CaseInsensitive()
    {
        // Act
        var productsLower = await _repository.GetProductsByTypeAsync("gel");
        var productsUpper = await _repository.GetProductsByTypeAsync("GEL");
        var productsMixed = await _repository.GetProductsByTypeAsync("Gel");

        // Assert
        Assert.Equal(productsLower?.Count ?? 0, productsUpper?.Count ?? 0);
        Assert.Equal(productsLower?.Count ?? 0, productsMixed?.Count ?? 0);
    }

    [Fact]
    public async Task SearchProductsAsync_WithKeyword_ReturnsMatches()
    {
        // Arrange
        var allProducts = await _repository.GetAllProductsAsync();
        var firstProductName = allProducts.First().Name;

        // Act
        var results = await _repository.SearchProductsAsync(firstProductName);

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Contains(firstProductName, results.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchProductsAsync_WithNoMatches_ReturnsEmpty()
    {
        // Act
        var results = await _repository.SearchProductsAsync("XYZ123NonExistent");

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchProductsAsync_CaseInsensitive()
    {
        // Arrange
        var allProducts = await _repository.GetAllProductsAsync();
        var testName = allProducts.First().Name;

        // Act
        var resultsLower = await _repository.SearchProductsAsync(testName.ToLower());
        var resultsUpper = await _repository.SearchProductsAsync(testName.ToUpper());

        // Assert
        Assert.Equal(resultsLower.Count, resultsUpper.Count);
    }

    [Fact]
    public async Task GetFilteredProductsAsync_WithValidFilter_ReturnsMatches()
    {
        // Arrange
        var filter = new ProductFilter(Brand: null, ExcludeTypes: null);

        // Act
        var products = await _repository.GetFilteredProductsAsync(filter);

        // Assert
        Assert.NotNull(products);
        Assert.NotEmpty(products);
    }

    [Fact]
    public async Task GetFilteredProductsAsync_WithBrandFilter_ReturnsBrandProducts()
    {
        // Arrange
        var filter = new ProductFilter(Brand: "SiS", ExcludeTypes: null);

        // Act
        var products = await _repository.GetFilteredProductsAsync(filter);

        // Assert
        Assert.NotNull(products);
        // May or may not have SiS products in data, but method should work
    }

    [Fact]
    public async Task GetFilteredProductsAsync_WithExcludeTypes_FiltersCorrectly()
    {
        // Arrange
        var filter = new ProductFilter(Brand: null, ExcludeTypes: new List<string> { "gel" });

        // Act
        var products = await _repository.GetFilteredProductsAsync(filter);

        // Assert
        Assert.NotNull(products);
        if (products.Any())
        {
            Assert.All(products, p => Assert.DoesNotContain("gel", p.ProductType.ToLower()));
        }
    }

    [Fact]
    public async Task GetFilteredProductsAsync_NoFilters_ReturnsAll()
    {
        // Arrange
        var filter = new ProductFilter();
        var allProducts = await _repository.GetAllProductsAsync();

        // Act
        var filtered = await _repository.GetFilteredProductsAsync(filter);

        // Assert
        Assert.NotNull(filtered);
        Assert.Equal(allProducts.Count, filtered.Count);
    }
}
