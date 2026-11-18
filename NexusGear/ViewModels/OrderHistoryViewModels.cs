namespace NexusGear.ViewModels;

public class OrderHistoryListViewModel
{
    public string CurrentFilter { get; set; } = "All";
    public List<OrderSummaryViewModel> Orders { get; set; } = new();
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
}

public class OrderSummaryViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Unpaid";
    public string ShippingMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public List<OrderItemSummaryViewModel> Items { get; set; } = new();
}

public class OrderItemSummaryViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class OrderDetailsViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Unpaid";
    public string? StripeSessionId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public string ShippingStreet { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingCounty { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;

    public string ShippingMethod { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public string? DiscountCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }

    public List<OrderItemDetailViewModel> Items { get; set; } = new();

    public bool IsStepCompleted(string step) => step switch
    {
        "Ordered" => true,
        "Paid" => Status is "Paid" or "Shipped" or "Delivered",
        "Shipped" => Status is "Shipped" or "Delivered",
        "Delivered" => Status is "Delivered",
        _ => false
    };

    public bool IsCancelled => Status == "Cancelled";
}

public class OrderItemDetailViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
    public bool CanReview { get; set; }
}
