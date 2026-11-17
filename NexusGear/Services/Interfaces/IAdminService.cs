using NexusGear.ViewModels;

namespace NexusGear.Services.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardMetricsAsync();
    Task<AdminOrderListViewModel> GetAdminOrdersAsync(string? status = null, string? search = null);
    Task<AdminOrderDetailViewModel?> GetAdminOrderDetailAsync(int orderId);
    Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string newStatus);

    Task<AdminProductListViewModel> GetAdminProductsAsync(int? categoryId = null, string? search = null);
    Task<AdminProductFormViewModel> PrepareCreateProductFormAsync();
    Task<AdminProductFormViewModel?> PrepareEditProductFormAsync(int productId);
    Task<(bool Success, string Message)> CreateProductAsync(AdminProductFormViewModel model, string uploadsWebRootPath);
    Task<(bool Success, string Message)> EditProductAsync(AdminProductFormViewModel model, string uploadsWebRootPath);
    Task<(bool Success, string Message)> DeleteProductAsync(int productId);
}
