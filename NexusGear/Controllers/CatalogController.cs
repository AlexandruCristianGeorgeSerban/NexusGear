using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.ViewModels;

namespace NexusGear.Controllers;

public class CatalogController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(AppDbContext context, ILogger<CatalogController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Displays the paginated product catalog with combinable search and filters (A1, A2, B1).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? category,
        string? searchTerm,
        decimal? minPrice,
        decimal? maxPrice,
        bool inStockOnly = false,
        string sortOrder = "popular",
        int page = 1,
        int pageSize = 12)
    {
        // Enforce valid pagination boundaries
        if (page < 1) page = 1;
        if (pageSize is not (6 or 12 or 24 or 48)) pageSize = 12;

        // Base query with eager loading for category and active reviews
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Reviews.Where(r => !r.IsHidden))
            .AsQueryable();

        // 1. Category Filter (A1)
        Category? selectedCategory = null;
        if (!string.IsNullOrWhiteSpace(category))
        {
            selectedCategory = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug.ToLower() == category.Trim().ToLower());

            if (selectedCategory != null)
            {
                query = query.Where(p => p.CategoryId == selectedCategory.CategoryId);
            }
        }

        // 2. Case-Insensitive Search by Product Title & Category (A2)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var cleanTerm = searchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(cleanTerm) ||
                (p.Category != null && p.Category.Name.ToLower().Contains(cleanTerm)));
        }

        // 3. Price Range Filters (B1)
        if (minPrice.HasValue && minPrice.Value > 0)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // 4. In-Stock Only Filter (B1)
        if (inStockOnly)
        {
            query = query.Where(p => p.StockQuantity > 0);
        }

        // 5. Dynamic Sorting (B1)
        query = sortOrder switch
        {
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "name-asc" => query.OrderBy(p => p.Name),
            "popular" => query
                .OrderByDescending(p => p.Reviews.Count(r => !r.IsHidden))
                .ThenByDescending(p => p.ProductId),
            _ => query.OrderByDescending(p => p.ProductId)
        };

        // Total count after applying all filters
        var totalItems = await query.CountAsync();

        // Apply pagination
        var pagedProducts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductCardViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : "Hardware",
                CategorySlug = p.Category != null ? p.Category.Slug : "",
                ReviewCount = p.Reviews.Count(r => !r.IsHidden),
                AverageRating = p.Reviews.Where(r => !r.IsHidden).Any()
                    ? p.Reviews.Where(r => !r.IsHidden).Average(r => r.Rating)
                    : 0.0
            })
            .ToListAsync();

        // Load all categories with product counts for sidebar filtering
        var categories = await _context.Categories
            .AsNoTracking()
            .Select(c => new CategoryFilterItemViewModel
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Slug = c.Slug,
                ProductCount = c.Products.Count(),
                IsSelected = selectedCategory != null && c.CategoryId == selectedCategory.CategoryId
            })
            .ToListAsync();

        var viewModel = new CatalogIndexViewModel
        {
            Products = pagedProducts,
            Categories = categories,
            SelectedCategorySlug = selectedCategory?.Slug,
            SelectedCategoryName = selectedCategory?.Name,
            SearchTerm = searchTerm,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            InStockOnly = inStockOnly,
            SortOrder = sortOrder,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return View(viewModel);
    }

    /// <summary>
    /// Displays detailed information for a single product, including specifications,
    /// customer reviews, and dynamic recommendations (A1, B2, B3).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Reviews.Where(r => !r.IsHidden))
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound();
        }

        // Active customer reviews (B3)
        var activeReviews = product.Reviews
            .Where(r => !r.IsHidden)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var averageRating = activeReviews.Any()
            ? activeReviews.Average(r => r.Rating)
            : 0.0;

        var ratingCounts = new Dictionary<int, int>
        {
            { 5, activeReviews.Count(r => r.Rating == 5) },
            { 4, activeReviews.Count(r => r.Rating == 4) },
            { 3, activeReviews.Count(r => r.Rating == 3) },
            { 2, activeReviews.Count(r => r.Rating == 2) },
            { 1, activeReviews.Count(r => r.Rating == 1) }
        };

        var reviewItems = activeReviews.Select(r => new ReviewItemViewModel
        {
            ReviewId = r.ReviewId,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt,
            AuthorName = r.User != null ? r.User.FullName : "Verified Customer",
            IsVerifiedPurchase = true
        }).ToList();

        // Key Specifications based on product category
        var specifications = GenerateProductSpecifications(product);

        // =========================================================================
        // B2 Recommendations: Frequently Bought Together (Distinct Category Co-Occurrence)
        // =========================================================================
        var orderIdsWithCurrentProduct = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.ProductId == id)
            .Select(oi => oi.OrderId)
            .Distinct()
            .ToListAsync();

        var coProductIds = new List<int>();
        var usedCategories = new HashSet<int> { product.CategoryId };

        // 1. First preference: items co-occurring in actual orders from distinct other categories
        if (orderIdsWithCurrentProduct.Any())
        {
            var coOrderedProductIds = await _context.OrderItems
                .AsNoTracking()
                .Where(oi => orderIdsWithCurrentProduct.Contains(oi.OrderId) && oi.ProductId != id)
                .GroupBy(oi => oi.ProductId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToListAsync();

            var candidates = await _context.Products
                .AsNoTracking()
                .Where(p => coOrderedProductIds.Contains(p.ProductId))
                .Select(p => new { p.ProductId, p.CategoryId })
                .ToListAsync();

            foreach (var candId in coOrderedProductIds)
            {
                var cand = candidates.FirstOrDefault(p => p.ProductId == candId);
                if (cand != null && !usedCategories.Contains(cand.CategoryId))
                {
                    coProductIds.Add(cand.ProductId);
                    usedCategories.Add(cand.CategoryId);
                    if (coProductIds.Count == 2) break;
                }
            }
        }

        // 2. If fewer than 2 complementary items, pick top items from remaining unused categories
        if (coProductIds.Count < 2)
        {
            var allCategories = await _context.Categories
                .AsNoTracking()
                .Select(c => c.CategoryId)
                .ToListAsync();

            var remainingCategories = allCategories.Where(cId => !usedCategories.Contains(cId)).ToList();

            foreach (var catId in remainingCategories)
            {
                if (coProductIds.Count >= 2) break;

                var bestProductInCat = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.CategoryId == catId && p.ProductId != id && p.StockQuantity > 0)
                    .OrderByDescending(p => p.Price)
                    .Select(p => p.ProductId)
                    .FirstOrDefaultAsync();

                if (bestProductInCat > 0)
                {
                    coProductIds.Add(bestProductInCat);
                    usedCategories.Add(catId);
                }
            }
        }

        var frequentlyBoughtTogether = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Reviews.Where(r => !r.IsHidden))
            .Where(p => coProductIds.Contains(p.ProductId))
            .Select(p => new ProductCardViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : "Peripherals",
                CategorySlug = p.Category != null ? p.Category.Slug : "",
                ReviewCount = p.Reviews.Count(r => !r.IsHidden),
                AverageRating = p.Reviews.Where(r => !r.IsHidden).Any()
                    ? p.Reviews.Where(r => !r.IsHidden).Average(r => r.Rating)
                    : 0.0
            })
            .ToListAsync();

        // Calculate Bundle Total & Bundle Savings (5% discount on bundle)
        var bundleTotal = product.Price + frequentlyBoughtTogether.Sum(p => p.Price);
        var bundleSavings = Math.Round(bundleTotal * 0.05m, 2);

        // =========================================================================
        // B2 Recommendations: Similar Products (Same Category)
        // =========================================================================
        var similarProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Reviews.Where(r => !r.IsHidden))
            .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id)
            .OrderByDescending(p => p.StockQuantity > 0)
            .ThenByDescending(p => p.CreatedAt)
            .Take(4)
            .Select(p => new ProductCardViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : "Hardware",
                CategorySlug = p.Category != null ? p.Category.Slug : "",
                ReviewCount = p.Reviews.Count(r => !r.IsHidden),
                AverageRating = p.Reviews.Where(r => !r.IsHidden).Any()
                    ? p.Reviews.Where(r => !r.IsHidden).Average(r => r.Rating)
                    : 0.0
            })
            .ToListAsync();

        var viewModel = new ProductDetailsViewModel
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            CategoryName = product.Category != null ? product.Category.Name : "Hardware",
            CategorySlug = product.Category != null ? product.Category.Slug : "",
            CreatedAt = product.CreatedAt,
            AverageRating = averageRating,
            ReviewCount = activeReviews.Count,
            RatingCounts = ratingCounts,
            Reviews = reviewItems,
            Specifications = specifications,
            FrequentlyBoughtTogether = frequentlyBoughtTogether,
            SimilarProducts = similarProducts,
            BundleTotalPrice = bundleTotal - bundleSavings,
            BundleSavings = bundleSavings
        };

        return View(viewModel);
    }

    /// <summary>
    /// Generates structured technical specifications tailored to the product line.
    /// </summary>
    private static Dictionary<string, string> GenerateProductSpecifications(Product product)
    {
        var specs = new Dictionary<string, string>();
        var slug = product.Category?.Slug ?? "";

        switch (slug)
        {
            case "mechanical-keyboards":
                specs["Form Factor"] = product.Name.Contains("60") ? "60% Compact Layout" :
                                      product.Name.Contains("75") ? "75% Exploded Layout" : "Tenkeyless (TKL)";
                specs["Mounting Style"] = "Silicone Gasket Mount with Poron foam layers";
                specs["Connectivity"] = "Tri-Mode (2.4GHz Wireless, Bluetooth 5.2, USB-C Wired)";
                specs["Switches"] = "Factory-Lubed Hot-Swappable 5-Pin Linear / Tactile";
                specs["Keycaps"] = "Cherry Profile Double-Shot PBT (Fade-Resistant)";
                specs["Polling Rate"] = "1000Hz (Wired / 2.4GHz)";
                specs["Battery Life"] = "Up to 240 hours (RGB off)";
                specs["Warranty"] = "2-Year Manufacturer Warranty";
                break;

            case "gaming-mice":
                specs["Sensor"] = "PixArt PAW3395 Next-Gen Optical Gaming Sensor";
                specs["Resolution"] = "26,000 DPI (Adjustable in 50 DPI steps)";
                specs["Polling Rate"] = product.Name.Contains("8K") ? "8000Hz Ultra-Low Latency" : "4000Hz High-Speed";
                specs["Weight"] = product.Description.Contains("42g") ? "42g Ultra-Lightweight Shell" : "49g Lightweight Ergonomic";
                specs["Switches"] = "Optical Micro-Switches (Rated for 100M Clicks)";
                specs["Skates"] = "100% Virgin Grade PTFE Rounded Glides";
                specs["Battery Life"] = "Up to 90 hours continuous gaming";
                specs["Warranty"] = "2-Year Manufacturer Warranty";
                break;

            case "deskmats-mousepads":
                specs["Dimensions"] = product.Name.Contains("900") || product.Description.Contains("900") ? "900 mm x 400 mm x 4 mm" : "480 mm x 400 mm x 4 mm";
                specs["Surface Material"] = product.Name.Contains("Glass") ? "Micro-Etched Silicate Tempered Glass" :
                                            product.Name.Contains("Cordura") ? "High-Speed Military Cordura Fabric" :
                                            "Micro-Weave High-Density Polyester";
                specs["Base Material"] = "Textured Natural Anti-Slip Cellular Rubber";
                specs["Stitching"] = "Sub-Surface Ultra-Fine Anti-Fray Precision Stitching";
                specs["Water Resistance"] = "Hydrophobic Nano-Coated Spill Guard";
                specs["Maintenance"] = "Easy to clean with damp cloth and mild soap";
                specs["Warranty"] = "1-Year Manufacturer Warranty";
                break;

            case "ergonomic-gaming-chairs":
                specs["Frame Construction"] = "2.0mm High-Tensile Cold-Rolled Steel Frame";
                specs["Upholstery"] = "Breathable Krall-Tex Elastomer Dual-Zone Mesh";
                specs["Lumbar Support"] = "Dynamic Adaptive Biomechanical Lumbar Matrix";
                specs["Armrests"] = "4D Directional (Height, Depth, Angle, Horizontal)";
                specs["Gas Lift"] = "Class-4 Heavy Duty SGS Certified Explosion-Proof";
                specs["Recline Range"] = "90° to 135° with Multi-Angle Tilt Lock";
                specs["Base & Casters"] = "350mm Solid Aluminum Star Base with 65mm PU Casters";
                specs["Max Weight Capacity"] = "150 kg (330 lbs)";
                specs["Warranty"] = "5-Year Structural Frame Warranty";
                break;

            default:
                specs["Brand"] = "NexusGear Original Hardware";
                specs["Build Quality"] = "Enthusiast Grade Precision Engineered";
                specs["Warranty"] = "2-Year Manufacturer Warranty";
                break;
        }

        return specs;
    }
}

