using NexusGear.Repositories.Interfaces;
using NexusGear.Services.Interfaces;
using NexusGear.ViewModels;

namespace NexusGear.Services;

public class DiscountService : IDiscountService
{
    private readonly IRepositoryWrapper _repoWrapper;

    public DiscountService(IRepositoryWrapper repoWrapper)
    {
        _repoWrapper = repoWrapper;
    }

    public async Task<DiscountValidationResult> ValidateCouponAsync(string? code, decimal subtotal, decimal shippingCost = 0)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new DiscountValidationResult
            {
                IsValid = false,
                Message = "Please enter a promo code.",
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                NewGrandTotal = subtotal + shippingCost
            };
        }

        var normalized = code.Trim().ToUpperInvariant();
        var coupon = await _repoWrapper.DiscountCode.GetByCodeAsync(normalized);

        if (coupon == null)
        {
            return new DiscountValidationResult
            {
                IsValid = false,
                Message = $"Promo code '{code}' was not found or is invalid.",
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                NewGrandTotal = subtotal + shippingCost
            };
        }

        if (DateTime.UtcNow > coupon.ExpiryDate)
        {
            return new DiscountValidationResult
            {
                IsValid = false,
                Message = $"Promo code '{coupon.Code}' has expired on {coupon.ExpiryDate:dd MMM yyyy}.",
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                NewGrandTotal = subtotal + shippingCost
            };
        }

        if (coupon.UsedCount >= coupon.MaxUses)
        {
            return new DiscountValidationResult
            {
                IsValid = false,
                Message = $"Promo code '{coupon.Code}' has reached its maximum allowed redemptions ({coupon.MaxUses}).",
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                NewGrandTotal = subtotal + shippingCost
            };
        }

        decimal discountAmount;
        if (string.Equals(coupon.DiscountType, "Percentage", StringComparison.OrdinalIgnoreCase))
        {
            discountAmount = Math.Round(subtotal * (coupon.DiscountValue / 100m), 2);
        }
        else if (string.Equals(coupon.DiscountType, "FixedAmount", StringComparison.OrdinalIgnoreCase))
        {
            discountAmount = Math.Min(coupon.DiscountValue, subtotal);
        }
        else
        {
            discountAmount = 0m;
        }

        decimal newGrandTotal = Math.Max(0m, subtotal - discountAmount + shippingCost);

        return new DiscountValidationResult
        {
            IsValid = true,
            Message = $"Promo code '{coupon.Code}' applied successfully! You saved {discountAmount:N2} RON.",
            DiscountCodeId = coupon.DiscountCodeId,
            Code = coupon.Code,
            DiscountType = coupon.DiscountType,
            DiscountValue = coupon.DiscountValue,
            DiscountAmount = discountAmount,
            Subtotal = subtotal,
            ShippingCost = shippingCost,
            NewGrandTotal = newGrandTotal
        };
    }
}
