using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.Services.Inventory;

public interface IInventoryLedgerService
{
    Task<long?> ResolveDefaultLocationIdAsync(CancellationToken ct = default);

    Task ApplyReceiptApprovalAsync(
        GoodsReceipt receipt,
        long? fulfillmentLocationId,
        DateTime now,
        CancellationToken ct = default);

    Task ApplyOrderSaleAsync(
        Order order,
        CancellationToken ct = default);

    Task ApplyOrderSaleAsync(
        Order order,
        long? fulfillmentLocationId,
        CancellationToken ct = default);

    Task ApplyOrderStatusChangeAsync(
        Order order,
        OrderStatus from,
        OrderStatus to,
        CancellationToken ct = default);
}
