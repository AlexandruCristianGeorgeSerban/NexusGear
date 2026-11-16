using NexusGear.ViewModels;

namespace NexusGear.Services.Interfaces;

public interface IWebhookService
{
    Task<WebhookProcessingResult> ProcessWebhookAsync(string jsonPayload, string? stripeSignatureHeader);
    Task<WebhookProcessingResult> ProcessOrderPaymentSuccessAsync(int orderId);
}
