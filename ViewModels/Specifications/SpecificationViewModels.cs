using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_admin.ViewModels.Specifications;

// ── Index ──────────────────────────────────────────────────────────────────

public sealed class SpecificationIndexQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class SpecificationIndexViewModel
{
    public List<SpecificationRowViewModel> Specifications { get; set; } = new();
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalCount { get; set; }
    public int TotalCategoryUsageCount { get; set; }
    public int TotalProductUsageCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed class SpecificationRowViewModel
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? Icon { get; set; }
    public int CategoryCount { get; set; }
    public int ProductCount { get; set; }
}

// ── Form ───────────────────────────────────────────────────────────────────

public sealed class SpecificationFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Key là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Key tối đa 100 ký tự.")]
    [RegularExpression(@"^[a-z0-9_]+$",
        ErrorMessage = "Key chỉ gồm chữ thường, số và dấu gạch dưới (_).")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên thông số là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Đơn vị tối đa 50 ký tự.")]
    public string? Unit { get; set; }

    [StringLength(100, ErrorMessage = "Icon tối đa 100 ký tự.")]
    public string? Icon { get; set; }
}
