using NexusGear.Models;

namespace NexusGear.Services.Interfaces;

public interface IEmailService
{
    Task SendOrderConfirmationEmailAsync(Order order);
}
