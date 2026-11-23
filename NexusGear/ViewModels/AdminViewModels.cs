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
