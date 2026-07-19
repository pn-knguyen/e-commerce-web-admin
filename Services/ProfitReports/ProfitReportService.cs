using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.ViewModels.ProfitReports;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.ProfitReports;

public sealed class ProfitReportService : IProfitReportService
{
    private const double VietnamSqlOffsetHours = 7d;

    private readonly ApplicationDbContext _db;
    private readonly TimeZoneInfo _vietnamTimeZone = TimeZoneHelper.GetVietnamTimeZone();

    public ProfitReportService(ApplicationDbContext db)
        => _db = db;

    public async Task<ProfitReportViewModel> GetReportAsync(
        ProfitReportQuery query,
        CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;
        var localNow = FromUtc(nowUtc);
        var period = NormalizePeriod(query.Period);
        var range = GetDateRange(period, query.StartDate, query.EndDate, localNow);
        var startUtc = ToUtc(range.StartLocal);
        var endUtc = ToUtc(range.EndLocalExclusive);

        var successfulOrdersQuery = _db.Orders
            .AsNoTracking()
            .Where(order =>
                order.OrderStatus == OrderStatus.Completed &&
                order.PaymentStatus == PaymentStatus.Paid &&
                order.CreatedAt >= startUtc &&
                order.CreatedAt < endUtc &&
                order.CreatedAt < nowUtc);

        var salesQuery = _db.OrderItems
            .AsNoTracking()
            .Where(item =>
                item.Order != null &&
                item.Order.OrderStatus == OrderStatus.Completed &&
                item.Order.PaymentStatus == PaymentStatus.Paid &&
                item.Order.CreatedAt >= startUtc &&
                item.Order.CreatedAt < endUtc &&
                item.Order.CreatedAt < nowUtc);

        var orderSummary = await successfulOrdersQuery
            .GroupBy(_ => 1)
            .Select(group => new ProfitOrderSummary
            {
                CompletedOrderCount = group.Count(),
                Revenue = group.Sum(order => order.TotalAmount),
            })
            .FirstOrDefaultAsync(ct) ?? new ProfitOrderSummary();

        var itemSummary = await salesQuery
            .GroupBy(_ => 1)
            .Select(group => new ProfitItemSummary
            {
                SoldQuantity = group.Sum(item => item.Quantity),
                Cost = group.Sum(item => item.UnitCost * item.Quantity),
            })
            .FirstOrDefaultAsync(ct) ?? new ProfitItemSummary();

        var revenue = orderSummary.Revenue;
        var cost = itemSummary.Cost;
        var grossProfit = revenue - cost;
        var completedOrderCount = orderSummary.CompletedOrderCount;
        var inventoryCostValue = await _db.ProductVariants
            .AsNoTracking()
            .SumAsync(variant => (decimal?)(variant.Quantity * variant.AverageCost), ct) ?? 0m;
        var inventoryPotentialProfit = await _db.ProductVariants
            .AsNoTracking()
            .SumAsync(variant => (decimal?)(variant.Quantity * (variant.Price - variant.AverageCost)), ct) ?? 0m;
        var trend = await BuildTrendAsync(successfulOrdersQuery, salesQuery, range.StartLocal, range.DisplayEndLocal, ct);
        var topProducts = await BuildTopProductsAsync(salesQuery, ct);
        var categories = await BuildCategoriesAsync(salesQuery, ct);

        return new ProfitReportViewModel
        {
            Period = period,
            StartDate = range.StartLocal,
            EndDate = range.DisplayEndLocal,
            PeriodOptions = BuildPeriodOptions(period),
            CompletedOrderCount = completedOrderCount,
            SoldQuantity = itemSummary.SoldQuantity,
            Revenue = revenue,
            Cost = cost,
            GrossProfit = grossProfit,
            GrossMarginPercent = CalculatePercent(grossProfit, revenue),
            AverageProfitPerOrder = completedOrderCount > 0
                ? Math.Round(grossProfit / completedOrderCount, 2, MidpointRounding.AwayFromZero)
                : 0m,
            InventoryCostValue = inventoryCostValue,
            InventoryPotentialProfit = inventoryPotentialProfit,
            Trend = trend,
            TopProducts = topProducts,
            Categories = categories,
        };
    }

    private static string NormalizePeriod(string? period)
        => period?.Trim().ToLowerInvariant() switch
        {
            "today" => "today",
            "yesterday" => "yesterday",
            "7d" or "last7days" => "last7days",
            "30d" or "month" or "last30days" => "last30days",
            "thismonth" => "thismonth",
            "lastmonth" => "lastmonth",
            "quarter" or "thisquarter" => "thisquarter",
            "year" or "thisyear" => "thisyear",
            "custom" => "custom",
            _ => "last30days",
        };

