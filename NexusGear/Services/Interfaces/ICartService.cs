using NexusGear.ViewModels;

namespace NexusGear.Services.Interfaces;

public interface ICartService
{
    Task<CartSummaryViewModel> GetCartAsync();
    Task<CartActionResult> AddToCartAsync(int productId, int quantity = 1);
    Task<CartActionResult> UpdateQuantityAsync(int productId, int quantity);
    Task<CartActionResult> RemoveFromCartAsync(int productId);
    Task ClearCartAsync();
    Task<int> GetCartItemCountAsync();
    Task MergeGuestCartIntoUserCartAsync(int userId);
}
