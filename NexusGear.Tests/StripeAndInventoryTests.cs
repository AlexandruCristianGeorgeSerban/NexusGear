using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NexusGear.Controllers;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Services;
using NexusGear.Services.Interfaces;
using Xunit;

namespace NexusGear.Tests;

public class StripeAndInventoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _contextOptions;

    public StripeAndInventoryTests()
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
        var user = new User
        {
            UserId = 1,
            Email = "alex.serban@nexusgear.ro",
            FullName = "Alexandru Serban",
            PasswordHash = "hash123",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        var address = new Address
        {
            AddressId = 1,
            Street = "Bulevardul Decebal 12",
            City = "Craiova",
            County = "Dolj",
            PostalCode = "200100",
            Phone = "+40722112233"
        };
        context.Addresses.Add(address);

        var category = new Category
        {
            CategoryId = 1,
            Name = "Gaming Chairs",
            Slug = "gaming-chairs",
            Description = "Ergonomic Chairs"
        };
        context.Categories.Add(category);

        var product1 = new Product
        {
            ProductId = 1,
            CategoryId = 1,
            Name = "NexusThrone Sovereign Executive Dual",
            Description = "Luxury gaming chair",
            Price = 399.99m,
            StockQuantity = 10,
            ImageUrl = "/images/products/chair1.webp",
            RowVersion = Guid.NewGuid()
        };

        var product2 = new Product
        {
            ProductId = 2,
            CategoryId = 1,
            Name = "NexusThrone CarbonRacer GT Sport",
            Description = "Racing style chair",
            Price = 369.99m,
            StockQuantity = 1, // Low stock for edge case test
            ImageUrl = "/images/products/chair2.webp",
            RowVersion = Guid.NewGuid()
        };
        context.Products.AddRange(product1, product2);

        var voucher = new DiscountCode
        {
            DiscountCodeId = 1,
            Code = "NEXUS10",
            DiscountType = "Percentage",
            DiscountValue = 10m,
            ExpiryDate = DateTime.UtcNow.AddMonths(1),
            MaxUses = 100,
            UsedCount = 0
        };
        context.DiscountCodes.Add(voucher);

        var order1 = new Order
        {
            OrderId = 1,
            OrderNumber = "NX-TEST-001",
            UserId = 1,
            AddressId = 1,
            DiscountCodeId = 1,
            ShippingMethod = "Standard Courier (Fan Courier)",
            ShippingCost = 0m,
            DiscountAmount = 39.99m,
            TotalAmount = 360.00m,
            Status = "Unpaid",
            OrderDate = DateTime.UtcNow
        };
        context.Orders.Add(order1);

        var orderItem1 = new OrderItem
        {
            OrderItemId = 1,
            OrderId = 1,
            ProductId = 1,
            Quantity = 2,
            UnitPrice = 399.99m
        };
        context.OrderItems.Add(orderItem1);

        context.SaveChanges();
    }

    [Fact]
    public async Task ProcessOrderPaymentSuccess_AtomicallyDecrementsStock_AndMarksOrderPaid()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var configMock = new Mock<IConfiguration>();
        var emailMock = new Mock<IEmailService>();
        var webhookService = new WebhookService(
            context,
            configMock.Object,
            emailMock.Object,
            NullLogger<WebhookService>.Instance);

        // Act
        var result = await webhookService.ProcessOrderPaymentSuccessAsync(1);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("NX-TEST-001", result.OrderNumber);

        var updatedOrder = await context.Orders.FindAsync(1);
        Assert.NotNull(updatedOrder);
        Assert.Equal("Paid", updatedOrder.Status);

        var updatedProduct = await context.Products.FindAsync(1);
        Assert.NotNull(updatedProduct);
        Assert.Equal(8, updatedProduct.StockQuantity); // 10 - 2 = 8
    }

    [Fact]
    public async Task ProcessOrderPaymentSuccess_IsIdempotent_DoesNotDecrementStockTwice()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var configMock = new Mock<IConfiguration>();
        var emailMock = new Mock<IEmailService>();
        var webhookService = new WebhookService(
            context,
            configMock.Object,
            emailMock.Object,
            NullLogger<WebhookService>.Instance);

        // Act - First payment webhook event
        var firstResult = await webhookService.ProcessOrderPaymentSuccessAsync(1);
        Assert.True(firstResult.Success);
        Assert.False(firstResult.IsIdempotentDuplicate);

        // Act - Duplicate payment webhook event (e.g. Stripe webhook retry)
        var secondResult = await webhookService.ProcessOrderPaymentSuccessAsync(1);

        // Assert
        Assert.True(secondResult.Success);
        Assert.True(secondResult.IsIdempotentDuplicate);

        var updatedProduct = await context.Products.FindAsync(1);
        Assert.NotNull(updatedProduct);
        Assert.Equal(8, updatedProduct.StockQuantity); // Stays at 8, NOT decremented to 6!
    }

    [Fact]
    public async Task ProcessOrderPaymentSuccess_CancelsOrder_WhenStockBecomesInsufficient()
    {
        // Arrange: create an order requesting 3 units of Product 2 (which only has stock 1)
        using var context = new AppDbContext(_contextOptions);
        var order = new Order
        {
            OrderId = 2,
            OrderNumber = "NX-TEST-002",
            UserId = 1,
            AddressId = 1,
            ShippingMethod = "Standard",
            ShippingCost = 0m,
            TotalAmount = 1109.97m,
            Status = "Unpaid",
            OrderDate = DateTime.UtcNow
        };
        context.Orders.Add(order);

        var item = new OrderItem
        {
            OrderItemId = 2,
            OrderId = 2,
            ProductId = 2, // Stock is 1
            Quantity = 3,  // Requested 3
            UnitPrice = 369.99m
        };
        context.OrderItems.Add(item);
        await context.SaveChangesAsync();

        var configMock = new Mock<IConfiguration>();
        var emailMock = new Mock<IEmailService>();
        var webhookService = new WebhookService(
            context,
            configMock.Object,
            emailMock.Object,
            NullLogger<WebhookService>.Instance);

        // Act
        var result = await webhookService.ProcessOrderPaymentSuccessAsync(2);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Insufficient stock", result.Message);

        var updatedOrder = await context.Orders.FindAsync(2);
        Assert.NotNull(updatedOrder);
        Assert.Equal("Cancelled", updatedOrder.Status);

        var product = await context.Products.FindAsync(2);
        Assert.NotNull(product);
        Assert.Equal(1, product.StockQuantity); // Stock preserved
    }

    [Fact]
    public async Task ProcessOrderPaymentSuccess_UpdatesConcurrencyToken_RowVersion()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var initialProduct = await context.Products.AsNoTracking().FirstAsync(p => p.ProductId == 1);
        var initialRowVersion = initialProduct.RowVersion;

        var configMock = new Mock<IConfiguration>();
        var emailMock = new Mock<IEmailService>();
        var webhookService = new WebhookService(
            context,
            configMock.Object,
            emailMock.Object,
            NullLogger<WebhookService>.Instance);

        // Act
        var result = await webhookService.ProcessOrderPaymentSuccessAsync(1);

        // Assert
        Assert.True(result.Success);
        var updatedProduct = await context.Products.FindAsync(1);
        Assert.NotNull(updatedProduct);
        Assert.NotEqual(initialRowVersion, updatedProduct.RowVersion);
    }

    [Fact]
    public async Task ProcessOrderPaymentSuccess_IncrementsDiscountCode_UsedCount()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var configMock = new Mock<IConfiguration>();
        var emailMock = new Mock<IEmailService>();
        var webhookService = new WebhookService(
            context,
            configMock.Object,
            emailMock.Object,
            NullLogger<WebhookService>.Instance);

        // Act
        var result = await webhookService.ProcessOrderPaymentSuccessAsync(1);

        // Assert
        Assert.True(result.Success);
        var voucher = await context.DiscountCodes.FindAsync(1);
        Assert.NotNull(voucher);
        Assert.Equal(1, voucher.UsedCount);
    }

    [Fact]
    public async Task CreateCheckoutSession_FallsBackToSimulator_WhenKeysArePlaceholder()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Stripe:SecretKey"]).Returns("sk_test_placeholder_for_grading");

        var paymentService = new PaymentService(
            context,
            configMock.Object,
            NullLogger<PaymentService>.Instance);

        // Act
        var result = await paymentService.CreateCheckoutSessionAsync(1, 1, "https://nexusgear.ro");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.IsSimulator);
        Assert.NotNull(result.RedirectUrl);
        Assert.Contains("/Checkout/StripeSimulator/1", result.RedirectUrl);
        Assert.StartsWith("cs_test_sim_", result.SessionId);
    }

    [Fact]
    public async Task CreateCheckoutSession_Fails_WhenOrderAlreadyPaid()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var order = await context.Orders.FindAsync(1);
        order!.Status = "Paid";
        await context.SaveChangesAsync();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Stripe:SecretKey"]).Returns("sk_test_placeholder");

        var paymentService = new PaymentService(
            context,
            configMock.Object,
            NullLogger<PaymentService>.Instance);

        // Act
        var result = await paymentService.CreateCheckoutSessionAsync(1, 1, "https://nexusgear.ro");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already been paid", result.Message);
    }

    [Fact]
    public async Task SimulatePayment_Action_ConfirmsOrderAndRedirects()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var checkoutServiceMock = new Mock<ICheckoutService>();
        var paymentServiceMock = new Mock<IPaymentService>();
        var webhookServiceMock = new Mock<IWebhookService>();

        webhookServiceMock
            .Setup(w => w.ProcessOrderPaymentSuccessAsync(1))
            .ReturnsAsync(new ViewModels.WebhookProcessingResult
            {
                Success = true,
                OrderId = 1,
                OrderNumber = "NX-TEST-001",
                Message = "Payment confirmed"
            });

        var controller = new CheckoutController(
            checkoutServiceMock.Object,
            paymentServiceMock.Object,
            webhookServiceMock.Object);

        // Set authenticated user context
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

        // Act
        var actionResult = await controller.SimulatePayment(1);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal("Confirmation", redirect.ActionName);
        Assert.Equal(1, redirect.RouteValues?["id"]);
        Assert.NotNull(controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task SimulateDeclinedPayment_LeavesOrderUnpaidAndRedirectsWithErrorMessage()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var checkoutServiceMock = new Mock<ICheckoutService>();
        var paymentServiceMock = new Mock<IPaymentService>();
        var webhookServiceMock = new Mock<IWebhookService>();

        checkoutServiceMock
            .Setup(c => c.GetOrderConfirmationAsync(1, 1))
            .ReturnsAsync(new ViewModels.OrderConfirmationViewModel
            {
                OrderId = 1,
                OrderNumber = "NX-TEST-001",
                Status = "Unpaid",
                TotalAmount = 299.99m
            });

        var controller = new CheckoutController(
            checkoutServiceMock.Object,
            paymentServiceMock.Object,
            webhookServiceMock.Object);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

        // Act: Simulate card decline
        var actionResult = await controller.SimulateDeclinedPayment(1);

        // Assert: A6 & FAQ - Declined payment leaves order Unpaid and notifies customer
        var redirect = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal("Confirmation", redirect.ActionName);
        Assert.Equal(1, redirect.RouteValues?["id"]);

        var errorMessage = Assert.IsType<string>(controller.TempData["ErrorMessage"]);
        Assert.Contains("declined", errorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unpaid", errorMessage);
    }

    [Fact]
    public async Task SimulateDeclinedPayment_OrderAlreadyPaid_RejectsWithErrorMessage()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var checkoutServiceMock = new Mock<ICheckoutService>();
        var paymentServiceMock = new Mock<IPaymentService>();
        var webhookServiceMock = new Mock<IWebhookService>();

        checkoutServiceMock
            .Setup(c => c.GetOrderConfirmationAsync(2, 1))
            .ReturnsAsync(new ViewModels.OrderConfirmationViewModel
            {
                OrderId = 2,
                OrderNumber = "NX-PAID-002",
                Status = "Paid",
                TotalAmount = 199.99m
            });

        var controller = new CheckoutController(
            checkoutServiceMock.Object,
            paymentServiceMock.Object,
            webhookServiceMock.Object);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

        // Act: Attempt to simulate decline on an already paid order
        var actionResult = await controller.SimulateDeclinedPayment(2);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal("Confirmation", redirect.ActionName);
        Assert.Equal(2, redirect.RouteValues?["id"]);

        var errorMessage = Assert.IsType<string>(controller.TempData["ErrorMessage"]);
        Assert.Contains("already been paid", errorMessage);
    }

    [Fact]
    public async Task SimulatePayment_WithDeclinedCardType_RoutesToDeclinedPayment()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var checkoutServiceMock = new Mock<ICheckoutService>();
        var paymentServiceMock = new Mock<IPaymentService>();
        var webhookServiceMock = new Mock<IWebhookService>();

        checkoutServiceMock
            .Setup(c => c.GetOrderConfirmationAsync(1, 1))
            .ReturnsAsync(new ViewModels.OrderConfirmationViewModel
            {
                OrderId = 1,
                OrderNumber = "NX-TEST-001",
                Status = "Unpaid",
                TotalAmount = 299.99m
            });

        var controller = new CheckoutController(
            checkoutServiceMock.Object,
            paymentServiceMock.Object,
            webhookServiceMock.Object);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

        // Act: Submit SimulatePayment with testCardType = "declined" (from dropdown selector)
        var actionResult = await controller.SimulatePayment(1, testCardType: "declined");

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal("Confirmation", redirect.ActionName);
        Assert.Equal(1, redirect.RouteValues?["id"]);

        var errorMessage = Assert.IsType<string>(controller.TempData["ErrorMessage"]);
        Assert.Contains("declined", errorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unpaid", errorMessage);
    }

    [Fact]
    public async Task ProcessOrderPaymentSuccess_CancelledOrder_RejectsPaymentAndPreservesStock()
    {
        // Arrange: Mark Order 1 as Cancelled before webhook arrives
        using var context = new AppDbContext(_contextOptions);
        var order = await context.Orders.FindAsync(1);
        order!.Status = "Cancelled";
        await context.SaveChangesAsync();

        var configMock = new Mock<IConfiguration>();
        var emailMock = new Mock<IEmailService>();
        var webhookService = new WebhookService(
            context,
            configMock.Object,
            emailMock.Object,
            NullLogger<WebhookService>.Instance);

        // Act
        var result = await webhookService.ProcessOrderPaymentSuccessAsync(1);

        // Assert: Delayed webhook must NOT mark cancelled order Paid or deduct stock
        Assert.False(result.Success);
        Assert.Contains("cancelled", result.Message, StringComparison.OrdinalIgnoreCase);

        var updatedOrder = await context.Orders.FindAsync(1);
        Assert.Equal("Cancelled", updatedOrder!.Status);

        var product = await context.Products.FindAsync(1);
        Assert.Equal(10, product!.StockQuantity);
    }

    [Fact]
    public async Task ProcessOrderPaymentSuccess_ShippedOrder_IsIdempotentAndPreservesStatus()
    {
        // Arrange: Mark Order 1 as already Shipped
        using var context = new AppDbContext(_contextOptions);
        var order = await context.Orders.FindAsync(1);
        order!.Status = "Shipped";
        await context.SaveChangesAsync();

        var configMock = new Mock<IConfiguration>();
        var emailMock = new Mock<IEmailService>();
        var webhookService = new WebhookService(
            context,
            configMock.Object,
            emailMock.Object,
            NullLogger<WebhookService>.Instance);

        // Act: Duplicate webhook delivery
        var result = await webhookService.ProcessOrderPaymentSuccessAsync(1);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.IsIdempotentDuplicate);

        var updatedOrder = await context.Orders.FindAsync(1);
        Assert.Equal("Shipped", updatedOrder!.Status);

        var product = await context.Products.FindAsync(1);
        Assert.Equal(10, product!.StockQuantity);
    }

    [Fact]
    public async Task ProcessWebhook_RealSecretWithoutSignatureHeader_IsRejected()
    {
        // Arrange
        using var context = new AppDbContext(_contextOptions);
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("whsec_real_live_test_secret_12345");
        var emailMock = new Mock<IEmailService>();

        var webhookService = new WebhookService(
            context,
            configMock.Object,
            emailMock.Object,
            NullLogger<WebhookService>.Instance);

        // Act: Attempt bypass by omitting Stripe-Signature header
        var result = await webhookService.ProcessWebhookAsync("{\"type\":\"checkout.session.completed\"}", null);

        // Assert (A6/A10 security)
        Assert.False(result.Success);
        Assert.Contains("Stripe-Signature", result.Message);
    }
}
