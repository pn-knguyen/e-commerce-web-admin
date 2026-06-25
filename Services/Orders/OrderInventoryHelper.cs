using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.Services.Orders;

public static class OrderInventoryHelper
{
    /// <summary>
    /// Cập nhật tồn kho khi trạng thái đơn hàng thay đổi.
    ///
    /// Nguyên tắc phối hợp với storefront:
    ///   - Storefront chịu trách nhiệm trừ Quantity và cộng SoldCount khi đơn được đặt.
    ///   - Admin chịu trách nhiệm HOÀN LẠI cả Quantity lẫn SoldCount khi đơn bị hủy hoặc trả.
    ///   - Trạng thái Completed không thay đổi SoldCount (storefront đã ghi nhận từ lúc đặt).
    ///
    /// Quy tắc:
    ///   - → Cancelled (mọi trạng thái trước): Quantity += qty, SoldCount -= qty.
    ///   - → Returned  (mọi trạng thái trước): Quantity += qty, SoldCount -= qty.
    ///   - → Completed: không thay đổi tồn kho (storefront quản lý).
    /// </summary>
    public static void ApplyInventoryChange(Order order, OrderStatus from, OrderStatus to)
    {
        if (from == to) return;

        var now = DateTime.UtcNow;
        var items = order.OrderItems
            .Where(item => item.ProductVariant is not null)
            .GroupBy(item => item.ProductVariantId)
            .Select(g => (Variant: g.First().ProductVariant!, Qty: g.Sum(item => item.Quantity)))
            .ToList();

        if (items.Count == 0) return;

        if (to is not (OrderStatus.Cancelled or OrderStatus.Returned)) return;

        // Hoàn trả Quantity và SoldCount cho mọi trường hợp hủy / trả hàng,
        // bất kể trạng thái trước đó là gì.
        foreach (var (variant, qty) in items)
        {
            variant.Quantity += qty;
            variant.SoldCount = Math.Max(0, variant.SoldCount - qty);
            variant.UpdatedAt = now;
            if (variant.Product is not null)
            {
                variant.Product.TotalSoldCount = Math.Max(0, variant.Product.TotalSoldCount - qty);
                variant.Product.UpdatedAt = now;
            }
        }
    }
}
