using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.ViewModels.Ratings;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Ratings;

public sealed class RatingAdminService : IRatingAdminService
{
    private const int DefaultPageSize = 20;

    private readonly ApplicationDbContext _db;

    public RatingAdminService(ApplicationDbContext db)
        => _db = db;

    public async Task<RatingIndexViewModel> GetIndexAsync(
        RatingIndexQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var filteredQuery = ApplyFilters(_db.Ratings.AsNoTracking(), query);

        var summary = await filteredQuery
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                ApprovedCount = group.Count(rating => rating.IsApproved),
                PendingCount = group.Count(rating => !rating.IsApproved),
                AverageStars = group.Average(rating => (decimal)rating.Stars),
            })
            .FirstOrDefaultAsync(ct);

        var rows = await filteredQuery
            .OrderBy(rating => rating.IsApproved)
            .ThenByDescending(rating => rating.CreatedAt)
            .ThenByDescending(rating => rating.Id)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(rating => new RatingRowViewModel
            {
                Id = rating.Id,
                OrderItemId = rating.OrderItemId,
                OrderCode = rating.OrderItem != null && rating.OrderItem.Order != null
                    ? rating.OrderItem.Order.OrderCode
                    : "N/A",
                CustomerName = rating.User != null ? rating.User.FullName : "Khách hàng",
                CustomerEmail = rating.User != null ? rating.User.Email : null,
                ProductName = rating.OrderItem != null &&
                    rating.OrderItem.ProductVariant != null &&
                    rating.OrderItem.ProductVariant.Product != null
                        ? rating.OrderItem.ProductVariant.Product.Name
                        : "Sản phẩm không xác định",
                VariantCode = rating.OrderItem != null && rating.OrderItem.ProductVariant != null
                    ? rating.OrderItem.ProductVariant.Code
                    : "N/A",
                Stars = rating.Stars,
                Comment = rating.Comment,
                IsApproved = rating.IsApproved,
                CreatedAt = rating.CreatedAt,
                UpdatedAt = rating.UpdatedAt,
            })
            .ToListAsync(ct);

        return new RatingIndexViewModel
        {
            Ratings = rows,
            Search = query.Search,
            Status = NormalizeStatus(query.Status),
            Stars = NormalizeStars(query.Stars),
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = summary?.TotalCount ?? 0,
            ApprovedCount = summary?.ApprovedCount ?? 0,
            PendingCount = summary?.PendingCount ?? 0,
            AverageStars = Math.Round(summary?.AverageStars ?? 0m, 2),
        };
    }

    public async Task<RatingToggleResult?> ToggleApprovalAsync(long id, CancellationToken ct = default)
    {
        var rating = await _db.Ratings
            .Include(item => item.OrderItem)
                .ThenInclude(item => item!.ProductVariant)
                .ThenInclude(variant => variant!.Product)
            .FirstOrDefaultAsync(item => item.Id == id, ct);

        if (rating is null)
        {
            return null;
        }

        var wasApproved = rating.IsApproved;
        rating.IsApproved = !rating.IsApproved;
        rating.UpdatedAt = DateTime.UtcNow;

        AdjustProductRating(rating, wasApproved, rating.IsApproved);

        await _db.SaveChangesAsync(ct);

        var message = rating.IsApproved
            ? "Đã duyệt đánh giá."
            : "Đã chuyển đánh giá về trạng thái chờ duyệt.";

        return new RatingToggleResult(rating.IsApproved, message);
    }

    public async Task<RatingDeleteResult> DeleteAsync(long id, CancellationToken ct = default)
    {
        var rating = await _db.Ratings
            .Include(item => item.OrderItem)
                .ThenInclude(item => item!.ProductVariant)
                .ThenInclude(variant => variant!.Product)
            .FirstOrDefaultAsync(item => item.Id == id, ct);

        if (rating is null)
        {
            return RatingDeleteResult.NotFound();
        }

        if (rating.IsApproved)
        {
            AdjustProductRating(rating, wasApproved: true, isApproved: false);
        }

        _db.Ratings.Remove(rating);
        await _db.SaveChangesAsync(ct);

        return RatingDeleteResult.Success("Đã xoá đánh giá thành công.");
    }

    private static IQueryable<Rating> ApplyFilters(IQueryable<Rating> query, RatingIndexQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(rating =>
                (rating.Comment != null && rating.Comment.Contains(search)) ||
                (rating.User != null &&
                    (rating.User.FullName.Contains(search) || rating.User.Email.Contains(search))) ||
                (rating.OrderItem != null &&
                    rating.OrderItem.Order != null &&
                    rating.OrderItem.Order.OrderCode.Contains(search)) ||
                (rating.OrderItem != null &&
                    rating.OrderItem.ProductVariant != null &&
                    (rating.OrderItem.ProductVariant.Code.Contains(search) ||
                        (rating.OrderItem.ProductVariant.Product != null &&
                            rating.OrderItem.ProductVariant.Product.Name.Contains(search)))));
        }

        var status = NormalizeStatus(filters.Status);
        if (status == "approved")
        {
            query = query.Where(rating => rating.IsApproved);
        }
        else if (status == "pending")
        {
            query = query.Where(rating => !rating.IsApproved);
        }

        var stars = NormalizeStars(filters.Stars);
        if (stars.HasValue)
        {
            query = query.Where(rating => rating.Stars == stars.Value);
        }

        return query;
    }

    private static string? NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "approved" => "approved",
            "pending" => "pending",
            _ => null,
        };
    }

    private static int? NormalizeStars(int? value)
        => value is >= 1 and <= 5 ? value.Value : null;

    private static void AdjustProductRating(Rating rating, bool wasApproved, bool isApproved)
    {
        var product = rating.OrderItem?.ProductVariant?.Product;
        if (product is null || wasApproved == isApproved)
        {
            return;
        }

        var delta = isApproved ? 1 : -1;
        var currentCount = Math.Max(0, product.RatingCount);
        var nextCount = Math.Max(0, currentCount + delta);
        var currentTotal = product.RatingAverage * currentCount;
        var nextTotal = currentTotal + rating.Stars * delta;

        product.RatingCount = nextCount;
        product.RatingAverage = nextCount == 0
            ? 0m
            : Math.Round(Math.Clamp(nextTotal / nextCount, 0m, 5m), 2);
        product.UpdatedAt = DateTime.UtcNow;
    }
}
