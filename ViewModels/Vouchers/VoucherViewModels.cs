using System.ComponentModel.DataAnnotations;
using System.Globalization;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.Models.Validation;

namespace e_commerce_web_admin.ViewModels.Vouchers;

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

    [Required(ErrorMessage = VoucherValidationMessages.CodeRequired)]
    [StringLength(VoucherValidationRules.CodeMaxLength, ErrorMessage = VoucherValidationMessages.CodeMaxLength)]
    [RegularExpression(VoucherValidationRules.CodePattern, ErrorMessage = VoucherValidationMessages.CodePattern)]
    public string Code { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = VoucherValidationMessages.DescriptionMaxLength)]
    public string? Description { get; set; }

    [Required(ErrorMessage = VoucherValidationMessages.DiscountTypeRequired)]
    public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;

    [Required(ErrorMessage = VoucherValidationMessages.DiscountValueRequired)]
    [Range(VoucherValidationRules.PositiveAmountMin, double.MaxValue, ErrorMessage = VoucherValidationMessages.DiscountValuePositive)]
    public decimal DiscountValue { get; set; }

    [Required(ErrorMessage = VoucherValidationMessages.MinOrderRequired)]
    [Range(0, double.MaxValue, ErrorMessage = VoucherValidationMessages.MinOrderNonNegative)]
    public decimal MinOrderValue { get; set; }

    [Range(VoucherValidationRules.PositiveAmountMin, double.MaxValue, ErrorMessage = VoucherValidationMessages.MaxDiscountPositive)]
    public decimal? MaxDiscountValue { get; set; }

    [Range(VoucherValidationRules.PositiveIntegerMin, int.MaxValue, ErrorMessage = VoucherValidationMessages.MaxUsesPositive)]
    public int? MaxUses { get; set; }

    [Range(VoucherValidationRules.PositiveIntegerMin, int.MaxValue, ErrorMessage = VoucherValidationMessages.MaxUsesPerUserPositive)]
    public int? MaxUsesPerUser { get; set; }

    [Required(ErrorMessage = VoucherValidationMessages.StartDateRequired)]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = VoucherValidationMessages.EndDateRequired)]
    public DateTime EndDate { get; set; }

    [Required(ErrorMessage = VoucherValidationMessages.PriorityRequired)]
    [Range(VoucherValidationRules.PriorityMin, VoucherValidationRules.PriorityMax, ErrorMessage = VoucherValidationMessages.PriorityRange)]
    public int Priority { get; set; }

    public bool IsActive { get; set; } = true;
    public int UsedCount { get; set; }

    public List<VoucherDiscountTypeOption> DiscountTypeOptions { get; set; } = new();

    public int CodeInputMaxLength => VoucherValidationRules.CodeInputMaxLength;
    public int CodeMaxLength => VoucherValidationRules.CodeMaxLength;
    public int PercentageDiscountMax => VoucherValidationRules.PercentageDiscountMax;
    public string PercentageDiscountMaxMessage => VoucherValidationMessages.PercentageDiscountMax;
    public string FixedMaxDiscountMessage => VoucherValidationMessages.FixedMaxDiscount;
    public string MaxUsesPerUserExceedsMaxUsesMessage => VoucherValidationMessages.MaxUsesPerUserExceedsMaxUses;
    public string EndDateAfterStartMessage => VoucherValidationMessages.EndDateAfterStart;
}

public sealed class VoucherDiscountTypeOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
}
