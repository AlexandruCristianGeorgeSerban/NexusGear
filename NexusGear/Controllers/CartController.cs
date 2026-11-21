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
    // POST: /Cart/AddBundle
    // =========================================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddBundle([FromForm] List<int> productIds, string? returnUrl = null)
    {
        if (productIds == null || !productIds.Any())
        {
            TempData["ErrorMessage"] = "No products were specified for the bundle.";
            return RedirectToAction(nameof(Index));
        }

        int addedCount = 0;
        var addedProductIds = new List<int>();
        foreach (var pid in productIds.Distinct())
        {
            var result = await _cartService.AddToCartAsync(pid, 1);
            if (result.Success)
            {
                addedCount++;
                addedProductIds.Add(pid);
            }
        }

        if (addedCount > 0)
        {
            if (addedProductIds.Count >= 2)
            {
                _cartService.RegisterBundle(addedProductIds);
            }
            TempData["SuccessMessage"] = $"Added {addedCount} bundle items to your shopping cart! 5% Hardware Bundle savings applied.";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not add bundle items to your cart (items may be out of stock).";
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
