using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.Services.Promotions;

public sealed class PromotionIndexRequest
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
}

public sealed class PromotionIndexResult
{
    public List<PromotionListItem> Promotions { get; init; } = [];
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 30;
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int InactiveCount { get; init; }
    public int RunningCount { get; init; }
    public int UpcomingCount { get; init; }
    public int ExpiredCount { get; init; }
    public int ExhaustedCount { get; init; }
    public int TotalUsedCount { get; init; }
}

public sealed class PromotionListItem
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public DateTime StartDateUtc { get; init; }
    public DateTime EndDateUtc { get; init; }
    public decimal MinOrderValue { get; init; }
    public decimal? MaxDiscountValue { get; init; }
    public int? UsageLimit { get; init; }
    public int UsedCount { get; init; }
    public int TargetCount { get; init; }
    public int RuleCount { get; init; }
    public PromotionActionType ActionType { get; init; }
    public decimal DiscountValue { get; init; }
    public int BuyQuantity { get; init; }
    public int GetQuantity { get; init; }
    public string? GiftVariantLabel { get; init; }
    public string StatusKey { get; init; } = "inactive";
}

public sealed class PromotionFormData
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public decimal MinOrderValue { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public TargetType TargetType { get; set; } = TargetType.Category;
    public List<long> TargetIds { get; set; } = [];
    public long? RuleId { get; set; }
    public long? GiftProductVariantId { get; set; }
    public PromotionActionType ActionType { get; set; } = PromotionActionType.DiscountOrder;
    public decimal DiscountValue { get; set; }
    public int BuyQuantity { get; set; } = 1;
    public int GetQuantity { get; set; }
}
