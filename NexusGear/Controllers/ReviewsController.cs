using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.ViewModels;

namespace NexusGear.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(AppDbContext context, ILogger<ReviewsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Handles submission or editing of customer reviews with verified buyer verification (B3).
    /// Only customers who purchased the product in a non-cancelled order can submit a review.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ReviewSubmissionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please provide both a valid 1-5 star rating and your feedback comments.";
            return Redirect($"/Catalog/Details/{model.ProductId}#reviews");
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Challenge();
        }

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == model.ProductId);

        if (product == null)
        {
            return NotFound();
        }

        // B3: Verified Purchase Enforcement
        // Query if this user has any non-cancelled order containing this product
        var isVerifiedBuyer = await _context.Orders
            .AsNoTracking()
            .AnyAsync(o => o.UserId == userId &&
                           o.Status != "Cancelled" &&
                           o.OrderItems.Any(oi => oi.ProductId == model.ProductId));

        if (!isVerifiedBuyer)
        {
            _logger.LogWarning("User {UserId} attempted to submit a review for Product {ProductId} without a verified purchase.", userId, model.ProductId);
            TempData["ErrorMessage"] = "Only verified buyers who have purchased this product can leave a review.";
            return Redirect($"/Catalog/Details/{model.ProductId}#reviews");
        }

        // Check if an existing review is being updated or a new one created
        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == model.ProductId);

        if (existingReview != null)
        {
            existingReview.Rating = model.Rating;
            existingReview.Comment = model.Comment.Trim();
            existingReview.CreatedAt = DateTime.UtcNow;
            existingReview.IsHidden = false; // Verified reviews are visible
            TempData["SuccessMessage"] = "Your verified review has been successfully updated!";
        }
        else
        {
            var newReview = new Review
            {
                UserId = userId,
                ProductId = model.ProductId,
                Rating = model.Rating,
                Comment = model.Comment.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsHidden = false
            };

            _context.Reviews.Add(newReview);
            TempData["SuccessMessage"] = "Thank you! Your verified customer review has been published.";
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Verified review saved for Product {ProductId} by User {UserId}", model.ProductId, userId);

        return Redirect($"/Catalog/Details/{model.ProductId}#reviews");
    }
}

