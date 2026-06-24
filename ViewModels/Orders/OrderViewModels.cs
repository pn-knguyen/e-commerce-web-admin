using System.ComponentModel.DataAnnotations;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.ViewModels.Shipments;

namespace e_commerce_web_admin.ViewModels.Orders;

public sealed class OrderIndexQuery
{
    public string? Search { get; set; }
    public string? DateRange { get; set; }
    public string? OrderStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public long? PaymentMethodId { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class OrderIndexViewModel
{
    public List<OrderRowViewModel> Orders { get; set; } = [];
    public List<OrderFilterOption> DateRangeOptions { get; set; } = [];
    public List<OrderFilterOption> OrderStatusOptions { get; set; } = [];
    public List<OrderFilterOption> PaymentStatusOptions { get; set; } = [];
    public List<OrderFilterOption> PaymentMethodOptions { get; set; } = [];

    public string? Search { get; set; }
    public string? DateRange { get; set; }
    public string? OrderStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public long? PaymentMethodId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ShippingCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal CompletedRevenue { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        !string.IsNullOrWhiteSpace(DateRange) ||
        !string.IsNullOrWhiteSpace(OrderStatus) ||
        !string.IsNullOrWhiteSpace(PaymentStatus) ||
        PaymentMethodId.HasValue;
}

public sealed class OrderRowViewModel
{
    public long Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string ShippingPhone { get; set; } = string.Empty;
    public string PaymentMethodName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class OrderDetailsViewModel
{
    public long Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public long PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public bool IsCashOnDelivery { get; set; }
    public string? VoucherCode { get; set; }
    public string ShippingContactName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingProvince { get; set; } = string.Empty;
    public string ShippingWard { get; set; } = string.Empty;
    public string ShippingDetail { get; set; } = string.Empty;
    public decimal SubtotalAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal VoucherDiscount { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItemViewModel> Items { get; set; } = [];
    public List<OrderFilterOption> OrderStatusOptions { get; set; } = [];
    public List<OrderFilterOption> PaymentStatusOptions { get; set; } = [];
    public OrderStatusUpdateViewModel StatusForm { get; set; } = new();
    public ShipmentPanelViewModel ShipmentPanel { get; set; } = new();

    public int TotalQuantity => Items.Sum(item => item.Quantity);
    public string ShippingAddress => string.Join(", ", new[]
    {
        ShippingDetail,
        ShippingWard,
        ShippingProvince,
    }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed class OrderItemViewModel
{
    public long Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class OrderStatusUpdateViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Trạng thái đơn hàng là bắt buộc.")]
    public OrderStatus OrderStatus { get; set; }

    [Required(ErrorMessage = "Trạng thái thanh toán là bắt buộc.")]
    public PaymentStatus PaymentStatus { get; set; }
}

public sealed class OrderFilterOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

public static class OrderDisplay
{
    public static string GetOrderStatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Chờ xác nhận",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Processing => "Đang xử lý",
        OrderStatus.Shipping => "Đang giao",
        OrderStatus.Completed => "Đã giao",
        OrderStatus.Cancelled => "Đã hủy",
        OrderStatus.Returned => "Đã hoàn hàng",
        _ => "Không xác định",
    };

    public static string GetPaymentStatusLabel(PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.Paid => "Đã thanh toán",
        PaymentStatus.Failed => "Thanh toán lỗi",
        PaymentStatus.Refunded => "Đã hoàn tiền",
        _ => "Không xác định",
    };

    public static string GetOrderStatusClass(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "is-pending",
        OrderStatus.Confirmed => "is-confirmed",
        OrderStatus.Processing => "is-processing",
        OrderStatus.Shipping => "is-shipping",
        OrderStatus.Completed => "is-completed",
        OrderStatus.Cancelled => "is-cancelled",
        OrderStatus.Returned => "is-returned",
        _ => "is-muted",
    };

    public static string GetPaymentStatusClass(PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid => "is-unpaid",
        PaymentStatus.Paid => "is-paid",
        PaymentStatus.Failed => "is-failed",
        PaymentStatus.Refunded => "is-refunded",
        _ => "is-muted",
    };
}
