using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using NexusGear.Services.Interfaces;
using NexusGear.ViewModels;

namespace NexusGear.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/[action]")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IWebHostEnvironment _environment;

    public AdminController(IAdminService? adminService = null, IWebHostEnvironment? environment = null)
    {
        _adminService = adminService!;
        _environment = environment!;
    }

    [AllowAnonymous]
    [HttpGet("/Admin/Roadmap")]
    public IActionResult Roadmap()
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

    [HttpGet]
    [Route("/Admin")]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(string interval = "30days")
    {
        var model = await _adminService.GetDashboardMetricsAsync(interval);
        return View(model);
    }

    // ==========================================
    // Order Management (A9, A7 Restock)
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Orders(string? status = "All", string? search = null)
    {
        var model = await _adminService.GetAdminOrdersAsync(status, search);
        return View(model);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var model = await _adminService.GetAdminOrderDetailAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = $"Order #{id} not found.";
            return RedirectToAction(nameof(Orders));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, string newStatus)
    {
        var result = await _adminService.UpdateOrderStatusAsync(orderId, newStatus);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(OrderDetails), new { id = orderId });
    }

    // ==========================================
    // Product Management (A9 CRUD & Image Upload)
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Products(int? categoryId = null, string? search = null)
    {
        var model = await _adminService.GetAdminProductsAsync(categoryId, search);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProduct()
    {
        var model = await _adminService.PrepareCreateProductFormAsync();
        return View("ProductForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(AdminProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var freshForm = await _adminService.PrepareCreateProductFormAsync();
            model.AvailableCategories = freshForm.AvailableCategories;
            return View("ProductForm", model);
        }

        var result = await _adminService.CreateProductAsync(model, _environment.WebRootPath);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Products));
        }

        TempData["ErrorMessage"] = result.Message;
        var retryForm = await _adminService.PrepareCreateProductFormAsync();
        model.AvailableCategories = retryForm.AvailableCategories;
        return View("ProductForm", model);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> EditProduct(int id)
    {
        var model = await _adminService.PrepareEditProductFormAsync(id);
        if (model == null)
        {
            TempData["ErrorMessage"] = $"Product #{id} not found.";
            return RedirectToAction(nameof(Products));
        }

        return View("ProductForm", model);
    }

    [HttpPost]
    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(AdminProductFormViewModel model, int? id = null)
    {
        if (!model.ProductId.HasValue && id.HasValue)
        {
            model.ProductId = id.Value;
        }
        if (!ModelState.IsValid)
        {
            if (model.ProductId.HasValue)
            {
                var freshForm = await _adminService.PrepareEditProductFormAsync(model.ProductId.Value);
                if (freshForm != null)
                {
                    model.AvailableCategories = freshForm.AvailableCategories;
                    model.ExistingImageUrl = freshForm.ExistingImageUrl;
                }
            }
            return View("ProductForm", model);
        }

        var result = await _adminService.EditProductAsync(model, _environment.WebRootPath);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Products));
        }

        TempData["ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Products));
    }

    [HttpPost("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var result = await _adminService.DeleteProductAsync(id);
        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Products));
    }
}
