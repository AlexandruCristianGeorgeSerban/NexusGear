using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories;
using NexusGear.Services;
using NexusGear.Services.Interfaces;
using Xunit;

namespace NexusGear.Tests;

public class WishlistTests
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
            Email = "tester@nexusgear.ro",
            FullName = "Alexandru Tester",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
            Role = "Customer"
        };

        var category = new Category
        {
            CategoryId = 1,
            Name = "Keyboards",
            Slug = "keyboards",
            Description = "Gaming keyboards"
        };

        var product1 = new Product
        {
            ProductId = 1,
            Name = "NexusApex Pro Carbon",
            Description = "Magnetic switches",
            Price = 199.99m,
            StockQuantity = 10,
            CategoryId = 1,
            ImageUrl = "/images/products/Product_1.png"
        };

        var product2 = new Product
        {
            ProductId = 2,
            Name = "NexusThrone PostureMax",
            Description = "Ergonomic chair",
            Price = 479.99m,
            StockQuantity = 0, // Out of stock
            CategoryId = 1,
            ImageUrl = "/images/products/Product_2.png"
        };

        context.Users.Add(user);
        context.Categories.Add(category);
        context.Products.AddRange(product1, product2);
        context.SaveChanges();
    }

    private static (IWishlistService wishlistService, ICartService cartService, AppDbContext context) CreateTestSetup(string dbName)
    {
        var context = CreateInMemoryDbContext(dbName);
        var repo = new RepositoryWrapper(context);

        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Name, "Alexandru Tester"),
            new(ClaimTypes.Email, "tester@nexusgear.ro")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var cartService = new CartService(repo, accessor);
        var wishlistService = new WishlistService(repo, cartService, NullLogger<WishlistService>.Instance);

        return (wishlistService, cartService, context);
    }

    [Fact]
    public async Task ToggleWishlist_WhenItemNotInWishlist_AddsItemAndReturnsTrue()
    {
        var (service, _, context) = CreateTestSetup(nameof(ToggleWishlist_WhenItemNotInWishlist_AddsItemAndReturnsTrue));

        var result = await service.ToggleWishlistAsync(userId: 1, productId: 1);

        Assert.True(result.Success);
        Assert.True(result.IsAdded);
        Assert.Equal(1, result.WishlistCount);

        var dbItem = await context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == 1 && w.ProductId == 1);
        Assert.NotNull(dbItem);
    }

    [Fact]
    public async Task ToggleWishlist_WhenItemAlreadyInWishlist_RemovesItemAndReturnsFalse()
    {
        var (service, _, context) = CreateTestSetup(nameof(ToggleWishlist_WhenItemAlreadyInWishlist_RemovesItemAndReturnsFalse));

        // First toggle: adds item
        await service.ToggleWishlistAsync(userId: 1, productId: 1);

        // Second toggle: removes item
        var result = await service.ToggleWishlistAsync(userId: 1, productId: 1);

        Assert.True(result.Success);
        Assert.False(result.IsAdded);
        Assert.Equal(0, result.WishlistCount);

        var dbItem = await context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == 1 && w.ProductId == 1);
        Assert.Null(dbItem);
    }

    [Fact]
    public async Task ToggleWishlist_NonExistentProduct_ReturnsFailure()
    {
        var (service, _, _) = CreateTestSetup(nameof(ToggleWishlist_NonExistentProduct_ReturnsFailure));

        var result = await service.ToggleWishlistAsync(userId: 1, productId: 9999);

        Assert.False(result.Success);
        Assert.False(result.IsAdded);
    }

    [Fact]
    public async Task GetWishlist_ReturnsAllItemsWithProductDetails()
    {
        var (service, _, _) = CreateTestSetup(nameof(GetWishlist_ReturnsAllItemsWithProductDetails));

        await service.ToggleWishlistAsync(userId: 1, productId: 1);

        var wishlist = await service.GetWishlistAsync(userId: 1);

        Assert.Single(wishlist.Items);
        Assert.Equal(1, wishlist.TotalCount);
        Assert.False(wishlist.IsEmpty);
        Assert.Equal("NexusApex Pro Carbon", wishlist.Items[0].ProductName);
        Assert.Equal(199.99m, wishlist.Items[0].UnitPrice);
        Assert.True(wishlist.Items[0].IsInStock);
    }

    [Fact]
    public async Task RemoveFromWishlist_ExistingItem_RemovesAndReturnsTrue()
    {
        var (service, _, context) = CreateTestSetup(nameof(RemoveFromWishlist_ExistingItem_RemovesAndReturnsTrue));

        await service.ToggleWishlistAsync(userId: 1, productId: 1);

        bool removed = await service.RemoveFromWishlistAsync(userId: 1, productId: 1);

        Assert.True(removed);
        var dbItem = await context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == 1 && w.ProductId == 1);
        Assert.Null(dbItem);
    }

    [Fact]
    public async Task RemoveFromWishlist_NonExistingItem_ReturnsFalse()
    {
        var (service, _, _) = CreateTestSetup(nameof(RemoveFromWishlist_NonExistingItem_ReturnsFalse));

        bool removed = await service.RemoveFromWishlistAsync(userId: 1, productId: 1);

        Assert.False(removed);
    }

    [Fact]
    public async Task MoveToCart_InStockProduct_AddsToCartAndRemovesFromWishlist()
    {
        var (service, cartService, context) = CreateTestSetup(nameof(MoveToCart_InStockProduct_AddsToCartAndRemovesFromWishlist));

        await service.ToggleWishlistAsync(userId: 1, productId: 1);

        var moveResult = await service.MoveToCartAsync(userId: 1, productId: 1);

        Assert.True(moveResult.Success);
        Assert.Equal(0, moveResult.WishlistCount);
        Assert.Equal(1, moveResult.CartCount);

        // Verify removed from wishlist
        var wishlistItem = await context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == 1 && w.ProductId == 1);
        Assert.Null(wishlistItem);

        // Verify added to cart
        var cartItem = await context.CartItems.FirstOrDefaultAsync(c => c.UserId == 1 && c.ProductId == 1);
        Assert.NotNull(cartItem);
        Assert.Equal(1, cartItem.Quantity);
    }

    [Fact]
    public async Task MoveToCart_OutOfStockProduct_FailsAndRetainsInWishlist()
    {
        var (service, _, context) = CreateTestSetup(nameof(MoveToCart_OutOfStockProduct_FailsAndRetainsInWishlist));

        // Product 2 has StockQuantity = 0
        await service.ToggleWishlistAsync(userId: 1, productId: 2);

        var moveResult = await service.MoveToCartAsync(userId: 1, productId: 2);

        Assert.False(moveResult.Success);
        Assert.Contains("out of stock", moveResult.Message, StringComparison.OrdinalIgnoreCase);

        // Verify still in wishlist
        var wishlistItem = await context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == 1 && w.ProductId == 2);
        Assert.NotNull(wishlistItem);
    }

    [Fact]
    public async Task ClearWishlist_RemovesAllItemsForUser()
    {
        var (service, _, context) = CreateTestSetup(nameof(ClearWishlist_RemovesAllItemsForUser));

        await service.ToggleWishlistAsync(userId: 1, productId: 1);
        await service.ToggleWishlistAsync(userId: 1, productId: 2);

        Assert.Equal(2, await service.GetWishlistCountAsync(userId: 1));

        await service.ClearWishlistAsync(userId: 1);

        Assert.Equal(0, await service.GetWishlistCountAsync(userId: 1));
        var remaining = await context.WishlistItems.Where(w => w.UserId == 1).ToListAsync();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task GetUserWishlistProductIds_ReturnsMatchingHashSet()
    {
        var (service, _, _) = CreateTestSetup(nameof(GetUserWishlistProductIds_ReturnsMatchingHashSet));

        await service.ToggleWishlistAsync(userId: 1, productId: 1);

        var ids = await service.GetUserWishlistProductIdsAsync(userId: 1);

        Assert.Contains(1, ids);
        Assert.DoesNotContain(2, ids);
    }
}
