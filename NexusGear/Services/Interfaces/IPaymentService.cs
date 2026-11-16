using NexusGear.ViewModels;

namespace NexusGear.Services.Interfaces;

public interface IPaymentService
{
    Task<StripeSessionResult> CreateCheckoutSessionAsync(int orderId, int userId, string baseUrl);
}
