using e_commerce_web_admin.Data;
using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Dashboard", Permissions.View)]
public class DashboardController : Controller
{
    private const int SparklinePointCount = 8;

    private static readonly StatusDisplay[] OrderDisplays =
    [
        new(OrderStatus.Pending, "Chờ xác nhận", "pending", "#f59e0b"),
        new(OrderStatus.Confirmed, "Đã xác nhận", "processing", "#0ea5e9"),
        new(OrderStatus.Processing, "Đang xử lý", "processing", "#3b82f6"),
        new(OrderStatus.Shipping, "Đang giao", "shipping", "#8b5cf6"),
        new(OrderStatus.Completed, "Đã giao", "delivered", "#10b981"),
        new(OrderStatus.Cancelled, "Đã hủy", "cancelled", "#ef4444"),
        new(OrderStatus.Returned, "Đã trả hàng", "cancelled", "#f97316"),
    ];

    private readonly ApplicationDbContext _db;
    private readonly TimeZoneInfo _vietnamTimeZone = TimeZoneHelper.GetVietnamTimeZone();

    public DashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Bảng điều khiển";
        return View();
    }

    [HttpGet("/api/dashboard/kpis")]
    public async Task<IActionResult> GetKpis(string? period = "month", CancellationToken ct = default)
    {
        var range = GetPeriodRange(period);

        var orderRows = await _db.Orders
            .AsNoTracking()
            .Where(order => order.CreatedAt >= range.StartUtc && order.CreatedAt < range.EndUtc)
            .Select(order => new OrderMetricRow(
                order.CreatedAt,
                order.TotalAmount,
                order.OrderStatus,
                order.PaymentStatus))
            .ToListAsync(ct);

        var currentRevenue = orderRows
            .Where(IsSuccessfulOrder)
            .Sum(order => order.TotalAmount);
        var currentOrderCount = orderRows.Count;

        var previousRevenue = await SuccessfulOrders()
            .Where(order => order.CreatedAt >= range.PreviousStartUtc && order.CreatedAt < range.PreviousEndUtc)
            .SumAsync(order => (decimal?)order.TotalAmount, ct) ?? 0m;
        var previousOrderCount = await _db.Orders
            .AsNoTracking()
            .CountAsync(order => order.CreatedAt >= range.PreviousStartUtc && order.CreatedAt < range.PreviousEndUtc, ct);

        var currentCustomerCount = await _db.Users.AsNoTracking()
            .CountAsync(user => user.CreatedAt < range.EndUtc, ct);
        var previousCustomerCount = await _db.Users.AsNoTracking()
            .CountAsync(user => user.CreatedAt < range.StartUtc, ct);
        var newCustomerDates = await _db.Users.AsNoTracking()
            .Where(user => user.CreatedAt >= range.StartUtc && user.CreatedAt < range.EndUtc)
            .Select(user => user.CreatedAt)
            .ToListAsync(ct);

        var currentProductCount = await _db.Products.AsNoTracking()
            .CountAsync(product => product.CreatedAt < range.EndUtc, ct);
        var previousProductCount = await _db.Products.AsNoTracking()
            .CountAsync(product => product.CreatedAt < range.StartUtc, ct);
        var newProductDates = await _db.Products.AsNoTracking()
            .Where(product => product.CreatedAt >= range.StartUtc && product.CreatedAt < range.EndUtc)
            .Select(product => product.CreatedAt)
            .ToListAsync(ct);

        var revenueSparkline = BuildSeries(
            orderRows.Where(IsSuccessfulOrder),
            row => row.CreatedAt,
            row => row.TotalAmount,
            range.StartUtc,
            range.EndUtc);
        var orderSparkline = BuildSeries(
            orderRows,
            row => row.CreatedAt,
            _ => 1m,
            range.StartUtc,
            range.EndUtc);
        var customerSparkline = BuildCumulativeCountSeries(
            newCustomerDates,
            previousCustomerCount,
            range.StartUtc,
            range.EndUtc);
        var productSparkline = BuildCumulativeCountSeries(
            newProductDates,
            previousProductCount,
            range.StartUtc,
            range.EndUtc);

        return Ok(new
        {
            revenue = BuildKpi(currentRevenue, previousRevenue, revenueSparkline),
            orders = BuildKpi(currentOrderCount, previousOrderCount, orderSparkline),
            customers = BuildKpi(currentCustomerCount, previousCustomerCount, customerSparkline),
            products = BuildKpi(currentProductCount, previousProductCount, productSparkline),
        });
    }

    [HttpGet("/api/dashboard/revenue-chart")]
    public async Task<IActionResult> GetRevenueChart(CancellationToken ct = default)
    {
        var localNow = GetVietnamNow();
        var nextYearStart = ToUtc(new DateTime(localNow.Year + 1, 1, 1));
        var previousYearStart = ToUtc(new DateTime(localNow.Year - 1, 1, 1));

        var rows = await SuccessfulOrders()
            .Where(order => order.CreatedAt >= previousYearStart && order.CreatedAt < nextYearStart)
            .Select(order => new RevenueRow(order.CreatedAt, order.TotalAmount))
            .ToListAsync(ct);

        var currentYear = new decimal[12];
        var previousYear = new decimal[12];

        foreach (var row in rows)
        {
            var localDate = FromUtc(row.CreatedAt);
            var target = localDate.Year == localNow.Year ? currentYear : previousYear;
            target[localDate.Month - 1] += row.TotalAmount / 1_000_000m;
        }

        return Ok(new
        {
            labels = Enumerable.Range(1, 12).Select(month => $"T{month}").ToArray(),
            currentYear,
            previousYear,
        });
    }

    [HttpGet("/api/dashboard/order-status")]
    public async Task<IActionResult> GetOrderStatus(string? period = "month", CancellationToken ct = default)
    {
        var range = GetPeriodRange(period);
        var counts = await _db.Orders
            .AsNoTracking()
            .Where(order => order.CreatedAt >= range.StartUtc && order.CreatedAt < range.EndUtc)
            .GroupBy(order => order.OrderStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, ct);

        return Ok(new
        {
            labels = OrderDisplays.Select(item => item.Label).ToArray(),
            values = OrderDisplays.Select(item => counts.GetValueOrDefault(item.Status)).ToArray(),
            colors = OrderDisplays.Select(item => item.Color).ToArray(),
        });
    }

    [HttpGet("/api/dashboard/top-products")]
    public async Task<IActionResult> GetTopProducts(string? period = "month", CancellationToken ct = default)
    {
        var range = GetPeriodRange(period);
        var currentSales = await QueryProductSales(range.StartUtc, range.EndUtc)
            .OrderByDescending(item => item.Revenue)
            .ThenByDescending(item => item.Sold)
            .Take(5)
            .ToListAsync(ct);

        if (currentSales.Count == 0)
        {
            return Ok(Array.Empty<object>());
        }

        var productIds = currentSales.Select(item => item.ProductId).ToArray();
        var previousSales = await QueryProductSales(range.PreviousStartUtc, range.PreviousEndUtc)
            .Where(item => productIds.Contains(item.ProductId))
            .ToDictionaryAsync(item => item.ProductId, item => item.Sold, ct);

        var result = currentSales.Select((item, index) => new
        {
            rank = index + 1,
            name = item.Name,
            category = item.Category,
            sold = item.Sold,
            revenue = item.Revenue,
            growth = CalculateChange(item.Sold, previousSales.GetValueOrDefault(item.ProductId)),
        });

        return Ok(result);
    }

    [HttpGet("/api/dashboard/recent-orders")]
    public async Task<IActionResult> GetRecentOrders(CancellationToken ct = default)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Take(10)
            .Select(order => new RecentOrderRow(
                order.OrderCode,
                order.User != null && order.User.FullName != string.Empty
                    ? order.User.FullName
                    : order.ShippingContactName,
                order.TotalAmount,
                order.OrderStatus,
                order.CreatedAt))
            .ToListAsync(ct);

        var result = orders.Select(order =>
        {
            var display = GetStatusDisplay(order.Status);
            return new
            {
                id = order.OrderCode.StartsWith('#') ? order.OrderCode : $"#{order.OrderCode}",
                customer = string.IsNullOrWhiteSpace(order.Customer) ? "Khách hàng" : order.Customer,
                total = order.TotalAmount,
                status = display.Label,
                statusKey = display.BadgeKey,
                date = FromUtc(order.CreatedAt).ToString("dd/MM/yyyy HH:mm"),
            };
        });

        return Ok(result);
    }

    [HttpGet("/api/dashboard/category-revenue")]
    public async Task<IActionResult> GetCategoryRevenue(string? period = "month", CancellationToken ct = default)
    {
        var range = GetPeriodRange(period);
        var rows = await SuccessfulOrderItems(range.StartUtc, range.EndUtc)
            .GroupBy(item => new
            {
                item.ProductVariant!.Product!.CategoryId,
                CategoryName = item.ProductVariant.Product.Category!.Name,
            })
            .Select(group => new CategoryRevenueRow
            {
                CategoryId = group.Key.CategoryId,
                Name = group.Key.CategoryName,
                Revenue = group.Sum(item => item.UnitPrice * item.Quantity),
            })
            .OrderByDescending(item => item.Revenue)
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            return Ok(new { labels = Array.Empty<string>(), values = Array.Empty<double>() });
        }

        var totalRevenue = rows.Sum(item => item.Revenue);
        var displayed = rows.Take(5).ToList();
        if (rows.Count > 5)
        {
            displayed.Add(new CategoryRevenueRow
            {
                CategoryId = 0,
                Name = "Khác",
                Revenue = rows.Skip(5).Sum(item => item.Revenue),
            });
        }

        return Ok(new
        {
            labels = displayed.Select(item => item.Name).ToArray(),
            values = displayed.Select(item => Math.Round((double)(item.Revenue / totalRevenue * 100m), 1)).ToArray(),
        });
    }

    [HttpGet("/api/dashboard/traffic")]
    public async Task<IActionResult> GetTraffic(CancellationToken ct = default)
    {
        var localNow = GetVietnamNow();
        var localStart = localNow.Date.AddDays(-6);
        var startUtc = ToUtc(localStart);
        var endUtc = ToUtc(localNow);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(order => order.CreatedAt >= startUtc && order.CreatedAt < endUtc)
            .Select(order => new OrderMetricRow(
                order.CreatedAt,
                order.TotalAmount,
                order.OrderStatus,
                order.PaymentStatus))
            .ToListAsync(ct);
        var customers = await _db.Users
            .AsNoTracking()
            .Where(user => user.CreatedAt >= startUtc && user.CreatedAt < endUtc)
            .Select(user => user.CreatedAt)
            .ToListAsync(ct);

        var dates = Enumerable.Range(0, 7).Select(offset => localStart.AddDays(offset)).ToArray();
        var orderCounts = new int[7];
        var revenues = new decimal[7];
        var newCustomers = new int[7];

        foreach (var order in orders)
        {
            var index = (FromUtc(order.CreatedAt).Date - localStart).Days;
            if (index is < 0 or > 6)
            {
                continue;
            }

            orderCounts[index]++;
            if (IsSuccessfulOrder(order))
            {
                revenues[index] += order.TotalAmount / 1_000_000m;
            }
        }

        foreach (var createdAt in customers)
        {
            var index = (FromUtc(createdAt).Date - localStart).Days;
            if (index is >= 0 and <= 6)
            {
                newCustomers[index]++;
            }
        }

        return Ok(new
        {
            labels = dates.Select(date => date.ToString("dd/MM")).ToArray(),
            orders = orderCounts,
            revenue = revenues,
            newCustomers,
        });
    }

    private IQueryable<Models.Entities.Order> SuccessfulOrders()
        => _db.Orders
            .AsNoTracking()
            .Where(order => order.OrderStatus == OrderStatus.Completed && order.PaymentStatus == PaymentStatus.Paid);

    private IQueryable<Models.Entities.OrderItem> SuccessfulOrderItems(DateTime startUtc, DateTime endUtc)
        => _db.OrderItems
            .AsNoTracking()
            .Where(item =>
                item.Order!.CreatedAt >= startUtc &&
                item.Order.CreatedAt < endUtc &&
                item.Order.OrderStatus == OrderStatus.Completed &&
                item.Order.PaymentStatus == PaymentStatus.Paid);

    private IQueryable<ProductSalesRow> QueryProductSales(DateTime startUtc, DateTime endUtc)
        => SuccessfulOrderItems(startUtc, endUtc)
            .GroupBy(item => new
            {
                ProductId = item.ProductVariant!.ProductId,
                ProductName = item.ProductVariant.Product!.Name,
                CategoryName = item.ProductVariant.Product.Category!.Name,
            })
            .Select(group => new ProductSalesRow
            {
                ProductId = group.Key.ProductId,
                Name = group.Key.ProductName,
                Category = group.Key.CategoryName,
                Sold = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.UnitPrice * item.Quantity),
            });

    private DashboardRange GetPeriodRange(string? period)
    {
        var localNow = GetVietnamNow();
        var localStart = period?.ToLowerInvariant() switch
        {
            "today" => localNow.Date,
            "week" => localNow.Date.AddDays(-6),
            "year" => new DateTime(localNow.Year, 1, 1),
            _ => localNow.Date.AddDays(-29),
        };

        DateTime previousLocalStart;
        DateTime previousLocalEnd;
        if (string.Equals(period, "year", StringComparison.OrdinalIgnoreCase))
        {
            previousLocalStart = localStart.AddYears(-1);
            previousLocalEnd = localNow.AddYears(-1);
        }
        else
        {
            var duration = localNow - localStart;
            previousLocalEnd = localStart;
            previousLocalStart = localStart - duration;
        }

        return new DashboardRange(
            ToUtc(localStart),
            ToUtc(localNow),
            ToUtc(previousLocalStart),
            ToUtc(previousLocalEnd));
    }

    private DateTime GetVietnamNow()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vietnamTimeZone);

    private DateTime ToUtc(DateTime localDateTime)
        => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified),
            _vietnamTimeZone);

    private DateTime FromUtc(DateTime utcDateTime)
        => TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc),
            _vietnamTimeZone);

    private static bool IsSuccessfulOrder(OrderMetricRow order)
        => order.Status == OrderStatus.Completed && order.PaymentStatus == PaymentStatus.Paid;

    private static object BuildKpi(decimal current, decimal previous, decimal[] sparkline)
    {
        var change = CalculateChange(current, previous);
        return new
        {
            value = current,
            change,
            trend = change >= 0 ? "up" : "down",
            sparkline,
        };
    }

    private static object BuildKpi(int current, int previous, decimal[] sparkline)
        => BuildKpi((decimal)current, previous, sparkline);

    private static double CalculateChange(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0 : 100;
        }

        return Math.Round((double)((current - previous) / previous * 100m), 1);
    }

    private static decimal[] BuildSeries<T>(
        IEnumerable<T> rows,
        Func<T, DateTime> dateSelector,
        Func<T, decimal> valueSelector,
        DateTime startUtc,
        DateTime endUtc)
    {
        var values = new decimal[SparklinePointCount];
        var durationTicks = Math.Max(1, (endUtc - startUtc).Ticks);

        foreach (var row in rows)
        {
            var elapsedTicks = Math.Clamp((dateSelector(row) - startUtc).Ticks, 0, durationTicks - 1);
            var index = (int)(elapsedTicks * SparklinePointCount / durationTicks);
            values[index] += valueSelector(row);
        }

        return values;
    }

    private static decimal[] BuildCumulativeCountSeries(
        IEnumerable<DateTime> dates,
        int baseline,
        DateTime startUtc,
        DateTime endUtc)
    {
        var increments = BuildSeries(dates, date => date, _ => 1m, startUtc, endUtc);
        var result = new decimal[SparklinePointCount];
        decimal runningTotal = baseline;
        for (var index = 0; index < increments.Length; index++)
        {
            runningTotal += increments[index];
            result[index] = runningTotal;
        }

        return result;
    }

    private static StatusDisplay GetStatusDisplay(OrderStatus status)
        => OrderDisplays.FirstOrDefault(item => item.Status == status)
            ?? new StatusDisplay(status, status.ToString(), "processing", "#64748b");

    private sealed record DashboardRange(
        DateTime StartUtc,
        DateTime EndUtc,
        DateTime PreviousStartUtc,
        DateTime PreviousEndUtc);

    private sealed record OrderMetricRow(
        DateTime CreatedAt,
        decimal TotalAmount,
        OrderStatus Status,
        PaymentStatus PaymentStatus);

    private sealed record RevenueRow(DateTime CreatedAt, decimal TotalAmount);

    private sealed class ProductSalesRow
    {
        public long ProductId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public int Sold { get; init; }
        public decimal Revenue { get; init; }
    }

    private sealed class CategoryRevenueRow
    {
        public long CategoryId { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal Revenue { get; init; }
    }

    private sealed record RecentOrderRow(
        string OrderCode,
        string Customer,
        decimal TotalAmount,
        OrderStatus Status,
        DateTime CreatedAt);

    private sealed record StatusDisplay(
        OrderStatus Status,
        string Label,
        string BadgeKey,
        string Color);
}
