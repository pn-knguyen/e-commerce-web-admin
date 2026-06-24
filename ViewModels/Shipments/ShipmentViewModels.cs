using System.ComponentModel.DataAnnotations;
using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.ViewModels.Shipments;

public sealed class ShipmentPanelViewModel
{
    public bool IsProviderConfigured { get; set; }
    public bool HasFulfillmentLocations => FulfillmentLocations.Count > 0;
    public List<FulfillmentLocationOptionViewModel> FulfillmentLocations { get; set; } = [];
    public ShipmentSummaryViewModel? CurrentShipment { get; set; }
    public List<ShipmentSummaryViewModel> ShipmentHistory { get; set; } = [];
    public ShipmentQuoteCreateViewModel QuoteForm { get; set; } = new();
}

public sealed class FulfillmentLocationOptionViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public sealed class ShipmentSummaryViewModel
{
    public long Id { get; set; }
    public ShipmentStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public string Provider { get; set; } = "GHN";
    public string? ProviderDeliveryId { get; set; }
    public string? ProviderStatus { get; set; }
    public string? TrackingUrl { get; set; }
    public decimal? QuotedFee { get; set; }
    public decimal? ActualFee { get; set; }
    public string Currency { get; set; } = "VND";
    public string PickupAddress { get; set; } = string.Empty;
    public string DropoffAddress { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? BookedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public List<ShipmentPackageSummaryViewModel> Packages { get; set; } = [];
    public List<ShipmentEventSummaryViewModel> RecentEvents { get; set; } = [];
    public bool CanBook => Status is ShipmentStatus.Quoted;
    public bool CanCancel => ShipmentDisplay.CanCancel(Status);
    public bool CanSync => !string.IsNullOrWhiteSpace(ProviderDeliveryId) &&
        ShipmentDisplay.CanSync(Status);
}

public sealed class ShipmentPackageSummaryViewModel
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? WeightGrams { get; set; }
    public decimal? DeclaredValue { get; set; }
}

public sealed class ShipmentEventSummaryViewModel
{
    public string StatusLabel { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime OccurredAt { get; set; }
}

public sealed class ShipmentQuoteCreateViewModel : IValidatableObject
{
    public const int MaxQuantity = 999;
    public const int MaxWeightGrams = 30_000;
    public const decimal MinDimensionCm = 1m;
    public const decimal MaxDimensionCm = 150m;

