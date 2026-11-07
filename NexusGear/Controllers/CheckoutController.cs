using Microsoft.AspNetCore.Mvc;
using NexusGear.ViewModels;

namespace NexusGear.Controllers;

public class CheckoutController : Controller
{
    [HttpGet]
    public IActionResult Index()
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

