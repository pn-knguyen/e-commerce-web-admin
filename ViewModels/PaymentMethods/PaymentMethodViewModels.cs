using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_admin.ViewModels.PaymentMethods;

public sealed class PaymentMethodIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class PaymentMethodIndexViewModel
{
    public List<PaymentMethodRowViewModel> PaymentMethods { get; set; } = [];
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int TotalOrderUsageCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed class PaymentMethodRowViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int OrderCount { get; set; }
}

public sealed class PaymentMethodFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tên phương thức thanh toán là bắt buộc.")]
    [StringLength(120, ErrorMessage = "Tên phương thức thanh toán tối đa 120 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
