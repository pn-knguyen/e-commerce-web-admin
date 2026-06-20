using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.ViewModels.Customers;

public sealed class CustomerIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Gender { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class CustomerIndexViewModel
{
    public List<CustomerRowViewModel> Customers { get; set; } = [];
    public List<CustomerFilterOption> StatusOptions { get; set; } = [];
    public List<CustomerFilterOption> GenderOptions { get; set; } = [];

    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Gender { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalCustomerCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int NewThisMonthCount { get; set; }
    public decimal CompletedRevenue { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        !string.IsNullOrWhiteSpace(Status) ||
        !string.IsNullOrWhiteSpace(Gender);
}

public sealed class CustomerRowViewModel
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Gender Gender { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int OrderCount { get; set; }
    public int CompletedOrderCount { get; set; }
    public decimal TotalSpent { get; set; }
    public int AddressCount { get; set; }
    public int RatingCount { get; set; }
}

public sealed class CustomerDetailsViewModel
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Gender Gender { get; set; }
    public bool IsActive { get; set; }
    public string? AvatarImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int OrderCount { get; set; }
    public int CompletedOrderCount { get; set; }
    public decimal TotalSpent { get; set; }
    public int AddressCount { get; set; }
    public int RatingCount { get; set; }
    public List<CustomerOrderRowViewModel> RecentOrders { get; set; } = [];
    public List<CustomerAddressRowViewModel> Addresses { get; set; } = [];
    public List<CustomerRatingRowViewModel> RecentRatings { get; set; } = [];
}

public sealed class CustomerOrderRowViewModel
{
    public long Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CustomerAddressRowViewModel
{
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ProvinceName { get; set; } = string.Empty;
    public string WardName { get; set; } = string.Empty;
    public string DetailAddress { get; set; } = string.Empty;
    public AddressType Type { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class CustomerRatingRowViewModel
{
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class CustomerFilterOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

public sealed class CustomerActionResult
{
    public bool Found { get; init; } = true;
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;

    public static CustomerActionResult NotFound() => new() { Found = false };

    public static CustomerActionResult Success(string message) =>
        new() { Succeeded = true, Message = message };
}

public static class CustomerDisplay
{
    public static string GetGenderLabel(Gender gender) => gender switch
    {
        Gender.Male => "Nam",
        Gender.Female => "Nữ",
        Gender.Other => "Khác",
        _ => "Chưa rõ",
    };

    public static string GetAddressTypeLabel(AddressType type) => type switch
    {
        AddressType.Shipping => "Giao hàng",
        AddressType.Billing => "Thanh toán",
        _ => "Khác",
    };
}
