using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.Services.Shipping;

internal static class ShipmentStatusMapper
{
    public static ShipmentStatus FromGiaoHangNhanhStatus(string? providerStatus)
    {
        var normalized = NormalizeProviderStatus(providerStatus);
        return normalized switch
        {
            "READY_TO_PICK" => ShipmentStatus.ReadyToPick,
            "PICKING" => ShipmentStatus.Picking,
            "MONEY_COLLECT_PICKING" => ShipmentStatus.MoneyCollectPicking,
            "PICKED" => ShipmentStatus.Picked,
            "STORING" => ShipmentStatus.Storing,
            "TRANSPORTING" => ShipmentStatus.Transporting,
            "SORTING" => ShipmentStatus.Sorting,
            "DELIVERING" => ShipmentStatus.Delivering,
            "MONEY_COLLECT_DELIVERING" => ShipmentStatus.MoneyCollectDelivering,
            "DELIVERED" => ShipmentStatus.Delivered,
            "CANCEL" or "CANCELED" or "CANCELLED" => ShipmentStatus.Cancelled,
            "DELIVERY_FAIL" => ShipmentStatus.DeliveryFail,
            "WAITING_TO_RETURN" => ShipmentStatus.WaitingToReturn,
            "RETURN" => ShipmentStatus.Return,
            "RETURN_TRANSPORTING" => ShipmentStatus.ReturnTransporting,
            "RETURN_SORTING" => ShipmentStatus.ReturnSorting,
            "RETURNING" => ShipmentStatus.Returning,
            "RETURN_FAIL" => ShipmentStatus.ReturnFail,
            "RETURNED" => ShipmentStatus.Returned,
            "EXCEPTION" => ShipmentStatus.Exception,
            "DAMAGE" => ShipmentStatus.Damage,
            "LOST" => ShipmentStatus.Lost,
            _ => ShipmentStatus.ProviderUnknown,
        };
    }

    public static bool IsPickupProgressStatus(ShipmentStatus status) => status is
        ShipmentStatus.PickingUp or
        ShipmentStatus.Picking or
        ShipmentStatus.MoneyCollectPicking or
        ShipmentStatus.Picked;

    public static bool IsFailureStatus(ShipmentStatus status) => status is
        ShipmentStatus.Failed or
        ShipmentStatus.DeliveryFail or
        ShipmentStatus.ReturnFail or
        ShipmentStatus.Exception or
        ShipmentStatus.Damage or
        ShipmentStatus.Lost;

    public static void SyncOrderStatusFromShipment(Order? order, ShipmentStatus shipmentStatus, DateTime now)
    {
        if (order is null)
        {
            return;
        }

        var orderStatus = shipmentStatus == ShipmentStatus.Cancelled
            ? order.OrderStatus == OrderStatus.Shipping ? OrderStatus.Processing : null
            : MapOrderStatusFromShipment(shipmentStatus);

        if (!orderStatus.HasValue)
        {
            return;
        }

        var changed = false;
        if (order.OrderStatus != orderStatus.Value)
        {
            order.OrderStatus = orderStatus.Value;
            changed = true;
        }

        if (shipmentStatus == ShipmentStatus.Delivered &&
            IsCashOnDelivery(order) &&
            order.PaymentStatus != PaymentStatus.Refunded)
        {
            if (order.PaymentStatus != PaymentStatus.Paid)
            {
                order.PaymentStatus = PaymentStatus.Paid;
                changed = true;
            }
        }

        if (changed)
        {
            order.UpdatedAt = now;
        }
    }

    public static bool IsCashOnDelivery(Order order)
    {
        if (order.PaymentMethodId == PaymentMethodIds.CashOnDelivery)
        {
            return true;
        }

        var paymentName = order.PaymentMethod?.Name;
        return !string.IsNullOrWhiteSpace(paymentName) &&
            (paymentName.Contains("COD", StringComparison.OrdinalIgnoreCase) ||
                paymentName.Contains("nhận hàng", StringComparison.OrdinalIgnoreCase));
    }

    private static OrderStatus? MapOrderStatusFromShipment(ShipmentStatus shipmentStatus) => shipmentStatus switch
    {
        ShipmentStatus.ReadyToPick or
        ShipmentStatus.Booked or
        ShipmentStatus.PickingUp or
        ShipmentStatus.Picking or
        ShipmentStatus.MoneyCollectPicking or
        ShipmentStatus.Picked or
        ShipmentStatus.Storing or
        ShipmentStatus.Transporting or
        ShipmentStatus.Sorting or
        ShipmentStatus.InTransit or
        ShipmentStatus.Delivering or
        ShipmentStatus.MoneyCollectDelivering or
        ShipmentStatus.DeliveryFail or
        ShipmentStatus.WaitingToReturn or
        ShipmentStatus.Return or
        ShipmentStatus.ReturnTransporting or
        ShipmentStatus.ReturnSorting or
        ShipmentStatus.Returning or
        ShipmentStatus.ReturnFail or
        ShipmentStatus.Exception => OrderStatus.Shipping,

        ShipmentStatus.Delivered => OrderStatus.Completed,
        ShipmentStatus.Returned or ShipmentStatus.Damage or ShipmentStatus.Lost => OrderStatus.Returned,
        _ => null,
    };

    private static string NormalizeProviderStatus(string? providerStatus) =>
        string.IsNullOrWhiteSpace(providerStatus)
            ? string.Empty
            : providerStatus.Trim()
                .Replace("-", "_", StringComparison.Ordinal)
                .Replace(" ", "_", StringComparison.Ordinal)
                .ToUpperInvariant();
}
