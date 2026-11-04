namespace NexusGear.Repositories.Interfaces;

public interface IRepositoryWrapper
{
    ICartRepository Cart { get; }
    IWishlistRepository Wishlist { get; }
    IProductRepository Product { get; }
    ICategoryRepository Category { get; }

    void Save();
    Task SaveAsync();
}

