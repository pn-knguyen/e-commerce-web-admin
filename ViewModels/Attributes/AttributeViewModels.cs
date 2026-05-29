using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_admin.ViewModels.Attributes;

// ── Index ──────────────────────────────────────────────────────────────────

public sealed class AttributeIndexQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class AttributeIndexViewModel
{
    public List<AttributeRowViewModel> Attributes { get; set; } = [];
    public string? Search { get; set; }

    // Pagination
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;

    // Stats
    public int TotalOptionCount { get; set; }
    public int TotalCategoryUsageCount { get; set; }
    public int TotalVariantUsageCount { get; set; }
}

public sealed class AttributeRowViewModel
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int OptionCount { get; set; }
    public int CategoryCount { get; set; }
    public int VariantUsageCount { get; set; }
}

// ── Form (Create / Edit) ───────────────────────────────────────────────────

public sealed class AttributeFormViewModel
{
    public long? Id { get; set; }

    [Required(ErrorMessage = "Mã thuộc tính là bắt buộc.")]
    [StringLength(80, ErrorMessage = "Mã thuộc tính tối đa 80 ký tự.")]
    [RegularExpression(@"^[a-z0-9_]+$", ErrorMessage = "Mã thuộc tính chỉ gồm chữ thường a-z, số và dấu gạch dưới (_).")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên thuộc tính là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    public List<AttributeOptionDraftViewModel> Options { get; set; } = [];
}

public sealed class AttributeOptionDraftViewModel
{
    [Required(ErrorMessage = "Mã giá trị là bắt buộc.")]
    [StringLength(120, ErrorMessage = "Mã giá trị tối đa 120 ký tự.")]
    public string Value { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên hiển thị là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên hiển thị tối đa 255 ký tự.")]
    public string Label { get; set; } = string.Empty;
}

// ── Options ────────────────────────────────────────────────────────────────

public sealed class AttributeOptionsViewModel
{
    public long AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string AttributeCode { get; set; } = string.Empty;
    public List<AttributeOptionRowViewModel> Options { get; set; } = [];
}

public sealed class AttributeOptionRowViewModel
{
    public long Id { get; set; }
    public long AttributeId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int VariantUsageCount { get; set; }
}

public sealed class AttributeOptionFormViewModel
{
    public long AttributeId { get; set; }

    [Required(ErrorMessage = "Mã giá trị là bắt buộc.")]
    [StringLength(120, ErrorMessage = "Mã giá trị tối đa 120 ký tự.")]
    public string Value { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên hiển thị là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên hiển thị tối đa 255 ký tự.")]
    public string Label { get; set; } = string.Empty;
}

public sealed class AttributeOptionUpdateViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tên hiển thị là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên hiển thị tối đa 255 ký tự.")]
    public string Label { get; set; } = string.Empty;
}

// ── Edit page (attribute form + options panel) ─────────────────────────────

public sealed class AttributeEditViewModel
{
    public AttributeFormViewModel Form { get; set; } = new();
    public AttributeOptionsViewModel Options { get; set; } = new();
}
