using NexusGear.Data;
using NexusGear.Repositories.Interfaces;

namespace NexusGear.Repositories;

public class RepositoryWrapper : IRepositoryWrapper
{
    private readonly AppDbContext _context;
    private ICartRepository? _cart;
    private IWishlistRepository? _wishlist;
    private IProductRepository? _product;
    private ICategoryRepository? _category;

    public RepositoryWrapper(AppDbContext context)
    {
        _context = context;
    }

    public ICartRepository Cart => _cart ??= new CartRepository(_context);
    public IWishlistRepository Wishlist => _wishlist ??= new WishlistRepository(_context);
    public IProductRepository Product => _product ??= new ProductRepository(_context);
    public ICategoryRepository Category => _category ??= new CategoryRepository(_context);

    public void Save() => _context.SaveChanges();
    public async Task SaveAsync() => await _context.SaveChangesAsync();
}

