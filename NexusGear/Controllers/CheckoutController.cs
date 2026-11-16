using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexusGear.Services.Interfaces;
using NexusGear.ViewModels;

namespace NexusGear.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICheckoutService _checkoutService;
    private readonly IPaymentService _paymentService;
    private readonly IWebhookService _webhookService;

    public CheckoutController(
        ICheckoutService? checkoutService = null,
        IPaymentService? paymentService = null,
        IWebhookService? webhookService = null)
    {
        _checkoutService = checkoutService!;
        _paymentService = paymentService!;
        _webhookService = webhookService!;
    }

    private int? GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idClaim, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? shippingMethod, [FromQuery] string? coupon)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var model = await _checkoutService.PrepareCheckoutViewModelAsync(userId.Value, shippingMethod, coupon);

        if (model.Items.Count == 0)
        {
            TempData["ErrorMessage"] = "Your cart is empty. Add gaming gear before checking out.";
            return RedirectToAction("Index", "Cart");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyDiscount([FromBody] ApplyDiscountDto request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { isValid = false, message = "Please sign in to apply promo codes." });
        }

        if (string.IsNullOrWhiteSpace(request?.Code))
        {
            return BadRequest(new { isValid = false, message = "Please provide a promo code." });
        }

        var result = await _checkoutService.ApplyCouponAsync(userId.Value, request.Code, request.ShippingMethod ?? "Standard Courier");
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(PlaceOrderRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var result = await _checkoutService.PlaceOrderAsync(userId.Value, request);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index), new { shippingMethod = request.ShippingMethod, coupon = request.DiscountCode });
        }

        TempData["SuccessMessage"] = $"Order {result.OrderNumber} placed successfully!";
        return RedirectToAction(nameof(Confirmation), new { id = result.OrderId });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var model = await _checkoutService.GetOrderConfirmationAsync(id, userId.Value);
        if (model == null)
        {
            TempData["ErrorMessage"] = "Order not found or unauthorized.";
            return RedirectToAction("Index", "Home");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayWithStripe(int orderId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _paymentService.CreateCheckoutSessionAsync(orderId, userId.Value, baseUrl);

        if (!result.Success || string.IsNullOrEmpty(result.RedirectUrl))
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Confirmation), new { id = orderId });
        }

        return Redirect(result.RedirectUrl);
    }

    [HttpGet]
    public async Task<IActionResult> StripeSimulator(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var order = await _checkoutService.GetOrderConfirmationAsync(id, userId.Value);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToAction("Index", "Home");
        }

        if (order.Status == "Paid")
        {
            TempData["SuccessMessage"] = "Order is already paid!";
            return RedirectToAction(nameof(Confirmation), new { id = id });
        }

        var model = new StripeSimulatorViewModel
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            ShippingMethod = order.ShippingMethod,
            ShippingCost = order.ShippingCost,
            DiscountCode = order.DiscountCode,
            DiscountAmount = order.DiscountAmount,
            Items = order.Items
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulatePayment(int orderId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var result = await _webhookService.ProcessOrderPaymentSuccessAsync(orderId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Confirmation), new { id = orderId });
        }

        TempData["SuccessMessage"] = $"Payment for Order {result.OrderNumber} confirmed via Stripe sandbox webhook!";
        return RedirectToAction(nameof(Confirmation), new { id = orderId });
    }

    [AllowAnonymous]
    [HttpGet("Checkout/Roadmap")]
    public IActionResult Roadmap()
    {
        var model = new RoadmapPreviewViewModel
        {
            ModuleTitle = "Checkout & Stripe Sandbox Payments",
            Subtitle = "Multi-step checkout wizard with address confirmation, shipping method selection, server-side coupon validation, and asynchronous Stripe webhook settlement.",
            StageBadge = "Stage 3 (Week 8–9)",
            IconClass = "bi-credit-card-2-front-fill",
            RequirementTags = new List<string>
            {
                "A5: Multi-Step Checkout Flow",
                "A6: Stripe Sandbox & Verified Webhook",
                "A7: Concurrency-Safe Stock Management",
                "B5: Discount Codes Server Validation",
                "B6: Transactional Order Confirmation Email"
            },
            PlannedFeatures = new List<string>
            {
                "Step 1: Saved delivery address selection or new shipping address input",
                "Step 2: Shipping courier choice (Standard Courier 15 RON vs. Express Next-Day 35 RON)",
                "Step 3: Server-validated discount coupon engine (percentage or fixed amount, expiration, usage limits)",
                "Step 4: Real-time order review, tax summary, and redirect to Stripe Checkout sandbox session",
                "Step 5: Verified server-to-server webhook (/api/webhook/stripe) that updates order to Paid and locks stock atomically",
                "Step 6: HTML transactional confirmation receipt sent via Mailtrap SMTP"
            },
            PrimaryActionUrl = "/Cart",
            PrimaryActionText = "Return to Shopping Cart",
            SecondaryActionUrl = "/Catalog",
            SecondaryActionText = "Continue Shopping"
        };

        return View("~/Views/Shared/RoadmapPreview.cshtml", model);
    }
}

public class ApplyDiscountDto
{
    public string? Code { get; set; }
    public string? ShippingMethod { get; set; }
}
