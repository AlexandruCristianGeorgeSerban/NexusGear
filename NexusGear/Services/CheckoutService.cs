using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories.Interfaces;
using NexusGear.Services.Interfaces;
using NexusGear.ViewModels;

namespace NexusGear.Services;

public class CheckoutService : ICheckoutService
{
    private readonly AppDbContext _context;
    private readonly IRepositoryWrapper _repoWrapper;
    private readonly ICartService _cartService;
    private readonly IDiscountService _discountService;

    public const decimal FreeShippingThreshold = 300.00m;
    public const decimal StandardShippingFee = 15.00m;
    public const decimal ExpressShippingFee = 35.00m;

    public CheckoutService(
        AppDbContext context,
        IRepositoryWrapper repoWrapper,
        ICartService cartService,
        IDiscountService discountService)
    {
        _context = context;
        _repoWrapper = repoWrapper;
        _cartService = cartService;
        _discountService = discountService;
    }

    public async Task<CheckoutIndexViewModel> PrepareCheckoutViewModelAsync(
        int userId,
        string? selectedShippingMethod = null,
        string? appliedCoupon = null)
    {
        var cart = await _cartService.GetCartAsync();

        var model = new CheckoutIndexViewModel
        {
            Subtotal = cart.Subtotal,
            BundleDiscount = cart.BundleDiscount,
            FreeShippingUnlocked = cart.Subtotal >= FreeShippingThreshold,
            FreeShippingRemaining = Math.Max(0m, FreeShippingThreshold - cart.Subtotal),
            EstimatedVat = cart.EstimatedVat
        };

        // Map items
        foreach (var item in cart.Items)
        {
            model.Items.Add(new CheckoutItemViewModel
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ImageUrl = item.ImageUrl,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                AvailableStock = item.MaxAvailableStock,
                IsBundleItem = item.IsBundleItem
            });
        }

        // Fetch user's saved addresses from HAS_ADDRESS
        var userAddresses = await _context.UserAddresses
            .AsNoTracking()
            .Where(ua => ua.UserId == userId && ua.Address != null)
            .OrderByDescending(ua => ua.IsDefault)
            .ThenByDescending(ua => ua.UserAddressId)
            .Select(ua => new CheckoutAddressViewModel
            {
                AddressId = ua.AddressId,
                Street = ua.Address!.Street,
                City = ua.Address.City,
                County = ua.Address.County,
                PostalCode = ua.Address.PostalCode,
                Phone = ua.Address.Phone,
                IsDefault = ua.IsDefault
            })
            .ToListAsync();

        model.SavedAddresses = userAddresses;

        // Determine default selected address
        var defaultAddr = userAddresses.FirstOrDefault(a => a.IsDefault) ?? userAddresses.FirstOrDefault();
        model.SelectedAddressId = defaultAddr?.AddressId ?? 0;

        // Shipping method and options
        bool isExpress = string.Equals(selectedShippingMethod, "Express Next-Day", StringComparison.OrdinalIgnoreCase);
        model.SelectedShippingMethod = isExpress ? "Express Next-Day" : "Standard Courier";

        decimal standardFee = model.FreeShippingUnlocked ? 0.00m : StandardShippingFee;
        model.ShippingOptions = new List<ShippingOptionViewModel>
        {
            new ShippingOptionViewModel
            {
                Method = "Standard Courier",
                Price = standardFee,
                EstimatedDelivery = "2–3 business days",
                IsFree = model.FreeShippingUnlocked,
                Description = "Fast door-to-door delivery with live SMS tracking."
            },
            new ShippingOptionViewModel
            {
                Method = "Express Next-Day",
                Price = ExpressShippingFee,
                EstimatedDelivery = "Next business day morning",
                IsFree = false,
                Description = "Priority courier dispatch with guaranteed morning delivery."
            }
        };

        model.ShippingCost = isExpress ? ExpressShippingFee : standardFee;

        // Validate and apply coupon if provided
        decimal couponDiscount = 0.00m;
        if (!string.IsNullOrWhiteSpace(appliedCoupon))
        {
            var discountResult = await _discountService.ValidateCouponAsync(appliedCoupon, model.Subtotal, model.ShippingCost);
            if (discountResult.IsValid)
            {
                model.AppliedDiscountCode = discountResult.Code;
                couponDiscount = discountResult.DiscountAmount;
            }
        }

