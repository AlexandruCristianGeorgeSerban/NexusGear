using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.ViewModels;
using NexusGear.Services;
using NexusGear.Services.Interfaces;

namespace NexusGear.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<AccountController> _logger;
    private readonly ICartService? _cartService;

    public AccountController(AppDbContext context, ILogger<AccountController> logger, ICartService? cartService = null)
    {
        _context = context;
        _logger = logger;
        _cartService = cartService;
    }

    // =========================================================================
    // 1. Registration (A4)
    // =========================================================================

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToLocal(returnUrl, fallbackAction: "Profile");
        }

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        // Ensure unique email
        var emailExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail);

        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists.");
            return View(model);
        }

        // Salted BCrypt password hash
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New user registered successfully: {Email} (ID: {UserId})", user.Email, user.UserId);

        // Automatically sign in the registered user
        await SignInUserAsync(user, isPersistent: true);

        if (_cartService != null)
        {
            await _cartService.MergeGuestCartIntoUserCartAsync(user.UserId);
        }

        TempData["SuccessMessage"] = $"Welcome to NexusGear, {user.FullName}! Your account has been registered.";
        return RedirectToLocal(model.ReturnUrl, fallbackAction: "Profile");
    }

    // =========================================================================
    // 2. Authentication / Login (A4)
    // =========================================================================

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToLocal(returnUrl, fallbackAction: "Profile");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        // Constant-time style verification with BCrypt
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid email address or password. Please try again.");
            return View(model);
        }

        await SignInUserAsync(user, model.RememberMe);

        if (_cartService != null)
        {
            await _cartService.MergeGuestCartIntoUserCartAsync(user.UserId);
        }

        _logger.LogInformation("User logged in successfully: {Email} (Role: {Role})", user.Email, user.Role);

        TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";
        return RedirectToLocal(model.ReturnUrl, fallbackAction: "Index", fallbackController: "Home");
    }

    // =========================================================================
    // 3. Logout (A4)
    // =========================================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();

        TempData["SuccessMessage"] = "You have been successfully signed out.";
        return RedirectToAction("Index", "Home");
    }

    // =========================================================================
    // 4. Access Denied (A4, A10)
    // =========================================================================

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // =========================================================================
    // 5. Customer Profile Dashboard & Address Book (A4)
    // =========================================================================

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Challenge();
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Addresses)
            .Include(u => u.Orders)
            .Include(u => u.Reviews)
            .Include(u => u.WishlistItems)
            .FirstOrDefaultAsync(u => u.UserId == userId.Value);

        if (user == null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        var viewModel = new ProfileViewModel
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            OrdersCount = user.Orders.Count,
            ReviewsCount = user.Reviews.Count(r => !r.IsHidden),
            WishlistCount = user.WishlistItems.Count,
            Addresses = user.Addresses
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.AddressId)
                .Select(a => new AddressItemViewModel
                {
                    AddressId = a.AddressId,
                    Street = a.Street,
                    City = a.City,
                    County = a.County,
                    PostalCode = a.PostalCode,
                    Phone = a.Phone,
                    IsDefault = a.IsDefault
                })
                .ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please provide a valid full name (2 to 100 characters).";
            return RedirectToAction("Profile");
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return NotFound();

        user.FullName = model.FullName.Trim();
        await _context.SaveChangesAsync();

        // Refresh sign-in cookie with the updated name
        await SignInUserAsync(user, isPersistent: true);

        TempData["SuccessMessage"] = "Profile information updated successfully.";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Password change failed. Please verify that new passwords match and contain at least 6 characters.";
            return RedirectToAction("Profile");
        }

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
        {
            TempData["ErrorMessage"] = "The current password you entered is incorrect.";
            return RedirectToAction("Profile");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed successfully for User ID {UserId}", user.UserId);

        TempData["SuccessMessage"] = "Your password has been changed successfully.";
        return RedirectToAction("Profile");
    }

    // =========================================================================
    // 6. Address Book CRUD (A4)
    // =========================================================================

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(AddressInputViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please fill in all required address fields with valid data.";
            return RedirectToAction("Profile");
        }

        var hasExistingAddresses = await _context.Addresses.AnyAsync(a => a.UserId == userId.Value);

        // If user has no existing addresses, this one must be default
        var shouldBeDefault = model.IsDefault || !hasExistingAddresses;

        if (shouldBeDefault)
        {
            var oldDefaults = await _context.Addresses
                .Where(a => a.UserId == userId.Value && a.IsDefault)
                .ToListAsync();

            foreach (var addr in oldDefaults)
            {
                addr.IsDefault = false;
            }
        }

        var address = new Address
        {
            Street = model.Street.Trim(),
            City = model.City.Trim(),
            County = model.County.Trim(),
            PostalCode = model.PostalCode.Trim(),
            Phone = model.Phone.Trim(),
            IsDefault = shouldBeDefault,
            UserId = userId.Value
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "New delivery address saved to your address book.";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var targetAddress = await _context.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId.Value);

        if (targetAddress == null)
        {
            TempData["ErrorMessage"] = "Address not found.";
            return RedirectToAction("Profile");
        }

        var userAddresses = await _context.Addresses
            .Where(a => a.UserId == userId.Value)
            .ToListAsync();

        foreach (var addr in userAddresses)
        {
            addr.IsDefault = (addr.AddressId == id);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Default delivery address updated.";
        return RedirectToAction("Profile");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId.Value);

        if (address == null)
        {
            TempData["ErrorMessage"] = "Address not found.";
            return RedirectToAction("Profile");
        }

        // Prevent deletion if address is linked to past orders (foreign key integrity)
        var hasOrders = await _context.Orders.AnyAsync(o => o.AddressId == id);
        if (hasOrders)
        {
            TempData["ErrorMessage"] = "Cannot remove this address because it is associated with past orders.";
            return RedirectToAction("Profile");
        }

        bool wasDefault = address.IsDefault;
        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync();

        // If the removed address was default, promote the first remaining address to default
        if (wasDefault)
        {
            var remaining = await _context.Addresses
                .Where(a => a.UserId == userId.Value)
                .OrderByDescending(a => a.AddressId)
                .FirstOrDefaultAsync();

            if (remaining != null)
            {
                remaining.IsDefault = true;
                await _context.SaveChangesAsync();
            }
        }

        TempData["SuccessMessage"] = "Address removed from your address book.";
        return RedirectToAction("Profile");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private async Task SignInUserAsync(User user, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(14) : null,
            IssuedUtc = DateTimeOffset.UtcNow
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out var id))
        {
            return id;
        }
        return null;
    }

    private IActionResult RedirectToLocal(string? returnUrl, string fallbackAction = "Index", string fallbackController = "Account")
    {
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            if (Url != null && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            if (returnUrl.StartsWith("/") && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
            {
                return Redirect(returnUrl);
            }
        }

        return RedirectToAction(fallbackAction, fallbackController);
    }
}
