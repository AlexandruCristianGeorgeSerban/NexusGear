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

        var category2 = new Category
        {
            CategoryId = 2,
            Name = "Gaming Mice",
            Slug = "gaming-mice",
            Description = "Precision gaming mice"
        };

        var product4 = new Product
        {
            ProductId = 4,
            Name = "NexusGhost Ultra Mouse",
            Description = "Ultralight wireless gaming mouse",
            Price = 100.00m,
            StockQuantity = 10,
            CategoryId = 2,
            ImageUrl = "/images/products/Product_4.png"
        };

        context.Users.Add(user);
        context.Categories.AddRange(category, category2);
        context.Products.AddRange(product1, product2, productOutOfStock, product4);
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

    [Fact]
    public async Task CartTotals_ComplementaryCategories_AppliesFivePercentBundleDiscount()
    {
        // Arrange: User adds Product 1 (Keyboard, Cat 1: 199.99 RON) and Product 4 (Mouse, Cat 2: 100.00 RON)
        var (service, _, _) = CreateTestSetup(nameof(CartTotals_ComplementaryCategories_AppliesFivePercentBundleDiscount));

        await service.AddToCartAsync(productId: 1, quantity: 1);
        await service.AddToCartAsync(productId: 4, quantity: 1);

        // Act
        var cart = await service.GetCartAsync();

        // Assert (B2 Bundle Discount)
        decimal expectedSubtotal = 199.99m + 100.00m; // 299.99 RON
        decimal expectedBundleDiscount = Math.Round(expectedSubtotal * 0.05m, 2); // 15.00 RON
        decimal expectedShipping = 15.00m; // Subtotal 299.99 < 300.00 free threshold
        decimal expectedGrandTotal = expectedSubtotal - expectedBundleDiscount + expectedShipping; // 299.99 RON

        Assert.Equal(expectedSubtotal, cart.Subtotal);
        Assert.True(cart.HasBundleDiscount);
        Assert.Equal(expectedBundleDiscount, cart.BundleDiscount);
        Assert.Equal(expectedShipping, cart.ShippingFee);
        Assert.Equal(expectedGrandTotal, cart.GrandTotal);
    }

    [Fact]
    public async Task CartTotals_RegisteredBundleWithUnrelatedItem_AppliesDiscountOnlyToBundleItems()
    {
        // Arrange: User adds Product 1 (Keyboard: 199.99 RON) and Product 4 (Mouse: 100.00 RON) as a Bundle
        // and also adds Product 2 (Deskmat: 49.99 RON) as an unrelated item
        var (service, _, _) = CreateTestSetup(nameof(CartTotals_RegisteredBundleWithUnrelatedItem_AppliesDiscountOnlyToBundleItems));

        service.RegisterBundle(new List<int> { 1, 4 });
        await service.AddToCartAsync(productId: 1, quantity: 1);
        await service.AddToCartAsync(productId: 4, quantity: 1);
        await service.AddToCartAsync(productId: 2, quantity: 1);

        // Act
        var cart = await service.GetCartAsync();

        // Assert:
        // Total subtotal = 199.99 + 100.00 + 49.99 = 349.98 RON
        // Bundle discount ONLY on Product 1 and Product 4: (199.99 * 0.05 = 10.00) + (100.00 * 0.05 = 5.00) = 15.00 RON
        // Deskmat (Product 2) MUST NOT receive bundle discount!
        decimal expectedBundleDiscount = 15.00m;
        decimal expectedSubtotal = 349.98m;

        Assert.Equal(expectedSubtotal, cart.Subtotal);
        Assert.Equal(expectedBundleDiscount, cart.BundleDiscount);

        var item1 = cart.Items.First(i => i.ProductId == 1);
        var item4 = cart.Items.First(i => i.ProductId == 4);
        var item2 = cart.Items.First(i => i.ProductId == 2);

        Assert.True(item1.IsBundleItem);
        Assert.Equal(10.00m, item1.BundleDiscountAmount);
        Assert.Equal(189.99m, item1.DiscountedLineTotal);

        Assert.True(item4.IsBundleItem);
        Assert.Equal(5.00m, item4.BundleDiscountAmount);
        Assert.Equal(95.00m, item4.DiscountedLineTotal);

        Assert.False(item2.IsBundleItem);
        Assert.Equal(0.00m, item2.BundleDiscountAmount);
        Assert.Equal(49.99m, item2.DiscountedLineTotal);
    }

    [Fact]
    public async Task Cart_RemoveItemFromBundle_ClearsDiscountAndBundleBadgeFromRemainingItem()
    {
        // Arrange: User adds Bundle [1, 4] (Keyboard + Mouse)
        var (service, _, _) = CreateTestSetup(nameof(Cart_RemoveItemFromBundle_ClearsDiscountAndBundleBadgeFromRemainingItem));

        service.RegisterBundle(new List<int> { 1, 4 });
        await service.AddToCartAsync(productId: 1, quantity: 1);
        await service.AddToCartAsync(productId: 4, quantity: 1);

        // Before removal: bundle is active
        var cartBefore = await service.GetCartAsync();
        Assert.Equal(15.00m, cartBefore.BundleDiscount);
        Assert.True(cartBefore.Items.First(i => i.ProductId == 1).IsBundleItem);

        // Act: Remove Product 4
        var removeResult = await service.RemoveFromCartAsync(productId: 4);

        // Assert:
        // Remaining cart should have 0 bundle discount
        var cartAfter = await service.GetCartAsync();
        Assert.Equal(0.00m, cartAfter.BundleDiscount);
        Assert.False(cartAfter.HasBundleDiscount);

        var remainingItem = cartAfter.Items.First(i => i.ProductId == 1);
        Assert.False(remainingItem.IsBundleItem);
        Assert.Equal(0.00m, remainingItem.BundleDiscountAmount);
        Assert.Equal(remainingItem.LineTotal, remainingItem.DiscountedLineTotal);

        // removeResult.Items DTO must inform frontend that item 1 is NO LONGER a bundle item
        var itemDto = removeResult.Items.FirstOrDefault(i => i.ProductId == 1);
        Assert.NotNull(itemDto);
        Assert.False(itemDto.IsBundleItem);
        Assert.Equal(0.00m, itemDto.BundleDiscountAmount);
    }

    [Fact]
    public async Task Cart_TwoBundlesWithSharedItem_DecreasingSharedItemQuantity_DiscountsOnlyOneBundle()
    {
        // Arrange:
        // Bundle 1: Product 1 (Keyboard: 199.99) + Product 2 (Deskmat: 49.99) -> 5% = 12.50 RON
        // Bundle 2: Product 4 (Mouse: 100.00) + Product 2 (Deskmat: 49.99) -> 5% = 7.50 RON
        // Shared item: Product 2 (Deskmat)
        var (service, _, _) = CreateTestSetup(nameof(Cart_TwoBundlesWithSharedItem_DecreasingSharedItemQuantity_DiscountsOnlyOneBundle));

        service.RegisterBundle(new List<int> { 1, 2 });
        service.RegisterBundle(new List<int> { 4, 2 });

        await service.AddToCartAsync(productId: 1, quantity: 1);
        await service.AddToCartAsync(productId: 2, quantity: 2); // Qty = 2 allows BOTH bundles to be formed
        await service.AddToCartAsync(productId: 4, quantity: 1);

        // Step 1: When shared item quantity = 2, BOTH bundles are active
        var cartWithTwoShared = await service.GetCartAsync();
        Assert.Equal(20.00m, cartWithTwoShared.BundleDiscount); // 12.50 + 7.50
        Assert.True(cartWithTwoShared.Items.First(i => i.ProductId == 1).IsBundleItem);
        Assert.True(cartWithTwoShared.Items.First(i => i.ProductId == 4).IsBundleItem);
        Assert.True(cartWithTwoShared.Items.First(i => i.ProductId == 2).IsBundleItem);
        Assert.Equal(5.00m, cartWithTwoShared.Items.First(i => i.ProductId == 2).BundleDiscountAmount); // 2.50 * 2 units

        // Step 2: Reduce shared item (Product 2) quantity to 1
        var updateResult = await service.UpdateQuantityAsync(productId: 2, quantity: 1);

        // Step 3: Now only ONE bundle can be formed! The second bundle MUST NOT receive discount!
        var cartWithOneShared = await service.GetCartAsync();
        Assert.Equal(12.50m, cartWithOneShared.BundleDiscount); // Only Bundle 1 is formed!

        var item1 = cartWithOneShared.Items.First(i => i.ProductId == 1);
        var item2 = cartWithOneShared.Items.First(i => i.ProductId == 2);
        var item4 = cartWithOneShared.Items.First(i => i.ProductId == 4);

        // Bundle 1 items get discount
        Assert.True(item1.IsBundleItem);
        Assert.Equal(10.00m, item1.BundleDiscountAmount);
        Assert.True(item2.IsBundleItem);
        Assert.Equal(2.50m, item2.BundleDiscountAmount);

        // Product 4 (unfulfilled bundle item) MUST NOT receive discount
        Assert.False(item4.IsBundleItem);
        Assert.Equal(0.00m, item4.BundleDiscountAmount);
        Assert.Equal(100.00m, item4.DiscountedLineTotal);

        // AJAX DTO list must mirror this exact state for client-side DOM sync
        var item4Dto = updateResult.Items.First(i => i.ProductId == 4);
        Assert.False(item4Dto.IsBundleItem);
        Assert.Equal(0.00m, item4Dto.BundleDiscountAmount);
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
