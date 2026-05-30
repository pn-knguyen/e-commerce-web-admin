using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_admin.ViewModels.Products;

public sealed class ProductIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Featured { get; set; }
    public long? BrandId { get; set; }
    public long? CategoryId { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class ProductIndexViewModel
{
    public List<ProductRowViewModel> Products { get; set; } = [];
    public List<ProductSelectItem> BrandOptions { get; set; } = [];
    public List<ProductCategorySelectItem> CategoryOptions { get; set; } = [];

    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Featured { get; set; }
    public long? BrandId { get; set; }
    public long? CategoryId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int FeaturedCount { get; set; }
    public int TotalVariantCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed class ProductRowViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int VariantCount { get; set; }
    public int ViewsCount { get; set; }
    public int TotalSoldCount { get; set; }
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ProductFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên sản phẩm tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Slug tối đa 255 ký tự.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu.")]
    [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn thương hiệu.")]
    public long? BrandId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    public long? CategoryId { get; set; }

    [StringLength(4000, ErrorMessage = "Mô tả tối đa 4000 ký tự.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    public List<ProductSelectItem> BrandOptions { get; set; } = [];
    public List<ProductCategorySelectItem> CategoryOptions { get; set; } = [];
}

public sealed class ProductSelectItem
{
    public long Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ProductCategorySelectItem
{
    public long Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Depth { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HasChildren { get; set; }
}
