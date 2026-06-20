using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.ViewModels.Customers;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Customers;

public sealed class CustomerAdminService(ApplicationDbContext db) : ICustomerAdminService
{
    private const int DefaultPageSize = 20;

    public async Task<CustomerIndexViewModel> GetIndexAsync(
        CustomerIndexQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var baseQuery = db.Users
            .AsNoTracking()
            .Where(user => user.Role == UserRole.Customer);
        var filteredQuery = ApplyFilters(baseQuery, query);
        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalCount = await filteredQuery.CountAsync(ct);
        var totalCustomerCount = await baseQuery.CountAsync(ct);
        var activeCount = await baseQuery.CountAsync(user => user.IsActive, ct);
        var inactiveCount = totalCustomerCount - activeCount;
        var newThisMonthCount = await baseQuery.CountAsync(user => user.CreatedAt >= firstDayOfMonth, ct);
        var completedRevenue = await db.Orders
            .AsNoTracking()
            .Where(order =>
                order.User != null &&
                order.User.Role == UserRole.Customer &&
                order.OrderStatus == OrderStatus.Completed &&
                order.PaymentStatus == PaymentStatus.Paid)
            .SumAsync(order => (decimal?)order.TotalAmount, ct) ?? 0m;

        var rows = await filteredQuery
            .OrderByDescending(user => user.CreatedAt)
            .ThenByDescending(user => user.Id)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(user => new CustomerRowViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Gender = user.Gender,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                OrderCount = user.Orders.Count,
                CompletedOrderCount = user.Orders.Count(order => order.OrderStatus == OrderStatus.Completed),
                TotalSpent = user.Orders
                    .Where(order => order.OrderStatus == OrderStatus.Completed && order.PaymentStatus == PaymentStatus.Paid)
                    .Sum(order => (decimal?)order.TotalAmount) ?? 0m,
                AddressCount = user.Addresses.Count(address => !address.IsDeleted),
                RatingCount = user.Ratings.Count,
            })
            .ToListAsync(ct);

