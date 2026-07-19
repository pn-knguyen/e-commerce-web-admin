namespace e_commerce_web_admin.ViewModels.ProfitReports;

public sealed class ProfitReportQuery
{
    public string? Period { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public sealed class ProfitReportViewModel
{
    public string Period { get; set; } = "last30days";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<ProfitPeriodOption> PeriodOptions { get; set; } = [];

    public int CompletedOrderCount { get; set; }
    public int SoldQuantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPercent { get; set; }
    public decimal AverageProfitPerOrder { get; set; }
    public decimal InventoryCostValue { get; set; }
    public decimal InventoryPotentialProfit { get; set; }

    public List<ProfitTrendPoint> Trend { get; set; } = [];
    public List<ProfitProductRow> TopProducts { get; set; } = [];
    public List<ProfitCategoryRow> Categories { get; set; } = [];
}

public sealed class ProfitPeriodOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public sealed class ProfitTrendPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossProfit { get; set; }
}

public sealed class ProfitProductRow
{
    public string ProductName { get; set; } = string.Empty;
    public string VariantCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPercent { get; set; }
}

public sealed class ProfitCategoryRow
{
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPercent { get; set; }
}
