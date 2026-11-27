using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NexusGear.ViewModels;

public class AdminDashboardViewModel
{
    // Lifetime / Global Platform Metrics
    public decimal TotalRevenue { get; set; }
    public int TotalOrdersCount { get; set; }
    public int TotalProductsCount { get; set; }
    public int LowStockProductsCount { get; set; }
    public int PendingOrdersCount { get; set; }

    // B4: Time Interval Selection & Sales Analytics
    public string SelectedInterval { get; set; } = "30days"; // "7days", "30days", "all"
    public decimal IntervalRevenue { get; set; }
    public int IntervalOrdersCount { get; set; }
    public decimal AverageOrderValue { get; set; } // AOV (IntervalRevenue / IntervalOrdersCount)
    public List<DailySalesPointViewModel> DailySalesChart { get; set; } = new();
    public List<AdminTopProductViewModel> TopSellingProducts { get; set; } = new(); // Top 5 bestsellers

    public List<AdminOrderRowViewModel> RecentOrders { get; set; } = new();
    public List<AdminLowStockProductViewModel> LowStockProducts { get; set; } = new();
}

public class DailySalesPointViewModel
{
    public string DateLabel { get; set; } = string.Empty; // e.g. "14 Nov"
    public string FullDate { get; set; } = string.Empty;  // e.g. "2026-11-14"
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class AdminTopProductViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int UnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int StockQuantity { get; set; }
}

public class AdminLowStockProductViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}

public class AdminOrderListViewModel
{
    public string StatusFilter { get; set; } = "All";
    public string? SearchTerm { get; set; }
    public List<AdminOrderRowViewModel> Orders { get; set; } = new();
    public int TotalCount { get; set; }
}

public class AdminOrderRowViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = "Unpaid";
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public string ShippingMethod { get; set; } = string.Empty;
}

public class AdminOrderDetailViewModel : OrderDetailsViewModel
{
    public List<string> AllowedTransitions { get; set; } = new();
}

public class AdminProductListViewModel
{
    public int? CategoryId { get; set; }
    public string? SearchTerm { get; set; }
    public List<AdminProductRowViewModel> Products { get; set; } = new();
    public List<CategorySelectItem> Categories { get; set; } = new();
}

public class AdminProductRowViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string StockStatus => StockQuantity switch
    {
        0 => "Out of Stock",
        <= 5 => "Low Stock",
        _ => "In Stock"
    };
}

public class CategorySelectItem
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AdminProductFormViewModel
{
    public int? ProductId { get; set; }

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, 100000.00, ErrorMessage = "Price must be between 0.01 and 100,000.00 RON.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Stock quantity is required.")]
    [Range(0, 100000, ErrorMessage = "Stock quantity cannot be negative.")]
    public int StockQuantity { get; set; }

    [Required(ErrorMessage = "Category selection is required.")]
    public int CategoryId { get; set; }

    public string? ExistingImageUrl { get; set; }

    public IFormFile? ImageFile { get; set; }

    public List<CategorySelectItem> AvailableCategories { get; set; } = new();
}

public class AdminCategoryListViewModel
{
    public List<AdminCategoryRowViewModel> Categories { get; set; } = new();
    public int TotalCategoriesCount => Categories.Count;
}

public class AdminCategoryRowViewModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
}

public class AdminCategoryFormViewModel
{
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Slug cannot exceed 100 characters.")]
    [RegularExpression(@"^[a-z0-9\-]*$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens.")]
    public string? Slug { get; set; }

    public string? Description { get; set; }

    public int AssociatedProductCount { get; set; }
}

public class AdminReviewListViewModel
{
    public List<AdminReviewRowViewModel> Reviews { get; set; } = new();
    public int TotalReviewsCount => Reviews.Count;
    public int VisibleReviewsCount => Reviews.Count(r => !r.IsHidden);
    public int HiddenReviewsCount => Reviews.Count(r => r.IsHidden);
    public string Filter { get; set; } = "All"; // "All", "Visible", "Hidden"
    public string? SearchTerm { get; set; }
}

public class AdminReviewRowViewModel
{
    public int ReviewId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImageUrl { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsHidden { get; set; }
}

// ==========================================
// Discount Codes Management ViewModels (B5 Admin CRUD)
// ==========================================

public class AdminDiscountListViewModel
{
    public List<AdminDiscountRowViewModel> Discounts { get; set; } = new();
    public string Filter { get; set; } = "All"; // "All", "Active", "Expired", "Exhausted"
    public string? SearchTerm { get; set; }
    public int TotalDiscountsCount { get; set; }
    public int ActiveDiscountsCount { get; set; }
    public int ExpiredDiscountsCount { get; set; }
}

public class AdminDiscountRowViewModel
{
    public int DiscountCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int MaxUses { get; set; }
    public int UsedCount { get; set; }
    public int OrdersCount { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiryDate;
    public bool IsExhausted => UsedCount >= MaxUses;
    public bool IsActive => !IsExpired && !IsExhausted;
    public string StatusLabel => IsExpired ? "Expired" : IsExhausted ? "Exhausted" : "Active";
}

public class AdminDiscountFormViewModel : IValidatableObject
{
    public int? DiscountCodeId { get; set; }

    [Required(ErrorMessage = "Promo code is required.")]
    [StringLength(50, ErrorMessage = "Promo code cannot exceed 50 characters.")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Promo code can only contain letters, numbers, underscores, and hyphens.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Discount type is required.")]
    public string DiscountType { get; set; } = "Percentage";

    [Required(ErrorMessage = "Discount value is required.")]
    [Range(0.01, 100000.00, ErrorMessage = "Discount value must be greater than zero.")]
    public decimal DiscountValue { get; set; }

    [Required(ErrorMessage = "Expiry date is required.")]
    [DataType(DataType.Date)]
    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddMonths(3).Date;

    [Required(ErrorMessage = "Maximum uses limit is required.")]
    [Range(1, 1000000, ErrorMessage = "Maximum uses must be at least 1.")]
    public int MaxUses { get; set; } = 100;

    public int UsedCount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.Equals(DiscountType, "Percentage", StringComparison.Ordinal) &&
            !string.Equals(DiscountType, "FixedAmount", StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "Discount type must be either 'Percentage' or 'FixedAmount'.",
                new[] { nameof(DiscountType) });
        }

        if (string.Equals(DiscountType, "Percentage", StringComparison.Ordinal) &&
            (DiscountValue <= 0 || DiscountValue > 100))
        {
            yield return new ValidationResult(
                "Percentage discount value must be between 0.01% and 100%.",
                new[] { nameof(DiscountValue) });
        }
    }
}
