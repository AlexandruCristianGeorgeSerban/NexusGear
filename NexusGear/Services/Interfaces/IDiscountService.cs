using NexusGear.ViewModels;

namespace NexusGear.Services.Interfaces;

public interface IDiscountService
{
    Task<DiscountValidationResult> ValidateCouponAsync(string? code, decimal subtotal, decimal shippingCost = 0);
}
