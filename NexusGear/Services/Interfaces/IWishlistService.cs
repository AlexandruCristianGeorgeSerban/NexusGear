using NexusGear.ViewModels;

namespace NexusGear.Services.Interfaces;

public interface IWishlistService
{
    Task<WishlistViewModel> GetWishlistAsync(int userId);
    Task<WishlistToggleResult> ToggleWishlistAsync(int userId, int productId);
    Task<bool> RemoveFromWishlistAsync(int userId, int productId);
    Task<int> GetWishlistCountAsync(int userId);
    Task<WishlistMoveResult> MoveToCartAsync(int userId, int productId);
    Task ClearWishlistAsync(int userId);
    Task<HashSet<int>> GetUserWishlistProductIdsAsync(int userId);
}
