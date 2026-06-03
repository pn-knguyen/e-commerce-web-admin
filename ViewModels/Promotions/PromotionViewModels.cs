using System.ComponentModel.DataAnnotations;
using System.Globalization;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.Models.Validation;

namespace e_commerce_web_admin.ViewModels.Promotions;

public sealed class PromotionIndexViewModel
{
    public List<PromotionRowViewModel> Promotions { get; set; } = [];
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
    public bool HasFilters => !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Status);
}

public sealed class PromotionRowViewModel
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MinOrderValue { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public int TargetCount { get; set; }
    public int RuleCount { get; set; }
    public PromotionActionType ActionType { get; set; }
    public decimal DiscountValue { get; set; }
    public int BuyQuantity { get; set; }
    public int GetQuantity { get; set; }
    public string? GiftVariantLabel { get; set; }
    public string StatusKey { get; set; } = "inactive";
    public string StatusLabel { get; set; } = "Tạm tắt";

    public string ActionLabel => PromotionDisplay.GetActionTypeLabel(ActionType);
    public string DiscountDisplay => $"{DiscountValue.ToString("N0", ViCulture)} đ";
    public string MinOrderDisplay => $"{MinOrderValue.ToString("N0", ViCulture)} đ";
    public string MaxDiscountDisplay => MaxDiscountValue.HasValue
        ? $"{MaxDiscountValue.Value.ToString("N0", ViCulture)} đ"
        : "Không giới hạn";

    public string UsageDisplay => UsageLimit.HasValue
        ? $"{UsedCount.ToString("N0", ViCulture)} / {UsageLimit.Value.ToString("N0", ViCulture)}"
        : $"{UsedCount.ToString("N0", ViCulture)} / Không giới hạn";

    public int UsagePercent
    {
        get
        {
            if (!UsageLimit.HasValue || UsageLimit.Value <= 0)
            {
                return 0;
            }

            return Math.Min(100, (int)Math.Round(UsedCount * 100.0 / UsageLimit.Value));
        }
    }

    public bool IsDeleteBlocked => UsedCount > 0;
}

public sealed class PromotionFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = PromotionValidationMessages.NameRequired)]
    [StringLength(PromotionValidationRules.NameMaxLength, ErrorMessage = PromotionValidationMessages.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(PromotionValidationRules.DescriptionMaxLength, ErrorMessage = PromotionValidationMessages.DescriptionMaxLength)]
    public string? Description { get; set; }

    [Required(ErrorMessage = PromotionValidationMessages.PriorityRequired)]
    [Range(PromotionValidationRules.PriorityMin, PromotionValidationRules.PriorityMax, ErrorMessage = PromotionValidationMessages.PriorityRange)]
    public int? Priority { get; set; }

    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = PromotionValidationMessages.StartDateRequired)]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = PromotionValidationMessages.EndDateRequired)]
    public DateTime? EndDate { get; set; }

    [Required(ErrorMessage = PromotionValidationMessages.MinOrderRequired)]
    [Range(0, double.MaxValue, ErrorMessage = PromotionValidationMessages.MinOrderNonNegative)]
    public decimal? MinOrderValue { get; set; }

    [Range(PromotionValidationRules.PositiveAmountMin, double.MaxValue, ErrorMessage = PromotionValidationMessages.MaxDiscountPositive)]
    public decimal? MaxDiscountValue { get; set; }

    [Range(PromotionValidationRules.PositiveIntegerMin, int.MaxValue, ErrorMessage = PromotionValidationMessages.UsageLimitPositive)]
    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }
    public TargetType TargetType { get; set; } = TargetType.Category;
    public List<long> TargetIds { get; set; } = [];
    public long? RuleId { get; set; }

    [Required(ErrorMessage = PromotionValidationMessages.ActionTypeRequired)]
    public PromotionActionType ActionType { get; set; } = PromotionActionType.DiscountOrder;

    [Required(ErrorMessage = PromotionValidationMessages.DiscountValueRequired)]
    [Range(0, double.MaxValue, ErrorMessage = PromotionValidationMessages.DiscountValueNonNegative)]
    public decimal? DiscountValue { get; set; }

    [Required(ErrorMessage = PromotionValidationMessages.BuyQuantityRequired)]
    [Range(PromotionValidationRules.PositiveIntegerMin, int.MaxValue, ErrorMessage = PromotionValidationMessages.BuyQuantityPositive)]
    public int? BuyQuantity { get; set; } = 1;

    [Required(ErrorMessage = PromotionValidationMessages.GetQuantityRequired)]
    [Range(PromotionValidationRules.NonNegativeIntegerMin, int.MaxValue, ErrorMessage = PromotionValidationMessages.GetQuantityNonNegative)]
    public int? GetQuantity { get; set; }

    public long? GiftProductVariantId { get; set; }

    public List<PromotionActionTypeOption> ActionTypeOptions { get; set; } = [];
    public List<PromotionTargetTypeOption> TargetTypeOptions { get; set; } = [];
    public IReadOnlyList<PromotionTargetOption> TargetOptions { get; set; } = [];
    public IReadOnlyList<PromotionGiftVariantOption> GiftVariantOptions { get; set; } = [];

    public string EndDateAfterStartMessage => PromotionValidationMessages.EndDateAfterStart;
    public string UsageLimitLessThanUsedMessage => string.Format(PromotionValidationMessages.UsageLimitLessThanUsed, UsedCount);
    public string DiscountValuePositiveMessage => PromotionValidationMessages.DiscountValuePositive;
    public string GiftQuantityPositiveMessage => PromotionValidationMessages.GiftQuantityPositive;
    public string GiftVariantRequiredMessage => PromotionValidationMessages.GiftVariantRequired;
    public string BuyXGetYRequiresBenefitMessage => PromotionValidationMessages.BuyXGetYRequiresBenefit;
    public string MaxDiscountLessThanDiscountMessage => PromotionValidationMessages.MaxDiscountLessThanDiscount;
    public string TargetRequiredMessage => PromotionValidationMessages.TargetRequired;
}

public sealed class PromotionActionTypeOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
}

public sealed class PromotionTargetTypeOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
}

public sealed class PromotionTargetOption
{
    public TargetType TargetType { get; set; }
    public long Value { get; set; }
    public string Text { get; set; } = string.Empty;
}

public sealed class PromotionGiftVariantOption
{
    public long Value { get; set; }
    public string Text { get; set; } = string.Empty;
}

public static class PromotionDisplay
{
    public static string GetActionTypeLabel(PromotionActionType actionType) => actionType switch
    {
        PromotionActionType.DiscountOrder => "Giảm trên đơn",
        PromotionActionType.DiscountProduct => "Giảm sản phẩm",
        PromotionActionType.BuyXGetY => "Mua X nhận Y",
        PromotionActionType.GiftProduct => "Tặng sản phẩm",
        _ => "Không xác định",
    };

    public static string GetTargetTypeLabel(TargetType targetType) => targetType switch
    {
        TargetType.Category => "Danh mục",
        TargetType.Brand => "Thương hiệu",
        TargetType.Product => "Sản phẩm",
        TargetType.ProductVariant => "Biến thể sản phẩm",
        _ => "Không xác định",
    };

    public static string GetStatusLabel(string statusKey) => statusKey switch
    {
        "running" => "Đang chạy",
        "upcoming" => "Sắp diễn ra",
        "expired" => "Hết hạn",
        "exhausted" => "Hết lượt",
        _ => "Tạm tắt",
    };
}
