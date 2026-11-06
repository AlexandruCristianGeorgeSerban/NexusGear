using Microsoft.AspNetCore.Mvc;
using NexusGear.Services;
using NexusGear.Services.Interfaces;
using NexusGear.ViewModels;

namespace NexusGear.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // =========================================================================
    // GET: /Cart
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync();
        return View(cart);
    }

    // =========================================================================
    // POST: /Cart/Add
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
    {
        var result = await _cartService.AddToCartAsync(productId, quantity);

        if (IsAjaxRequest())
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

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // POST: /Cart/Update
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        var result = await _cartService.UpdateQuantityAsync(productId, quantity);

        if (IsAjaxRequest())
        {
            return Json(result);
        }

        if (!result.Success && !string.IsNullOrEmpty(result.Message))
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // POST: /Cart/Remove
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        var result = await _cartService.RemoveFromCartAsync(productId);

        if (IsAjaxRequest())
        {
            return Json(result);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // POST: /Cart/Clear
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        await _cartService.ClearCartAsync();

        if (IsAjaxRequest())
        {
            return Json(new { success = true, message = "Cart cleared.", totalCartCount = 0 });
        }

        TempData["SuccessMessage"] = "All items have been removed from your shopping cart.";
        return RedirectToAction(nameof(Index));
    }

    // =========================================================================
    // GET: /Cart/Count (Live badge counter)
    // =========================================================================
    [HttpGet]
    public async Task<IActionResult> Count()
    {
        int count = await _cartService.GetCartItemCountAsync();
        return Json(new { count });
    }

    // =========================================================================
    // Helper
    // =========================================================================
    private bool IsAjaxRequest()
    {
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
               (Request.ContentType != null && Request.ContentType.Contains("application/json"));
    }
}
