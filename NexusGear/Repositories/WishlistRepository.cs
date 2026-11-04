using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories.Interfaces;

namespace NexusGear.Repositories;

public class WishlistRepository : RepositoryBase<WishlistItem>, IWishlistRepository
{
    public WishlistRepository(AppDbContext context) : base(context) { }

    public async Task<List<WishlistItem>> GetUserWishlistItemsWithProductAsync(int userId)
    {
        return await _context.WishlistItems
            .Include(w => w.Product!)
                .ThenInclude(p => p.Category)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();
    }

    public async Task<WishlistItem?> GetByUserAndProductAsync(int userId, int productId)
    {
        return await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
    }

    public async Task<int> GetWishlistCountByUserIdAsync(int userId)
    {
        return await _context.WishlistItems
            .CountAsync(w => w.UserId == userId);
    }
}

