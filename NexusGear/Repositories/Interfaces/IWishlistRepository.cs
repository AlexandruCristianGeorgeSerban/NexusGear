using NexusGear.Models;

namespace NexusGear.Repositories.Interfaces;

public interface IWishlistRepository : IRepositoryBase<WishlistItem>
{
    Task<List<WishlistItem>> GetUserWishlistItemsWithProductAsync(int userId);
    Task<WishlistItem?> GetByUserAndProductAsync(int userId, int productId);
    Task<int> GetWishlistCountByUserIdAsync(int userId);
    Task<HashSet<int>> GetUserWishlistProductIdsAsync(int userId);
    void DeleteRange(IEnumerable<WishlistItem> items);
}
