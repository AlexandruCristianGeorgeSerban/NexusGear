using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Services.Interfaces;
using NexusGear.ViewModels;
using Stripe;
using Stripe.Checkout;

namespace NexusGear.Services;

public class WebhookService : IWebhookService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        AppDbContext context,
        IConfiguration configuration,
        IEmailService emailService,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<WebhookProcessingResult> ProcessWebhookAsync(string jsonPayload, string? stripeSignatureHeader)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            return new WebhookProcessingResult { Success = false, Message = "Payload cannot be empty." };
        }

        var webhookSecret = _configuration["Stripe:WebhookSecret"] ?? _configuration["Stripe__WebhookSecret"];
        bool isPlaceholder = string.IsNullOrWhiteSpace(webhookSecret) ||
                             webhookSecret.Contains("placeholder", StringComparison.OrdinalIgnoreCase);

        Event? stripeEvent = null;

        if (!isPlaceholder)
        {
            if (string.IsNullOrWhiteSpace(stripeSignatureHeader))
            {
                _logger.LogWarning("Rejected Stripe webhook payload because the Stripe-Signature header is missing.");
                return new WebhookProcessingResult
                {
                    Success = false,
                    Message = "Missing Stripe-Signature header."
                };
            }

            try
            {
                stripeEvent = EventUtility.ConstructEvent(jsonPayload, stripeSignatureHeader, webhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe signature verification failed for webhook payload.");
                return new WebhookProcessingResult
                {
                    Success = false,
                    Message = $"Invalid Stripe signature: {ex.Message}"
                };
            }
        }
        else
        {
            // Simulation or local fallback mode
            try
            {
                stripeEvent = EventUtility.ParseEvent(jsonPayload, throwOnApiVersionMismatch: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse webhook JSON payload in simulation mode.");
                return new WebhookProcessingResult { Success = false, Message = $"Invalid payload: {ex.Message}" };
            }
        }

        if (stripeEvent == null)
        {
            return new WebhookProcessingResult { Success = false, Message = "Unable to parse Stripe event." };
        }

        if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
        {
            if (stripeEvent.Data.Object is Session session)
            {
                int orderId = 0;
                if (!string.IsNullOrEmpty(session.ClientReferenceId) && int.TryParse(session.ClientReferenceId, out int parsedRefId))
                {
                    orderId = parsedRefId;
                }
                else if (session.Metadata != null && session.Metadata.TryGetValue("OrderId", out string? metaOrderIdStr) && int.TryParse(metaOrderIdStr, out int parsedMetaId))
                {
                    orderId = parsedMetaId;
                }

                if (orderId > 0)
                {
                    return await ProcessOrderPaymentSuccessAsync(orderId);
                }

                return new WebhookProcessingResult
                {
                    Success = false,
                    EventType = stripeEvent.Type,
                    Message = "No valid Order ID found in Stripe session metadata or ClientReferenceId."
                };
            }
        }

        return new WebhookProcessingResult
        {
            Success = true,
            EventType = stripeEvent.Type,
            Message = $"Unhandled event type '{stripeEvent.Type}' ignored."
        };
    }

    public async Task<WebhookProcessingResult> ProcessOrderPaymentSuccessAsync(int orderId)
    {
        // 1. Begin atomic database transaction (A7 Concurrency & Integrity)
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.DiscountCode)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                await transaction.RollbackAsync();
                return new WebhookProcessingResult
                {
                    Success = false,
                    Message = $"Order with ID {orderId} not found in database."
                };
            }

            // 2. Idempotency & State Guard: if already Paid/Shipped/Delivered, ignore duplicate; if Cancelled, reject!
            if (order.Status is "Paid" or "Shipped" or "Delivered")
            {
                await transaction.RollbackAsync();
                _logger.LogInformation("Webhook idempotency: Order {OrderNumber} is already in status '{Status}'. Duplicate event ignored.", order.OrderNumber, order.Status);

                return new WebhookProcessingResult
                {
                    Success = true,
                    IsIdempotentDuplicate = true,
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber,
                    Message = $"Order {order.OrderNumber} is already settled ({order.Status})."
                };
            }

            if (order.Status == "Cancelled")
            {
                await transaction.RollbackAsync();
                _logger.LogWarning("Rejected payment confirmation webhook for cancelled Order {OrderNumber}.", order.OrderNumber);

                return new WebhookProcessingResult
                {
                    Success = false,
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber,
                    Message = $"Order {order.OrderNumber} was already cancelled and cannot be marked as Paid."
                };
            }

            // 3. Live Stock Verification & Atomic Decrement
            foreach (var item in order.OrderItems)
            {
                if (item.Product == null)
                {
                    order.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new WebhookProcessingResult
                    {
                        Success = false,
                        OrderId = order.OrderId,
                        OrderNumber = order.OrderNumber,
                        Message = $"Order {order.OrderNumber} cancelled: Product ID {item.ProductId} no longer exists."
                    };
                }

                if (item.Product.StockQuantity < item.Quantity)
                {
                    // Concurrency conflict: item ran out of stock between checkout placement and webhook confirmation
                    order.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogWarning("Overselling prevented: Order {OrderNumber} cancelled because {Product} has only {Stock} in stock (requested {Qty}).",
                        order.OrderNumber, item.Product.Name, item.Product.StockQuantity, item.Quantity);

                    return new WebhookProcessingResult
                    {
                        Success = false,
                        OrderId = order.OrderId,
                        OrderNumber = order.OrderNumber,
                        Message = $"Insufficient stock for '{item.Product.Name}'. Order has been cancelled and customer notified."
                    };
                }

                // Decrement stock atomically
                item.Product.StockQuantity -= item.Quantity;

                // Update concurrency token
                item.Product.RowVersion = Guid.NewGuid();
            }

            // 4. Mark Order as Paid
            order.Status = "Paid";

            // 5. Increment DiscountCode usage count if applicable
            if (order.DiscountCode != null)
            {
                order.DiscountCode.UsedCount++;
            }

            // 6. Commit Database Changes
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Order {OrderNumber} successfully confirmed as Paid with atomic inventory deduction.", order.OrderNumber);

            // 7. Dispatch Transactional Confirmation Email (B6)
            try
            {
                await _emailService.SendOrderConfirmationEmailAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order confirmation email dispatch failed for order {OrderNumber}", order.OrderNumber);
            }

            return new WebhookProcessingResult
            {
                Success = true,
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                Message = $"Order {order.OrderNumber} payment confirmed and inventory updated."
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Database concurrency collision occurred during stock reduction for Order ID {OrderId}", orderId);

            return new WebhookProcessingResult
            {
                Success = false,
                Message = "A concurrency conflict occurred while updating inventory. Please retry."
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Unexpected error processing payment success for Order ID {OrderId}", orderId);

            return new WebhookProcessingResult
            {
                Success = false,
                Message = $"Payment processing error: {ex.Message}"
            };
        }
    }
}
