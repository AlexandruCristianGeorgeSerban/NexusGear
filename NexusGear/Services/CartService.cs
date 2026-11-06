using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.ViewModels;
using NexusGear.Services.Interfaces;
using NexusGear.Repositories;
using NexusGear.Repositories.Interfaces;

namespace NexusGear.Services;

public class CartService : ICartService
{
    public const string SessionCartKey = "NexusGear_GuestCart";

    // Concurrency lock dictionary to serialize rapid cart mutations per user or session
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _userLocks = new();

    private readonly IRepositoryWrapper _repo;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartService(
        IRepositoryWrapper repo,
        IHttpContextAccessor httpContextAccessor)
    {
        _repo = repo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<CartSummaryViewModel> GetCartAsync()
    {
        var summary = new CartSummaryViewModel();
        var userId = GetCurrentUserId();

        if (userId.HasValue)
        {
            var dbItems = await _repo.Cart.GetUserCartItemsWithProductAsync(userId.Value);

            // Self-healing cart: deduplicate by ProductId if concurrent requests created duplicate rows
            var groups = dbItems.GroupBy(ci => ci.ProductId).ToList();
            bool hasDuplicateRows = false;

            foreach (var group in groups)
            {
                var primary = group.First();
                if (primary.Product == null) continue;

                int maxStock = primary.Product.StockQuantity;
                int combinedQty = group.Sum(ci => ci.Quantity);
                int finalQty = Math.Min(combinedQty, maxStock);

                if (group.Count() > 1)
                {
                    hasDuplicateRows = true;
                    primary.Quantity = finalQty;
                    _repo.Cart.DeleteRange(group.Skip(1));
                }

                summary.Items.Add(new CartItemViewModel
                {
                    ProductId = primary.ProductId,
                    ProductName = primary.Product.Name,
                    CategoryName = primary.Product.Category?.Name ?? "Peripherals",
                    ImageUrl = primary.Product.ImageUrl,
                    UnitPrice = primary.Product.Price,
                    Quantity = finalQty,
                    MaxAvailableStock = maxStock
                });
            }

            if (hasDuplicateRows)
            {
                await _repo.SaveAsync();
            }
        }
        else
        {
            var sessionItems = GetSessionCart();
            if (sessionItems.Count > 0)
            {
                var productIds = sessionItems.Select(s => s.ProductId).Distinct().ToList();
                var productsList = await _repo.Product.GetProductsByIdsAsync(productIds);
                var products = productsList.ToDictionary(p => p.ProductId);

                var groups = sessionItems.GroupBy(s => s.ProductId).ToList();
                var deduplicatedSession = new List<SessionCartItem>();

                foreach (var group in groups)
                {
                    if (products.TryGetValue(group.Key, out var product))
                    {
                        int combinedQty = group.Sum(s => s.Quantity);
                        int finalQty = Math.Min(combinedQty, product.StockQuantity);

                        deduplicatedSession.Add(new SessionCartItem
                        {
                            ProductId = product.ProductId,
                            Quantity = finalQty
                        });

                        summary.Items.Add(new CartItemViewModel
                        {
                            ProductId = product.ProductId,
                            ProductName = product.Name,
                            CategoryName = product.Category?.Name ?? "Peripherals",
                            ImageUrl = product.ImageUrl,
                            UnitPrice = product.Price,
                            Quantity = finalQty,
                            MaxAvailableStock = product.StockQuantity
                        });
                    }
                }

                if (deduplicatedSession.Count != sessionItems.Count)
                {
                    SaveSessionCart(deduplicatedSession);
                }
            }
        }

        return summary;
    }

    public async Task<CartActionResult> AddToCartAsync(int productId, int quantity = 1)
    {
        if (quantity <= 0)
        {
            quantity = 1;
        }

        var product = await _repo.Product.GetProductByIdAsync(productId);
        if (product == null)
        {
            return new CartActionResult
            {
                Success = false,
                Message = "Product could not be found."
            };
        }

        if (product.StockQuantity <= 0)
        {
            return new CartActionResult
            {
                Success = false,
                Message = $"'{product.Name}' is currently out of stock."
            };
        }

        var userId = GetCurrentUserId();
        var semaphore = GetLockForUser(userId);

        await semaphore.WaitAsync();
        try
        {
            int finalQuantity;
            string feedbackMessage;

            if (userId.HasValue)
            {
                var existingItems = await _repo.Cart.GetCartItemsByUserAndProductAsync(userId.Value, productId);

                var primaryItem = existingItems.FirstOrDefault();
                int currentQty = existingItems.Sum(ci => ci.Quantity);

                // Clean up any race-condition duplicate rows
                if (existingItems.Count > 1)
                {
                    _repo.Cart.DeleteRange(existingItems.Skip(1));
                }

                if (currentQty >= product.StockQuantity)
                {
                    return new CartActionResult
                    {
                        Success = false,
                        Message = $"You already have the maximum available stock ({product.StockQuantity}) of '{product.Name}' in your cart."
                    };
                }

                int targetQty = currentQty + quantity;
                if (targetQty > product.StockQuantity)
                {
                    targetQty = product.StockQuantity;
                    feedbackMessage = $"Added {targetQty - currentQty}x '{product.Name}' to cart (maximum available stock reached).";
                }
                else
                {
                    feedbackMessage = $"Added {quantity}x '{product.Name}' to your cart.";
                }

                if (primaryItem != null)
                {
                    primaryItem.Quantity = targetQty;
                    _repo.Cart.Update(primaryItem);
                }
                else
                {
                    _repo.Cart.Create(new CartItem
                    {
                        UserId = userId.Value,
                        ProductId = productId,
                        Quantity = targetQty,
                        AddedAt = DateTime.UtcNow
                    });
                }

                await _repo.SaveAsync();
                finalQuantity = targetQty;
            }
            else
            {
                var sessionItems = GetSessionCart();
                var existingItems = sessionItems.Where(s => s.ProductId == productId).ToList();
                int currentQty = existingItems.Sum(s => s.Quantity);

                sessionItems.RemoveAll(s => s.ProductId == productId);

                if (currentQty >= product.StockQuantity)
                {
                    return new CartActionResult
                    {
                        Success = false,
                        Message = $"You already have the maximum available stock ({product.StockQuantity}) of '{product.Name}' in your cart."
                    };
                }

                int targetQty = currentQty + quantity;
                if (targetQty > product.StockQuantity)
                {
                    targetQty = product.StockQuantity;
                    feedbackMessage = $"Added {targetQty - currentQty}x '{product.Name}' to cart (maximum available stock reached).";
                }
                else
                {
                    feedbackMessage = $"Added {quantity}x '{product.Name}' to your cart.";
                }

                sessionItems.Add(new SessionCartItem
                {
                    ProductId = productId,
                    Quantity = targetQty
                });

                SaveSessionCart(sessionItems);
                finalQuantity = targetQty;
            }

            var cart = await GetCartAsync();
            return new CartActionResult
            {
                Success = true,
                Message = feedbackMessage,
                TotalCartCount = cart.TotalItemsCount,
                Subtotal = cart.Subtotal,
                ShippingFee = cart.ShippingFee,
                GrandTotal = cart.GrandTotal,
                LineTotal = product.Price * finalQuantity,
                UpdatedQuantity = finalQuantity,
                IsEmpty = cart.IsEmpty
            };
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<CartActionResult> UpdateQuantityAsync(int productId, int quantity)
    {
        if (quantity <= 0)
        {
            return await RemoveFromCartAsync(productId);
        }

        var product = await _repo.Product.GetProductByIdAsync(productId);
        if (product == null)
        {
            return new CartActionResult
            {
                Success = false,
                Message = "Product could not be found."
            };
        }

        var userId = GetCurrentUserId();
        var semaphore = GetLockForUser(userId);

        await semaphore.WaitAsync();
        try
        {
            int finalQuantity = Math.Min(quantity, product.StockQuantity);

            if (userId.HasValue)
            {
                var dbItems = await _repo.Cart.GetCartItemsByUserAndProductAsync(userId.Value, productId);

                if (dbItems.Count > 0)
                {
                    var primary = dbItems[0];
                    primary.Quantity = finalQuantity;
                    _repo.Cart.Update(primary);

                    if (dbItems.Count > 1)
                    {
                        _repo.Cart.DeleteRange(dbItems.Skip(1));
                    }

                    await _repo.SaveAsync();
                }
            }
            else
            {
                var sessionItems = GetSessionCart();
                sessionItems.RemoveAll(s => s.ProductId == productId);
                sessionItems.Add(new SessionCartItem
                {
                    ProductId = productId,
                    Quantity = finalQuantity
                });
                SaveSessionCart(sessionItems);
            }

            var cart = await GetCartAsync();
            var result = new CartActionResult
            {
                Success = true,
                TotalCartCount = cart.TotalItemsCount,
                Subtotal = cart.Subtotal,
                ShippingFee = cart.ShippingFee,
                GrandTotal = cart.GrandTotal,
                LineTotal = product.Price * finalQuantity,
                UpdatedQuantity = finalQuantity,
                IsEmpty = cart.IsEmpty
            };

            if (quantity > product.StockQuantity)
            {
                result.Message = $"Quantity adjusted to maximum available stock ({product.StockQuantity}).";
            }

            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<CartActionResult> RemoveFromCartAsync(int productId)
    {
        var product = await _repo.Product.GetProductByIdAsync(productId);
        string productName = product?.Name ?? "Item";

        var userId = GetCurrentUserId();
        var semaphore = GetLockForUser(userId);

        await semaphore.WaitAsync();
        try
        {
            if (userId.HasValue)
            {
                var dbItems = await _repo.Cart.GetCartItemsByUserAndProductAsync(userId.Value, productId);

                if (dbItems.Count > 0)
                {
                    _repo.Cart.DeleteRange(dbItems);
                    await _repo.SaveAsync();
                }
            }
            else
            {
                var sessionItems = GetSessionCart();
                sessionItems.RemoveAll(s => s.ProductId == productId);
                SaveSessionCart(sessionItems);
            }

            var cart = await GetCartAsync();
            return new CartActionResult
            {
                Success = true,
                Message = $"'{productName}' was removed from your cart.",
                TotalCartCount = cart.TotalItemsCount,
                Subtotal = cart.Subtotal,
                ShippingFee = cart.ShippingFee,
                GrandTotal = cart.GrandTotal,
                LineTotal = 0,
                UpdatedQuantity = 0,
                IsEmpty = cart.IsEmpty
            };
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task ClearCartAsync()
    {
        var userId = GetCurrentUserId();
        var semaphore = GetLockForUser(userId);

        await semaphore.WaitAsync();
        try
        {
            if (userId.HasValue)
            {
                var dbItems = await _repo.Cart.GetUserCartItemsWithProductAsync(userId.Value);

                if (dbItems.Count > 0)
                {
                    _repo.Cart.DeleteRange(dbItems);
                    await _repo.SaveAsync();
                }
            }
            else
            {
                ClearSessionCart();
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<int> GetCartItemCountAsync()
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            return await _repo.Cart.GetTotalItemCountByUserIdAsync(userId.Value);
        }

        var sessionItems = GetSessionCart();
        return sessionItems.Sum(s => s.Quantity);
    }

    public async Task MergeGuestCartIntoUserCartAsync(int userId)
    {
        var semaphore = GetLockForUser(userId);
        await semaphore.WaitAsync();
        try
        {
            var sessionItems = GetSessionCart();
            if (sessionItems.Count == 0)
            {
                return;
            }

            var productIds = sessionItems.Select(s => s.ProductId).Distinct().ToList();
            var productsList = await _repo.Product.GetProductsByIdsAsync(productIds);
            var products = productsList.ToDictionary(p => p.ProductId);

            var existingDbItems = (await _repo.Cart.GetUserCartItemsWithProductAsync(userId))
                .ToDictionary(ci => ci.ProductId);

            foreach (var guestItem in sessionItems)
            {
                if (!products.TryGetValue(guestItem.ProductId, out var product) || product.StockQuantity <= 0)
                {
                    continue;
                }

                if (existingDbItems.TryGetValue(guestItem.ProductId, out var existingItem))
                {
                    existingItem.Quantity = Math.Min(existingItem.Quantity + guestItem.Quantity, product.StockQuantity);
                    _repo.Cart.Update(existingItem);
                }
                else
                {
                    _repo.Cart.Create(new CartItem
                    {
                        UserId = userId,
                        ProductId = guestItem.ProductId,
                        Quantity = Math.Min(guestItem.Quantity, product.StockQuantity),
                        AddedAt = DateTime.UtcNow
                    });
                }
            }

            await _repo.SaveAsync();
            ClearSessionCart();
        }
        finally
        {
            semaphore.Release();
        }
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    private SemaphoreSlim GetLockForUser(int? userId)
    {
        string key = userId.HasValue
            ? $"user_{userId.Value}"
            : $"session_{_httpContextAccessor.HttpContext?.Session?.Id ?? "guest"}";

        return _userLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
    }

    private int? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out var id))
            {
                return id;
            }
        }
        return null;
    }

    private List<SessionCartItem> GetSessionCart()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return new List<SessionCartItem>();
        }

        try
        {
            var json = session.GetString(SessionCartKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<SessionCartItem>();
            }

            return JsonSerializer.Deserialize<List<SessionCartItem>>(json) ?? new List<SessionCartItem>();
        }
        catch
        {
            return new List<SessionCartItem>();
        }
    }

    private void SaveSessionCart(List<SessionCartItem> items)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(items);
            session.SetString(SessionCartKey, json);
        }
        catch
        {
            // Session write failure safeguard
        }
    }

    private void ClearSessionCart()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.Remove(SessionCartKey);
    }
}

public class SessionCartItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
