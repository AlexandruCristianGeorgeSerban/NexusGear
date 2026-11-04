using NexusGear.Models;
using NexusGear.Repositories.Interfaces;
using NexusGear.Services.Interfaces;
using NexusGear.ViewModels;

namespace NexusGear.Services;

public class WishlistService : IWishlistService
{
    private readonly IRepositoryWrapper _repo;
    private readonly ICartService _cartService;
    private readonly ILogger<WishlistService> _logger;

    public WishlistService(
        IRepositoryWrapper repo,
        ICartService cartService,
        ILogger<WishlistService> logger)
    {
        _repo = repo;
        _cartService = cartService;
        _logger = logger;
    }

    public async Task<WishlistViewModel> GetWishlistAsync(int userId)
    {
        var items = await _repo.Wishlist.GetUserWishlistItemsWithProductAsync(userId);

        var viewModel = new WishlistViewModel
        {
            Items = items
                .Where(w => w.Product != null)
                .Select(w => new WishlistItemViewModel
                {
                    ProductId = w.ProductId,
                    ProductName = w.Product!.Name,
                    CategoryName = w.Product.Category?.Name ?? "Peripherals",
                    ImageUrl = w.Product.ImageUrl,
                    UnitPrice = w.Product.Price,
                    StockQuantity = w.Product.StockQuantity,
                    AddedAt = w.AddedAt
                })
                .ToList()
        };

        return viewModel;
    }

    public async Task<WishlistToggleResult> ToggleWishlistAsync(int userId, int productId)
    {
        var product = await _repo.Product.GetProductByIdAsync(productId);
        if (product == null)
        {
            return new WishlistToggleResult
            {
                Success = false,
                Message = "Product could not be found."
            };
        }

        var existingItem = await _repo.Wishlist.GetByUserAndProductAsync(userId, productId);
        bool isAdded;
        string message;

        if (existingItem != null)
        {
            _repo.Wishlist.Delete(existingItem);
            await _repo.SaveAsync();
            isAdded = false;
            message = $"'{product.Name}' was removed from your wishlist.";
        }
        else
        {
            var newItem = new WishlistItem
            {
                UserId = userId,
                ProductId = productId,
                AddedAt = DateTime.UtcNow
            };
            _repo.Wishlist.Create(newItem);
            await _repo.SaveAsync();
            isAdded = true;
            message = $"'{product.Name}' was added to your wishlist!";
        }

        int count = await _repo.Wishlist.GetWishlistCountByUserIdAsync(userId);
        return new WishlistToggleResult
        {
            Success = true,
            IsAdded = isAdded,
            WishlistCount = count,
            Message = message
        };
    }

    public async Task<bool> RemoveFromWishlistAsync(int userId, int productId)
    {
        var existingItem = await _repo.Wishlist.GetByUserAndProductAsync(userId, productId);
        if (existingItem == null)
        {
            return false;
        }

        _repo.Wishlist.Delete(existingItem);
        await _repo.SaveAsync();
        return true;
    }

    public async Task<int> GetWishlistCountAsync(int userId)
    {
        return await _repo.Wishlist.GetWishlistCountByUserIdAsync(userId);
    }

    public async Task<WishlistMoveResult> MoveToCartAsync(int userId, int productId)
    {
        var product = await _repo.Product.GetProductByIdAsync(productId);
        if (product == null)
        {
            return new WishlistMoveResult
            {
                Success = false,
                Message = "Product could not be found."
            };
        }

        if (product.StockQuantity <= 0)
        {
            return new WishlistMoveResult
            {
                Success = false,
                Message = $"'{product.Name}' is out of stock and cannot be moved to cart."
            };
        }

        var cartResult = await _cartService.AddToCartAsync(productId, 1);
        if (!cartResult.Success)
        {
            return new WishlistMoveResult
            {
                Success = false,
                Message = cartResult.Message
            };
        }

        var wishlistItem = await _repo.Wishlist.GetByUserAndProductAsync(userId, productId);
        if (wishlistItem != null)
        {
            _repo.Wishlist.Delete(wishlistItem);
            await _repo.SaveAsync();
        }

        int wishlistCount = await _repo.Wishlist.GetWishlistCountByUserIdAsync(userId);

        return new WishlistMoveResult
        {
            Success = true,
            Message = $"'{product.Name}' moved to your cart!",
            WishlistCount = wishlistCount,
            CartCount = cartResult.TotalCartCount
        };
    }

    public async Task ClearWishlistAsync(int userId)
    {
        var items = await _repo.Wishlist.GetUserWishlistItemsWithProductAsync(userId);
        if (items.Count > 0)
        {
            _repo.Wishlist.DeleteRange(items);
            await _repo.SaveAsync();
        }
    }

    public async Task<HashSet<int>> GetUserWishlistProductIdsAsync(int userId)
    {
        return await _repo.Wishlist.GetUserWishlistProductIdsAsync(userId);
    }
}
