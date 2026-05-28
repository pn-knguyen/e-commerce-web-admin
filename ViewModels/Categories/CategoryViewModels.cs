using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace e_commerce_web_admin.ViewModels.Categories;

public sealed class CategoryIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class CategoryIndexViewModel
{
    public List<CategoryRowViewModel> Categories { get; set; } = new();

    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }

    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int TotalProductCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed class CategoryRowViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public long? ParentId { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public int Position { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public int ChildCount { get; set; }
    public int Depth { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CategoryFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Slug tối đa 255 ký tự.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
    public string Slug { get; set; } = string.Empty;

    public long? ParentId { get; set; }

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự.")]
    public string? ImagePath { get; set; }

    public IFormFile? ImageFile { get; set; }

    [Range(0, 9999, ErrorMessage = "Thứ tự phải từ 0 đến 9999.")]
    public int Position { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CategorySelectItem> ParentOptions { get; set; } = new();
}

public sealed class CategorySelectItem
{
    public long Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Depth { get; set; }
}
