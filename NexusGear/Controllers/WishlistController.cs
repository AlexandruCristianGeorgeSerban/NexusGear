using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusGear.Services.Interfaces;

namespace NexusGear.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly IWishlistService _wishlistService;
    private readonly ILogger<WishlistController> _logger;

    public WishlistController(
        IWishlistService wishlistService,
        ILogger<WishlistController> logger)
    {
        _wishlistService = wishlistService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int userId = GetCurrentUserId();
        var model = await _wishlistService.GetWishlistAsync(userId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId)
    {
        int userId = GetCurrentUserId();
        var result = await _wishlistService.ToggleWishlistAsync(userId, productId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(result);
        }

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        int userId = GetCurrentUserId();
        bool success = await _wishlistService.RemoveFromWishlistAsync(userId, productId);
        int count = await _wishlistService.GetWishlistCountAsync(userId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new
            {
                success,
                wishlistCount = count,
                message = success ? "Item removed from your wishlist." : "Could not remove item from wishlist."
            });
        }

        if (success)
        {
            TempData["SuccessMessage"] = "Item removed from your wishlist.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not remove item from wishlist.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveToCart(int productId)
    {
        int userId = GetCurrentUserId();
        var result = await _wishlistService.MoveToCartAsync(userId, productId);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(result);
        }

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        int userId = GetCurrentUserId();
        await _wishlistService.ClearWishlistAsync(userId);
        TempData["SuccessMessage"] = "All items have been removed from your wishlist.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Count()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Json(new { count = 0 });
        }

        int userId = GetCurrentUserId();
        int count = await _wishlistService.GetWishlistCountAsync(userId);
        return Json(new { count });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claim, out var id))
        {
            return id;
        }
        throw new InvalidOperationException("User ID claim could not be retrieved from authenticated session.");
    }
}
