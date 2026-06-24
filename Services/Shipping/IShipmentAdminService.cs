using e_commerce_web_admin.ViewModels.Shipments;

namespace e_commerce_web_admin.Services.Shipping;

public interface IShipmentAdminService
{
    Task<ShipmentPanelViewModel> GetPanelAsync(long orderId, CancellationToken ct = default);
    Task<ShipmentActionResult> CreateQuoteAsync(
        long orderId,
        ShipmentQuoteCreateViewModel form,
        long? staffId,
        CancellationToken ct = default);
    Task<ShipmentActionResult> BookShipmentAsync(long orderId, long shipmentId, CancellationToken ct = default);
    Task<ShipmentActionResult> CancelShipmentAsync(long orderId, long shipmentId, CancellationToken ct = default);
    Task<ShipmentActionResult> SyncShipmentStatusAsync(long orderId, long shipmentId, CancellationToken ct = default);
    Task<int> SyncActiveProviderStatusesAsync(CancellationToken ct = default);
    Task<ShipmentActionResult> HandleProviderWebhookAsync(string rawPayload, CancellationToken ct = default);
}