        model.DiscountAmount = couponDiscount;
        decimal totalDiscount = model.BundleDiscount + couponDiscount;
        model.GrandTotal = Math.Max(0m, model.Subtotal + model.ShippingCost - totalDiscount);
        model.EstimatedVat = Math.Round(Math.Max(0m, model.Subtotal - totalDiscount) - (Math.Max(0m, model.Subtotal - totalDiscount) / 1.19m), 2);

        return model;
    }

    public async Task<DiscountValidationResult> ApplyCouponAsync(int userId, string code, string shippingMethod)
    {
        var cart = await _cartService.GetCartAsync();

        bool isExpress = string.Equals(shippingMethod, "Express Next-Day", StringComparison.OrdinalIgnoreCase);
        decimal shippingCost = isExpress ? ExpressShippingFee : (cart.Subtotal >= FreeShippingThreshold ? 0.00m : StandardShippingFee);

        return await _discountService.ValidateCouponAsync(code, cart.Subtotal, shippingCost);
    }

    public async Task<OrderCreationResult> PlaceOrderAsync(int userId, PlaceOrderRequest request)
    {
        // 1. Fetch user's cart items from database
        var cartItems = await _context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.UserId == userId)
            .ToListAsync();

        if (cartItems.Count == 0)
        {
            return new OrderCreationResult
            {
                Success = false,
                Message = "Your shopping cart is empty. Please add peripherals before checking out."
            };
        }

        // 2. Validate live stock availability for every line item
        foreach (var item in cartItems)
        {
            if (item.Product == null)
            {
                return new OrderCreationResult
                {
                    Success = false,
                    Message = "One or more products in your cart are no longer available."
                };
            }

            if (item.Product.StockQuantity < item.Quantity)
            {
                return new OrderCreationResult
                {
                    Success = false,
                    Message = $"Item '{item.Product.Name}' has only {item.Product.StockQuantity} units in stock. Please adjust your cart quantity."
                };
            }
        }

        // 3. Resolve Delivery Address
        int resolvedAddressId;

        if (request.UseNewAddress && request.NewAddress != null)
        {
            var newAddr = request.NewAddress;

            if (string.IsNullOrWhiteSpace(newAddr.Street) ||
                string.IsNullOrWhiteSpace(newAddr.City) ||
                string.IsNullOrWhiteSpace(newAddr.County) ||
                string.IsNullOrWhiteSpace(newAddr.PostalCode) ||
                string.IsNullOrWhiteSpace(newAddr.Phone))
            {
                return new OrderCreationResult
                {
                    Success = false,
                    Message = "Please fill in all required shipping address fields."
                };
            }

            // Check if exact address already exists
            var existingAddress = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Street.ToLower() == newAddr.Street.Trim().ToLower() &&
                                          a.City.ToLower() == newAddr.City.Trim().ToLower() &&
                                          a.County.ToLower() == newAddr.County.Trim().ToLower() &&
                                          a.PostalCode == newAddr.PostalCode.Trim());

            if (existingAddress == null)
            {
                existingAddress = new Address
                {
                    Street = newAddr.Street.Trim(),
                    City = newAddr.City.Trim(),
                    County = newAddr.County.Trim(),
                    PostalCode = newAddr.PostalCode.Trim(),
                    Phone = newAddr.Phone.Trim()
                };
                _context.Addresses.Add(existingAddress);
                await _context.SaveChangesAsync();
            }

            // Link to user in HAS_ADDRESS
            var userAddress = await _context.UserAddresses
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AddressId == existingAddress.AddressId);

            if (userAddress == null)
            {
                bool hasAnyAddresses = await _context.UserAddresses.AnyAsync(ua => ua.UserId == userId);
                userAddress = new UserAddress
                {
                    UserId = userId,
                    AddressId = existingAddress.AddressId,
                    IsDefault = !hasAnyAddresses || newAddr.IsDefault
                };

                if (userAddress.IsDefault)
                {
                    var previousDefaults = await _context.UserAddresses
                        .Where(ua => ua.UserId == userId && ua.IsDefault)
                        .ToListAsync();
                    foreach (var pd in previousDefaults) pd.IsDefault = false;
                }

                _context.UserAddresses.Add(userAddress);
                await _context.SaveChangesAsync();
            }

            resolvedAddressId = existingAddress.AddressId;
        }
        else
        {
            if (!request.SelectedAddressId.HasValue || request.SelectedAddressId.Value <= 0)
            {
                return new OrderCreationResult
                {
                    Success = false,
                    Message = "Please select or enter a valid delivery address."
                };
            }

            var addressExists = await _context.UserAddresses
                .AnyAsync(ua => ua.UserId == userId && ua.AddressId == request.SelectedAddressId.Value);

            if (!addressExists)
            {
                return new OrderCreationResult
                {
                    Success = false,
                    Message = "Selected delivery address was not found in your address book."
                };
            }

            resolvedAddressId = request.SelectedAddressId.Value;
        }

        // 4. Calculate Subtotal & Shipping Cost
        decimal subtotal = cartItems.Sum(ci => ci.Quantity * ci.Product!.Price);
        bool isExpress = string.Equals(request.ShippingMethod, "Express Next-Day", StringComparison.OrdinalIgnoreCase);
        decimal shippingCost = isExpress ? ExpressShippingFee : (subtotal >= FreeShippingThreshold ? 0.00m : StandardShippingFee);
        string normalizedShippingMethod = isExpress ? "Express Next-Day" : "Standard Courier";

        // Hardware Bundle Discount (B2): 5% automatic discount strictly on complementary bundle gear
        var cartSummary = await _cartService.GetCartAsync();
        decimal bundleDiscount = cartSummary.BundleDiscount;

        // 5. Validate Discount Code if provided
        int? discountCodeId = null;
        decimal couponDiscount = 0.00m;

        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            var discountResult = await _discountService.ValidateCouponAsync(request.DiscountCode, subtotal, shippingCost);
            if (discountResult.IsValid)
            {
                discountCodeId = discountResult.DiscountCodeId;
                couponDiscount = discountResult.DiscountAmount;
            }
            else
            {
                return new OrderCreationResult
                {
                    Success = false,
                    Message = discountResult.Message
                };
            }
        }

        decimal totalDiscount = bundleDiscount + couponDiscount;
        decimal grandTotal = Math.Max(0m, subtotal + shippingCost - totalDiscount);

        // 6. Generate unique OrderNumber: NXG-2026-XXXX
        string orderNumber;
        do
        {
            orderNumber = $"NXG-2026-{Random.Shared.Next(1000, 9999)}";
        } while (await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber));

        // 7. Create Order entity with status = Unpaid (P2/P3 frozen spec)
        var order = new Order
        {
            OrderNumber = orderNumber,
            OrderDate = DateTime.UtcNow,
            Status = "Unpaid",
            ShippingMethod = normalizedShippingMethod,
            ShippingCost = shippingCost,
            DiscountAmount = totalDiscount,
            TotalAmount = grandTotal,
            UserId = userId,
            AddressId = resolvedAddressId,
            DiscountCodeId = discountCodeId
        };

        _repoWrapper.Order.Create(order);
        await _repoWrapper.SaveAsync();

        // 8. Create OrderItems snapshotted in CONTAINS junction table
        foreach (var item in cartItems)
        {
            var orderItem = new OrderItem
            {
                OrderId = order.OrderId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.Product!.Price // Snapshot historical price
            };
            _context.OrderItems.Add(orderItem);
        }

        // 9. Clear User's Cart
        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        return new OrderCreationResult
        {
            Success = true,
            Message = "Order created successfully.",
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber
        };
    }

    public async Task<OrderConfirmationViewModel?> GetOrderConfirmationAsync(int orderId, int userId)
    {
        var order = await _repoWrapper.Order.GetOrderByIdAsync(orderId);
        if (order == null || order.UserId != userId) return null;

        var model = new OrderConfirmationViewModel
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Status = order.Status,
            CustomerName = order.User?.FullName ?? "Valued Customer",
            CustomerEmail = order.User?.Email ?? string.Empty,
            Street = order.Address?.Street ?? string.Empty,
            City = order.Address?.City ?? string.Empty,
            County = order.Address?.County ?? string.Empty,
            PostalCode = order.Address?.PostalCode ?? string.Empty,
            Phone = order.Address?.Phone ?? string.Empty,
            ShippingMethod = order.ShippingMethod,
            ShippingCost = order.ShippingCost,
            DiscountCode = order.DiscountCode?.Code,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount
        };

        decimal calculatedSubtotal = 0m;
        foreach (var item in order.OrderItems)
        {
            calculatedSubtotal += item.Quantity * item.UnitPrice;
            model.Items.Add(new CheckoutItemViewModel
            {
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? "Gaming Peripheral",
                ImageUrl = item.Product?.ImageUrl ?? "/images/products/placeholder.svg",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        model.Subtotal = calculatedSubtotal;
        return model;
    }
}