    private static List<ProfitPeriodOption> BuildPeriodOptions(string selected)
    {
        (string Value, string Text)[] options =
        [
            ("today", "Hôm nay"),
            ("yesterday", "Hôm qua"),
            ("last7days", "7 ngày gần nhất"),
            ("last30days", "30 ngày gần nhất"),
            ("thismonth", "Tháng này"),
            ("lastmonth", "Tháng trước"),
            ("thisquarter", "Quý này"),
            ("thisyear", "Năm nay"),
            ("custom", "Tùy chọn khoảng thời gian"),
        ];

        return options
            .Select(option => new ProfitPeriodOption
            {
                Value = option.Value,
                Text = option.Text,
                IsSelected = option.Value == selected,
            })
            .ToList();
    }

    private static ProfitDateRange GetDateRange(
        string period,
        DateTime? startDate,
        DateTime? endDate,
        DateTime localNow)
    {
        var today = localNow.Date;
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);

        if (period == "custom")
        {
            var localStart = (startDate ?? today.AddDays(-29)).Date;
            var localEndDate = (endDate ?? localNow).Date;
            if (localEndDate < localStart)
            {
                (localStart, localEndDate) = (localEndDate, localStart);
            }

            var customEndExclusive = localEndDate.AddDays(1);
            return new ProfitDateRange(
                localStart,
                customEndExclusive,
                customEndExclusive.AddTicks(-1));
        }

        var localStartDate = period switch
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
        var localEndExclusive = period switch
        {
            "yesterday" => today,
            "lastmonth" => thisMonthStart,
            _ => localNow,
        };
        var displayEnd = period is "yesterday" or "lastmonth"
            ? localEndExclusive.AddTicks(-1)
            : localEndExclusive;

