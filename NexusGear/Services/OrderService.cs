using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Repositories.Interfaces;
using NexusGear.Services.Interfaces;
using NexusGear.ViewModels;

namespace NexusGear.Services;

public class OrderService : IOrderService
{
    private readonly IRepositoryWrapper _repository;
    private readonly AppDbContext _context;

    public OrderService(IRepositoryWrapper repository, AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<OrderHistoryListViewModel> GetUserOrderHistoryAsync(int userId, string? statusFilter = null)
    {
        var orders = (await _repository.Order.GetOrdersByUserIdAsync(userId)).ToList();

        var totalOrders = orders.Count;
        var totalSpent = orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount);

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            orders = orders.Where(o => o.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var orderSummaries = orders.Select(o => new OrderSummaryViewModel
        {
            OrderId = o.OrderId,
            OrderNumber = o.OrderNumber,
            OrderDate = o.OrderDate,
            Status = o.Status,
            ShippingMethod = o.ShippingMethod,
            TotalAmount = o.TotalAmount,
            ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
            Items = o.OrderItems.Select(oi => new OrderItemSummaryViewModel
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? "Item",
                ImageUrl = oi.Product?.ImageUrl ?? "/images/products/placeholder.webp",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            }).ToList()
        }).ToList();

        return new OrderHistoryListViewModel
        {
            CurrentFilter = string.IsNullOrWhiteSpace(statusFilter) ? "All" : statusFilter,
            Orders = orderSummaries,
            TotalOrders = totalOrders,
            TotalSpent = totalSpent
        };
    }

    public async Task<OrderDetailsViewModel?> GetUserOrderDetailsAsync(int orderId, int userId)
    {
        var order = await _repository.Order.GetOrderByIdAsync(orderId);

        // Security check: Customer can only access their own orders (A4, A8)
        if (order == null || order.UserId != userId)
        {
            return null;
        }

        // Check which products can be reviewed (B3: verified buyer gate for Paid/Delivered orders)
        var reviewedProductIds = await _context.Reviews
            .Where(r => r.UserId == userId)
            .Select(r => r.ProductId)
            .ToListAsync();

        bool isEligibleForReview = order.Status is "Paid" or "Shipped" or "Delivered";

        var items = order.OrderItems.Select(oi => new OrderItemDetailViewModel
        {
            ProductId = oi.ProductId,
            ProductName = oi.Product?.Name ?? "Item",
            ImageUrl = oi.Product?.ImageUrl ?? "/images/products/placeholder.webp",
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            CanReview = isEligibleForReview && !reviewedProductIds.Contains(oi.ProductId)
        }).ToList();

        var subtotal = items.Sum(i => i.LineTotal);

        return new OrderDetailsViewModel
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
            Items = items
        };
    }
}
