using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories;
using NexusGear.Services;
using NexusGear.ViewModels;
using Xunit;

namespace NexusGear.Tests;

public class CheckoutTests
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
            FullName = "Gear Customer",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer123!"),
            Role = "Customer",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var address = new Address
        {
            AddressId = 1,
            Street = "Strada Victoriei 42",
            City = "Craiova",
            County = "Dolj",
            PostalCode = "200100",
            Phone = "+40721000111"
        };

        var userAddress = new UserAddress
        {
            UserAddressId = 1,
            UserId = 1,
            AddressId = 1,
            IsDefault = true
        };

        var category = new Category
        {
            CategoryId = 1,
            Name = "Mechanical Keyboards",
            Slug = "mechanical-keyboards",
            Description = "Enthusiast mechanical keyboards"
        };

        var product1 = new Product
        {
            ProductId = 1,
            Name = "NexusApex Pro Carbon",
            Description = "Hall effect magnetic gaming keyboard",
            Price = 200.00m,
            StockQuantity = 10,
            CategoryId = 1,
            ImageUrl = "/images/products/Product_1.png"
        };

        var product2 = new Product
        {
            ProductId = 2,
            Name = "NexusGlide XL Speed",
            Description = "Cordura hybrid deskmat",
            Price = 120.00m,
            StockQuantity = 2,
            CategoryId = 1,
            ImageUrl = "/images/products/Product_2.png"
        };

        var discountPercentage = new DiscountCode
        {
            DiscountCodeId = 1,
            Code = "NEXUS10",
            DiscountType = "Percentage",
            DiscountValue = 10.00m,
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            MaxUses = 100,
            UsedCount = 0
        };

        var discountFixed = new DiscountCode
        {
            DiscountCodeId = 2,
            Code = "SETUP50",
            DiscountType = "FixedAmount",
            DiscountValue = 50.00m,
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            MaxUses = 100,
            UsedCount = 0
        };

        var discountExpired = new DiscountCode
        {
            DiscountCodeId = 3,
            Code = "EXPIRED20",
            DiscountType = "Percentage",
            DiscountValue = 20.00m,
            ExpiryDate = DateTime.UtcNow.AddDays(-10),
            MaxUses = 100,
            UsedCount = 0
        };

        var discountExhausted = new DiscountCode
        {
            DiscountCodeId = 4,
            Code = "MAXED5",
            DiscountType = "FixedAmount",
            DiscountValue = 5.00m,
            ExpiryDate = DateTime.UtcNow.AddMonths(1),
            MaxUses = 5,
            UsedCount = 5
        };

        var category2 = new Category
        {
            CategoryId = 2,
            Name = "Gaming Mice",
            Slug = "gaming-mice",
            Description = "Precision gaming mice"
        };

        var product3 = new Product
        {
            ProductId = 3,
            Name = "NexusGlide Pro Mouse",
            Description = "Ultralight gaming mouse",
            Price = 100.00m,
            StockQuantity = 10,
            CategoryId = 2,
            ImageUrl = "/images/products/Product_3.png"
        };

        context.Users.Add(user);
        context.Addresses.Add(address);
        context.UserAddresses.Add(userAddress);
        context.Categories.AddRange(category, category2);
        context.Products.AddRange(product1, product2, product3);
        context.DiscountCodes.AddRange(discountPercentage, discountFixed, discountExpired, discountExhausted);
        context.SaveChanges();
    }

    private static (CheckoutService checkoutService, DiscountService discountService, AppDbContext context)
        CreateTestEnvironment(string dbName, int userId = 1)
    {
        var context = CreateInMemoryDbContext(dbName);
        var repoWrapper = new RepositoryWrapper(context);

        var httpContext = new DefaultHttpContext();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "customer@nexusgear.ro"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var cartService = new CartService(repoWrapper, accessor);
        var discountService = new DiscountService(repoWrapper);
        var checkoutService = new CheckoutService(context, repoWrapper, cartService, discountService);

        return (checkoutService, discountService, context);
    }

    private static (CheckoutService checkoutService, CartService cartService, DiscountService discountService, AppDbContext context)
        CreateTestEnvironmentWithCart(string dbName, int userId = 1)
    {
        var context = CreateInMemoryDbContext(dbName);
        var repoWrapper = new RepositoryWrapper(context);

        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSession();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "customer@nexusgear.ro"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var cartService = new CartService(repoWrapper, accessor);
        var discountService = new DiscountService(repoWrapper);
        var checkoutService = new CheckoutService(context, repoWrapper, cartService, discountService);

        return (checkoutService, cartService, discountService, context);
    }

    // =========================================================================
    // Discount Validation Tests (B5)
    // =========================================================================

    [Fact]
    public async Task ValidateCoupon_ValidPercentageCoupon_CalculatesAccurateTenPercentDiscount()
    {
        var (_, discountService, _) = CreateTestEnvironment(nameof(ValidateCoupon_ValidPercentageCoupon_CalculatesAccurateTenPercentDiscount));

        var result = await discountService.ValidateCouponAsync("NEXUS10", 200.00m, 15.00m);

        Assert.True(result.IsValid);
        Assert.Equal("NEXUS10", result.Code);
        Assert.Equal(20.00m, result.DiscountAmount); // 10% of 200
        Assert.Equal(195.00m, result.NewGrandTotal); // 200 - 20 + 15
    }

    [Fact]
    public async Task ValidateCoupon_ValidFixedAmountCoupon_CalculatesFiftyRonDiscount()
    {
        var (_, discountService, _) = CreateTestEnvironment(nameof(ValidateCoupon_ValidFixedAmountCoupon_CalculatesFiftyRonDiscount));

        var result = await discountService.ValidateCouponAsync("SETUP50", 200.00m, 15.00m);

        Assert.True(result.IsValid);
        Assert.Equal("SETUP50", result.Code);
        Assert.Equal(50.00m, result.DiscountAmount);
        Assert.Equal(165.00m, result.NewGrandTotal); // 200 - 50 + 15
    }

    [Fact]
    public async Task ValidateCoupon_FixedAmountExceedingSubtotal_ClampsToSubtotal()
    {
        var (_, discountService, _) = CreateTestEnvironment(nameof(ValidateCoupon_FixedAmountExceedingSubtotal_ClampsToSubtotal));

        // Subtotal is 30, coupon is 50 -> discount clamped to 30
        var result = await discountService.ValidateCouponAsync("SETUP50", 30.00m, 15.00m);

        Assert.True(result.IsValid);
        Assert.Equal(30.00m, result.DiscountAmount);
        Assert.Equal(15.00m, result.NewGrandTotal); // 30 - 30 + 15
    }

    [Fact]
    public async Task ValidateCoupon_ExpiredCoupon_ReturnsInvalidWithExplanation()
    {
        var (_, discountService, _) = CreateTestEnvironment(nameof(ValidateCoupon_ExpiredCoupon_ReturnsInvalidWithExplanation));

        var result = await discountService.ValidateCouponAsync("EXPIRED20", 200.00m, 15.00m);

        Assert.False(result.IsValid);
        Assert.Contains("expired", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0.00m, result.DiscountAmount);
    }

    [Fact]
    public async Task ValidateCoupon_NonExistentCoupon_ReturnsInvalid()
    {
        var (_, discountService, _) = CreateTestEnvironment(nameof(ValidateCoupon_NonExistentCoupon_ReturnsInvalid));

        var result = await discountService.ValidateCouponAsync("NOTREAL99", 200.00m, 15.00m);

        Assert.False(result.IsValid);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateCoupon_ExceededUsageLimit_ReturnsInvalid()
    {
        var (_, discountService, _) = CreateTestEnvironment(nameof(ValidateCoupon_ExceededUsageLimit_ReturnsInvalid));

        var result = await discountService.ValidateCouponAsync("MAXED5", 200.00m, 15.00m);

        Assert.False(result.IsValid);
        Assert.Contains("maximum allowed redemptions", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // Checkout Flow Calculation & Address Tests (A5)
    // =========================================================================

    [Fact]
    public async Task PrepareCheckout_CalculatesStandardShipping_WhenBelowThreeHundred()
    {
        var (checkoutService, _, context) = CreateTestEnvironment(nameof(PrepareCheckout_CalculatesStandardShipping_WhenBelowThreeHundred));

        // Add item worth 200 RON (< 300 threshold)
        context.CartItems.Add(new CartItem { UserId = 1, ProductId = 1, Quantity = 1 });
        context.SaveChanges();

        var model = await checkoutService.PrepareCheckoutViewModelAsync(1);

        Assert.Equal(200.00m, model.Subtotal);
        Assert.False(model.FreeShippingUnlocked);
        Assert.Equal(15.00m, model.ShippingCost);
        Assert.Equal(215.00m, model.GrandTotal);
        Assert.NotEmpty(model.SavedAddresses);
        Assert.Equal(1, model.SelectedAddressId);
    }

    [Fact]
    public async Task PrepareCheckout_UnlocksFreeStandardShipping_WhenAboveThreeHundred()
    {
        var (checkoutService, _, context) = CreateTestEnvironment(nameof(PrepareCheckout_UnlocksFreeStandardShipping_WhenAboveThreeHundred));

        // Add 2 keyboards = 400 RON (>= 300 threshold)
        context.CartItems.Add(new CartItem { UserId = 1, ProductId = 1, Quantity = 2 });
        context.SaveChanges();

        var model = await checkoutService.PrepareCheckoutViewModelAsync(1);

        Assert.Equal(400.00m, model.Subtotal);
        Assert.True(model.FreeShippingUnlocked);
        Assert.Equal(0.00m, model.ShippingCost);
        Assert.Equal(400.00m, model.GrandTotal);
    }

    // =========================================================================
    // Order Placement & Validation Tests (A5, A7)
    // =========================================================================

    [Fact]
    public async Task PlaceOrder_EmptyCart_ReturnsFailure()
    {
        var (checkoutService, _, _) = CreateTestEnvironment(nameof(PlaceOrder_EmptyCart_ReturnsFailure));

        var request = new PlaceOrderRequest
        {
            SelectedAddressId = 1,
            ShippingMethod = "Standard Courier"
        };

        var result = await checkoutService.PlaceOrderAsync(1, request);

        Assert.False(result.Success);
        Assert.Contains("empty", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlaceOrder_InsufficientProductStock_FailsWithDescriptiveError()
    {
        var (checkoutService, _, context) = CreateTestEnvironment(nameof(PlaceOrder_InsufficientProductStock_FailsWithDescriptiveError));

        // Product 2 has only 2 units in stock; try to order 5
        context.CartItems.Add(new CartItem { UserId = 1, ProductId = 2, Quantity = 5 });
        context.SaveChanges();

        var request = new PlaceOrderRequest
        {
            SelectedAddressId = 1,
            ShippingMethod = "Standard Courier"
        };

        var result = await checkoutService.PlaceOrderAsync(1, request);

        Assert.False(result.Success);
        Assert.Contains("units in stock", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlaceOrder_ValidOrder_CreatesUnpaidOrderAndSnapshotsContainsPrices()
    {
        var (checkoutService, _, context) = CreateTestEnvironment(nameof(PlaceOrder_ValidOrder_CreatesUnpaidOrderAndSnapshotsContainsPrices));

        // Add product 1 (200 RON, Qty 1) to cart
        context.CartItems.Add(new CartItem { UserId = 1, ProductId = 1, Quantity = 1 });
        context.SaveChanges();

        var request = new PlaceOrderRequest
        {
            SelectedAddressId = 1,
            ShippingMethod = "Standard Courier"
        };

        var result = await checkoutService.PlaceOrderAsync(1, request);

        Assert.True(result.Success);
        Assert.NotNull(result.OrderId);
        Assert.NotNull(result.OrderNumber);
        Assert.StartsWith("NXG-2026-", result.OrderNumber);

        // Verify order in database
        var order = await context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == result.OrderId.Value);

        Assert.NotNull(order);
        Assert.Equal("Unpaid", order.Status);
        Assert.Equal(15.00m, order.ShippingCost);
        Assert.Equal(215.00m, order.TotalAmount);
        Assert.Single(order.OrderItems);

        var line = order.OrderItems.First();
        Assert.Equal(1, line.ProductId);
        Assert.Equal(1, line.Quantity);
        Assert.Equal(200.00m, line.UnitPrice); // Historical price snapshot

        // Verify user cart was cleared
        var remainingCart = await context.CartItems.Where(ci => ci.UserId == 1).ToListAsync();
        Assert.Empty(remainingCart);
    }

    [Fact]
    public async Task PlaceOrder_WithDiscount_ReducesTotalAndAttachesDiscountCodeId()
    {
        var (checkoutService, _, context) = CreateTestEnvironment(nameof(PlaceOrder_WithDiscount_ReducesTotalAndAttachesDiscountCodeId));

        context.CartItems.Add(new CartItem { UserId = 1, ProductId = 1, Quantity = 1 }); // 200 RON
        context.SaveChanges();

        var request = new PlaceOrderRequest
        {
            SelectedAddressId = 1,
            ShippingMethod = "Standard Courier",
            DiscountCode = "NEXUS10" // 10% = 20 RON
        };

        var result = await checkoutService.PlaceOrderAsync(1, request);

        Assert.True(result.Success);
        Assert.NotNull(result.OrderId);
        var order = await context.Orders.FirstOrDefaultAsync(o => o.OrderId == result.OrderId.Value);

        Assert.NotNull(order);
        Assert.Equal(1, order.DiscountCodeId);
        Assert.Equal(20.00m, order.DiscountAmount);
        Assert.Equal(195.00m, order.TotalAmount); // 200 + 15 - 20
    }

    [Fact]
    public async Task GetOrderConfirmation_ValidOrder_ReturnsFormattedOrderDetails()
    {
        var (checkoutService, _, context) = CreateTestEnvironment(nameof(GetOrderConfirmation_ValidOrder_ReturnsFormattedOrderDetails));

        context.CartItems.Add(new CartItem { UserId = 1, ProductId = 1, Quantity = 1 });
        context.SaveChanges();

        var placeResult = await checkoutService.PlaceOrderAsync(1, new PlaceOrderRequest
        {
            SelectedAddressId = 1,
            ShippingMethod = "Standard Courier"
        });

        Assert.True(placeResult.Success);
        Assert.NotNull(placeResult.OrderId);
        var confirmation = await checkoutService.GetOrderConfirmationAsync(placeResult.OrderId.Value, 1);

        Assert.NotNull(confirmation);
        Assert.Equal(placeResult.OrderNumber, confirmation.OrderNumber);
        Assert.Equal("Unpaid", confirmation.Status);
        Assert.Equal("Gear Customer", confirmation.CustomerName);
        Assert.Equal("Strada Victoriei 42", confirmation.Street);
        Assert.Single(confirmation.Items);
        Assert.Equal(200.00m, confirmation.Subtotal);
        Assert.Equal(215.00m, confirmation.TotalAmount);
    }

    [Fact]
    public async Task PlaceOrder_WithBundleItemsFromDistinctCategories_AppliesBundleDiscount()
    {
        var (checkoutService, _, context) = CreateTestEnvironment(nameof(PlaceOrder_WithBundleItemsFromDistinctCategories_AppliesBundleDiscount));

        // Add Product 1 (Cat 1: 200 RON) and Product 3 (Cat 2: 100 RON) -> Subtotal = 300 RON
        context.CartItems.Add(new CartItem { UserId = 1, ProductId = 1, Quantity = 1 });
        context.CartItems.Add(new CartItem { UserId = 1, ProductId = 3, Quantity = 1 });
        context.SaveChanges();

        var request = new PlaceOrderRequest
        {
            SelectedAddressId = 1,
            ShippingMethod = "Standard Courier"
        };

        var result = await checkoutService.PlaceOrderAsync(1, request);

        Assert.True(result.Success);
        Assert.NotNull(result.OrderId);

        var order = await context.Orders.FindAsync(result.OrderId.Value);
        Assert.NotNull(order);
        Assert.Equal(15.00m, order.DiscountAmount); // 5% of 300.00 RON
        Assert.Equal(285.00m, order.TotalAmount); // 300 - 15 + 0 shipping
    }

    [Fact]
    public async Task PlaceOrder_RegisteredBundleWithUnrelatedItem_DiscountsOnlyBundleItems()
    {
        var (checkoutService, cartService, _, context) = CreateTestEnvironmentWithCart(nameof(PlaceOrder_RegisteredBundleWithUnrelatedItem_DiscountsOnlyBundleItems));

        // Register bundle for Product 1 (Cat 1: 200 RON) and Product 3 (Cat 2: 100 RON)
        cartService.RegisterBundle(new List<int> { 1, 3 });

        // Add Product 1 (Cat 1: 200 RON), Product 3 (Cat 2: 100 RON), AND unrelated Product 2 (Cat 1: 120 RON)
        context.CartItems.Add(new CartItem { UserId = 1, ProductId = 1, Quantity = 1 });
        context.CartItems.Add(new CartItem { UserId = 1, ProductId = 3, Quantity = 1 });
        context.CartItems.Add(new CartItem { UserId = 1, ProductId = 2, Quantity = 1 });
        context.SaveChanges();

        var request = new PlaceOrderRequest
        {
            SelectedAddressId = 1,
            ShippingMethod = "Standard Courier"
        };

        var result = await checkoutService.PlaceOrderAsync(1, request);

        Assert.True(result.Success);
        Assert.NotNull(result.OrderId);

        var order = await context.Orders.FindAsync(result.OrderId.Value);
        Assert.NotNull(order);
        // Total subtotal: 200 + 100 + 120 = 420 RON
        // Bundle items: 200 + 100 = 300 RON -> 5% = 15.00 RON
        // Standalone Product 2 (120 RON) MUST NOT be discounted!
        Assert.Equal(15.00m, order.DiscountAmount);
        Assert.Equal(405.00m, order.TotalAmount); // 420 - 15 + 0 shipping (420 >= 300 threshold)
    }
}
