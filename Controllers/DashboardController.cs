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
    public async Task<IActionResult> GetKpis([FromQuery] DashboardQuery query, CancellationToken ct = default)
    {
        var range = GetPeriodRange(query);

        var currentSalesSummary = await BuildSalesSummaryAsync(query, range.StartUtc, range.EndUtc, ct);
        var previousSalesSummary = await BuildSalesSummaryAsync(query, range.PreviousStartUtc, range.PreviousEndUtc, ct);
        var currentOrderCount = await CountOrdersAsync(query, range.StartUtc, range.EndUtc, ct);
        var previousOrderCount = await CountOrdersAsync(query, range.PreviousStartUtc, range.PreviousEndUtc, ct);

        var currentCustomerCount = await _db.Users.AsNoTracking()
            .CountAsync(user => user.CreatedAt < range.EndUtc, ct);
        var previousCustomerCount = await _db.Users.AsNoTracking()
            .CountAsync(user => user.CreatedAt < range.StartUtc, ct);

        var currentProductCount = await _db.Products.AsNoTracking()
            .CountAsync(product => product.CreatedAt < range.EndUtc, ct);
        var previousProductCount = await _db.Products.AsNoTracking()
            .CountAsync(product => product.CreatedAt < range.StartUtc, ct);

        var revenueSparkline = await BuildSeriesAsync(
            RevenueSeriesSource(query, range.StartUtc, range.EndUtc),
            range.StartUtc,
            range.EndUtc,
            ct);
        var orderSparkline = await BuildSeriesAsync(
            FilterOrders(query, range.StartUtc, range.EndUtc)
                .Select(order => new SeriesSourceRow
                {
                    CreatedAt = order.CreatedAt,
                    Value = 1m,
                }),
            range.StartUtc,
            range.EndUtc,
            ct);
        var customerSparkline = BuildCumulativeCountSeries(
            await BuildSeriesAsync(
                _db.Users
                    .AsNoTracking()
                    .Where(user => user.CreatedAt >= range.StartUtc && user.CreatedAt < range.EndUtc)
                    .Select(user => new SeriesSourceRow
                    {
                        CreatedAt = user.CreatedAt,
                        Value = 1m,
                    }),
                range.StartUtc,
                range.EndUtc,
                ct),
            previousCustomerCount);
        var productSparkline = BuildCumulativeCountSeries(
            await BuildSeriesAsync(
                _db.Products
                    .AsNoTracking()
                    .Where(product => product.CreatedAt >= range.StartUtc && product.CreatedAt < range.EndUtc)
                    .Select(product => new SeriesSourceRow
                    {
                        CreatedAt = product.CreatedAt,
                        Value = 1m,
                    }),
                range.StartUtc,
                range.EndUtc,
                ct),
            previousProductCount);

        return Ok(new
        {
            revenue = BuildKpi(currentSalesSummary.Revenue, previousSalesSummary.Revenue, revenueSparkline),
            orders = BuildKpi(currentOrderCount, previousOrderCount, orderSparkline),
            customers = BuildKpi(currentCustomerCount, previousCustomerCount, customerSparkline),
            products = BuildKpi(currentProductCount, previousProductCount, productSparkline),
        });
    }

    [HttpGet("/api/dashboard/revenue-chart")]
    public async Task<IActionResult> GetRevenueChart([FromQuery] DashboardQuery query, CancellationToken ct = default)
    {
        var range = GetPeriodRange(query);
        var buckets = GetBucketDefinition(range.StartUtc, range.EndUtc);
        var current = await BuildSalesSeriesAsync(query, range.StartUtc, range.EndUtc, buckets, ct);
        var previous = await BuildSalesSeriesAsync(query, range.PreviousStartUtc, range.PreviousEndUtc, buckets, ct);

        return Ok(new
        {
            labels = buckets.Labels,
            revenue = current.Revenue,
            previousRevenue = previous.Revenue,
        });
    }

    [HttpGet("/api/dashboard/order-status")]
    public async Task<IActionResult> GetOrderStatus([FromQuery] DashboardQuery query, CancellationToken ct = default)
    {
        var range = GetPeriodRange(query);
        var counts = query.CategoryId.HasValue
            ? await FilterOrderItems(query, range.StartUtc, range.EndUtc, successfulOnly: false)
                .GroupBy(item => item.Order!.OrderStatus)
                .Select(group => new { Status = group.Key, Count = group.Select(item => item.OrderId).Distinct().Count() })
                .ToDictionaryAsync(item => item.Status, item => item.Count, ct)
            : await FilterOrders(query, range.StartUtc, range.EndUtc)
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
    public async Task<IActionResult> GetTopProducts([FromQuery] DashboardQuery query, CancellationToken ct = default)
    {
        var range = GetPeriodRange(query);
        var currentSales = await QueryProductSales(query, range.StartUtc, range.EndUtc)
            .OrderByDescending(item => item.Revenue)
            .ThenByDescending(item => item.Sold)
            .Take(5)
            .ToListAsync(ct);

        if (currentSales.Count == 0)
        {
            return Ok(Array.Empty<object>());
        }

        var productIds = currentSales.Select(item => item.ProductId).ToArray();
        var previousSales = await QueryProductSales(query, range.PreviousStartUtc, range.PreviousEndUtc)
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
    public async Task<IActionResult> GetRecentOrders([FromQuery] DashboardQuery query, CancellationToken ct = default)
    {
        var range = GetPeriodRange(query);
        var orders = await FilterOrders(query, range.StartUtc, range.EndUtc)
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
    public async Task<IActionResult> GetCategoryRevenue([FromQuery] DashboardQuery query, CancellationToken ct = default)
    {
        var range = GetPeriodRange(query);
        var drillDown = query.CategoryId.HasValue;
        var rows = await SuccessfulOrderItems(query, range.StartUtc, range.EndUtc)
            .GroupBy(item => new
            {
                CategoryId = drillDown
                    ? item.ProductVariant!.Product!.CategoryId
                    : item.ProductVariant!.Product!.Category!.ParentId ??
                        item.ProductVariant.Product.CategoryId,
                CategoryName = drillDown
                    ? item.ProductVariant!.Product!.Category!.Name
                    : item.ProductVariant!.Product!.Category!.Parent != null
                    ? item.ProductVariant.Product.Category.Parent.Name
                    : item.ProductVariant.Product.Category.Name,
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
            return Ok(new
            {
                labels = Array.Empty<string>(),
                values = Array.Empty<double>(),
                revenues = Array.Empty<decimal>(),
            });
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
            revenues = displayed.Select(item => item.Revenue).ToArray(),
        });
    }

    [HttpGet("/api/dashboard/traffic")]
    public async Task<IActionResult> GetTraffic([FromQuery] DashboardQuery query, CancellationToken ct = default)
    {
        var range = GetPeriodRange(query);
        var buckets = GetBucketDefinition(range.StartUtc, range.EndUtc);
        var orderCounts = await BuildOrderCountSeriesAsync(query, range.StartUtc, range.EndUtc, buckets, ct);
        var sales = await BuildSalesSeriesAsync(query, range.StartUtc, range.EndUtc, buckets, ct);
        var newCustomers = await BuildCustomerSeriesAsync(range.StartUtc, range.EndUtc, buckets, ct);

        return Ok(new
        {
            labels = buckets.Labels,
            orders = orderCounts,
            revenue = sales.Revenue,
            newCustomers,
        });
    }

    [HttpGet("/api/dashboard/filter-options")]
    public async Task<IActionResult> GetFilterOptions(CancellationToken ct = default)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .Select(category => new DashboardCategoryOption
            {
                Id = category.Id,
                Name = category.Name,
                ParentId = category.ParentId,
                ParentName = category.Parent != null ? category.Parent.Name : null,
                Position = category.Position,
                HasChildren = category.Children.Any(),
            })
            .OrderBy(category => category.ParentName)
            .ThenBy(category => category.Position)
            .ThenBy(category => category.Name)
            .ToListAsync(ct);

        var categoryOptions = categories
            .Where(category => category.ParentId == null || !category.HasChildren)
            .Select(category => new
            {
                id = category.Id,
                label = category.ParentName == null
                    ? category.Name
                    : $"{category.ParentName} / {category.Name}",
            })
            .ToArray();

        return Ok(new
        {
            categories = categoryOptions,
            statuses = OrderDisplays.Select(item => new
            {
                value = item.Status.ToString(),
                label = item.Label,
            }).ToArray(),
        });
    }

    private IQueryable<Models.Entities.Order> FilterOrders(
        DashboardQuery query,
        DateTime startUtc,
        DateTime endUtc)
    {
        var orders = _db.Orders
            .AsNoTracking()
            .Where(order => order.CreatedAt >= startUtc && order.CreatedAt < endUtc);

        var status = ParseOrderStatus(query.OrderStatus);
        if (status.HasValue)
        {
            orders = orders.Where(order => order.OrderStatus == status.Value);
        }

        if (query.CategoryId.HasValue)
        {
            var orderIds = FilterOrderItems(query, startUtc, endUtc, successfulOnly: false)
                .Select(item => item.OrderId)
                .Distinct();
            orders = orders.Where(order => orderIds.Contains(order.Id));
        }

        return orders;
    }

    private IQueryable<Models.Entities.OrderItem> SuccessfulOrderItems(
        DashboardQuery query,
        DateTime startUtc,
        DateTime endUtc)
        => FilterOrderItems(query, startUtc, endUtc, successfulOnly: true);

    private IQueryable<Models.Entities.Order> SuccessfulOrders(
        DashboardQuery query,
        DateTime startUtc,
        DateTime endUtc)
    {
        var orders = _db.Orders
            .AsNoTracking()
            .Where(order =>
                order.CreatedAt >= startUtc &&
                order.CreatedAt < endUtc &&
                order.OrderStatus == OrderStatus.Completed &&
                order.PaymentStatus == PaymentStatus.Paid);

        var status = ParseOrderStatus(query.OrderStatus);
        if (status.HasValue)
        {
            orders = orders.Where(order => order.OrderStatus == status.Value);
        }

        return orders;
    }

    private IQueryable<SeriesSourceRow> RevenueSeriesSource(
        DashboardQuery query,
        DateTime startUtc,
        DateTime endUtc)
    {
        if (query.CategoryId.HasValue)
        {
            return SuccessfulOrderItems(query, startUtc, endUtc)
                .Select(item => new SeriesSourceRow
                {
                    CreatedAt = item.Order!.CreatedAt,
                    Value = item.UnitPrice * item.Quantity,
                });
        }

        return SuccessfulOrders(query, startUtc, endUtc)
            .Select(order => new SeriesSourceRow
            {
                CreatedAt = order.CreatedAt,
                Value = order.TotalAmount,
            });
    }

    private IQueryable<Models.Entities.OrderItem> FilterOrderItems(
        DashboardQuery query,
        DateTime startUtc,
        DateTime endUtc,
        bool successfulOnly)
    {
        var items = _db.OrderItems
            .AsNoTracking()
            .Where(item => item.Order!.CreatedAt >= startUtc && item.Order.CreatedAt < endUtc);

        var status = ParseOrderStatus(query.OrderStatus);
        if (successfulOnly)
        {
            items = items.Where(item =>
                item.Order!.OrderStatus == OrderStatus.Completed &&
                item.Order.PaymentStatus == PaymentStatus.Paid);
        }
        else if (status.HasValue)
        {
            items = items.Where(item => item.Order!.OrderStatus == status.Value);
        }

        if (successfulOnly && status.HasValue)
        {
            items = items.Where(item => item.Order!.OrderStatus == status.Value);
        }

        if (query.CategoryId.HasValue)
        {
            var categoryId = query.CategoryId.Value;
            items = items.Where(item =>
                item.ProductVariant != null &&
                item.ProductVariant.Product != null &&
                item.ProductVariant.Product.Category != null &&
                (item.ProductVariant.Product.CategoryId == categoryId ||
                    item.ProductVariant.Product.Category.ParentId == categoryId));
        }

        return items;
    }

    private IQueryable<ProductSalesRow> QueryProductSales(
        DashboardQuery query,
        DateTime startUtc,
        DateTime endUtc)
        => SuccessfulOrderItems(query, startUtc, endUtc)
            .GroupBy(item => new
            {
                ProductId = item.ProductVariant!.ProductId,
                ProductName = item.ProductVariant.Product!.Name,
                CategoryName = item.ProductVariant.Product.Category!.Parent != null
                    ? item.ProductVariant.Product.Category.Parent.Name
                    : item.ProductVariant.Product.Category.Name,
            })
            .Select(group => new ProductSalesRow
            {
                ProductId = group.Key.ProductId,
                Name = group.Key.ProductName,
                Category = group.Key.CategoryName,
                Sold = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.UnitPrice * item.Quantity),
            });

    private DashboardRange GetPeriodRange(DashboardQuery query)
    {
        var localNow = GetVietnamNow();
        var period = NormalizeDashboardPeriod(query.Period);
        var today = localNow.Date;
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
        var localStart = period switch
        {
            "today" => today,
            "yesterday" => today.AddDays(-1),
            "last7days" => today.AddDays(-6),
            "last30days" => today.AddDays(-29),
            "thismonth" => thisMonthStart,
            "lastmonth" => thisMonthStart.AddMonths(-1),
            "thisquarter" => GetQuarterStart(today),
            "thisyear" => new DateTime(today.Year, 1, 1),
            _ => today.AddDays(-29),
        };
        var localEnd = period switch
        {
            "yesterday" => today,
            "lastmonth" => thisMonthStart,
            _ => localNow,
        };

        if (period == "custom" || query.StartDate.HasValue || query.EndDate.HasValue)
        {
            localStart = (query.StartDate ?? localStart).Date;
            localEnd = (query.EndDate ?? localNow).Date.AddDays(1).AddTicks(-1);
            if (localEnd < localStart)
            {
                (localStart, localEnd) = (localEnd.Date, localStart.Date.AddDays(1).AddTicks(-1));
            }
        }

        var (previousLocalStart, previousLocalEnd) = GetPreviousPeriodRange(period, localStart, localEnd);

        return new DashboardRange(
            ToUtc(localStart),
            ToUtc(localEnd),
            ToUtc(previousLocalStart),
            ToUtc(previousLocalEnd));
    }

    private static string NormalizeDashboardPeriod(string? period)
        => period?.Trim().ToLowerInvariant() switch
        {
            "today" => "today",
            "yesterday" => "yesterday",
            "week" or "last7days" => "last7days",
            "month" or "last30days" => "last30days",
            "thismonth" => "thismonth",
            "lastmonth" => "lastmonth",
            "quarter" or "thisquarter" => "thisquarter",
            "year" or "thisyear" => "thisyear",
            "custom" => "custom",
            _ => "last30days",
        };

    private static DateTime GetQuarterStart(DateTime date)
    {
        var quarterStartMonth = ((date.Month - 1) / 3 * 3) + 1;
        return new DateTime(date.Year, quarterStartMonth, 1);
    }

    private static (DateTime Start, DateTime End) GetPreviousPeriodRange(
        string period,
        DateTime localStart,
        DateTime localEnd)
        => period switch
        {
            "today" or "yesterday" => (localStart.AddDays(-1), localEnd.AddDays(-1)),
            "thismonth" or "lastmonth" => (localStart.AddMonths(-1), localEnd.AddMonths(-1)),
            "thisquarter" => (localStart.AddMonths(-3), localEnd.AddMonths(-3)),
            "thisyear" => (localStart.AddYears(-1), localEnd.AddYears(-1)),
            _ => (localStart - (localEnd - localStart), localStart),
        };

    private async Task<DashboardSalesSummary> BuildSalesSummaryAsync(
        DashboardQuery query,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken ct)
    {
        if (query.CategoryId.HasValue)
        {
            return await SuccessfulOrderItems(query, startUtc, endUtc)
                .GroupBy(_ => 1)
                .Select(group => new DashboardSalesSummary
                {
                    Revenue = group.Sum(item => item.UnitPrice * item.Quantity),
                })
                .FirstOrDefaultAsync(ct) ?? new DashboardSalesSummary();
        }

        return await SuccessfulOrders(query, startUtc, endUtc)
            .GroupBy(_ => 1)
            .Select(group => new DashboardSalesSummary
            {
                Revenue = group.Sum(order => order.TotalAmount),
            })
            .FirstOrDefaultAsync(ct) ?? new DashboardSalesSummary();
    }

    private async Task<int> CountOrdersAsync(
        DashboardQuery query,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken ct)
        => await FilterOrders(query, startUtc, endUtc).CountAsync(ct);

    private DashboardBucketDefinition GetBucketDefinition(DateTime startUtc, DateTime endUtc)
    {
        var startLocal = FromUtc(startUtc).Date;
        var endLocal = FromUtc(endUtc).Date;
        var useMonthly = (endLocal - startLocal).TotalDays > 120;

        if (useMonthly)
        {
            var startMonth = new DateTime(startLocal.Year, startLocal.Month, 1);
            var endMonth = new DateTime(endLocal.Year, endLocal.Month, 1);
            var monthCount = ((endMonth.Year - startMonth.Year) * 12) + endMonth.Month - startMonth.Month + 1;
            var labels = Enumerable.Range(0, monthCount)
                .Select(offset => startMonth.AddMonths(offset).ToString("MM/yyyy"))
                .ToArray();

            return new DashboardBucketDefinition(ToUtc(startMonth), monthCount, labels, UseMonthly: true);
        }

        var dayCount = Math.Max(1, (endLocal - startLocal).Days + 1);
        var dayLabels = Enumerable.Range(0, dayCount)
            .Select(offset => startLocal.AddDays(offset).ToString("dd/MM"))
            .ToArray();

        return new DashboardBucketDefinition(ToUtc(startLocal), dayCount, dayLabels, UseMonthly: false);
    }

    private async Task<DashboardSalesSeries> BuildSalesSeriesAsync(
        DashboardQuery query,
        DateTime startUtc,
        DateTime endUtc,
        DashboardBucketDefinition buckets,
        CancellationToken ct)
    {
        var source = RevenueSeriesSource(query, startUtc, endUtc);
        var rows = buckets.UseMonthly
            ? await source
                .GroupBy(item => EF.Functions.DateDiffMonth(buckets.StartUtc, item.CreatedAt))
                .Select(group => new DashboardSalesBucket
                {
                    Bucket = group.Key,
                    Revenue = group.Sum(item => item.Value),
                })
                .ToListAsync(ct)
            : await source
                .GroupBy(item => EF.Functions.DateDiffDay(buckets.StartUtc, item.CreatedAt))
                .Select(group => new DashboardSalesBucket
                {
                    Bucket = group.Key,
                    Revenue = group.Sum(item => item.Value),
                })
                .ToListAsync(ct);

        var revenue = new decimal[buckets.Count];
        foreach (var row in rows)
        {
            if (row.Bucket is < 0 || row.Bucket >= buckets.Count)
            {
                continue;
            }

            revenue[row.Bucket] = row.Revenue / 1_000_000m;
        }

        return new DashboardSalesSeries(revenue);
    }

    private async Task<int[]> BuildOrderCountSeriesAsync(
        DashboardQuery query,
        DateTime startUtc,
        DateTime endUtc,
        DashboardBucketDefinition buckets,
        CancellationToken ct)
    {
        if (query.CategoryId.HasValue)
        {
            var itemSource = FilterOrderItems(query, startUtc, endUtc, successfulOnly: false);
            var itemRows = buckets.UseMonthly
                ? await itemSource
                    .GroupBy(item => EF.Functions.DateDiffMonth(buckets.StartUtc, item.Order!.CreatedAt))
                    .Select(group => new DashboardCountBucket
                    {
                        Bucket = group.Key,
                        Count = group.Select(item => item.OrderId).Distinct().Count(),
                    })
                    .ToListAsync(ct)
                : await itemSource
                    .GroupBy(item => EF.Functions.DateDiffDay(buckets.StartUtc, item.Order!.CreatedAt))
                    .Select(group => new DashboardCountBucket
                    {
                        Bucket = group.Key,
                        Count = group.Select(item => item.OrderId).Distinct().Count(),
                    })
                    .ToListAsync(ct);

            return MaterializeCountSeries(itemRows, buckets.Count);
        }

        var orderSource = FilterOrders(query, startUtc, endUtc);
        var orderRows = buckets.UseMonthly
            ? await orderSource
                .GroupBy(order => EF.Functions.DateDiffMonth(buckets.StartUtc, order.CreatedAt))
                .Select(group => new DashboardCountBucket { Bucket = group.Key, Count = group.Count() })
                .ToListAsync(ct)
            : await orderSource
                .GroupBy(order => EF.Functions.DateDiffDay(buckets.StartUtc, order.CreatedAt))
                .Select(group => new DashboardCountBucket { Bucket = group.Key, Count = group.Count() })
                .ToListAsync(ct);

        return MaterializeCountSeries(orderRows, buckets.Count);
    }

    private async Task<int[]> BuildCustomerSeriesAsync(
        DateTime startUtc,
        DateTime endUtc,
        DashboardBucketDefinition buckets,
        CancellationToken ct)
    {
        var source = _db.Users
            .AsNoTracking()
            .Where(user => user.CreatedAt >= startUtc && user.CreatedAt < endUtc);

        var rows = buckets.UseMonthly
            ? await source
                .GroupBy(user => EF.Functions.DateDiffMonth(buckets.StartUtc, user.CreatedAt))
                .Select(group => new DashboardCountBucket { Bucket = group.Key, Count = group.Count() })
                .ToListAsync(ct)
            : await source
                .GroupBy(user => EF.Functions.DateDiffDay(buckets.StartUtc, user.CreatedAt))
                .Select(group => new DashboardCountBucket { Bucket = group.Key, Count = group.Count() })
                .ToListAsync(ct);

        return MaterializeCountSeries(rows, buckets.Count);
    }

    private static int[] MaterializeCountSeries(IEnumerable<DashboardCountBucket> rows, int count)
    {
        var values = new int[count];
        foreach (var row in rows)
        {
            if (row.Bucket is >= 0 && row.Bucket < count)
            {
                values[row.Bucket] = row.Count;
            }
        }

        return values;
    }

    private static OrderStatus? ParseOrderStatus(string? status)
        => Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var value)
            ? value
            : null;

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

    private async Task<decimal[]> BuildSeriesAsync(
        IQueryable<SeriesSourceRow> source,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken ct)
    {
        var totalSeconds = GetRangeSeconds(startUtc, endUtc);
        var rows = await source
            .Where(row => row.CreatedAt >= startUtc && row.CreatedAt < endUtc)
            .Select(row => new SeriesBucketRow
            {
                Bucket = EF.Functions.DateDiffSecond(startUtc, row.CreatedAt) * SparklinePointCount / totalSeconds,
                Value = row.Value,
            })
            .GroupBy(row => row.Bucket)
            .Select(group => new SeriesBucketRow
            {
                Bucket = group.Key,
                Value = group.Sum(row => row.Value),
            })
            .ToListAsync(ct);

        return MaterializeSeries(rows);
    }

    private static int GetRangeSeconds(DateTime startUtc, DateTime endUtc)
        => (int)Math.Min(
            int.MaxValue,
            Math.Max(1d, Math.Ceiling((endUtc - startUtc).TotalSeconds)));

    private static decimal[] MaterializeSeries(IEnumerable<SeriesBucketRow> rows)
    {
        var values = new decimal[SparklinePointCount];
        foreach (var row in rows)
        {
            var index = Math.Clamp(row.Bucket, 0, SparklinePointCount - 1);
            values[index] += row.Value;
        }

        return values;
    }

    private static decimal[] BuildCumulativeCountSeries(decimal[] increments, int baseline)
    {
        var result = new decimal[SparklinePointCount];
        decimal runningTotal = baseline;
        for (var index = 0; index < result.Length; index++)
        {
            runningTotal += index < increments.Length ? increments[index] : 0m;
            result[index] = runningTotal;
        }

        return result;
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

    public sealed class DashboardQuery
    {
        public string? Period { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public long? CategoryId { get; set; }
        public string? OrderStatus { get; set; }
    }

    private sealed class DashboardSalesSummary
    {
        public decimal Revenue { get; init; }
    }

    private sealed class SeriesSourceRow
    {
        public DateTime CreatedAt { get; init; }
        public decimal Value { get; init; }
    }

    private sealed record DashboardBucketDefinition(
        DateTime StartUtc,
        int Count,
        string[] Labels,
        bool UseMonthly);

    private sealed record DashboardSalesSeries(decimal[] Revenue);

    private sealed class DashboardSalesBucket
    {
        public int Bucket { get; init; }
        public decimal Revenue { get; init; }
    }

    private sealed class DashboardCountBucket
    {
        public int Bucket { get; init; }
        public int Count { get; init; }
    }

    private sealed class DashboardCategoryOption
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public long? ParentId { get; init; }
        public string? ParentName { get; init; }
        public int Position { get; init; }
        public bool HasChildren { get; init; }
    }

    private sealed class SeriesBucketRow
    {
        public int Bucket { get; init; }
        public decimal Value { get; init; }
    }

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