    public long OrderId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn điểm lấy hàng.")]
    [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn điểm lấy hàng.")]
    public long? FulfillmentLocationId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả kiện hàng.")]
    [StringLength(500, ErrorMessage = "Mô tả kiện hàng tối đa 500 ký tự.")]
    public string PackageDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành giao đến theo đơn vị vận chuyển.")]
    [StringLength(50, ErrorMessage = "Mã tỉnh/thành giao đến tối đa 50 ký tự.")]
    public string? ProviderDropoffProvinceCode { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành giao đến theo đơn vị vận chuyển.")]
    [StringLength(120, ErrorMessage = "Tên tỉnh/thành giao đến tối đa 120 ký tự.")]
    public string? ProviderDropoffProvinceName { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn quận/huyện giao đến theo đơn vị vận chuyển.")]
    [StringLength(50, ErrorMessage = "Mã quận/huyện giao đến tối đa 50 ký tự.")]
    public string? ProviderDropoffDistrictCode { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn quận/huyện giao đến theo đơn vị vận chuyển.")]
    [StringLength(120, ErrorMessage = "Tên quận/huyện giao đến tối đa 120 ký tự.")]
    public string? ProviderDropoffDistrictName { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phường/xã giao đến theo đơn vị vận chuyển.")]
    [StringLength(50, ErrorMessage = "Mã phường/xã giao đến tối đa 50 ký tự.")]
    public string? ProviderDropoffWardCode { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phường/xã giao đến theo đơn vị vận chuyển.")]
    [StringLength(120, ErrorMessage = "Tên phường/xã giao đến tối đa 120 ký tự.")]
    public string? ProviderDropoffWardName { get; set; }

    [Range(1, 999, ErrorMessage = "Số lượng kiện hàng phải lớn hơn 0.")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Vui lòng nhập cân nặng kiện hàng.")]
    [Range(1, MaxWeightGrams, ErrorMessage = "Cân nặng phải từ 1 đến 30000 gram.")]
    public int? WeightGrams { get; set; }

    [StringLength(40, ErrorMessage = "Chiều dài tối đa 40 ký tự.")]
    [Required(ErrorMessage = "Vui lòng nhập chiều dài kiện hàng.")]
    public string? LengthCm { get; set; }

    [StringLength(40, ErrorMessage = "Chiều rộng tối đa 40 ký tự.")]
    [Required(ErrorMessage = "Vui lòng nhập chiều rộng kiện hàng.")]
    public string? WidthCm { get; set; }

    [StringLength(40, ErrorMessage = "Chiều cao tối đa 40 ký tự.")]
    [Required(ErrorMessage = "Vui lòng nhập chiều cao kiện hàng.")]
    public string? HeightCm { get; set; }

    [StringLength(40, ErrorMessage = "Giá trị khai báo tối đa 40 ký tự.")]
    public string? DeclaredValue { get; set; }

    public bool IsFragile { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự.")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in ValidateDecimal(LengthCm, "Chiều dài", nameof(LengthCm), MinDimensionCm, MaxDimensionCm))
        {
            yield return result;
        }

        foreach (var result in ValidateDecimal(WidthCm, "Chiều rộng", nameof(WidthCm), MinDimensionCm, MaxDimensionCm))
        {
            yield return result;
        }

        foreach (var result in ValidateDecimal(HeightCm, "Chiều cao", nameof(HeightCm), MinDimensionCm, MaxDimensionCm))
        {
            yield return result;
        }

        foreach (var result in ValidateNonNegativeDecimal(DeclaredValue, "Giá trị khai báo", nameof(DeclaredValue)))
        {
            yield return result;
        }
    }

    private static IEnumerable<ValidationResult> ValidateDecimal(
        string? value,
        string label,
        string memberName,
        decimal min,
        decimal max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        if (!ShipmentFormNumberParser.TryParseDecimal(value, out var number))
        {
            yield return new ValidationResult($"{label} phải là số hợp lệ.", [memberName]);
            yield break;
        }

        if (number < min || number > max)
        {
            yield return new ValidationResult(
                $"{label} phải từ {ShipmentFormNumberParser.Format(min)} đến {ShipmentFormNumberParser.Format(max)}.",
                [memberName]);
        }
    }

    private static IEnumerable<ValidationResult> ValidateNonNegativeDecimal(
        string? value,
        string label,
        string memberName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        if (!ShipmentFormNumberParser.TryParseDecimal(value, out var number))
        {
            yield return new ValidationResult($"{label} phải là số hợp lệ.", [memberName]);
            yield break;
        }

        if (number < 0)
        {
            yield return new ValidationResult($"{label} không được là số âm.", [memberName]);
        }
    }
}

public static class ShipmentDisplay
{
    public static string GetStatusLabel(ShipmentStatus status) => status switch
    {
        ShipmentStatus.Draft => "Nháp",
        ShipmentStatus.Quoted => "Đã báo giá",
        ShipmentStatus.Booking => "Đang tạo vận đơn",
        ShipmentStatus.Booked => "Chờ lấy hàng",
        ShipmentStatus.ReadyToPick => "Mới tạo đơn",
        ShipmentStatus.PickingUp => "Đang lấy hàng",
        ShipmentStatus.Picking => "Nhân viên đang lấy hàng",
        ShipmentStatus.MoneyCollectPicking => "Đang thu tiền người gửi",
        ShipmentStatus.Picked => "Đã lấy hàng",
        ShipmentStatus.InTransit => "Đang vận chuyển",
        ShipmentStatus.Storing => "Hàng đang ở kho",
        ShipmentStatus.Transporting => "Đang luân chuyển",
        ShipmentStatus.Sorting => "Đang phân loại",
        ShipmentStatus.Delivering => "Đang giao cho khách",
        ShipmentStatus.MoneyCollectDelivering => "Đang thu tiền người nhận",
        ShipmentStatus.Delivered => "Đã giao",
        ShipmentStatus.Cancelled => "Đã hủy",
        ShipmentStatus.Failed => "Lỗi giao hàng",
        ShipmentStatus.DeliveryFail => "Giao hàng thất bại",
        ShipmentStatus.WaitingToReturn => "Chờ hoàn hàng",
        ShipmentStatus.Return => "Đang chờ trả hàng",
        ShipmentStatus.ReturnTransporting => "Đang luân chuyển hàng hoàn",
        ShipmentStatus.ReturnSorting => "Đang phân loại hàng hoàn",
        ShipmentStatus.Returning => "Đang trả hàng",
        ShipmentStatus.ReturnFail => "Trả hàng thất bại",
        ShipmentStatus.Returned => "Đã hoàn hàng",
        ShipmentStatus.Exception => "Đơn ngoại lệ",
        ShipmentStatus.Damage => "Hàng hư hỏng",
        ShipmentStatus.Lost => "Hàng thất lạc",
        ShipmentStatus.ProviderUnknown => "Trạng thái GHN chưa xác định",
        _ => "Không xác định",
    };

    public static string GetStatusClass(ShipmentStatus status) => status switch
    {
        ShipmentStatus.Draft => "is-muted",
        ShipmentStatus.Quoted => "is-confirmed",
        ShipmentStatus.Booking or ShipmentStatus.Booked or ShipmentStatus.ReadyToPick => "is-processing",
        ShipmentStatus.PickingUp or ShipmentStatus.Picking or ShipmentStatus.MoneyCollectPicking or ShipmentStatus.Picked => "is-shipping",
        ShipmentStatus.InTransit or ShipmentStatus.Storing or ShipmentStatus.Transporting or ShipmentStatus.Sorting or ShipmentStatus.Delivering or ShipmentStatus.MoneyCollectDelivering => "is-shipping",
        ShipmentStatus.Delivered => "is-completed",
        ShipmentStatus.Cancelled => "is-cancelled",
        ShipmentStatus.Failed or ShipmentStatus.DeliveryFail or ShipmentStatus.ReturnFail or ShipmentStatus.Exception or ShipmentStatus.Damage or ShipmentStatus.Lost => "is-failed",
        ShipmentStatus.WaitingToReturn or ShipmentStatus.Return or ShipmentStatus.ReturnTransporting or ShipmentStatus.ReturnSorting or ShipmentStatus.Returning or ShipmentStatus.Returned => "is-returned",
        _ => "is-muted",
    };

    public static bool CanCancel(ShipmentStatus status) => status is
        ShipmentStatus.Booked or
        ShipmentStatus.ReadyToPick or
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
        ShipmentStatus.Exception;

    public static bool CanSync(ShipmentStatus status) => status is
        ShipmentStatus.Booked or
        ShipmentStatus.ReadyToPick or
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
        ShipmentStatus.Exception or
        ShipmentStatus.ProviderUnknown;
}