        return new CustomerIndexViewModel
        {
            Customers = rows,
            Search = query.Search?.Trim(),
            Status = NormalizeStatus(query.Status),
            Gender = NormalizeGender(query.Gender),
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            TotalCustomerCount = totalCustomerCount,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount,
            NewThisMonthCount = newThisMonthCount,
            CompletedRevenue = completedRevenue,
            StatusOptions = BuildStatusOptions(query.Status),
            GenderOptions = BuildGenderOptions(query.Gender),
        };
    }

    public async Task<CustomerDetailsViewModel?> GetDetailsAsync(
        long id,
        CancellationToken ct = default)
    {
        return await db.Users
            .AsNoTracking()
            .Where(user => user.Id == id && user.Role == UserRole.Customer)
            .Select(user => new CustomerDetailsViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Gender = user.Gender,
                IsActive = user.IsActive,
                AvatarImage = user.AvatarImage,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                OrderCount = user.Orders.Count,
                CompletedOrderCount = user.Orders.Count(order => order.OrderStatus == OrderStatus.Completed),
                TotalSpent = user.Orders
                    .Where(order => order.OrderStatus == OrderStatus.Completed && order.PaymentStatus == PaymentStatus.Paid)
                    .Sum(order => (decimal?)order.TotalAmount) ?? 0m,
                AddressCount = user.Addresses.Count(address => !address.IsDeleted),
                RatingCount = user.Ratings.Count,
                RecentOrders = user.Orders
                    .OrderByDescending(order => order.CreatedAt)
                    .ThenByDescending(order => order.Id)
                    .Take(8)
                    .Select(order => new CustomerOrderRowViewModel
                    {
                        Id = order.Id,
                        OrderCode = order.OrderCode,
                        ItemCount = order.OrderItems.Sum(item => item.Quantity),
                        TotalAmount = order.TotalAmount,
                        OrderStatus = order.OrderStatus,
                        PaymentStatus = order.PaymentStatus,
                        CreatedAt = order.CreatedAt,
                    })
                    .ToList(),
                Addresses = user.Addresses
                    .Where(address => !address.IsDeleted)
                    .OrderByDescending(address => address.IsDefault)
                    .ThenByDescending(address => address.CreatedAt)
                    .Select(address => new CustomerAddressRowViewModel
                    {
                        ContactName = address.ContactName,
                        Phone = address.Phone,
                        ProvinceName = address.ProvinceName,
                        WardName = address.WardName,
                        DetailAddress = address.DetailAddress,
                        Type = address.Type,
                        IsDefault = address.IsDefault,
                    })
                    .ToList(),
                RecentRatings = user.Ratings
                    .OrderByDescending(rating => rating.CreatedAt)
                    .Take(8)
                    .Select(rating => new CustomerRatingRowViewModel
                    {
                        Stars = rating.Stars,
                        Comment = rating.Comment,
                        IsApproved = rating.IsApproved,
                        ProductName = rating.OrderItem != null &&
                                      rating.OrderItem.ProductVariant != null &&
                                      rating.OrderItem.ProductVariant.Product != null
                            ? rating.OrderItem.ProductVariant.Product.Name
                            : "Sản phẩm không xác định",
                        VariantCode = rating.OrderItem != null && rating.OrderItem.ProductVariant != null
                            ? rating.OrderItem.ProductVariant.Code
                            : "N/A",
                        CreatedAt = rating.CreatedAt,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CustomerActionResult> ToggleActiveAsync(
        long id,
        CancellationToken ct = default)
    {
        var customer = await db.Users
            .FirstOrDefaultAsync(user => user.Id == id && user.Role == UserRole.Customer, ct);

        if (customer is null)
        {
            return CustomerActionResult.NotFound();
        }

        customer.IsActive = !customer.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return CustomerActionResult.Success(customer.IsActive
            ? $"Đã mở khóa khách hàng \"{customer.FullName}\"."
            : $"Đã tạm khóa khách hàng \"{customer.FullName}\".");
    }

    private static IQueryable<User> ApplyFilters(
        IQueryable<User> query,
        CustomerIndexQuery input)
    {
        var search = input.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(user =>
                user.Username.Contains(search) ||
                user.Email.Contains(search) ||
                user.FullName.Contains(search) ||
                (user.Phone != null && user.Phone.Contains(search)));
        }

        query = NormalizeStatus(input.Status) switch
        {
            "active" => query.Where(user => user.IsActive),
            "inactive" => query.Where(user => !user.IsActive),
            _ => query,
        };

        var gender = ParseGender(input.Gender);
        if (gender.HasValue)
        {
            query = query.Where(user => user.Gender == gender.Value);
        }

        return query;
    }

    private static string? NormalizeStatus(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant();
        return normalized is "active" or "inactive" ? normalized : null;
    }

    private static string? NormalizeGender(string? gender)
    {
        var parsed = ParseGender(gender);
        return parsed?.ToString();
    }

    private static Gender? ParseGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
        {
            return null;
        }

        return Enum.TryParse<Gender>(gender, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }

    private static List<CustomerFilterOption> BuildStatusOptions(string? selectedStatus)
    {
        var selected = NormalizeStatus(selectedStatus);
        return
        [
            new() { Value = string.Empty, Text = "Tất cả", Selected = selected is null },
            new() { Value = "active", Text = "Đang hoạt động", Selected = selected == "active" },
            new() { Value = "inactive", Text = "Tạm khóa", Selected = selected == "inactive" },
        ];
    }

    private static List<CustomerFilterOption> BuildGenderOptions(string? selectedGender)
    {
        var selected = ParseGender(selectedGender);
        return
        [
            new() { Value = string.Empty, Text = "Tất cả", Selected = selected is null },
            new() { Value = Gender.Male.ToString(), Text = CustomerDisplay.GetGenderLabel(Gender.Male), Selected = selected == Gender.Male },
            new() { Value = Gender.Female.ToString(), Text = CustomerDisplay.GetGenderLabel(Gender.Female), Selected = selected == Gender.Female },
            new() { Value = Gender.Other.ToString(), Text = CustomerDisplay.GetGenderLabel(Gender.Other), Selected = selected == Gender.Other },
            new() { Value = Gender.Unknown.ToString(), Text = CustomerDisplay.GetGenderLabel(Gender.Unknown), Selected = selected == Gender.Unknown },
        ];
    }
}
