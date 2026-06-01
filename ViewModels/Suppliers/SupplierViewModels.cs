using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_admin.ViewModels.Suppliers;

public sealed class SupplierIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class SupplierIndexViewModel
{
    public List<SupplierRowViewModel> Suppliers { get; set; } = [];
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int TotalGoodsReceiptCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed class SupplierRowViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public int GoodsReceiptCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SupplierFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tên nhà cung cấp là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên nhà cung cấp tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số.")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Địa chỉ là bắt buộc.")]
    [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự.")]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}
