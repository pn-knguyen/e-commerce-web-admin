using e_commerce_web_admin.ViewModels.Orders;

namespace e_commerce_web_admin.Services.Orders;

public interface IOrderAdminService
{
    Task<OrderIndexViewModel> GetIndexAsync(OrderIndexQuery query, CancellationToken ct = default);
    Task<OrderDetailsViewModel?> GetDetailsAsync(long id, CancellationToken ct = default);
    Task<OrderProfitReportViewModel> GetProfitReportAsync(OrderProfitReportQuery query, CancellationToken ct = default);
    Task<OrderStatusUpdateResult> UpdateStatusAsync(long id, OrderStatusUpdateViewModel form, CancellationToken ct = default);
}
