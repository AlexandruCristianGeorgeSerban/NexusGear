namespace NexusGear.ViewModels;

public class StripeSessionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string? RedirectUrl { get; set; }
    public bool IsSimulator { get; set; }
}

public class WebhookProcessingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string? EventType { get; set; }
    public bool IsIdempotentDuplicate { get; set; }
}

public class StripeSimulatorViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ShippingMethod { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public string? DiscountCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public List<CheckoutItemViewModel> Items { get; set; } = new();
}
