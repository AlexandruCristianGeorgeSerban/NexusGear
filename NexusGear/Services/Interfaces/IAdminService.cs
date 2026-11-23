using NexusGear.ViewModels;

namespace NexusGear.Services.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardMetricsAsync(string interval = "30days");
    Task<AdminOrderListViewModel> GetAdminOrdersAsync(string? status = null, string? search = null);
    Task<AdminOrderDetailViewModel?> GetAdminOrderDetailAsync(int orderId);
    Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string newStatus);

    Task<AdminProductListViewModel> GetAdminProductsAsync(int? categoryId = null, string? search = null);
    Task<AdminProductFormViewModel> PrepareCreateProductFormAsync();
    Task<AdminProductFormViewModel?> PrepareEditProductFormAsync(int productId);
    Task<(bool Success, string Message)> CreateProductAsync(AdminProductFormViewModel model, string uploadsWebRootPath);
    Task<(bool Success, string Message)> EditProductAsync(AdminProductFormViewModel model, string uploadsWebRootPath);
    Task<(bool Success, string Message)> DeleteProductAsync(int productId);

    Task<AdminCategoryListViewModel> GetAdminCategoriesAsync();
    Task<AdminCategoryFormViewModel?> PrepareEditCategoryFormAsync(int categoryId);
    Task<(bool Success, string Message)> CreateCategoryAsync(AdminCategoryFormViewModel model);
    Task<(bool Success, string Message)> EditCategoryAsync(AdminCategoryFormViewModel model);
    Task<(bool Success, string Message)> DeleteCategoryAsync(int categoryId);

    Task<AdminReviewListViewModel> GetAdminReviewsAsync(string? filter = "All", string? search = null);
    Task<(bool Success, string Message, bool IsHidden)> ToggleReviewVisibilityAsync(int reviewId);
}
