using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories.Interfaces;
using NexusGear.Services.Interfaces;
using NexusGear.ViewModels;

namespace NexusGear.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly IRepositoryWrapper _repository;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        AppDbContext context,
        IRepositoryWrapper repository,
        ILogger<AdminService> logger)
    {
        _context = context;
        _repository = repository;
        _logger = logger;
    }

    public async Task<AdminDashboardViewModel> GetDashboardMetricsAsync(string interval = "30days")
    {
        // 1. Overall platform lifetime metrics
        var totalOrders = await _context.Orders.CountAsync();
        var totalRevenue = await _context.Orders
            .Where(o => o.Status == "Paid" || o.Status == "Shipped" || o.Status == "Delivered")
            .SumAsync(o => o.TotalAmount);

        var totalProducts = await _context.Products.CountAsync();
        var lowStockCount = await _context.Products.CountAsync(p => p.StockQuantity <= 5);
        var pendingOrdersCount = await _context.Orders.CountAsync(o => o.Status == "Paid" || o.Status == "Unpaid");

        // 2. Normalize Interval (B4)
        var normalizedInterval = (interval ?? "30days").ToLowerInvariant().Trim();
        if (normalizedInterval != "7days" && normalizedInterval != "30days" && normalizedInterval != "all")
        {
            normalizedInterval = "30days";
        }

        DateTime? intervalStartDate = normalizedInterval switch
        {
            "7days" => DateTime.UtcNow.Date.AddDays(-6),
            "30days" => DateTime.UtcNow.Date.AddDays(-29),
            _ => null
        };

        // Query completed orders for the selected interval
        var completedOrdersQuery = _context.Orders
            .Where(o => o.Status == "Paid" || o.Status == "Shipped" || o.Status == "Delivered");

        if (intervalStartDate.HasValue)
        {
            completedOrdersQuery = completedOrdersQuery.Where(o => o.OrderDate >= intervalStartDate.Value);
        }

        var completedOrders = await completedOrdersQuery
            .Include(o => o.OrderItems)
            .OrderBy(o => o.OrderDate)
            .ToListAsync();

        var intervalRevenue = completedOrders.Sum(o => o.TotalAmount);
        var intervalOrdersCount = completedOrders.Count;
        var averageOrderValue = intervalOrdersCount > 0
            ? Math.Round(intervalRevenue / intervalOrdersCount, 2)
            : 0m;

        // 3. Generate Daily Sales Chart Points (B4)
        var dailySalesChart = new List<DailySalesPointViewModel>();
        var ordersByDate = completedOrders
            .GroupBy(o => o.OrderDate.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var chartStartDate = intervalStartDate ?? (completedOrders.Any()
            ? completedOrders.Min(o => o.OrderDate).Date
            : DateTime.UtcNow.Date.AddDays(-29));
        var chartEndDate = DateTime.UtcNow.Date;

        if (chartStartDate > chartEndDate)
        {
            chartStartDate = chartEndDate.AddDays(-6);
        }
        else if ((chartEndDate - chartStartDate).TotalDays > 90)
        {
            chartStartDate = chartEndDate.AddDays(-89);
        }

        for (var date = chartStartDate; date <= chartEndDate; date = date.AddDays(1))
        {
            decimal dayRevenue = 0m;
            int dayOrdersCount = 0;

            if (ordersByDate.TryGetValue(date, out var dayOrders))
            {
                dayRevenue = dayOrders.Sum(o => o.TotalAmount);
                dayOrdersCount = dayOrders.Count;
            }

            dailySalesChart.Add(new DailySalesPointViewModel
            {
                DateLabel = date.ToString("dd MMM"),
                FullDate = date.ToString("yyyy-MM-dd"),
                Revenue = dayRevenue,
                OrderCount = dayOrdersCount
            });
        }

        // 4. Calculate Top 5 Selling Products (B4)
        var intervalItemGroups = completedOrders
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                UnitsSold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.UnitsSold)
            .ThenByDescending(x => x.TotalRevenue)
            .Take(5)
            .ToList();

        var topProductIds = intervalItemGroups.Select(x => x.ProductId).ToList();
        if (topProductIds.Count < 5 && normalizedInterval != "all")
        {
            var allTimeItems = await _context.OrderItems
                .Where(oi => oi.Order != null && (oi.Order.Status == "Paid" || oi.Order.Status == "Shipped" || oi.Order.Status == "Delivered"))
                .Where(oi => !topProductIds.Contains(oi.ProductId))
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    UnitsSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.UnitsSold)
                .ThenByDescending(x => x.TotalRevenue)
                .Take(5 - topProductIds.Count)
                .ToListAsync();

            intervalItemGroups.AddRange(allTimeItems);
        }

        var allProductIdsNeeded = intervalItemGroups.Select(x => x.ProductId).Distinct().ToList();
        var productEntities = await _context.Products
            .Include(p => p.Category)
            .Where(p => allProductIdsNeeded.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        var topSellingProducts = intervalItemGroups
            .Where(g => productEntities.ContainsKey(g.ProductId))
            .Select(g =>
            {
                var prod = productEntities[g.ProductId];
                return new AdminTopProductViewModel
                {
                    ProductId = prod.ProductId,
                    Name = prod.Name,
                    CategoryName = prod.Category != null ? prod.Category.Name : "Peripherals",
                    ImageUrl = prod.ImageUrl,
                    Price = prod.Price,
                    UnitsSold = g.UnitsSold,
                    TotalRevenue = g.TotalRevenue,
                    StockQuantity = prod.StockQuantity
                };
            })
            .OrderByDescending(p => p.UnitsSold)
            .ThenByDescending(p => p.TotalRevenue)
            .ToList();

        // 5. Recent Orders
        var recentOrders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .Take(5)
            .Select(o => new AdminOrderRowViewModel
            {
                OrderId = o.OrderId,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                CustomerName = o.User != null ? o.User.FullName : "Customer",
                CustomerEmail = o.User != null ? o.User.Email : "",
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
                ShippingMethod = o.ShippingMethod
            })
            .ToListAsync();

        // 6. Low Stock Products
        var lowStockProducts = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.StockQuantity <= 5)
            .OrderBy(p => p.StockQuantity)
            .Take(5)
            .Select(p => new AdminLowStockProductViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                CategoryName = p.Category != null ? p.Category.Name : "Uncategorized",
                Price = p.Price,
                StockQuantity = p.StockQuantity
            })
            .ToListAsync();

        return new AdminDashboardViewModel
        {
            TotalRevenue = totalRevenue,
            TotalOrdersCount = totalOrders,
            TotalProductsCount = totalProducts,
            LowStockProductsCount = lowStockCount,
            PendingOrdersCount = pendingOrdersCount,
            SelectedInterval = normalizedInterval,
            IntervalRevenue = intervalRevenue,
            IntervalOrdersCount = intervalOrdersCount,
            AverageOrderValue = averageOrderValue,
            DailySalesChart = dailySalesChart,
            TopSellingProducts = topSellingProducts,
            RecentOrders = recentOrders,
            LowStockProducts = lowStockProducts
        };
    }

    public async Task<AdminOrderListViewModel> GetAdminOrdersAsync(string? status = null, string? search = null)
    {
        var orders = await _repository.Order.GetAllOrdersWithDetailsAsync(status, search);

        var rows = orders.Select(o => new AdminOrderRowViewModel
        {
            OrderId = o.OrderId,
            OrderNumber = o.OrderNumber,
            OrderDate = o.OrderDate,
            CustomerName = o.User != null ? o.User.FullName : "Guest",
            CustomerEmail = o.User != null ? o.User.Email : "",
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
            ShippingMethod = o.ShippingMethod
        }).ToList();

        return new AdminOrderListViewModel
        {
            StatusFilter = string.IsNullOrWhiteSpace(status) ? "All" : status,
            SearchTerm = search,
            Orders = rows,
            TotalCount = rows.Count
        };
    }

    public async Task<AdminOrderDetailViewModel?> GetAdminOrderDetailAsync(int orderId)
    {
        var order = await _repository.Order.GetOrderByIdAsync(orderId);
        if (order == null) return null;

        var items = order.OrderItems.Select(oi => new OrderItemDetailViewModel
        {
            ProductId = oi.ProductId,
            ProductName = oi.Product?.Name ?? "Item",
            ImageUrl = oi.Product?.ImageUrl ?? "/images/products/placeholder.webp",
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            CanReview = false
        }).ToList();

        var subtotal = items.Sum(i => i.LineTotal);

        var allowedTransitions = order.Status switch
        {
            "Unpaid" => new List<string> { "Paid", "Cancelled" },
            "Paid" => new List<string> { "Shipped", "Cancelled" },
            "Shipped" => new List<string> { "Delivered" },
            _ => new List<string>()
        };

        return new AdminOrderDetailViewModel
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Status = order.Status,
            StripeSessionId = order.StripeSessionId,
            CustomerName = order.User?.FullName ?? "Customer",
            CustomerEmail = order.User?.Email ?? string.Empty,
            CustomerPhone = order.Address?.Phone ?? string.Empty,
            ShippingStreet = order.Address?.Street ?? string.Empty,
            ShippingCity = order.Address?.City ?? string.Empty,
            ShippingCounty = order.Address?.County ?? string.Empty,
            ShippingPostalCode = order.Address?.PostalCode ?? string.Empty,
            ShippingMethod = order.ShippingMethod,
            ShippingCost = order.ShippingCost,
            DiscountCode = order.DiscountCode?.Code,
            DiscountAmount = order.DiscountAmount,
            Subtotal = subtotal,
            TotalAmount = order.TotalAmount,
            Items = items,
            AllowedTransitions = allowedTransitions
        };
    }

    public async Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        var validStatuses = new[] { "Unpaid", "Paid", "Shipped", "Delivered", "Cancelled" };
        if (!validStatuses.Contains(newStatus))
        {
            return (false, $"Invalid target status '{newStatus}'.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                await transaction.RollbackAsync();
                return (false, $"Order #{orderId} not found.");
            }

            if (order.Status == newStatus)
            {
                await transaction.RollbackAsync();
                return (true, $"Order #{order.OrderNumber} is already in status '{newStatus}'.");
            }

            var allowedTransitions = order.Status switch
            {
                "Unpaid" => new[] { "Paid", "Cancelled" },
                "Paid" => new[] { "Shipped", "Cancelled" },
                "Shipped" => new[] { "Delivered" },
                _ => Array.Empty<string>()
            };

            if (!allowedTransitions.Contains(newStatus))
            {
                await transaction.RollbackAsync();
                return (false, $"Orders in status '{order.Status}' cannot be transitioned to '{newStatus}'.");
            }

            var previousStatus = order.Status;

            // Restock on cancel (A7 requirement)
            // If the order transitions from a non-Cancelled status to Cancelled, return inventory!
            if (newStatus == "Cancelled" && previousStatus != "Cancelled")
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.StockQuantity += item.Quantity;
                        item.Product.RowVersion = Guid.NewGuid();
                    }
                }
                _logger.LogInformation("Restocked inventory for cancelled order {OrderNumber}", order.OrderNumber);
            }

            order.Status = newStatus;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Order {OrderNumber} status transitioned from {Prev} to {Next}", order.OrderNumber, previousStatus, newStatus);
            return (true, $"Order #{order.OrderNumber} transitioned to '{newStatus}' successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to transition order status for Order ID {OrderId}", orderId);
            return (false, $"Failed to update order status: {ex.Message}");
        }
    }

    public async Task<AdminProductListViewModel> GetAdminProductsAsync(int? categoryId = null, string? search = null)
    {
        var query = _context.Products.Include(p => p.Category).AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
        }

        var products = await query
            .OrderByDescending(p => p.ProductId)
            .Select(p => new AdminProductRowViewModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                CategoryName = p.Category != null ? p.Category.Name : "Uncategorized",
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl
            })
            .ToListAsync();

        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategorySelectItem
            {
                CategoryId = c.CategoryId,
                Name = c.Name
            })
            .ToListAsync();

        return new AdminProductListViewModel
        {
            CategoryId = categoryId,
            SearchTerm = search,
            Products = products,
            Categories = categories
        };
    }

    public async Task<AdminProductFormViewModel> PrepareCreateProductFormAsync()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategorySelectItem
            {
                CategoryId = c.CategoryId,
                Name = c.Name
            })
            .ToListAsync();

        return new AdminProductFormViewModel
        {
            AvailableCategories = categories
        };
    }

    public async Task<AdminProductFormViewModel?> PrepareEditProductFormAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return null;

        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategorySelectItem
            {
                CategoryId = c.CategoryId,
                Name = c.Name
            })
            .ToListAsync();

        return new AdminProductFormViewModel
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            ExistingImageUrl = product.ImageUrl,
            AvailableCategories = categories
        };
    }

    public async Task<(bool Success, string Message)> CreateProductAsync(AdminProductFormViewModel model, string uploadsWebRootPath)
    {
        try
        {
            string imageUrl = "/images/products/placeholder.webp";

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                imageUrl = await SaveUploadedFileAsync(model.ImageFile, uploadsWebRootPath);
            }

            var product = new Product
            {
                Name = model.Name.Trim(),
                Description = model.Description.Trim(),
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                CategoryId = model.CategoryId,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow,
                RowVersion = Guid.NewGuid()
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin created product '{Name}' (ID: {Id})", product.Name, product.ProductId);
            return (true, $"Product '{product.Name}' created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create product");
            return (false, $"Failed to create product: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> EditProductAsync(AdminProductFormViewModel model, string uploadsWebRootPath)
    {
        if (!model.ProductId.HasValue)
        {
            return (false, "Product ID is missing.");
        }

        try
        {
            var product = await _context.Products.FindAsync(model.ProductId.Value);
            if (product == null)
            {
                return (false, "Product not found.");
            }

            product.Name = model.Name.Trim();
            product.Description = model.Description.Trim();
            product.Price = model.Price;
            product.StockQuantity = model.StockQuantity;
            product.CategoryId = model.CategoryId;
            product.RowVersion = Guid.NewGuid();

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                product.ImageUrl = await SaveUploadedFileAsync(model.ImageFile, uploadsWebRootPath);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin updated product '{Name}' (ID: {Id})", product.Name, product.ProductId);
            return (true, $"Product '{product.Name}' updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update product {ProductId}", model.ProductId);
            return (false, $"Failed to update product: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteProductAsync(int productId)
    {
        try
        {
            var product = await _context.Products
                .Include(p => p.OrderItems)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                return (false, "Product not found.");
            }

            // Check if product is referenced in existing orders
            if (product.OrderItems.Any())
            {
                // Soft action: zero out stock to prevent future orders while keeping historical order references intact
                product.StockQuantity = 0;
                product.RowVersion = Guid.NewGuid();
                await _context.SaveChangesAsync();

                return (true, $"Product '{product.Name}' is associated with past orders. Stock has been set to 0 (archived).");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin deleted product ID {ProductId}", productId);
            return (true, $"Product '{product.Name}' has been deleted.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete product {ProductId}", productId);
            return (false, $"Failed to delete product: {ex.Message}");
        }
    }

    private static async Task<string> SaveUploadedFileAsync(Microsoft.AspNetCore.Http.IFormFile file, string uploadsWebRootPath)
    {
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
        {
            ext = ".webp";
        }

        var targetFolder = Path.Combine(uploadsWebRootPath, "uploads", "products");
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        var uniqueFileName = $"prod_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(targetFolder, uniqueFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/products/{uniqueFileName}";
    }
}
