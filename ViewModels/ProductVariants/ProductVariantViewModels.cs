using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace e_commerce_web_admin.ViewModels.ProductVariants;

public sealed class ProductVariantIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Stock { get; set; }
    public long? ProductId { get; set; }
    public long? CategoryId { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class ProductVariantIndexViewModel
{
    public List<ProductVariantRowViewModel> Variants { get; set; } = [];
    public List<ProductVariantProductOptionViewModel> ProductOptions { get; set; } = [];
    public List<ProductVariantCategoryOptionViewModel> CategoryOptions { get; set; } = [];

    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Stock { get; set; }
    public long? ProductId { get; set; }
    public long? CategoryId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int TotalImageCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed class ProductVariantRowViewModel
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int SoldCount { get; set; }
    public int Quantity { get; set; }
    public string? ColorName { get; set; }
    public string? ColorHex { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string AttributeSummary { get; set; } = string.Empty;
    public int ImageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ProductVariantFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn sản phẩm.")]
    [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn sản phẩm.")]
    public long? ProductId { get; set; }

    public bool IsProductLocked { get; set; }
    public string? ProductName { get; set; }
    public string? ProductMeta { get; set; }

    [Required(ErrorMessage = "Mã biến thể là bắt buộc.")]
    [StringLength(80, ErrorMessage = "Mã biến thể tối đa 80 ký tự.")]
    [RegularExpression(@"^[A-Z0-9][A-Z0-9_-]{1,79}$",
        ErrorMessage = "Mã biến thể chỉ gồm chữ in hoa, số, dấu gạch ngang hoặc gạch dưới.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giá bán là bắt buộc.")]
    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Giá bán không được âm.")]
    public decimal? Price { get; set; }

    public int Quantity { get; set; }

    [StringLength(120, ErrorMessage = "Tên màu tối đa 120 ký tự.")]
    public string? ColorName { get; set; }

    [StringLength(7, ErrorMessage = "Mã màu tối đa 7 ký tự.")]
    [RegularExpression(@"^#[0-9a-fA-F]{6}$", ErrorMessage = "Mã màu phải đúng định dạng #RRGGBB.")]
    public string? ColorHex { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ProductVariantProductOptionViewModel> ProductOptions { get; set; } = [];
    public List<ProductVariantAttributeInputViewModel> Attributes { get; set; } = [];
    public List<ProductVariantImageInputViewModel> Images { get; set; } = [];
    public List<IFormFile> BulkImageFiles { get; set; } = [];
}

public sealed class ProductVariantProductOptionViewModel
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ProductVariantCategoryOptionViewModel
{
    public long Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class ProductVariantAttributeInputViewModel
{
    public long CategoryId { get; set; }
    public long AttributeId { get; set; }
    public string AttributeCode { get; set; } = string.Empty;
    public string AttributeName { get; set; } = string.Empty;
    public long? SelectedOptionId { get; set; }
    public List<ProductVariantAttributeOptionViewModel> Options { get; set; } = [];
}

public sealed class ProductVariantAttributeOptionViewModel
{
    public long Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed class ProductVariantImageInputViewModel
{
    public long? Id { get; set; }
    public string? ImagePath { get; set; }
    public IFormFile? ImageFile { get; set; }
    public string? AltText { get; set; }
    public int? Position { get; set; }
    public bool Remove { get; set; }
}
