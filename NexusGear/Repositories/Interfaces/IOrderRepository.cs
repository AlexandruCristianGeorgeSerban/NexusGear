using NexusGear.Models;

namespace NexusGear.Repositories.Interfaces;

public interface IOrderRepository : IRepositoryBase<Order>
{
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<Order?> GetOrderByNumberAsync(string orderNumber);
    Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId);
}
