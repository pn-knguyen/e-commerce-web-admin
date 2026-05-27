using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_admin.Models.ViewModels;


// ─── List ──────────────────────────────────────────────────────────────────

/// <summary>Một dòng trong bảng danh sách danh mục.</summary>
public class CategoryRowViewModel
{
    public long   Id           { get; set; }
    public string Name         { get; set; } = string.Empty;
    public string Slug         { get; set; } = string.Empty;
    public long?  ParentId     { get; set; }
    public string ParentName   { get; set; } = string.Empty;
    public int    Position     { get; set; }
    public bool   IsActive     { get; set; }
    public int    ProductCount { get; set; }
    public int    ChildCount   { get; set; }
    public int    Depth        { get; set; }   // 0 = root, 1 = cấp 1, ...
    public DateTime CreatedAt  { get; set; }
}

/// <summary>ViewModel cho trang danh sách.</summary>
public class CategoryIndexViewModel
{
    public List<CategoryRowViewModel> Categories { get; set; } = new();

    // Bộ lọc / tìm kiếm
    public string?  Search    { get; set; }
    public string?  Status    { get; set; }   // "" | "active" | "inactive"
    public int      Page      { get; set; } = 1;
    public int      PageSize  { get; set; } = 20;
    public int      TotalCount{ get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev   => Page > 1;
    public bool HasNext   => Page < TotalPages;
}

// ─── Form (Create / Edit) ──────────────────────────────────────────────────

/// <summary>ViewModel cho form tạo / chỉnh sửa danh mục.</summary>
public class CategoryFormViewModel
{
    public long    Id          { get; set; }

    // --- Core fields ---
    [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên tối đa 255 ký tự.")]
    public string  Name        { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Slug tối đa 255 ký tự.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
    public string  Slug        { get; set; } = string.Empty;

    public long?   ParentId    { get; set; }

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự.")]
    public string? ImagePath   { get; set; }

    [Range(0, 9999, ErrorMessage = "Thứ tự phải từ 0 đến 9999.")]
    public int     Position    { get; set; }

    public bool    IsActive    { get; set; } = true;

    // --- Dropdown choices ---
    public List<CategorySelectItem> ParentOptions { get; set; } = new();
}


/// <summary>Mục trong dropdown chọn danh mục cha.</summary>
public class CategorySelectItem
{
    public long   Id    { get; set; }
    public string Label { get; set; } = string.Empty;   // "── Tên" (có thụt lề)
    public int    Depth { get; set; }
}
