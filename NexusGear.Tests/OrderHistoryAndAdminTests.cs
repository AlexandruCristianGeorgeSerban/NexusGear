using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories;
using NexusGear.Services;
using NexusGear.ViewModels;
using Xunit;

namespace NexusGear.Tests;

public class OrderHistoryAndAdminTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _contextOptions;

    public OrderHistoryAndAdminTests()
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
        var admin = new User
        {
            UserId = 1,
            Email = "admin@nexusgear.ro",
            FullName = "Admin User",
            PasswordHash = "hash1",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        var customer1 = new User
        {
            UserId = 2,
            Email = "customer1@nexusgear.ro",
            FullName = "Customer One",
            PasswordHash = "hash2",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };

        var customer2 = new User
        {
            UserId = 3,
            Email = "customer2@nexusgear.ro",
            FullName = "Customer Two",
            PasswordHash = "hash3",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.AddRange(admin, customer1, customer2);

        var address1 = new Address
        {
            AddressId = 1,
            Street = "Strada Tehnologiei 10",
            City = "Craiova",
            County = "Dolj",
            PostalCode = "200100",
            Phone = "+40720000001"
        };
        context.Addresses.Add(address1);

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
            StockQuantity = 10,
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
            StockQuantity = 3, // Low stock <= 5
            ImageUrl = "/images/products/Product_17.png",
            RowVersion = Guid.NewGuid()
        };
        context.Products.AddRange(product1, product2);

        // Order 1 for Customer 1
        var order1 = new Order
        {
            OrderId = 1,
            OrderNumber = "NXG-2026-9001",
            UserId = 2,
            AddressId = 1,
            ShippingMethod = "Standard Courier",
            ShippingCost = 15.00m,
            TotalAmount = 465.00m,
            Status = "Paid",
            OrderDate = DateTime.UtcNow.AddDays(-2)
        };

        var item1 = new OrderItem
        {
            OrderItemId = 1,
            OrderId = 1,
            ProductId = 1,
            Quantity = 1,
            UnitPrice = 450.00m
        };
        context.Orders.Add(order1);
        context.OrderItems.Add(item1);

        // Order 2 for Customer 2
        var order2 = new Order
        {
            OrderId = 2,
            OrderNumber = "NXG-2026-9002",
            UserId = 3,
            AddressId = 1,
            ShippingMethod = "Express Courier",
            ShippingCost = 25.00m,
            TotalAmount = 265.00m,
            Status = "Delivered",
            OrderDate = DateTime.UtcNow.AddDays(-5)
        };

        var item2 = new OrderItem
        {
            OrderItemId = 2,
            OrderId = 2,
            ProductId = 2,
            Quantity = 2,
            UnitPrice = 120.00m
        };
        context.Orders.Add(order2);
        context.OrderItems.Add(item2);

        context.SaveChanges();
    }

    [Fact]
    public async Task GetUserOrderHistory_ReturnsOnlyOrdersForRequestedUser_Isolated()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var orderService = new OrderService(repo, context);

        // Act
        var result = await orderService.GetUserOrderHistoryAsync(userId: 2);

        // Assert (A4, A8 Security: Customer 1 sees only Order 1, NOT Order 2)
        Assert.Single(result.Orders);
        Assert.Equal("NXG-2026-9001", result.Orders[0].OrderNumber);
        Assert.Equal(465.00m, result.TotalSpent);
    }

    [Fact]
    public async Task GetUserOrderDetails_ReturnsNull_WhenOrderBelongsToDifferentUser()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var orderService = new OrderService(repo, context);

        // Act - Customer 1 attempts to access Order 2 (which belongs to Customer 2)
        var result = await orderService.GetUserOrderDetailsAsync(orderId: 2, userId: 2);

        // Assert: Security guard prevents unauthorized access
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserOrderDetails_ReturnsCompleteOrderBreakdown_ForOwner()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var orderService = new OrderService(repo, context);

        // Act
        var result = await orderService.GetUserOrderDetailsAsync(orderId: 1, userId: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NXG-2026-9001", result.OrderNumber);
        Assert.Equal("Paid", result.Status);
        Assert.True(result.IsStepCompleted("Paid"));
        Assert.False(result.IsStepCompleted("Shipped"));
        Assert.Single(result.Items);
        Assert.Equal("NexusStrike Pro K-98", result.Items[0].ProductName);
        Assert.Equal(450.00m, result.Subtotal);
        Assert.Equal(465.00m, result.TotalAmount);
    }

    [Fact]
    public async Task AdminService_GetDashboardMetrics_CalculatesRevenueAndLowStockCounts()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Act
        var metrics = await adminService.GetDashboardMetricsAsync();

        // Assert (A9)
        Assert.Equal(2, metrics.TotalOrdersCount);
        Assert.Equal(465.00m + 265.00m, metrics.TotalRevenue);
        Assert.Equal(2, metrics.TotalProductsCount);
        Assert.Equal(1, metrics.LowStockProductsCount); // Product 2 has stock = 3 (<= 5)
        Assert.Single(metrics.LowStockProducts);
        Assert.Equal("NexusDesk CyberMat XL", metrics.LowStockProducts[0].Name);
    }

    [Fact]
    public async Task AdminService_UpdateOrderStatus_TransitionsFromPaidToShippedAndDelivered()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Act 1: Transition Paid -> Shipped
        var result1 = await adminService.UpdateOrderStatusAsync(1, "Shipped");
        Assert.True(result1.Success);

        var orderAfterShipped = await context.Orders.FindAsync(1);
        Assert.Equal("Shipped", orderAfterShipped!.Status);

        // Act 2: Transition Shipped -> Delivered
        var result2 = await adminService.UpdateOrderStatusAsync(1, "Delivered");
        Assert.True(result2.Success);

        var orderAfterDelivered = await context.Orders.FindAsync(1);
        Assert.Equal("Delivered", orderAfterDelivered!.Status);
    }

    [Fact]
    public async Task AdminService_UpdateOrderStatus_Cancelled_RestocksInventoryAtomically()
    {
        // Arrange: Order 1 has 1 unit of Product 1 (current stock: 10)
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        var productBefore = await context.Products.FindAsync(1);
        var initialRowVersion = productBefore!.RowVersion;
        Assert.Equal(10, productBefore.StockQuantity);

        // Act: Admin cancels Order 1 -> must restock 1 unit
        var result = await adminService.UpdateOrderStatusAsync(1, "Cancelled");

        // Assert (A7 & A9 requirement)
        Assert.True(result.Success);

        var updatedOrder = await context.Orders.FindAsync(1);
        Assert.Equal("Cancelled", updatedOrder!.Status);

        var productAfter = await context.Products.FindAsync(1);
        Assert.NotNull(productAfter);
        Assert.Equal(11, productAfter.StockQuantity); // 10 + 1 = 11 (Restocked!)
        Assert.NotEqual(initialRowVersion, productAfter.RowVersion); // Concurrency token updated
    }

    [Fact]
    public async Task AdminService_UpdateOrderStatus_PreventsCancellation_WhenOrderIsShippedOrDelivered()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Order 2 is already "Delivered" in seed data
        var resultDelivered = await adminService.UpdateOrderStatusAsync(2, "Cancelled");
        Assert.False(resultDelivered.Success);
        Assert.Contains("cannot be transitioned to 'Cancelled'", resultDelivered.Message);

        // Transition Order 1 to "Shipped"
        await adminService.UpdateOrderStatusAsync(1, "Shipped");

        // Attempt to cancel "Shipped" order -> must be rejected
        var resultShipped = await adminService.UpdateOrderStatusAsync(1, "Cancelled");
        Assert.False(resultShipped.Success);
        Assert.Contains("cannot be transitioned to 'Cancelled'", resultShipped.Message);
    }

    [Fact]
    public async Task AdminService_CreateProduct_PersistsNewProductInCatalog()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        var form = new AdminProductFormViewModel
        {
            Name = "NexusMouse Wireless Ultralight",
            Description = "Ultralight gaming mouse 45g",
            Price = 299.99m,
            StockQuantity = 20,
            CategoryId = 1
        };

        // Act
        var result = await adminService.CreateProductAsync(form, uploadsWebRootPath: "");

        // Assert
        Assert.True(result.Success);

        var createdProduct = await context.Products.FirstOrDefaultAsync(p => p.Name == "NexusMouse Wireless Ultralight");
        Assert.NotNull(createdProduct);
        Assert.Equal(299.99m, createdProduct.Price);
        Assert.Equal(20, createdProduct.StockQuantity);
        Assert.Equal(1, createdProduct.CategoryId);
    }

    [Fact]
    public async Task AdminService_EditProduct_UpdatesProductFieldsAndConcurrencyToken()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        var productBefore = await context.Products.AsNoTracking().FirstAsync(p => p.ProductId == 1);
        var initialRowVersion = productBefore.RowVersion;

        var form = new AdminProductFormViewModel
        {
            ProductId = 1,
            Name = "NexusStrike Pro K-98 V2 Custom",
            Description = "Updated mechanical keyboard specs",
            Price = 499.99m,
            StockQuantity = 15,
            CategoryId = 1
        };

        // Act
        var result = await adminService.EditProductAsync(form, uploadsWebRootPath: "");

        // Assert
        Assert.True(result.Success);

        var updated = await context.Products.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal("NexusStrike Pro K-98 V2 Custom", updated.Name);
        Assert.Equal(499.99m, updated.Price);
        Assert.Equal(15, updated.StockQuantity);
        Assert.NotEqual(initialRowVersion, updated.RowVersion);
    }

    [Fact]
    public async Task AdminService_DeleteProduct_ArchivesByZeroingStock_WhenReferencedInOrders()
    {
        // Arrange: Product 1 is part of Order 1
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Act: Attempt to delete Product 1
        var result = await adminService.DeleteProductAsync(1);

        // Assert: Product is archived (stock set to 0) rather than throwing FK error
        Assert.True(result.Success);
        Assert.Contains("archived", result.Message);

        var product = await context.Products.FindAsync(1);
        Assert.NotNull(product);
        Assert.Equal(0, product.StockQuantity);
    }

    [Fact]
    public async Task AdminService_GetAdminCategories_ReturnsAllCategoriesWithAccurateProductCounts()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Act
        var result = await adminService.GetAdminCategoriesAsync();

        // Assert (A9)
        Assert.NotNull(result);
        Assert.True(result.Categories.Count >= 1);
        var cat1 = result.Categories.FirstOrDefault(c => c.CategoryId == 1);
        Assert.NotNull(cat1);
        Assert.Equal("Mechanical Keyboards", cat1.Name);
        Assert.Equal("keyboards", cat1.Slug);
        Assert.Equal(2, cat1.ProductCount); // Product 1 and 2 in seed data
    }

    [Fact]
    public async Task AdminService_CreateCategory_GeneratesSlugAndPersistsCategory()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        var form = new AdminCategoryFormViewModel
        {
            Name = "Ergonomic Desk Accessories",
            Description = "Wrist rests, monitor arms, and desk organizers"
        };

        // Act
        var result = await adminService.CreateCategoryAsync(form);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("created successfully", result.Message);

        var savedCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Ergonomic Desk Accessories");
        Assert.NotNull(savedCat);
        Assert.Equal("ergonomic-desk-accessories", savedCat.Slug);
        Assert.Equal("Wrist rests, monitor arms, and desk organizers", savedCat.Description);
    }

    [Fact]
    public async Task AdminService_CreateCategory_RejectsDuplicateSlug()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        var duplicateForm = new AdminCategoryFormViewModel
        {
            Name = "Mechanical Keyboards Duplicate",
            Slug = "keyboards" // Already exists in seed data
        };

        // Act
        var result = await adminService.CreateCategoryAsync(duplicateForm);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
    }

    [Fact]
    public async Task AdminService_EditCategory_UpdatesNameDescriptionAndSlug()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        var editForm = new AdminCategoryFormViewModel
        {
            CategoryId = 1,
            Name = "Custom Enthusiast Keyboards",
            Slug = "custom-keyboards",
            Description = "Hot-swap, wireless, and gasket-mounted mechanical keyboards"
        };

        // Act
        var result = await adminService.EditCategoryAsync(editForm);

        // Assert
        Assert.True(result.Success);

        var updated = await context.Categories.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal("Custom Enthusiast Keyboards", updated.Name);
        Assert.Equal("custom-keyboards", updated.Slug);
        Assert.Equal("Hot-swap, wireless, and gasket-mounted mechanical keyboards", updated.Description);
    }

    [Fact]
    public async Task AdminService_DeleteCategory_BlockedWhenCategoryContainsProducts()
    {
        // Arrange: Category 1 contains Product 1 and Product 2
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Act
        var result = await adminService.DeleteCategoryAsync(1);

        // Assert: Referential integrity protection
        Assert.False(result.Success);
        Assert.Contains("active product(s)", result.Message);

        var category = await context.Categories.FindAsync(1);
        Assert.NotNull(category);
    }

    [Fact]
    public async Task AdminService_DeleteCategory_SucceedsWhenCategoryHasNoProducts()
    {
        // Arrange: Create a temporary empty category
        using var context = new AppDbContext(_contextOptions);
        var emptyCat = new Category
        {
            CategoryId = 99,
            Name = "Temporary Hardware",
            Slug = "temporary-hardware"
        };
        context.Categories.Add(emptyCat);
        await context.SaveChangesAsync();

        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Act
        var result = await adminService.DeleteCategoryAsync(99);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("deleted", result.Message);

        var deleted = await context.Categories.FindAsync(99);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task AdminService_GetAdminReviews_ReturnsAllReviewsAndFiltersByVisibility()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        context.Reviews.AddRange(
            new Review { ReviewId = 1, UserId = 2, ProductId = 1, Rating = 5, Comment = "First visible review", IsHidden = false, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Review { ReviewId = 2, UserId = 3, ProductId = 1, Rating = 1, Comment = "Inappropriate spam comment", IsHidden = true, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        );
        await context.SaveChangesAsync();

        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Act 1: Get All
        var allResult = await adminService.GetAdminReviewsAsync("All");
        Assert.Equal(2, allResult.TotalReviewsCount);
        Assert.Equal(1, allResult.VisibleReviewsCount);
        Assert.Equal(1, allResult.HiddenReviewsCount);

        // Act 2: Filter Hidden Only
        var hiddenResult = await adminService.GetAdminReviewsAsync("Hidden");
        Assert.Single(hiddenResult.Reviews);
        Assert.Equal(2, hiddenResult.Reviews[0].ReviewId);
        Assert.True(hiddenResult.Reviews[0].IsHidden);

        // Act 3: Filter Visible Only
        var visibleResult = await adminService.GetAdminReviewsAsync("Visible");
        Assert.Single(visibleResult.Reviews);
        Assert.Equal(1, visibleResult.Reviews[0].ReviewId);
        Assert.False(visibleResult.Reviews[0].IsHidden);
    }

    [Fact]
    public async Task AdminService_ToggleReviewVisibility_HidesAndUnhidesReview()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        context.Reviews.Add(new Review
        {
            ReviewId = 10,
            UserId = 2,
            ProductId = 1,
            Rating = 4,
            Comment = "Solid typing experience.",
            IsHidden = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Act 1: Toggle from visible -> hidden (B3 Admin Moderation)
        var result1 = await adminService.ToggleReviewVisibilityAsync(10);
        Assert.True(result1.Success);
        Assert.True(result1.IsHidden);

        var reviewAfterFirstToggle = await context.Reviews.FindAsync(10);
        Assert.NotNull(reviewAfterFirstToggle);
        Assert.True(reviewAfterFirstToggle.IsHidden);

        // Act 2: Toggle from hidden -> visible
        var result2 = await adminService.ToggleReviewVisibilityAsync(10);
        Assert.True(result2.Success);
        Assert.False(result2.IsHidden);

        var reviewAfterSecondToggle = await context.Reviews.FindAsync(10);
        Assert.NotNull(reviewAfterSecondToggle);
        Assert.False(reviewAfterSecondToggle.IsHidden);
    }

    [Fact]
    public async Task AdminService_ToggleReviewVisibility_NonExistentReview_ReturnsFailure()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var repo = new RepositoryWrapper(context);
        var adminService = new AdminService(context, repo, NullLogger<AdminService>.Instance);

        // Act
        var result = await adminService.ToggleReviewVisibilityAsync(99999);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }
}
