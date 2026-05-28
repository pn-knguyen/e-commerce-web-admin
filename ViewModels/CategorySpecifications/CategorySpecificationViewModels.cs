using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_admin.ViewModels.CategorySpecifications;

// ── Index (specs gắn với 1 category) ─────────────────────────────────────

public sealed class CategorySpecIndexQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class CategorySpecIndexViewModel
{
    // Thông tin category
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;

    // Specs đã gán
    public List<CategorySpecRowViewModel> AssignedSpecs { get; set; } = new();

    // Specs chưa gán (để assign)
    public List<AvailableSpecOption> AvailableSpecs { get; set; } = new();

    // Form assign mới
    public CategorySpecAssignViewModel AssignForm { get; set; } = new();

    // Filter / Pagination cho assigned list
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalAssigned { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalAssigned / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed class CategorySpecRowViewModel
{
    public long SpecificationId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? GroupName { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public int ProductUsageCount { get; set; }
}

public sealed class AvailableSpecOption
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
}

// ── Assign / Update form ───────────────────────────────────────────────────

public sealed class CategorySpecAssignViewModel
{
    [Required]
    public long CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thông số.")]
    public long SpecificationId { get; set; }

    [StringLength(120, ErrorMessage = "Tên nhóm tối đa 120 ký tự.")]
    public string? GroupName { get; set; }

    public bool IsRequired { get; set; }

    [Range(0, 9999)]
    public int SortOrder { get; set; }
}

public sealed class CategorySpecUpdateViewModel
{
    [Required]
    public long CategoryId { get; set; }

    [Required]
    public long SpecificationId { get; set; }

    [StringLength(120)]
    public string? GroupName { get; set; }

    public bool IsRequired { get; set; }

    [Range(0, 9999)]
    public int SortOrder { get; set; }
}
