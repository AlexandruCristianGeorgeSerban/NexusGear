using NexusGear.ViewModels;

namespace NexusGear.Services.Interfaces;

public interface IOrderService
{
    Task<OrderHistoryListViewModel> GetUserOrderHistoryAsync(int userId, string? statusFilter = null);
    Task<OrderDetailsViewModel?> GetUserOrderDetailsAsync(int orderId, int userId);
}