        return new ProfitDateRange(localStartDate, localEndExclusive, displayEnd);
    }

    private static DateTime GetQuarterStart(DateTime date)
    {
        var quarterStartMonth = ((date.Month - 1) / 3 * 3) + 1;
        return new DateTime(date.Year, quarterStartMonth, 1);
    }

    private static async Task<List<ProfitTrendPoint>> BuildTrendAsync(
        IQueryable<Order> successfulOrdersQuery,
        IQueryable<OrderItem> salesQuery,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct)
    {
        var useMonthly = (endDate.Date - startDate.Date).TotalDays > 120;
        var trend = new List<ProfitTrendPoint>();

        if (useMonthly)
        {
            var startMonth = new DateTime(startDate.Year, startDate.Month, 1);
            var endMonth = new DateTime(endDate.Year, endDate.Month, 1);
            var revenueRows = await successfulOrdersQuery
                .GroupBy(order => EF.Functions.DateDiffMonth(
                    startMonth,
                    order.CreatedAt.AddHours(VietnamSqlOffsetHours)))
                .Select(group => new ProfitTrendBucket
                {
                    Offset = group.Key,
                    Revenue = group.Sum(order => order.TotalAmount),
                })
                .ToListAsync(ct);
            var costRows = await salesQuery
                .GroupBy(item => EF.Functions.DateDiffMonth(
                    startMonth,
                    item.Order!.CreatedAt.AddHours(VietnamSqlOffsetHours)))
                .Select(group => new ProfitTrendBucket
                {
                    Offset = group.Key,
                    Cost = group.Sum(item => item.UnitCost * item.Quantity),
                })
                .ToListAsync(ct);
            var revenueByOffset = revenueRows.ToDictionary(item => item.Offset);
            var costByOffset = costRows.ToDictionary(item => item.Offset);

            for (var cursor = startMonth; cursor <= endMonth; cursor = cursor.AddMonths(1))
            {
                var offset = ((cursor.Year - startMonth.Year) * 12) + cursor.Month - startMonth.Month;
                revenueByOffset.TryGetValue(offset, out var revenueValue);
                costByOffset.TryGetValue(offset, out var costValue);
                var revenue = revenueValue?.Revenue ?? 0m;
                var cost = costValue?.Cost ?? 0m;
                trend.Add(new ProfitTrendPoint
                {
                    Label = cursor.ToString("MM/yyyy"),
                    Revenue = revenue,
                    Cost = cost,
                    GrossProfit = revenue - cost,
                });
            }

            return trend;
        }

        var startDay = startDate.Date;
        var endDay = endDate.Date;
        var revenueDayRows = await successfulOrdersQuery
            .GroupBy(order => EF.Functions.DateDiffDay(
                startDay,
                order.CreatedAt.AddHours(VietnamSqlOffsetHours)))
            .Select(group => new ProfitTrendBucket
            {
                Offset = group.Key,
                Revenue = group.Sum(order => order.TotalAmount),
            })
            .ToListAsync(ct);
        var costDayRows = await salesQuery
            .GroupBy(item => EF.Functions.DateDiffDay(
                startDay,
                item.Order!.CreatedAt.AddHours(VietnamSqlOffsetHours)))
            .Select(group => new ProfitTrendBucket
            {
                Offset = group.Key,
                Cost = group.Sum(item => item.UnitCost * item.Quantity),
            })
            .ToListAsync(ct);
        var revenueByDayOffset = revenueDayRows.ToDictionary(item => item.Offset);
        var costByDayOffset = costDayRows.ToDictionary(item => item.Offset);

        for (var cursor = startDay; cursor <= endDay; cursor = cursor.AddDays(1))
        {
            var offset = (cursor - startDay).Days;
            revenueByDayOffset.TryGetValue(offset, out var revenueValue);
            costByDayOffset.TryGetValue(offset, out var costValue);
            var revenue = revenueValue?.Revenue ?? 0m;
            var cost = costValue?.Cost ?? 0m;
            trend.Add(new ProfitTrendPoint
            {
                Label = cursor.ToString("dd/MM"),
                Revenue = revenue,
                Cost = cost,
                GrossProfit = revenue - cost,
            });
        }

        return trend;
    }

    private static async Task<List<ProfitProductRow>> BuildTopProductsAsync(
        IQueryable<OrderItem> salesQuery,
        CancellationToken ct)
    {
        var rows = await salesQuery
            .GroupBy(item => new
            {
                ProductName = item.ProductVariant != null && item.ProductVariant.Product != null
                    ? item.ProductVariant.Product.Name
                    : "Sản phẩm không xác định",
                VariantCode = item.ProductVariant != null ? item.ProductVariant.Code : "N/A",
            })
            .Select(group => new ProfitProductRow
            {
                ProductName = group.Key.ProductName,
                VariantCode = group.Key.VariantCode,
                Quantity = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.UnitPrice * item.Quantity),
                Cost = group.Sum(item => item.UnitCost * item.Quantity),
                GrossProfit = group.Sum(item => item.UnitPrice * item.Quantity) -
                    group.Sum(item => item.UnitCost * item.Quantity),
            })
            .OrderByDescending(item => item.GrossProfit)
            .ThenByDescending(item => item.Revenue)
            .Take(10)
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            row.GrossMarginPercent = CalculatePercent(row.GrossProfit, row.Revenue);
        }

        return rows;
    }

    private static async Task<List<ProfitCategoryRow>> BuildCategoriesAsync(
        IQueryable<OrderItem> salesQuery,
        CancellationToken ct)
    {
        var rows = await salesQuery
            .GroupBy(item =>
                item.ProductVariant != null &&
                item.ProductVariant.Product != null &&
                item.ProductVariant.Product.Category != null
                    ? item.ProductVariant.Product.Category.Parent != null
                        ? item.ProductVariant.Product.Category.Parent.Name
                        : item.ProductVariant.Product.Category.Name
                    : "Chưa phân loại")
            .Select(group => new ProfitCategoryRow
            {
                CategoryName = group.Key,
                Quantity = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.UnitPrice * item.Quantity),
                GrossProfit = group.Sum(item => item.UnitPrice * item.Quantity) -
                    group.Sum(item => item.UnitCost * item.Quantity),
            })
            .OrderByDescending(item => item.GrossProfit)
            .Take(8)
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            row.GrossMarginPercent = CalculatePercent(row.GrossProfit, row.Revenue);
        }

        return rows;
    }

    private static decimal CalculatePercent(decimal value, decimal total)
        => total > 0m
            ? Math.Round(value / total * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

    private DateTime ToUtc(DateTime localDateTime)
        => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified),
            _vietnamTimeZone);

    private DateTime FromUtc(DateTime utcDateTime)
        => TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc),
            _vietnamTimeZone);

    private sealed record ProfitDateRange(
        DateTime StartLocal,
        DateTime EndLocalExclusive,
        DateTime DisplayEndLocal);

    private sealed class ProfitOrderSummary
    {
        public int CompletedOrderCount { get; init; }
        public decimal Revenue { get; init; }
    }

    private sealed class ProfitItemSummary
    {
        public int SoldQuantity { get; init; }
        public decimal Cost { get; init; }
    }

    private sealed class ProfitTrendBucket
    {
        public int Offset { get; init; }
        public decimal Revenue { get; init; }
        public decimal Cost { get; init; }
    }

}
