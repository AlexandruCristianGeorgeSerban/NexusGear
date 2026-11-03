using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NexusGear.Controllers;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.ViewModels;
using Xunit;

namespace NexusGear.Tests;

public class AccountAndReviewTests
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
            Role = "Customer",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var user2 = new User
        {
            UserId = 2,
            Email = "second@nexusgear.ro",
            FullName = "Second Customer",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = "Customer",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        context.Users.AddRange(user, user2);

        var address1 = new Address
        {
            AddressId = 1,
            UserId = 1,
            Street = "Bulevardul Decebal Nr. 10",
            City = "Craiova",
            County = "Dolj",
            PostalCode = "200734",
            Phone = "+40 722 123 456",
            IsDefault = true
        };

        // Addresses for User 2 without orders (for deletion tests)
        var address2 = new Address
        {
            AddressId = 2,
            UserId = 2,
            Street = "Strada Primaverii Nr. 1",
            City = "Bucuresti",
            County = "Sector 1",
            PostalCode = "010001",
            Phone = "+40 733 111 222",
            IsDefault = true
        };

        var address3 = new Address
        {
            AddressId = 3,
            UserId = 2,
            Street = "Aleea Rozelor Nr. 5",
            City = "Craiova",
            County = "Dolj",
            PostalCode = "200100",
            Phone = "+40 744 333 444",
            IsDefault = false
        };

        context.Addresses.AddRange(address1, address2, address3);

        var category = new Category
        {
            CategoryId = 1,
            Name = "Mechanical Keyboards",
            Slug = "mechanical-keyboards"
        };
        context.Categories.Add(category);

        var product = new Product
        {
            ProductId = 1,
            Name = "NexusForge K-75 Horizon",
            Description = "75% custom mechanical keyboard",
            Price = 189.99m,
            StockQuantity = 10,
            CategoryId = 1,
            ImageUrl = "/images/products/Product_1.png"
        };
        context.Products.Add(product);

        // Seed an order for user 1 containing product 1 (User 1 is verified buyer for Product 1)
        var order = new Order
        {
            OrderId = 1,
            OrderNumber = "ORD-2026-0001",
            UserId = 1,
            AddressId = 1,
            Status = "Completed",
            TotalAmount = 189.99m,
            ShippingMethod = "Standard Courier",
            ShippingCost = 0m,
            OrderDate = DateTime.UtcNow.AddDays(-5),
            OrderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    OrderItemId = 1,
                    OrderId = 1,
                    ProductId = 1,
                    Quantity = 1,
                    UnitPrice = 189.99m
                }
            }
        };
        context.Orders.Add(order);

        context.SaveChanges();
    }

    private static (AccountController controller, Mock<IAuthenticationService> authMock) SetupAccountController(AppDbContext context, int? userId = null)
    {
        var loggerMock = new Mock<ILogger<AccountController>>();
        var controller = new AccountController(context, loggerMock.Object);

        var authServiceMock = new Mock<IAuthenticationService>();
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns((string url) => url != null && url.StartsWith("/") && !url.StartsWith("//"));
        urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("test-url");

        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(urlHelperMock.Object);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuthenticationService))).Returns(authServiceMock.Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProviderMock.Object
        };

        if (userId.HasValue)
        {
            var user = context.Users.Find(userId.Value);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(ClaimTypes.Name, user?.FullName ?? "Test User"),
                new Claim(ClaimTypes.Email, user?.Email ?? "test@nexusgear.ro"),
                new Claim(ClaimTypes.Role, user?.Role ?? "Customer")
            };
            var identity = new ClaimsIdentity(claims, "CookieAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.Url = urlHelperMock.Object;
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        return (controller, authServiceMock);
    }

    private static ReviewsController SetupReviewsController(AppDbContext context, int userId)
    {
        var loggerMock = new Mock<ILogger<ReviewsController>>();
        var controller = new ReviewsController(context, loggerMock.Object);

        var httpContext = new DefaultHttpContext();
        var user = context.Users.Find(userId);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, user?.FullName ?? "Test User"),
            new Claim(ClaimTypes.Email, user?.Email ?? "test@nexusgear.ro"),
            new Claim(ClaimTypes.Role, user?.Role ?? "Customer")
        };
        var identity = new ClaimsIdentity(claims, "CookieAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        return controller;
    }

    // =========================================================================
    // Account Registration Tests (A4)
    // =========================================================================

    [Fact]
    public async Task Register_CreatesUserWithBcryptPassword_AndDefaultCustomerRole()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Register_CreatesUserWithBcryptPassword_AndDefaultCustomerRole));
        var (controller, _) = SetupAccountController(context);

        var model = new RegisterViewModel
        {
            FullName = "New Enthusiast",
            Email = "newbie@nexusgear.ro",
            Password = "SecurePassword123!",
            ConfirmPassword = "SecurePassword123!",
            AcceptTerms = true
        };

        // Act
        var result = await controller.Register(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", redirectResult.ActionName);

        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "newbie@nexusgear.ro");
        Assert.NotNull(savedUser);
        Assert.Equal("New Enthusiast", savedUser.FullName);
        Assert.Equal("Customer", savedUser.Role);
        // Verify BCrypt hash verification works
        Assert.True(BCrypt.Net.BCrypt.Verify("SecurePassword123!", savedUser.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsValidationError()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Register_DuplicateEmail_ReturnsValidationError));
        var (controller, _) = SetupAccountController(context);

        var model = new RegisterViewModel
        {
            FullName = "Imposter Customer",
            Email = "customer@nexusgear.ro", // Already seeded
            Password = "Password999!",
            ConfirmPassword = "Password999!",
            AcceptTerms = true
        };

        // Act
        var result = await controller.Register(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("Email"));
    }

    // =========================================================================
    // Account Authentication Tests (A4)
    // =========================================================================

    [Fact]
    public async Task Login_ValidCredentials_SignsInUserSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Login_ValidCredentials_SignsInUserSuccessfully));
        var (controller, _) = SetupAccountController(context);

        var model = new LoginViewModel
        {
            Email = "customer@nexusgear.ro",
            Password = "Customer123!",
            RememberMe = true
        };

        // Act
        var result = await controller.Login(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
        Assert.True(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_InvalidPassword_FailsAuthentication()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(Login_InvalidPassword_FailsAuthentication));
        var (controller, _) = SetupAccountController(context);

        var model = new LoginViewModel
        {
            Email = "customer@nexusgear.ro",
            Password = "WrongPassword999!"
        };

        // Act
        var result = await controller.Login(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    // =========================================================================
    // Password Change Tests (A4)
    // =========================================================================

    [Fact]
    public async Task ChangePassword_ValidCurrentPassword_UpdatesBcryptHash()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(ChangePassword_ValidCurrentPassword_UpdatesBcryptHash));
        var (controller, _) = SetupAccountController(context, userId: 1);

        var model = new ChangePasswordViewModel
        {
            CurrentPassword = "Customer123!",
            NewPassword = "BrandNewPassword2026!",
            ConfirmNewPassword = "BrandNewPassword2026!"
        };

        // Act
        var result = await controller.ChangePassword(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", redirectResult.ActionName);

        var updatedUser = await context.Users.FindAsync(1);
        Assert.NotNull(updatedUser);
        Assert.True(BCrypt.Net.BCrypt.Verify("BrandNewPassword2026!", updatedUser.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("Customer123!", updatedUser.PasswordHash));
    }

    // =========================================================================
    // Address Book Management Tests (A4)
    // =========================================================================

    [Fact]
    public async Task AddressBook_AddAddress_WithDefaultFlag_UnsetsPreviousDefault()
    {
        // Arrange
        using var context = CreateInMemoryDbContext(nameof(AddressBook_AddAddress_WithDefaultFlag_UnsetsPreviousDefault));
        var (controller, _) = SetupAccountController(context, userId: 1);

        var newAddress = new AddressInputViewModel
        {
            Street = "Strada Victoriei Nr. 45",
            City = "Bucuresti",
            County = "Sector 1",
            PostalCode = "010071",
            Phone = "+40 733 999 888",
            IsDefault = true
        };

        // Act
        var result = await controller.AddAddress(newAddress);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", redirectResult.ActionName);

        var userAddresses = await context.Addresses.Where(a => a.UserId == 1).ToListAsync();
        Assert.Equal(2, userAddresses.Count);

        var oldAddress = userAddresses.First(a => a.AddressId == 1);
        var addedAddress = userAddresses.First(a => a.Street == "Strada Victoriei Nr. 45");

        Assert.False(oldAddress.IsDefault);
        Assert.True(addedAddress.IsDefault);
    }

    [Fact]
    public async Task AddressBook_DeleteDefaultAddress_PromotesRemainingAddress()
    {
        // Arrange
        // User 2 has Address 2 (default) and Address 3 (non-default), with no orders
        using var context = CreateInMemoryDbContext(nameof(AddressBook_DeleteDefaultAddress_PromotesRemainingAddress));
        var (controller, _) = SetupAccountController(context, userId: 2);

        // Act - Delete address 2 (which was the default for User 2)
        var result = await controller.DeleteAddress(2);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", redirectResult.ActionName);

        var remaining = await context.Addresses.FirstOrDefaultAsync(a => a.UserId == 2);
        Assert.NotNull(remaining);
        Assert.Equal(3, remaining.AddressId);
        Assert.True(remaining.IsDefault); // Promoted to default
    }

    [Fact]
    public async Task AddressBook_DeleteAddress_WithAssociatedOrders_IsBlocked()
    {
        // Arrange
        // User 1 has Address 1 associated with Order 1
        using var context = CreateInMemoryDbContext(nameof(AddressBook_DeleteAddress_WithAssociatedOrders_IsBlocked));
        var (controller, _) = SetupAccountController(context, userId: 1);

        // Act - Try to delete Address 1 which has existing orders
        var result = await controller.DeleteAddress(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Profile", redirectResult.ActionName);

        // Verify Address 1 was NOT deleted
        var addressStillExists = await context.Addresses.AnyAsync(a => a.AddressId == 1);
        Assert.True(addressStillExists);
        Assert.Equal("Cannot remove this address because it is associated with past orders.", controller.TempData["ErrorMessage"]);
    }

    // =========================================================================
    // Verified Customer Reviews Gate Tests (B3)
    // =========================================================================

    [Fact]
    public async Task Review_VerifiedBuyer_CanSubmitReview()
    {
        // Arrange
        // User 1 has completed Order 1 for Product 1 (seeded in DbContext)
        using var context = CreateInMemoryDbContext(nameof(Review_VerifiedBuyer_CanSubmitReview));
        var controller = SetupReviewsController(context, userId: 1);

        var model = new ReviewSubmissionViewModel
        {
            ProductId = 1,
            Rating = 5,
            Comment = "Phenomenal linear switches and gasket mounting!"
        };

        // Act
        var result = await controller.Submit(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("/Catalog/Details/1#reviews", redirectResult.Url);

        var savedReview = await context.Reviews
            .FirstOrDefaultAsync(r => r.UserId == 1 && r.ProductId == 1);

        Assert.NotNull(savedReview);
        Assert.Equal(5, savedReview.Rating);
        Assert.Equal("Phenomenal linear switches and gasket mounting!", savedReview.Comment);
        Assert.False(savedReview.IsHidden);
    }

    [Fact]
    public async Task Review_UnverifiedBuyer_IsBlockedFromSubmitting()
    {
        // Arrange
        // User 2 has NEVER purchased Product 1
        using var context = CreateInMemoryDbContext(nameof(Review_UnverifiedBuyer_IsBlockedFromSubmitting));
        var controller = SetupReviewsController(context, userId: 2);

        var model = new ReviewSubmissionViewModel
        {
            ProductId = 1,
            Rating = 1,
            Comment = "I have never ordered this, testing unauthorized review submission."
        };

        // Act
        var result = await controller.Submit(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("/Catalog/Details/1#reviews", redirectResult.Url);

        // Verify review was NOT saved to the database
        var reviewExists = await context.Reviews
            .AnyAsync(r => r.UserId == 2 && r.ProductId == 1);

        Assert.False(reviewExists);
        Assert.Equal("Only verified buyers who have purchased this product can leave a review.", controller.TempData["ErrorMessage"]);
    }
}
