using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.ViewModels.ProfitReports;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.ProfitReports;

public sealed class ProfitReportService : IProfitReportService
{
    private readonly ApplicationDbContext _db;

    public ProfitReportService(ApplicationDbContext db)
        => _db = db;

    public async Task<ProfitReportViewModel> GetReportAsync(
        ProfitReportQuery query,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var period = NormalizePeriod(query.Period);
        var startDate = GetStartDate(period, now);

        var salesQuery = _db.OrderItems
            .AsNoTracking()
            .Where(item =>
                item.Order != null &&
                item.Order.OrderStatus == OrderStatus.Completed &&
                item.Order.PaymentStatus == PaymentStatus.Paid &&
                item.Order.CreatedAt <= now);

        if (startDate.HasValue)
        {
            salesQuery = salesQuery.Where(item => item.Order!.CreatedAt >= startDate.Value);
        }

        var sales = await salesQuery
            .Select(item => new SaleRow(
                item.Order!.Id,
                item.Order.CreatedAt,
                item.ProductVariant != null && item.ProductVariant.Product != null
                    ? item.ProductVariant.Product.Name
                    : "Sản phẩm không xác định",
                item.ProductVariant != null ? item.ProductVariant.Code : "N/A",
                item.ProductVariant != null &&
                    item.ProductVariant.Product != null &&
                    item.ProductVariant.Product.Category != null
                        ? item.ProductVariant.Product.Category.Name
                        : "Chưa phân loại",
                item.Quantity,
                item.UnitPrice,
                item.UnitCost))
            .ToListAsync(ct);

        var effectiveStartDate = startDate
            ?? (sales.Count > 0 ? sales.Min(item => item.OrderedAt).Date : new DateTime(now.Year, 1, 1));

        var revenue = sales.Sum(item => item.Revenue);
        var cost = sales.Sum(item => item.Cost);
        var grossProfit = revenue - cost;
        var completedOrderCount = sales.Select(item => item.OrderId).Distinct().Count();
        var inventoryCostValue = await _db.ProductVariants
            .AsNoTracking()
            .SumAsync(variant => (decimal?)(variant.Quantity * variant.AverageCost), ct) ?? 0m;
        var inventoryPotentialProfit = await _db.ProductVariants
            .AsNoTracking()
            .SumAsync(variant => (decimal?)(variant.Quantity * (variant.Price - variant.AverageCost)), ct) ?? 0m;

        return new ProfitReportViewModel
        {
            Period = period,
            StartDate = effectiveStartDate,
            EndDate = now,
            PeriodOptions = BuildPeriodOptions(period),
            CompletedOrderCount = completedOrderCount,
            SoldQuantity = sales.Sum(item => item.Quantity),
            Revenue = revenue,
            Cost = cost,
            GrossProfit = grossProfit,
            GrossMarginPercent = CalculatePercent(grossProfit, revenue),
            AverageProfitPerOrder = completedOrderCount > 0
                ? Math.Round(grossProfit / completedOrderCount, 2, MidpointRounding.AwayFromZero)
                : 0m,
            InventoryCostValue = inventoryCostValue,
            InventoryPotentialProfit = inventoryPotentialProfit,
            Trend = BuildTrend(sales, effectiveStartDate, now),
            TopProducts = BuildTopProducts(sales),
            Categories = BuildCategories(sales),
        };
    }

    private static string NormalizePeriod(string? period)
        => period?.Trim().ToLowerInvariant() switch
        {
            "7d" => "7d",
            "90d" => "90d",
            "year" => "year",
            "all" => "all",
            _ => "30d",
        };

    private static DateTime? GetStartDate(string period, DateTime now)
        => period switch
        {
            "7d" => now.Date.AddDays(-6),
            "30d" => now.Date.AddDays(-29),
            "90d" => now.Date.AddDays(-89),
            "year" => new DateTime(now.Year, 1, 1),
            "all" => null,
            _ => now.Date.AddDays(-29),
        };

