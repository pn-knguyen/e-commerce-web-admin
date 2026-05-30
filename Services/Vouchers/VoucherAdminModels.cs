using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.Services.Vouchers;

public sealed class VoucherIndexRequest
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
}

public sealed class VoucherIndexResult
{
    public List<VoucherListItem> Vouchers { get; init; } = new();
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

public sealed class VoucherListItem
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal MinOrderValue { get; init; }
    public decimal? MaxDiscountValue { get; init; }
    public int? MaxUses { get; init; }
    public int? MaxUsesPerUser { get; init; }
    public int UsedCount { get; init; }
    public DateTime StartDateUtc { get; init; }
    public DateTime EndDateUtc { get; init; }
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public int OrderCount { get; init; }
    public int UsageCount { get; init; }
    public int AssignedUserCount { get; init; }
    public int TargetCount { get; init; }
    public string StatusKey { get; init; } = "inactive";
}

public sealed class VoucherFormData
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;
    public decimal DiscountValue { get; set; }
    public decimal MinOrderValue { get; set; }
    public decimal? MaxDiscountValue { get; set; }
    public int? MaxUses { get; set; }
    public int? MaxUsesPerUser { get; set; }
    public int UsedCount { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
}
