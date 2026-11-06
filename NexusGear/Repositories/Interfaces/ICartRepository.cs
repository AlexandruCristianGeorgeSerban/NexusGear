using NexusGear.Models;

namespace NexusGear.Repositories.Interfaces;

public interface ICartRepository : IRepositoryBase<CartItem>
{
    Task<List<CartItem>> GetUserCartItemsWithProductAsync(int userId);
    Task<List<CartItem>> GetCartItemsByUserAndProductAsync(int userId, int productId);
    void DeleteRange(IEnumerable<CartItem> items);
    Task<int> GetTotalItemCountByUserIdAsync(int userId);
}
