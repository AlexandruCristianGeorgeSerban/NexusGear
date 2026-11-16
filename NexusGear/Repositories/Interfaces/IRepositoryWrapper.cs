namespace NexusGear.Repositories.Interfaces;

public interface IRepositoryWrapper
{
    ICartRepository Cart { get; }
    IWishlistRepository Wishlist { get; }
    IProductRepository Product { get; }
    ICategoryRepository Category { get; }
    IOrderRepository Order { get; }
    IDiscountRepository DiscountCode { get; }

    void Save();
    Task SaveAsync();
}

