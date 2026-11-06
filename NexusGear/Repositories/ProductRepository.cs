using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories.Interfaces;

namespace NexusGear.Repositories;

public class ProductRepository : RepositoryBase<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == id);
    }

    public async Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> ids)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => ids.Contains(p.ProductId))
            .ToListAsync();
    }
}

