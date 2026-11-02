using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NexusGear.Controllers;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.ViewModels;
using Xunit;

namespace NexusGear.Tests;

public class CatalogTests
{
    private static AppDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new AppDbContext(options);
        SeedSampleData(context);
        return context;
    }

    private static void SeedSampleData(AppDbContext context)
    {
        var catKeyboards = new Category { CategoryId = 1, Name = "Mechanical Keyboards", Slug = "mechanical-keyboards" };
        var catMice = new Category { CategoryId = 2, Name = "Gaming Mice", Slug = "gaming-mice" };
        var catDeskmats = new Category { CategoryId = 3, Name = "Deskmats", Slug = "deskmats-mousepads" };

        context.Categories.AddRange(catKeyboards, catMice, catDeskmats);

        var p1 = new Product
        {
            ProductId = 1,
            Name = "NexusForge K-75 Horizon",
            Description = "75% custom mechanical keyboard aluminum case",
            Price = 189.99m,
            StockQuantity = 10,
            ImageUrl = "/images/products/product_1.svg",
            CategoryId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var p2 = new Product
        {
            ProductId = 2,
            Name = "NexusForge K-60 CyberKey",
            Description = "60% ultra-compact mechanical keyboard",
            Price = 119.99m,
            StockQuantity = 0, // Out of stock for testing
            ImageUrl = "/images/products/product_2.svg",
            CategoryId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        var p3 = new Product
        {
            ProductId = 3,
            Name = "NexusPulse X-42 Hyperlight",
            Description = "42g ultralight wireless gaming mouse 8000Hz",
            Price = 129.99m,
            StockQuantity = 8,
            ImageUrl = "/images/products/product_9.svg",
            CategoryId = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        };

        var p4 = new Product
        {
            ProductId = 4,
            Name = "NexusPulse G-49 Ergo Pro",
            Description = "49g ergonomic wireless gaming mouse PAW3395",
            Price = 139.99m,
            StockQuantity = 15,
            ImageUrl = "/images/products/product_10.svg",
            CategoryId = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var p5 = new Product
        {
            ProductId = 5,
            Name = "NexusSurface Stealth Deskmat",
            Description = "900x400mm extended stealth deskmat",
            Price = 39.99m,
            StockQuantity = 20,
            ImageUrl = "/images/products/product_17.svg",
            CategoryId = 3,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        context.Products.AddRange(p1, p2, p3, p4, p5);

        // Seed an order with p1 and p3 together (co-occurrence for B2)
        var user = new User { UserId = 1, Email = "test@nexusgear.ro", FullName = "Test Customer", PasswordHash = "hash" };
        context.Users.Add(user);

        var order = new Order
        {
            OrderId = 1,
            OrderNumber = "NX-TEST-001",
            UserId = 1,
            Status = "Paid",
            TotalAmount = 319.98m
        };
        context.Orders.Add(order);

        context.OrderItems.AddRange(
            new OrderItem { OrderItemId = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 189.99m },
            new OrderItem { OrderItemId = 2, OrderId = 1, ProductId = 3, Quantity = 1, UnitPrice = 129.99m }
        );

        // Seed a review for p1
        context.Reviews.Add(new Review
        {
            ReviewId = 1,
            ProductId = 1,
            UserId = 1,
            Rating = 5,
            Comment = "Outstanding keyboard build quality!",
            IsHidden = false,
            CreatedAt = DateTime.UtcNow
        });

        context.SaveChanges();
    }

    [Fact]
    public async Task Index_ReturnsAllProducts_WhenNoFiltersApplied()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Index_ReturnsAllProducts_WhenNoFiltersApplied));
        var loggerMock = new Mock<ILogger<CatalogController>>();
        var controller = new CatalogController(context, loggerMock.Object);

        // Act
        var result = await controller.Index(category: null, searchTerm: null, minPrice: null, maxPrice: null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CatalogIndexViewModel>(viewResult.Model);

        Assert.Equal(5, model.TotalItems);
        Assert.Equal(5, model.Products.Count);
        Assert.Equal(3, model.Categories.Count);
        Assert.False(model.HasActiveFilters);
    }

    [Fact]
    public async Task Index_FiltersByCategorySlug_Correctly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Index_FiltersByCategorySlug_Correctly));
        var loggerMock = new Mock<ILogger<CatalogController>>();
        var controller = new CatalogController(context, loggerMock.Object);

        // Act
        var result = await controller.Index(category: "gaming-mice", searchTerm: null, minPrice: null, maxPrice: null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CatalogIndexViewModel>(viewResult.Model);

        Assert.Equal(2, model.TotalItems);
        Assert.All(model.Products, p => Assert.Equal("Gaming Mice", p.CategoryName));
        Assert.Equal("gaming-mice", model.SelectedCategorySlug);
        Assert.True(model.HasActiveFilters);
    }

    [Fact]
    public async Task Index_CaseInsensitiveSearch_MatchesTitleOrCategory()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Index_CaseInsensitiveSearch_MatchesTitleOrCategory));
        var loggerMock = new Mock<ILogger<CatalogController>>();
        var controller = new CatalogController(context, loggerMock.Object);

        // Act - Search term matches product name
        var result = await controller.Index(category: null, searchTerm: "hyperlight", minPrice: null, maxPrice: null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CatalogIndexViewModel>(viewResult.Model);

        Assert.Single(model.Products);
        Assert.Contains("Hyperlight", model.Products[0].Name);
    }

    [Fact]
    public async Task Index_SearchTerm_DoesNotMatchLongDescriptionFalsePositives()
    {
        // Arrange
        // Product 3 has description "42g ultralight wireless gaming mouse 8000Hz"
        // But searching for "8000Hz" should not match because search is scoped to Title and Category (A2)
        using var context = CreateInMemoryDbContext(nameof(Index_SearchTerm_DoesNotMatchLongDescriptionFalsePositives));
        var loggerMock = new Mock<ILogger<CatalogController>>();
        var controller = new CatalogController(context, loggerMock.Object);

        // Act
        var result = await controller.Index(category: null, searchTerm: "8000Hz", minPrice: null, maxPrice: null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CatalogIndexViewModel>(viewResult.Model);

        Assert.Empty(model.Products);
        Assert.Equal(0, model.TotalItems);
    }

    [Fact]
    public async Task Index_ReturnsEmptyState_WhenNoProductsMatchSearch()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Index_ReturnsEmptyState_WhenNoProductsMatchSearch));
        var loggerMock = new Mock<ILogger<CatalogController>>();
        var controller = new CatalogController(context, loggerMock.Object);

        // Act
        var result = await controller.Index(category: null, searchTerm: "NonExistentKeyboardModel123", minPrice: null, maxPrice: null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CatalogIndexViewModel>(viewResult.Model);

        Assert.Empty(model.Products);
        Assert.Equal(0, model.TotalItems);
        Assert.Equal("NonExistentKeyboardModel123", model.SearchTerm);
        Assert.True(model.HasActiveFilters);
    }

    [Fact]
    public async Task Index_PriceRangeFilter_ReturnsProductsWithinRange()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Index_PriceRangeFilter_ReturnsProductsWithinRange));
        var loggerMock = new Mock<ILogger<CatalogController>>();
        var controller = new CatalogController(context, loggerMock.Object);

        // Act - Products between 100 and 135 RON: p2 (119.99), p3 (129.99)
        var result = await controller.Index(category: null, searchTerm: null, minPrice: 100m, maxPrice: 135m);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CatalogIndexViewModel>(viewResult.Model);

        Assert.Equal(2, model.Products.Count);
        Assert.All(model.Products, p => Assert.True(p.Price >= 100m && p.Price <= 135m));
    }

    [Fact]
    public async Task Index_InStockOnly_ExcludesOutOfStockItems()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Index_InStockOnly_ExcludesOutOfStockItems));
        var loggerMock = new Mock<ILogger<CatalogController>>();
        var controller = new CatalogController(context, loggerMock.Object);

        // Act
        var result = await controller.Index(category: null, searchTerm: null, minPrice: null, maxPrice: null, inStockOnly: true);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CatalogIndexViewModel>(viewResult.Model);

        Assert.Equal(4, model.Products.Count);
        Assert.DoesNotContain(model.Products, p => p.ProductId == 2); // p2 had StockQuantity = 0
    }

    [Fact]
    public async Task Index_SortOrder_PriceAscending_SortsCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Index_SortOrder_PriceAscending_SortsCorrectly));
        var loggerMock = new Mock<ILogger<CatalogController>>();
        var controller = new CatalogController(context, loggerMock.Object);

        // Act
        var result = await controller.Index(category: null, searchTerm: null, minPrice: null, maxPrice: null, sortOrder: "price-asc");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CatalogIndexViewModel>(viewResult.Model);

        Assert.True(model.Products[0].Price <= model.Products[1].Price);
        Assert.Equal(39.99m, model.Products[0].Price); // Deskmat cheapest
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Details_ReturnsNotFound_WhenProductDoesNotExist));
        var loggerMock = new Mock<ILogger<CatalogController>>();
        var controller = new CatalogController(context, loggerMock.Object);

        // Act
        var result = await controller.Details(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsProductWithRecommendations_AndReviews()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Details_ReturnsProductWithRecommendations_AndReviews));
        var loggerMock = new Mock<ILogger<CatalogController>>();
        var controller = new CatalogController(context, loggerMock.Object);

        // Act (Product 1 is K-75 Horizon, co-ordered with Product 3 Hyperlight Mouse in Order 1)
        var result = await controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ProductDetailsViewModel>(viewResult.Model);

        Assert.Equal(1, model.ProductId);
        Assert.Equal("NexusForge K-75 Horizon", model.Name);
        Assert.Equal(1, model.ReviewCount);
        Assert.Equal(5.0, model.AverageRating);

        // B2: Frequently Bought Together should include Product 3 (co-ordered)
        Assert.NotEmpty(model.FrequentlyBoughtTogether);
        Assert.Contains(model.FrequentlyBoughtTogether, p => p.ProductId == 3);

        // B2: Similar Products should include Product 2 (same Category 1)
        Assert.NotEmpty(model.SimilarProducts);
        Assert.All(model.SimilarProducts, p => Assert.Equal(1, p.CategoryId));
    }

    [Fact]
    public void CatalogIndexViewModel_GetRouteData_PreservesAllActiveFilters()
    {
        // Arrange
        var vm = new CatalogIndexViewModel
        {
            SelectedCategorySlug = "gaming-mice",
            SearchTerm = "light",
            MinPrice = 50m,
            MaxPrice = 200m,
            InStockOnly = true,
            SortOrder = "price-asc",
            PageSize = 24,
            CurrentPage = 2
        };

        // Act
        var route = vm.GetRouteData(targetPage: 3);

        // Assert
        Assert.Equal("gaming-mice", route["category"]);
        Assert.Equal("light", route["searchTerm"]);
        Assert.Equal("50", route["minPrice"]);
        Assert.Equal("200", route["maxPrice"]);
        Assert.Equal("true", route["inStockOnly"]);
        Assert.Equal("price-asc", route["sortOrder"]);
        Assert.Equal("24", route["pageSize"]);
        Assert.Equal("3", route["page"]);
    }
}

