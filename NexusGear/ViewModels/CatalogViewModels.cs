namespace NexusGear.ViewModels;

public class CatalogIndexViewModel
{
    public List<ProductCardViewModel> Products { get; set; } = new();
    public List<CategoryFilterItemViewModel> Categories { get; set; } = new();

    // Active Filter Parameters (A1, A2, B1)
    public string? SelectedCategorySlug { get; set; }
    public string? SelectedCategoryName { get; set; }
    public string? SearchTerm { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; }
    public string SortOrder { get; set; } = "popular";

    // Pagination (A1)
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / (PageSize > 0 ? PageSize : 12));
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    // Helper: checks if any filter is currently applied
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SelectedCategorySlug) ||
        !string.IsNullOrWhiteSpace(SearchTerm) ||
        MinPrice.HasValue ||
        MaxPrice.HasValue ||
        InStockOnly ||
        (SortOrder != "popular" && !string.IsNullOrWhiteSpace(SortOrder));

    // Helper to generate route dictionary preserving all active query parameters
    public Dictionary<string, string> GetRouteData(int? targetPage = null)
    {
        var dict = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(SelectedCategorySlug))
            dict["category"] = SelectedCategorySlug;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
            dict["searchTerm"] = SearchTerm;

        if (MinPrice.HasValue)
            dict["minPrice"] = MinPrice.Value.ToString("0.##");

        if (MaxPrice.HasValue)
            dict["maxPrice"] = MaxPrice.Value.ToString("0.##");

        if (InStockOnly)
            dict["inStockOnly"] = "true";

        if (!string.IsNullOrWhiteSpace(SortOrder) && SortOrder != "popular")
            dict["sortOrder"] = SortOrder;

        if (PageSize != 12)
            dict["pageSize"] = PageSize.ToString();

        dict["page"] = (targetPage ?? CurrentPage).ToString();

        return dict;
    }

    // Helper to generate route dictionary with one specific filter removed (for active filter badges)
    public Dictionary<string, string> GetRouteWithout(string filterKey)
    {
        var dict = GetRouteData(targetPage: 1);
        dict.Remove(filterKey);
        if (filterKey == "price")
        {
            dict.Remove("minPrice");
            dict.Remove("maxPrice");
        }
        return dict;
    }
}

public class CategoryFilterItemViewModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public bool IsSelected { get; set; }
}

public class ProductCardViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public bool IsInStock => StockQuantity > 0;
    public bool IsLowStock => StockQuantity > 0 && StockQuantity <= 5;
    public string StockLabel => StockQuantity switch
    {
        0 => "Out of Stock",
        <= 5 => $"Only {StockQuantity} left!",
        _ => $"In Stock ({StockQuantity})"
    };
}

public class ProductDetailsViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Rating & Reviews (B3)
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public Dictionary<int, int> RatingCounts { get; set; } = new()
    {
        { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 }
    };
    public List<ReviewItemViewModel> Reviews { get; set; } = new();

    // B3 Verified Buyer Gate
    public bool IsUserAuthenticated { get; set; }
    public bool CanUserReview { get; set; }
    public bool HasUserReviewed { get; set; }
    public ReviewItemViewModel? CurrentUserReview { get; set; }

    // Stock Helpers
    public bool IsInStock => StockQuantity > 0;
    public bool IsLowStock => StockQuantity > 0 && StockQuantity <= 5;
    public string StockBadgeClass => StockQuantity switch
    {
        0 => "stock-out",
        <= 5 => "stock-low",
        _ => "stock-in"
    };

    // Parsed Key Specifications
    public Dictionary<string, string> Specifications { get; set; } = new();

    // B2: Recommendations
    public List<ProductCardViewModel> FrequentlyBoughtTogether { get; set; } = new();
    public List<ProductCardViewModel> SimilarProducts { get; set; } = new();
    public decimal BundleTotalPrice { get; set; }
    public decimal BundleSavings { get; set; }
}

public class ReviewItemViewModel
{
    public int ReviewId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; } = true;
}
