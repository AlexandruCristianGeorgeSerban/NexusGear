using Microsoft.AspNetCore.Mvc;
using NexusGear.ViewModels;

namespace NexusGear.Controllers;

public class OrdersController : Controller
{
    [HttpGet]
    public IActionResult Index()
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

    [HttpGet]
    public IActionResult Details(int id)
    {
        return RedirectToAction(nameof(Index));
    }
}