    private static List<ProfitPeriodOption> BuildPeriodOptions(string selected)
    {
        (string Value, string Text)[] options =
        [
            ("7d", "7 ngày gần đây"),
            ("30d", "30 ngày gần đây"),
            ("90d", "90 ngày gần đây"),
            ("year", "Năm nay"),
            ("all", "Tất cả dữ liệu"),
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

    private static List<ProfitTrendPoint> BuildTrend(
        IReadOnlyCollection<SaleRow> sales,
        DateTime startDate,
        DateTime endDate)
    {
        var useMonthly = (endDate.Date - startDate.Date).TotalDays > 120;
        var groups = sales
            .GroupBy(item => useMonthly
                ? new DateTime(item.OrderedAt.Year, item.OrderedAt.Month, 1)
                : item.OrderedAt.Date)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Revenue = group.Sum(item => item.Revenue),
                    Cost = group.Sum(item => item.Cost),
                    Profit = group.Sum(item => item.Profit),
                });

        var trend = new List<ProfitTrendPoint>();
        if (useMonthly)
        {
            var cursor = new DateTime(startDate.Year, startDate.Month, 1);
            var end = new DateTime(endDate.Year, endDate.Month, 1);
            while (cursor <= end)
            {
                groups.TryGetValue(cursor, out var value);
                trend.Add(new ProfitTrendPoint
                {
                    Label = cursor.ToString("MM/yyyy"),
                    Revenue = value?.Revenue ?? 0m,
                    Cost = value?.Cost ?? 0m,
                    GrossProfit = value?.Profit ?? 0m,
                });
                cursor = cursor.AddMonths(1);
            }

            return trend;
        }

        for (var cursor = startDate.Date; cursor <= endDate.Date; cursor = cursor.AddDays(1))
        {
            groups.TryGetValue(cursor, out var value);
            trend.Add(new ProfitTrendPoint
            {
                Label = cursor.ToString("dd/MM"),
                Revenue = value?.Revenue ?? 0m,
                Cost = value?.Cost ?? 0m,
                GrossProfit = value?.Profit ?? 0m,
            });
        }

        return trend;
    }

    private static List<ProfitProductRow> BuildTopProducts(IReadOnlyCollection<SaleRow> sales)
        => sales
            .GroupBy(item => new { item.ProductName, item.VariantCode })
            .Select(group =>
            {
                var revenue = group.Sum(item => item.Revenue);
                var cost = group.Sum(item => item.Cost);
                var profit = revenue - cost;
                return new ProfitProductRow
                {
                    ProductName = group.Key.ProductName,
                    VariantCode = group.Key.VariantCode,
                    Quantity = group.Sum(item => item.Quantity),
                    Revenue = revenue,
                    Cost = cost,
                    GrossProfit = profit,
                    GrossMarginPercent = CalculatePercent(profit, revenue),
                };
            })
            .OrderByDescending(item => item.GrossProfit)
            .ThenByDescending(item => item.Revenue)
            .Take(10)
            .ToList();

    private static List<ProfitCategoryRow> BuildCategories(IReadOnlyCollection<SaleRow> sales)
        => sales
            .GroupBy(item => item.CategoryName)
            .Select(group =>
            {
                var revenue = group.Sum(item => item.Revenue);
                var profit = group.Sum(item => item.Profit);
                return new ProfitCategoryRow
                {
                    CategoryName = group.Key,
                    Quantity = group.Sum(item => item.Quantity),
                    Revenue = revenue,
                    GrossProfit = profit,
                    GrossMarginPercent = CalculatePercent(profit, revenue),
                };
            })
            .OrderByDescending(item => item.GrossProfit)
            .Take(8)
            .ToList();

    private static decimal CalculatePercent(decimal value, decimal total)
        => total > 0m
            ? Math.Round(value / total * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

    private sealed record SaleRow(
        long OrderId,
        DateTime OrderedAt,
        string ProductName,
        string VariantCode,
        string CategoryName,
        int Quantity,
        decimal UnitPrice,
        decimal UnitCost)
    {
        public decimal Revenue => UnitPrice * Quantity;
        public decimal Cost => UnitCost * Quantity;
        public decimal Profit => Revenue - Cost;
    }
}
