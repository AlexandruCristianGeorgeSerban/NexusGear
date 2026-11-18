using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NexusGear.ViewModels;

public class AdminDashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrdersCount { get; set; }
    public int TotalProductsCount { get; set; }
    public int LowStockProductsCount { get; set; }
    public int PendingOrdersCount { get; set; }

    public List<AdminOrderRowViewModel> RecentOrders { get; set; } = new();
    public List<AdminLowStockProductViewModel> LowStockProducts { get; set; } = new();
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
