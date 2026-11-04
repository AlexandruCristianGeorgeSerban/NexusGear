using NexusGear.Models;

namespace NexusGear.Repositories.Interfaces;

public interface IProductRepository : IRepositoryBase<Product>
{
    Task<Product?> GetProductByIdAsync(int id);
    Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> ids);
}

