using System.Globalization;

namespace e_commerce_web_admin.ViewModels.Ratings;

public sealed class RatingIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int? Stars { get; set; }
    public int Page { get; set; } = 1;
}

public sealed class RatingIndexViewModel
{
    public List<RatingRowViewModel> Ratings { get; set; } = [];
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int? Stars { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int ApprovedCount { get; set; }
    public int PendingCount { get; set; }
    public decimal AverageStars { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        !string.IsNullOrWhiteSpace(Status) ||
        Stars.HasValue;
}

public sealed class RatingRowViewModel
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public long Id { get; set; }
    public long OrderItemId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantCode { get; set; } = string.Empty;
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string StarsLabel => $"{Stars.ToString("N0", ViCulture)}/5";
    public string StatusLabel => IsApproved ? "Đã duyệt" : "Chờ duyệt";
    public string StatusKey => IsApproved ? "approved" : "pending";
    public string CommentDisplay => string.IsNullOrWhiteSpace(Comment) ? "Khách hàng chưa để lại nội dung đánh giá." : Comment.Trim();
}
