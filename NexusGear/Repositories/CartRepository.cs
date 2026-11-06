using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories.Interfaces;

namespace NexusGear.Repositories;

public class CartRepository : RepositoryBase<CartItem>, ICartRepository
{
    public CartRepository(AppDbContext context) : base(context) { }

    public async Task<List<CartItem>> GetUserCartItemsWithProductAsync(int userId)
    {
        return await _context.CartItems
            .Include(ci => ci.Product!)
                .ThenInclude(p => p.Category)
            .Where(ci => ci.UserId == userId)
            .OrderByDescending(ci => ci.AddedAt)
            .ToListAsync();
    }

    public async Task<List<CartItem>> GetCartItemsByUserAndProductAsync(int userId, int productId)
    {
        return await _context.CartItems
            .Where(ci => ci.UserId == userId && ci.ProductId == productId)
            .ToListAsync();
    }

    public void DeleteRange(IEnumerable<CartItem> items)
    {
        _context.CartItems.RemoveRange(items);
    }

    public async Task<int> GetTotalItemCountByUserIdAsync(int userId)
    {
        return await _context.CartItems
            .Where(ci => ci.UserId == userId)
            .SumAsync(ci => (int?)ci.Quantity) ?? 0;
    }
}
