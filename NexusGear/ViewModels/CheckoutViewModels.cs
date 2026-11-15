using System.ComponentModel.DataAnnotations;

namespace NexusGear.ViewModels;

public class CheckoutItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
    public int AvailableStock { get; set; }
}

public class CheckoutAddressViewModel
{
    public int AddressId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public string SingleLineSummary => $"{Street}, {City}, jud. {County} ({PostalCode}) — Tel: {Phone}";
}

public class ShippingOptionViewModel
{
    public string Method { get; set; } = "Standard Courier";
    public decimal Price { get; set; }
    public string EstimatedDelivery { get; set; } = string.Empty;
    public bool IsFree { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class DiscountValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? DiscountCodeId { get; set; }
    public string? Code { get; set; }
    public string? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal NewGrandTotal { get; set; }
}

public class CheckoutIndexViewModel
{
    public List<CheckoutItemViewModel> Items { get; set; } = new();
    public List<CheckoutAddressViewModel> SavedAddresses { get; set; } = new();
    public int SelectedAddressId { get; set; }

    // Optional inline new address form
    public AddressInputViewModel NewAddress { get; set; } = new();
    public bool UseNewAddress { get; set; } = false;

    [Required]
    public string SelectedShippingMethod { get; set; } = "Standard Courier";
    public List<ShippingOptionViewModel> ShippingOptions { get; set; } = new();

    public string? AppliedDiscountCode { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal EstimatedVat { get; set; }
    public bool FreeShippingUnlocked { get; set; }
    public decimal FreeShippingRemaining { get; set; }
}

public class PlaceOrderRequest
{
    public int? SelectedAddressId { get; set; }
    public bool UseNewAddress { get; set; } = false;
    public AddressInputViewModel? NewAddress { get; set; }

    [Required]
    public string ShippingMethod { get; set; } = "Standard Courier";

    public string? DiscountCode { get; set; }
}

public class OrderCreationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public string? OrderNumber { get; set; }
}

public class OrderConfirmationViewModel
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Unpaid";
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public string ShippingMethod { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }

    public string? DiscountCode { get; set; }
    public decimal DiscountAmount { get; set; }

    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal EstimatedVat => Math.Round(TotalAmount - (TotalAmount / 1.19m), 2);

    public List<CheckoutItemViewModel> Items { get; set; } = new();
}
