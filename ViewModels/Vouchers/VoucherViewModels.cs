using System.ComponentModel.DataAnnotations;
using System.Globalization;
using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.ViewModels.Vouchers;

public sealed class VoucherIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class VoucherIndexViewModel
{
    public List<VoucherRowViewModel> Vouchers { get; set; } = new();

    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int RunningCount { get; set; }
    public int UpcomingCount { get; set; }
    public int ExpiredCount { get; set; }
    public int ExhaustedCount { get; set; }
    public int TotalUsedCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed class VoucherRowViewModel
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal MinOrderValue { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public int? MaxUses { get; set; }
    public int? MaxUsesPerUser { get; set; }
    public int UsedCount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public int OrderCount { get; set; }
    public int UsageCount { get; set; }
    public int AssignedUserCount { get; set; }
    public int TargetCount { get; set; }
    public string StatusKey { get; set; } = "inactive";
    public string StatusLabel { get; set; } = "Tạm tắt";

    public string DiscountTypeLabel => DiscountType == DiscountType.Percentage ? "Theo phần trăm" : "Số tiền cố định";

    public string DiscountDisplay => DiscountType == DiscountType.Percentage
        ? $"{DiscountValue.ToString("N0", ViCulture)}%"
        : $"{DiscountValue.ToString("N0", ViCulture)} đ";

    public string MinOrderDisplay => $"{MinOrderValue.ToString("N0", ViCulture)} đ";

    public string MaxDiscountDisplay => MaxDiscountValue.HasValue
        ? $"{MaxDiscountValue.Value.ToString("N0", ViCulture)} đ"
        : "Không giới hạn";

    public string UsageDisplay => MaxUses.HasValue
        ? $"{UsedCount.ToString("N0", ViCulture)} / {MaxUses.Value.ToString("N0", ViCulture)}"
        : $"{UsedCount.ToString("N0", ViCulture)} / Không giới hạn";

    public int UsagePercent
    {
        get
        {
            if (!MaxUses.HasValue || MaxUses.Value <= 0)
            {
                return 0;
            }

            return Math.Min(100, (int)Math.Round(UsedCount * 100.0 / MaxUses.Value));
        }
    }

    public bool IsDeleteBlocked => UsedCount > 0 || UsageCount > 0 || OrderCount > 0;
}

public sealed class VoucherFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Mã voucher là bắt buộc.")]
    [StringLength(80, ErrorMessage = "Mã voucher tối đa 80 ký tự.")]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9_-]*$",
        ErrorMessage = "Mã voucher chỉ gồm chữ cái, số, dấu gạch ngang hoặc gạch dưới.")]
    public string Code { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;

    [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm phải lớn hơn 0.")]
    public decimal DiscountValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn tối thiểu không được âm.")]
    public decimal MinOrderValue { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Mức giảm tối đa phải lớn hơn 0.")]
    public decimal? MaxDiscountValue { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Tổng lượt dùng phải lớn hơn 0.")]
    public int? MaxUses { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Lượt dùng mỗi khách phải lớn hơn 0.")]
    public int? MaxUsesPerUser { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc.")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc là bắt buộc.")]
    public DateTime EndDate { get; set; }

    [Range(0, 9999, ErrorMessage = "Độ ưu tiên phải từ 0 đến 9999.")]
    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;
    public int UsedCount { get; set; }

    public List<VoucherDiscountTypeOption> DiscountTypeOptions { get; set; } = new();
}

public sealed class VoucherDiscountTypeOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
}
