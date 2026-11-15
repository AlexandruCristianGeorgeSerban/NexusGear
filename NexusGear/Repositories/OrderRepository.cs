using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories.Interfaces;

namespace NexusGear.Repositories;

public class OrderRepository : RepositoryBase<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Address)
            .Include(o => o.DiscountCode)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Address)
            .Include(o => o.DiscountCode)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Address)
            .Include(o => o.DiscountCode)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}
