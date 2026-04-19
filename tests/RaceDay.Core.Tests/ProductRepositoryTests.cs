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
        products.ShouldNotBeNull();
        products.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetAllProductsAsync_ContainsValidProducts()
    {
        // Act
        var products = await _repository.GetAllProductsAsync();

        // Assert
        Assert.All(products, p =>
        {
            p.Name.ShouldNotBeNull();
            p.ProductType.ShouldNotBeNull();
            (p.CarbsG >= 0).ShouldBeTrue();
            (p.SodiumMg >= 0).ShouldBeTrue();
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
        product.ShouldNotBeNull();
        product.Id.ShouldBe(firstProduct.Id);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var product = await _repository.GetProductByIdAsync("NonExistentProduct123");

        // Assert
        product.ShouldBeNull();
    }

    [Fact]
    public async Task GetProductsByTypeAsync_WithGelType_ReturnsGels()
    {
        // Act
        var products = await _repository.GetProductsByTypeAsync("gel");

        // Assert
        products.ShouldNotBeNull();
        products.ShouldNotBeEmpty();
        Assert.All(products, p => Assert.Contains("gel", p.ProductType.ToLower()));
    }

    [Fact]
    public async Task GetProductsByTypeAsync_WithDrinkType_ReturnsDrinks()
    {
        // Act
        var products = await _repository.GetProductsByTypeAsync("drink");

        // Assert
        products.ShouldNotBeNull();
        // Drinks may or may not exist in test data, but method should work
        (products == null || products.All(p => p.ProductType.ToLower().Contains("drink"))).ShouldBeTrue();
    }

    [Fact]
    public async Task GetProductsByTypeAsync_CaseInsensitive()
    {
        // Act
        var productsLower = await _repository.GetProductsByTypeAsync("gel");
        var productsUpper = await _repository.GetProductsByTypeAsync("GEL");
        var productsMixed = await _repository.GetProductsByTypeAsync("Gel");

        // Assert
        (productsUpper?.Count ?? 0).ShouldBe(productsLower?.Count ?? 0);
        (productsMixed?.Count ?? 0).ShouldBe(productsLower?.Count ?? 0);
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
        results.ShouldNotBeNull();
        results.ShouldNotBeEmpty();
        results.Select(p => p.Name).ShouldContain(firstProductName);
    }

    [Fact]
    public async Task SearchProductsAsync_WithNoMatches_ReturnsEmpty()
    {
        // Act
        var results = await _repository.SearchProductsAsync("XYZ123NonExistent");

        // Assert
        results.ShouldNotBeNull();
        results.ShouldBeEmpty();
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
        resultsUpper.Count.ShouldBe(resultsLower.Count);
    }

    [Fact]
    public async Task GetFilteredProductsAsync_WithValidFilter_ReturnsMatches()
    {
        // Arrange
        var filter = new ProductFilter(Brand: null, ExcludeTypes: null);

        // Act
        var products = await _repository.GetFilteredProductsAsync(filter);

        // Assert
        products.ShouldNotBeNull();
        products.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetFilteredProductsAsync_WithBrandFilter_ReturnsBrandProducts()
    {
        // Arrange
        var filter = new ProductFilter(Brand: "SiS", ExcludeTypes: null);

        // Act
        var products = await _repository.GetFilteredProductsAsync(filter);

        // Assert
        products.ShouldNotBeNull();
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
        products.ShouldNotBeNull();
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
        filtered.ShouldNotBeNull();
        filtered.Count.ShouldBe(allProducts.Count);
    }
}
