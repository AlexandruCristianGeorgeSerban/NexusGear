using NexusGear.ViewModels;

namespace NexusGear.Services.Interfaces;

public interface ICheckoutService
{
    Task<CheckoutIndexViewModel> PrepareCheckoutViewModelAsync(int userId, string? selectedShippingMethod = null, string? appliedCoupon = null);
    Task<DiscountValidationResult> ApplyCouponAsync(int userId, string code, string shippingMethod);
    Task<OrderCreationResult> PlaceOrderAsync(int userId, PlaceOrderRequest request);
    Task<OrderConfirmationViewModel?> GetOrderConfirmationAsync(int orderId, int userId);
}
