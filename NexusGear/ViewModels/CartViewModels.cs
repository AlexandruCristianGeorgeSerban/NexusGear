namespace NexusGear.ViewModels;

public class CartItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int MaxAvailableStock { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
    public bool IsInStock => MaxAvailableStock > 0;
    public bool IsQuantityAvailable => Quantity <= MaxAvailableStock;
}

public class CartSummaryViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();

    public decimal Subtotal => Items.Sum(i => i.LineTotal);

    public const decimal FreeShippingThreshold = 300.00m;
    public const decimal StandardShippingFee = 15.00m;

    public decimal ShippingFee => (Subtotal >= FreeShippingThreshold || Items.Count == 0) ? 0.00m : StandardShippingFee;

    public decimal AmountNeededForFreeShipping => Math.Max(0.00m, FreeShippingThreshold - Subtotal);

    public int FreeShippingProgressPercentage
    {
        get
        {
            if (Subtotal <= 0) return 0;
            if (Subtotal >= FreeShippingThreshold) return 100;
            return (int)Math.Round((Subtotal / FreeShippingThreshold) * 100);
        }
    }

    /// <summary>
    /// Estimated VAT breakdown (19% Romanian standard included in retail price).
    /// Formula: Subtotal - (Subtotal / 1.19)
    /// </summary>
    public decimal EstimatedVat => Math.Round(Subtotal - (Subtotal / 1.19m), 2);

    public decimal GrandTotal => Subtotal + ShippingFee;

    public int TotalItemsCount => Items.Sum(i => i.Quantity);

    public bool IsEmpty => Items.Count == 0;

    public bool HasStockIssues => Items.Any(i => !i.IsQuantityAvailable || !i.IsInStock);
}

public class CartActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalCartCount { get; set; }
    public int CartCount => TotalCartCount;
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal LineTotal { get; set; }
    public int UpdatedQuantity { get; set; }
    public bool IsEmpty { get; set; }
}
