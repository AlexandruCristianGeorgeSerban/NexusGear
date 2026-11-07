using Microsoft.AspNetCore.Mvc;
using NexusGear.ViewModels;

namespace NexusGear.Controllers;

public class AdminController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public IActionResult Dashboard()
    {
        var model = new RoadmapPreviewViewModel
        {
            ModuleTitle = "Custom Administration Panel & Analytics",
            Subtitle = "Secured administrative operations back-office with complete CRUD for catalog items, category hierarchies, image upload management, status progression workflows, and dynamic Chart.js analytics.",
            StageBadge = "Stage 3 (Week 10–11)",
            IconClass = "bi-speedometer2",
            RequirementTags = new List<string>
            {
                "A9: Custom Administration Panel (CRUD & Status Transitions)",
                "A10: Basic Security (Role Authorization & CSRF Protection)",
                "B3: Review Moderation (Approve / Hide)",
                "B4: Sales Analytics Dashboard (Chart.js, Top 5, AOV)",
                "B5: Discount Codes Management"
            },
            PlannedFeatures = new List<string>
            {
                "Product & Category management: full CRUD operations with image file upload validation to wwwroot/uploads/products/",
                "Order status workflow: filterable order queue with transition controls (New → Paid → Shipped → Delivered, or Cancelled with atomic restock)",
                "Sales analytics dashboard: interactive Chart.js revenue trendlines, top 5 bestsellers, average order value (AOV), and date interval filters",
                "Customer review moderation: inspect reported community reviews and toggle visibility (IsHidden)",
                "Discount code management: create and configure percentage or fixed vouchers with expiry dates and usage limits",
                "Server-side security: guarded strictly by [Authorize(Roles = \"Admin\")] and [ValidateAntiForgeryToken]"
            },
            PrimaryActionUrl = "/Catalog",
            PrimaryActionText = "Browse Live Storefront",
            SecondaryActionUrl = "/Account/Profile",
            SecondaryActionText = "My Account Profile"
        };

        return View("~/Views/Shared/RoadmapPreview.cshtml", model);
    }
}

