using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_admin.ViewModels.CategorySpecifications;

public sealed class CategorySpecIndexQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class CategorySpecIndexViewModel
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;

    public List<CategorySpecRowViewModel> AssignedSpecs { get; set; } = new();
    public List<AvailableSpecOption> AvailableSpecs { get; set; } = new();
    public CategorySpecAssignViewModel AssignForm { get; set; } = new();

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

public sealed class CategorySpecAssignViewModel
{
    [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "Không xác định được danh mục.")]
    public long CategoryId { get; set; }

    public List<CategorySpecAssignItemViewModel> Items { get; set; } = new();
}

public sealed class CategorySpecAssignItemViewModel
{
    public long SpecificationId { get; set; }
    public bool Selected { get; set; }
    public string? GroupName { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
}

public sealed class CategorySpecUpdateViewModel
{
    [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "Không xác định được danh mục.")]
    public long CategoryId { get; set; }

    [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "Không xác định được thông số.")]
    public long SpecificationId { get; set; }

    [StringLength(120)]
    public string? GroupName { get; set; }

    public bool IsRequired { get; set; }

    [Range(0, 9999)]
    public int SortOrder { get; set; }
}