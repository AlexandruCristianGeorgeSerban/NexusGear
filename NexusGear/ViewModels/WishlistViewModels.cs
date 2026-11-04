namespace NexusGear.ViewModels;

public class WishlistItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsInStock => StockQuantity > 0;
    public DateTime AddedAt { get; set; }
}

public class WishlistViewModel
{
    public List<WishlistItemViewModel> Items { get; set; } = new();
    public int TotalCount => Items.Count;
    public bool IsEmpty => Items.Count == 0;
}

public class WishlistToggleResult
{
    public bool Success { get; set; }
    public bool IsAdded { get; set; }
    public int WishlistCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class WishlistMoveResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int WishlistCount { get; set; }
    public int CartCount { get; set; }
}
