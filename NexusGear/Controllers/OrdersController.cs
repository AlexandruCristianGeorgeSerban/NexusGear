using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusGear.Services.Interfaces;
using NexusGear.ViewModels;

namespace NexusGear.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService? orderService = null)
    {
        _orderService = orderService!;
    }

    [AllowAnonymous]
    [HttpGet("Orders/Roadmap")]
    public IActionResult Roadmap()
    {
        var model = new RoadmapPreviewViewModel
        {
            ModuleTitle = "Customer Order History & Tracking",
            Subtitle = "Dedicated dashboard allowing authenticated customers to track real-time delivery status, review historical purchases, inspect line-item breakdowns, and access verified product review gates.",
            StageBadge = "Stage 3 (Week 10)",
            IconClass = "bi-box-seam-fill",
            RequirementTags = new List<string>
            {
                "A8: Customer Order History & Status Tracking",
                "A4: Customer Account Security & Isolation",
                "B3: Verified Buyer Review Eligibility"
            },
            PlannedFeatures = new List<string>
            {
                "Comprehensive order ledger: unique OrderNumber, order timestamp, status badges (New, Paid, Shipped, Delivered, Cancelled)",
                "Full line-item breakdown: product thumbnails, purchased quantities, historical unit prices, shipping costs, and applied discounts",
                "Live delivery tracking indicators reflecting courier progression",
                "Direct shortcuts to leave verified customer reviews for items with confirmed Paid/Delivered status"
            },
            PrimaryActionUrl = "/Catalog",
            PrimaryActionText = "Explore Product Catalog",
            SecondaryActionUrl = "/Account/Profile",
            SecondaryActionText = "View Profile & Addresses"
        };

        return View("~/Views/Shared/RoadmapPreview.cshtml", model);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out int id))
        {
            return id;
        }
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status = "All")
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var model = await _orderService.GetUserOrderHistoryAsync(userId.Value, status);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var model = await _orderService.GetUserOrderDetailsAsync(id, userId.Value);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Order not found or you do not have permission to view it.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }
}
