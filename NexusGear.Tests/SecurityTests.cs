using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusGear.Controllers;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories;
using NexusGear.Services;
using NexusGear.ViewModels;
using Xunit;

namespace NexusGear.Tests;

/// <summary>
/// Comprehensive verification for Requirement A10: Basic Security (Role Authorization, CSRF Tokens,
/// EF Core SQL Injection Immunity, Password Hashing, Server-Side Validation) and B4 Sales Analytics.
/// </summary>
public class SecurityTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _contextOptions;

    public SecurityTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _contextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_contextOptions);
        context.Database.EnsureCreated();
        SeedDatabase(context);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private static void SeedDatabase(AppDbContext context)
    {
        var category = new Category
        {
            CategoryId = 1,
            Name = "Mechanical Keyboards",
            Slug = "keyboards",
            Description = "Custom mechanical keyboards"
        };
        context.Categories.Add(category);

        var product1 = new Product
        {
            ProductId = 1,
            CategoryId = 1,
            Name = "NexusStrike Pro K-98",
            Description = "Custom Wireless Keyboard",
            Price = 450.00m,
            StockQuantity = 15,
            ImageUrl = "/images/products/Product_1.png",
            RowVersion = Guid.NewGuid()
        };

        var product2 = new Product
        {
            ProductId = 2,
            CategoryId = 1,
            Name = "NexusDesk CyberMat XL",
            Description = "Speed Deskmat",
            Price = 120.00m,
            StockQuantity = 4,
            ImageUrl = "/images/products/Product_17.png",
            RowVersion = Guid.NewGuid()
        };

        context.Products.AddRange(product1, product2);

        var customer = new User
        {
            UserId = 1,
            Email = "customer@nexusgear.ro",
            FullName = "Security Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePassword123!"),
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(customer);

        var address = new Address
        {
            AddressId = 1,
            Street = "Strada Tehnologiei 10",
            City = "Craiova",
            County = "Dolj",
            PostalCode = "200100",
            Phone = "+40720000001"
        };
        context.Addresses.Add(address);

        var order1 = new Order
        {
            OrderId = 1,
            OrderNumber = "NXG-SEC-001",
            UserId = 1,
            AddressId = 1,
            ShippingMethod = "Standard Courier",
            ShippingCost = 15.00m,
            TotalAmount = 570.00m,
            Status = "Paid",
            OrderDate = DateTime.UtcNow.AddDays(-2)
        };

        var order2 = new Order
        {
            OrderId = 2,
            OrderNumber = "NXG-SEC-002",
            UserId = 1,
            AddressId = 1,
            ShippingMethod = "Express Courier",
            ShippingCost = 25.00m,
            TotalAmount = 240.00m,
            Status = "Delivered",
            OrderDate = DateTime.UtcNow.AddDays(-12)
        };

        context.Orders.AddRange(order1, order2);

        var item1 = new OrderItem
        {
            OrderItemId = 1,
            OrderId = 1,
            ProductId = 1,
            Quantity = 1,
            UnitPrice = 450.00m
        };

        var item2 = new OrderItem
        {
            OrderItemId = 2,
            OrderId = 1,
            ProductId = 2,
            Quantity = 1,
            UnitPrice = 120.00m
        };

        var item3 = new OrderItem
        {
            OrderItemId = 3,
            OrderId = 2,
            ProductId = 2,
            Quantity = 2,
            UnitPrice = 120.00m
        };

        context.OrderItems.AddRange(item1, item2, item3);
        context.SaveChanges();
    }

    // =========================================================================
    // A10: Server-Side Role Authorization
    // =========================================================================

    [Fact]
    public void AdminController_DecoratedWithAuthorize_RestrictedStrictlyToAdminRole()
    {
        // Act (A10)
        var authorizeAttribute = typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("Admin", authorizeAttribute!.Roles);
    }

    [Fact]
    public void SensitiveCustomerControllers_DecoratedWithAuthorize()
    {
        // Act (A10)
        var ordersAuth = typeof(OrdersController).GetCustomAttribute<AuthorizeAttribute>();
        var checkoutAuth = typeof(CheckoutController).GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        Assert.NotNull(ordersAuth);
        Assert.NotNull(checkoutAuth);
    }

    [Fact]
    public void AccountController_SensitiveActions_RequireAuthentication()
    {
        // Act (A10)
        var profileMethod = typeof(AccountController).GetMethod(nameof(AccountController.Profile));
        var addAddressMethod = typeof(AccountController).GetMethod(nameof(AccountController.AddAddress));
        var deleteAddressMethod = typeof(AccountController).GetMethod(nameof(AccountController.DeleteAddress));

        // Assert
        Assert.NotNull(profileMethod?.GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(addAddressMethod?.GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(deleteAddressMethod?.GetCustomAttribute<AuthorizeAttribute>());
    }

    // =========================================================================
    // A10: CSRF Protection Verification
    // =========================================================================

    [Theory]
    [InlineData(typeof(AdminController))]
    [InlineData(typeof(AccountController))]
    [InlineData(typeof(CartController))]
    [InlineData(typeof(CheckoutController))]
    [InlineData(typeof(WishlistController))]
    [InlineData(typeof(ReviewsController))]
    public void Controllers_AllStateMutatingPostActions_MustEnforceAntiForgeryToken(Type controllerType)
    {
        // Act & Assert (A10)
        var postMethods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpPostAttribute>().Any())
            .ToList();

        Assert.NotEmpty(postMethods);

        foreach (var method in postMethods)
        {
            var hasAntiForgery = method.GetCustomAttributes<ValidateAntiForgeryTokenAttribute>().Any() ||
                                 controllerType.GetCustomAttributes<ValidateAntiForgeryTokenAttribute>().Any();

            Assert.True(hasAntiForgery,
                $"POST action '{method.Name}' on '{controllerType.Name}' is missing [ValidateAntiForgeryToken] attribute!");
        }
    }

    // =========================================================================
    // A10: Password Hashing Security (BCrypt)
    // =========================================================================

    [Fact]
    public void PasswordHashing_UsesSecureBcryptSaltAndValidatesCorrectly()
    {
        // Arrange
        const string rawPassword = "P@ssword_Nexus_2026";

        // Act
        var hash1 = BCrypt.Net.BCrypt.HashPassword(rawPassword);
        var hash2 = BCrypt.Net.BCrypt.HashPassword(rawPassword);

        // Assert (A10)
        Assert.StartsWith("$2", hash1);
        Assert.NotEqual(hash1, hash2); // Unique cryptographic salt for every hash
        Assert.True(BCrypt.Net.BCrypt.Verify(rawPassword, hash1));
        Assert.True(BCrypt.Net.BCrypt.Verify(rawPassword, hash2));
        Assert.False(BCrypt.Net.BCrypt.Verify("WrongPassword123!", hash1));
    }

    // =========================================================================
    // A10: ORM / Parameterized Queries (SQL Injection Immunity)
    // =========================================================================

    [Theory]
    [InlineData("'; DROP TABLE Products; --")]
    [InlineData("' OR '1'='1")]
    [InlineData("admin' --")]
    [InlineData("1; SELECT * FROM Users")]
    public async Task EfCore_ProtectsAgainstSqlInjection_WhenSearchingCatalog(string maliciousInput)
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);

        // Act - Simulating Catalog search via LINQ/EF Core parameterization
        var results = await context.Products
            .Where(p => EF.Functions.Like(p.Name, $"%{maliciousInput}%") ||
                        EF.Functions.Like(p.Description, $"%{maliciousInput}%"))
            .ToListAsync();

        // Assert (A10)
        Assert.Empty(results); // No product matches malicious SQL syntax

        // Verify Products table was NOT dropped or tampered with
        var productsCount = await context.Products.CountAsync();
        Assert.Equal(2, productsCount);
    }

    // =========================================================================
    // A10: Server-Side Validation Verification
    // =========================================================================

    [Fact]
    public void ServerSideValidation_RejectsInvalidProductForm()
    {
        // Arrange (A10) - Missing required name, negative price and stock
        var model = new AdminProductFormViewModel
        {
            Name = "", // Required
            Description = "", // Required
            Price = -50.00m, // Range(0.01, 100000.00)
            StockQuantity = -10, // Range(0, 100000)
            CategoryId = 0
        };

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model);

        // Act
        var isValid = Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(AdminProductFormViewModel.Name)));
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(AdminProductFormViewModel.Price)));
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(AdminProductFormViewModel.StockQuantity)));
    }

    // =========================================================================
    // B4: Sales Analytics Dashboard & Metrics Calculation
    // =========================================================================

    [Fact]
    public async Task AdminService_DashboardMetrics_CalculatesIntervalAovAndChartPoints_For7Days()
    {
        // Arrange (B4)
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Act
        var metrics = await adminService.GetDashboardMetricsAsync("7days");

        // Assert
        Assert.Equal("7days", metrics.SelectedInterval);
        Assert.Equal(7, metrics.DailySalesChart.Count); // 7 daily chronological points
        Assert.Equal(1, metrics.IntervalOrdersCount); // Only Order 1 was placed 2 days ago
        Assert.Equal(570.00m, metrics.IntervalRevenue);
        Assert.Equal(570.00m, metrics.AverageOrderValue); // AOV = 570 / 1
        Assert.NotEmpty(metrics.TopSellingProducts);
    }

    [Fact]
    public async Task AdminService_DashboardMetrics_CalculatesIntervalAovAndTopSelling_For30Days()
    {
        // Arrange (B4)
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Act
        var metrics = await adminService.GetDashboardMetricsAsync("30days");

        // Assert
        Assert.Equal("30days", metrics.SelectedInterval);
        Assert.Equal(30, metrics.DailySalesChart.Count); // 30 daily chronological points
        Assert.Equal(2, metrics.IntervalOrdersCount); // Order 1 (-2d) and Order 2 (-12d)
        Assert.Equal(570.00m + 240.00m, metrics.IntervalRevenue); // 810.00 RON
        Assert.Equal(405.00m, metrics.AverageOrderValue); // AOV = 810.00 / 2 = 405.00 RON

        // Top selling products check
        Assert.NotEmpty(metrics.TopSellingProducts);
        // Product 2 was sold 1 unit in order 1 and 2 units in order 2 = 3 units sold total
        var topProduct = metrics.TopSellingProducts.First();
        Assert.Equal(2, topProduct.ProductId);
        Assert.Equal(3, topProduct.UnitsSold);
    }
}
