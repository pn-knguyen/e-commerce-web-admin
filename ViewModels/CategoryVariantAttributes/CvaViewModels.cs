using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_admin.ViewModels.CategoryVariantAttributes;

// ── Query ──────────────────────────────────────────────────────────────────

public sealed record CvaIndexQuery
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
}

// ── Index ──────────────────────────────────────────────────────────────────

public sealed class CvaIndexViewModel
{
    public long CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string CategorySlug { get; init; } = string.Empty;

    public List<CvaRowViewModel> AssignedAttributes { get; init; } = [];
    public List<CvaAvailableOption> AvailableAttributes { get; init; } = [];

    public string? Search { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalAssigned { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalAssigned / PageSize) : 1;
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>Một thuộc tính đã được gán vào danh mục.</summary>
public sealed class CvaRowViewModel
{
    public long AttributeId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int OptionCount { get; init; }
    public int VariantUsageCount { get; init; }
    public DateTime AssignedAt { get; init; }
}

/// <summary>Thuộc tính chưa gán — dùng cho dropdown.</summary>
public sealed class CvaAvailableOption
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int OptionCount { get; init; }
}

// ── Assign form ────────────────────────────────────────────────────────────

public sealed class CvaAssignViewModel
{
    [Required]
    public long CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thuộc tính cần gán.")]
    [Range(1, long.MaxValue, ErrorMessage = "Thuộc tính không hợp lệ.")]
    public long AttributeId { get; set; }
}
