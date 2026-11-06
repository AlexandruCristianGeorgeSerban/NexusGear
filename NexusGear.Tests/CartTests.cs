using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories;
using NexusGear.Services;
using NexusGear.Services.Interfaces;
using NexusGear.ViewModels;
using Xunit;

namespace NexusGear.Tests;

public class CartTests
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
        var user = new User
        {
            UserId = 1,
            Email = "customer@nexusgear.ro",
            FullName = "Alexandru Customer",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
            Role = "Customer"
        };

        var category = new Category
        {
            CategoryId = 1,
            Name = "Mechanical Keyboards",
            Slug = "mechanical-keyboards",
            Description = "Custom mechanical gaming keyboards"
        };

        var product1 = new Product
        {
            ProductId = 1,
            Name = "NexusApex Pro Carbon",
            Description = "Hall effect magnetic gaming keyboard",
            Price = 199.99m,
            StockQuantity = 10,
            CategoryId = 1,
            ImageUrl = "/images/products/Product_1.png"
        };

        var product2 = new Product
        {
            ProductId = 2,
            Name = "NexusGlide Deskmat XL",
            Description = "Micro-weave deskmat",
            Price = 49.99m,
            StockQuantity = 5,
            CategoryId = 1,
            ImageUrl = "/images/products/Product_2.png"
        };

        var productOutOfStock = new Product
        {
            ProductId = 3,
            Name = "NexusKey Limited Artisan",
            Description = "Limited edition keycap set",
            Price = 79.99m,
            StockQuantity = 0,
            CategoryId = 1,
            ImageUrl = "/images/products/Product_3.png"
        };

        context.Users.Add(user);
        context.Categories.Add(category);
        context.Products.AddRange(product1, product2, productOutOfStock);
        context.SaveChanges();
    }

    private static (CartService service, AppDbContext context, DefaultHttpContext httpContext) CreateTestSetup(
        string dbName, int? userId = 1)
    {
        var context = CreateInMemoryDbContext(dbName);
        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSession();

        if (userId.HasValue)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new(ClaimTypes.Name, "Alexandru Customer"),
                new(ClaimTypes.Email, "customer@nexusgear.ro")
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CartService(new RepositoryWrapper(context), accessor);

        return (service, context, httpContext);
    }

    // =========================================================================
    // 1. Add to Cart Tests
    // =========================================================================

    [Fact]
    public async Task AddToCart_AuthenticatedUser_AddsNewCartItemToDatabase()
    {
        var (service, context, _) = CreateTestSetup(nameof(AddToCart_AuthenticatedUser_AddsNewCartItemToDatabase));

        var result = await service.AddToCartAsync(productId: 1, quantity: 2);

        Assert.True(result.Success);
        Assert.Equal(2, result.TotalCartCount);
        Assert.Equal(399.98m, result.Subtotal);

        var dbItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == 1 && ci.ProductId == 1);
        Assert.NotNull(dbItem);
        Assert.Equal(2, dbItem.Quantity);
    }

    [Fact]
    public async Task AddToCart_ExistingItemInCart_IncrementsQuantity()
    {
        var (service, context, _) = CreateTestSetup(nameof(AddToCart_ExistingItemInCart_IncrementsQuantity));

        await service.AddToCartAsync(productId: 1, quantity: 2);
        var result = await service.AddToCartAsync(productId: 1, quantity: 3);

        Assert.True(result.Success);
        Assert.Equal(5, result.TotalCartCount);

        var dbItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == 1 && ci.ProductId == 1);
        Assert.NotNull(dbItem);
        Assert.Equal(5, dbItem.Quantity);
    }

    [Fact]
    public async Task AddToCart_ExceedingAvailableStock_CapsToMaxStock()
    {
        var (service, context, _) = CreateTestSetup(nameof(AddToCart_ExceedingAvailableStock_CapsToMaxStock));

        // Product 2 has StockQuantity = 5. User attempts to add 10 items.
        var result = await service.AddToCartAsync(productId: 2, quantity: 10);

        Assert.True(result.Success);
        Assert.Equal(5, result.TotalCartCount);
        Assert.Contains("maximum available stock reached", result.Message, StringComparison.OrdinalIgnoreCase);

        var dbItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == 1 && ci.ProductId == 2);
        Assert.NotNull(dbItem);
        Assert.Equal(5, dbItem.Quantity);
    }

    [Fact]
    public async Task AddToCart_AlreadyAtMaxStock_ReturnsFailureMessage()
    {
        var (service, _, _) = CreateTestSetup(nameof(AddToCart_AlreadyAtMaxStock_ReturnsFailureMessage));

        // Product 2 has StockQuantity = 5
        await service.AddToCartAsync(productId: 2, quantity: 5);

        // Attempting to add even 1 more should fail gracefully
        var result = await service.AddToCartAsync(productId: 2, quantity: 1);

        Assert.False(result.Success);
        Assert.Contains("maximum available stock", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddToCart_OutOfStockProduct_FailsWithErrorMessage()
    {
        var (service, context, _) = CreateTestSetup(nameof(AddToCart_OutOfStockProduct_FailsWithErrorMessage));

        // Product 3 has StockQuantity = 0
        var result = await service.AddToCartAsync(productId: 3, quantity: 1);

        Assert.False(result.Success);
        Assert.Contains("out of stock", result.Message, StringComparison.OrdinalIgnoreCase);

        var dbItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.ProductId == 3);
        Assert.Null(dbItem);
    }

    [Fact]
    public async Task AddToCart_NonExistentProduct_ReturnsFailure()
    {
        var (service, _, _) = CreateTestSetup(nameof(AddToCart_NonExistentProduct_ReturnsFailure));

        var result = await service.AddToCartAsync(productId: 9999, quantity: 1);

        Assert.False(result.Success);
        Assert.Contains("could not be found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 2. Quantity Update & Remove Tests
    // =========================================================================

    [Fact]
    public async Task UpdateQuantity_ValidQuantity_UpdatesCartItemAndTotals()
    {
        var (service, context, _) = CreateTestSetup(nameof(UpdateQuantity_ValidQuantity_UpdatesCartItemAndTotals));

        await service.AddToCartAsync(productId: 1, quantity: 2);
        var result = await service.UpdateQuantityAsync(productId: 1, quantity: 4);

        Assert.True(result.Success);
        Assert.Equal(4, result.TotalCartCount);
        Assert.Equal(799.96m, result.Subtotal);

        var dbItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == 1 && ci.ProductId == 1);
        Assert.NotNull(dbItem);
        Assert.Equal(4, dbItem.Quantity);
    }

    [Fact]
    public async Task UpdateQuantity_ZeroOrNegative_RemovesItemFromCart()
    {
        var (service, context, _) = CreateTestSetup(nameof(UpdateQuantity_ZeroOrNegative_RemovesItemFromCart));

        await service.AddToCartAsync(productId: 1, quantity: 2);
        var result = await service.UpdateQuantityAsync(productId: 1, quantity: 0);

        Assert.True(result.Success);
        Assert.Equal(0, result.TotalCartCount);
        Assert.True(result.IsEmpty);

        var dbItem = await context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == 1 && ci.ProductId == 1);
        Assert.Null(dbItem);
    }

    [Fact]
    public async Task RemoveFromCart_ValidProduct_RemovesItemFromCart()
    {
        var (service, context, _) = CreateTestSetup(nameof(RemoveFromCart_ValidProduct_RemovesItemFromCart));

        await service.AddToCartAsync(productId: 1, quantity: 2);
        await service.AddToCartAsync(productId: 2, quantity: 1);

        var result = await service.RemoveFromCartAsync(productId: 1);

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalCartCount);
        Assert.Equal(49.99m, result.Subtotal);

        var dbItem1 = await context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == 1 && ci.ProductId == 1);
        var dbItem2 = await context.CartItems.FirstOrDefaultAsync(ci => ci.UserId == 1 && ci.ProductId == 2);
        Assert.Null(dbItem1);
        Assert.NotNull(dbItem2);
    }

    [Fact]
    public async Task ClearCart_RemovesAllItemsForCurrentUser()
    {
        var (service, context, _) = CreateTestSetup(nameof(ClearCart_RemovesAllItemsForCurrentUser));

        await service.AddToCartAsync(productId: 1, quantity: 1);
        await service.AddToCartAsync(productId: 2, quantity: 2);

        await service.ClearCartAsync();

        var cart = await service.GetCartAsync();
        Assert.True(cart.IsEmpty);
        Assert.Equal(0, cart.TotalItemsCount);
        Assert.Equal(0.00m, cart.Subtotal);
        Assert.Equal(0.00m, cart.GrandTotal);

        var dbItems = await context.CartItems.Where(ci => ci.UserId == 1).ToListAsync();
        Assert.Empty(dbItems);
    }

    // =========================================================================
    // 3. Totals & Shipping Calculation Tests
    // =========================================================================

    [Fact]
    public async Task CartTotals_SubtotalBelowThreshold_IncludesShippingFee()
    {
        var (service, _, _) = CreateTestSetup(nameof(CartTotals_SubtotalBelowThreshold_IncludesShippingFee));

        // Product 2 is 49.99 RON (below 300 RON threshold)
        await service.AddToCartAsync(productId: 2, quantity: 1);

        var cart = await service.GetCartAsync();

        Assert.Equal(49.99m, cart.Subtotal);
        Assert.Equal(15.00m, cart.ShippingFee);
        Assert.Equal(64.99m, cart.GrandTotal);
        Assert.Equal(250.01m, cart.AmountNeededForFreeShipping);
    }

    [Fact]
    public async Task CartTotals_SubtotalAboveOrEqualThreshold_UnlocksFreeShipping()
    {
        var (service, _, _) = CreateTestSetup(nameof(CartTotals_SubtotalAboveOrEqualThreshold_UnlocksFreeShipping));

        // Product 1 is 199.99 RON. 2 items = 399.98 RON (>= 300 RON threshold)
        await service.AddToCartAsync(productId: 1, quantity: 2);

        var cart = await service.GetCartAsync();

        Assert.Equal(399.98m, cart.Subtotal);
        Assert.Equal(0.00m, cart.ShippingFee);
        Assert.Equal(399.98m, cart.GrandTotal);
        Assert.Equal(0.00m, cart.AmountNeededForFreeShipping);
        Assert.Equal(100, cart.FreeShippingProgressPercentage);
    }

    // =========================================================================
    // 4. Guest Session & Cart Merge on Login Tests
    // =========================================================================

    [Fact]
    public async Task GuestCart_StoresItemsInSession_AndComputesCart()
    {
        // Unauthenticated guest (userId = null)
        var (service, _, httpContext) = CreateTestSetup(nameof(GuestCart_StoresItemsInSession_AndComputesCart), userId: null);

        var result = await service.AddToCartAsync(productId: 1, quantity: 1);

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalCartCount);

        var cart = await service.GetCartAsync();
        Assert.Single(cart.Items);
        Assert.Equal(1, cart.Items[0].Quantity);
        Assert.Equal(199.99m, cart.Subtotal);

        // Verify session holds the JSON
        var sessionJson = httpContext.Session.GetString(CartService.SessionCartKey);
        Assert.False(string.IsNullOrEmpty(sessionJson));
    }

    [Fact]
    public async Task MergeGuestCart_WhenUserLogsIn_TransfersSessionItemsToDatabase()
    {
        // 1. Guest user adds Product 1 and Product 2 to session cart
        var (guestService, context, httpContext) = CreateTestSetup(nameof(MergeGuestCart_WhenUserLogsIn_TransfersSessionItemsToDatabase), userId: null);

        await guestService.AddToCartAsync(productId: 1, quantity: 2);
        await guestService.AddToCartAsync(productId: 2, quantity: 1);

        // Pre-existing item in user's DB cart for Product 2 (quantity = 1)
        context.CartItems.Add(new CartItem
        {
            UserId = 1,
            ProductId = 2,
            Quantity = 1,
            AddedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // 2. Perform merge for User 1
        await guestService.MergeGuestCartIntoUserCartAsync(userId: 1);

        // Verify session was cleared
        var remainingSession = httpContext.Session.GetString(CartService.SessionCartKey);
        Assert.True(string.IsNullOrEmpty(remainingSession));

        // Verify database items for User 1
        var userDbItems = await context.CartItems.Where(ci => ci.UserId == 1).ToListAsync();
        Assert.Equal(2, userDbItems.Count);

        var p1Item = userDbItems.First(ci => ci.ProductId == 1);
        Assert.Equal(2, p1Item.Quantity);

        var p2Item = userDbItems.First(ci => ci.ProductId == 2);
        // 1 pre-existing + 1 from guest = 2 (capped at stock 5)
        Assert.Equal(2, p2Item.Quantity);
    }

    [Fact]
    public async Task GetCart_WithPreExistingDuplicateRows_AutomaticallyHealsAndDeduplicates()
    {
        var (service, context, _) = CreateTestSetup(nameof(GetCart_WithPreExistingDuplicateRows_AutomaticallyHealsAndDeduplicates));

        // Simulate 4 duplicate rows in database for Product 1 (each quantity = 5)
        context.CartItems.AddRange(
            new CartItem { UserId = 1, ProductId = 1, Quantity = 5, AddedAt = DateTime.UtcNow },
            new CartItem { UserId = 1, ProductId = 1, Quantity = 5, AddedAt = DateTime.UtcNow },
            new CartItem { UserId = 1, ProductId = 1, Quantity = 5, AddedAt = DateTime.UtcNow },
            new CartItem { UserId = 1, ProductId = 1, Quantity = 5, AddedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var cart = await service.GetCartAsync();

        // Must return only 1 item in cart
        Assert.Single(cart.Items);
        // Quantity must be capped at Product 1 max stock (10)
        Assert.Equal(10, cart.Items[0].Quantity);

        // Database must have been cleaned up to exactly 1 row
        var dbRows = await context.CartItems.Where(ci => ci.UserId == 1 && ci.ProductId == 1).ToListAsync();
        Assert.Single(dbRows);
        Assert.Equal(10, dbRows[0].Quantity);
    }

    [Fact]
    public async Task AddToCart_ConcurrentRequests_EnforcesStockCapAndPreventsDuplicates()
    {
        var (service, context, _) = CreateTestSetup(nameof(AddToCart_ConcurrentRequests_EnforcesStockCapAndPreventsDuplicates));

        // Product 2 has StockQuantity = 5.
        // Simulate 5 AddToCart requests of quantity 2
        var tasks = Enumerable.Range(0, 5).Select(_ => service.AddToCartAsync(productId: 2, quantity: 2));
        await Task.WhenAll(tasks);

        var dbRows = await context.CartItems.Where(ci => ci.UserId == 1 && ci.ProductId == 2).ToListAsync();

        // Exactly 1 row in database
        Assert.Single(dbRows);
        // Quantity capped at maximum available stock (5)
        Assert.Equal(5, dbRows[0].Quantity);
    }
}

// =============================================================================
// Helper: In-Memory ISession for xUnit testing
// =============================================================================
internal class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _storage = new();

    public bool IsAvailable => true;
    public string Id => Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _storage.Keys;

    public void Clear() => _storage.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _storage.Remove(key);

    public void Set(string key, byte[] value) => _storage[key] = value;

    public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? value)
    {
        if (_storage.TryGetValue(key, out var val))
        {
            value = val;
            return true;
        }
        value = null;
        return false;
    }
}
