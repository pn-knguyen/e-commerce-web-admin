using System.ComponentModel.DataAnnotations;
using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.ViewModels.Inventory;

public sealed class InventoryIndexQuery
{
    public string? Search { get; set; }
    public string? Stock { get; set; }
    public string? ReceiptStatus { get; set; }
    public long? SupplierId { get; set; }
    public long? CategoryId { get; set; }
    public int StockPage { get; set; } = 1;
    public int ReceiptPage { get; set; } = 1;
}

public sealed class InventoryIndexViewModel
{
    public List<InventoryStockRowViewModel> StockRows { get; set; } = [];
    public List<GoodsReceiptRowViewModel> Receipts { get; set; } = [];
    public List<InventoryFilterOption> SupplierOptions { get; set; } = [];
    public List<InventoryFilterOption> CategoryOptions { get; set; } = [];
    public List<InventoryFilterOption> ReceiptStatusOptions { get; set; } = [];

    public string? Search { get; set; }
    public string? Stock { get; set; }
    public string? ReceiptStatus { get; set; }
    public long? SupplierId { get; set; }
    public long? CategoryId { get; set; }

    public int StockPage { get; set; } = 1;
    public int StockPageSize { get; set; } = 20;
    public int StockTotalCount { get; set; }
    public int ReceiptPage { get; set; } = 1;
    public int ReceiptPageSize { get; set; } = 12;
    public int ReceiptTotalCount { get; set; }

    public int TotalVariantCount { get; set; }
    public int TotalStockQuantity { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int PendingReceiptCount { get; set; }

    public int StockTotalPages => StockPageSize <= 0
        ? 0
        : (int)Math.Ceiling((double)StockTotalCount / StockPageSize);

    public int ReceiptTotalPages => ReceiptPageSize <= 0
        ? 0
        : (int)Math.Ceiling((double)ReceiptTotalCount / ReceiptPageSize);

    public bool HasPrevStock => StockPage > 1;
    public bool HasNextStock => StockPage < StockTotalPages;
    public bool HasPrevReceipt => ReceiptPage > 1;
    public bool HasNextReceipt => ReceiptPage < ReceiptTotalPages;

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        !string.IsNullOrWhiteSpace(Stock) ||
        !string.IsNullOrWhiteSpace(ReceiptStatus) ||
        SupplierId.HasValue ||
        CategoryId.HasValue;
}

public sealed class InventoryStockRowViewModel
{
    public long VariantId { get; set; }
    public string VariantCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int SoldCount { get; set; }
    public int FifoQuantity { get; set; }
    public decimal FifoStockValue { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastReceiptAt { get; set; }

    public decimal StockValue => FifoStockValue;
    public decimal EstimatedRetailValue => Price * Quantity;
}

public sealed class InventoryStockDetailsViewModel
{
    public long VariantId { get; set; }
    public string VariantCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public int Quantity { get; set; }
    public int SoldCount { get; set; }
    public bool IsActive { get; set; }
    public int FifoQuantity { get; set; }
    public decimal RemainingCapital { get; set; }
    public decimal OriginalCapital { get; set; }
    public int ReceiptCount { get; set; }
    public DateTime? LastReceiptAt { get; set; }
    public List<InventoryBatchDetailsViewModel> Batches { get; set; } = [];

    public decimal AverageCost => FifoQuantity == 0 ? 0 : RemainingCapital / FifoQuantity;
    public decimal EstimatedRetailValue => SalePrice * Quantity;
}

public sealed class InventoryBatchDetailsViewModel
{
    public long BatchId { get; set; }
    public long ReceiptId { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public int QuantityReceived { get; set; }
    public int QuantityRemaining { get; set; }
    public int QuantitySold => Math.Max(0, QuantityReceived - QuantityRemaining);
    public decimal UnitCost { get; set; }
    public decimal OriginalCapital => QuantityReceived * UnitCost;
    public decimal RemainingCapital => QuantityRemaining * UnitCost;
}

public sealed class GoodsReceiptRowViewModel
{
    public long Id { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public GoodsReceiptStatus Status { get; set; }
    public int ItemCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class GoodsReceiptDetailsViewModel
{
    public long Id { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierPhone { get; set; }
    public string? SupplierEmail { get; set; }
    public GoodsReceiptStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string? ApprovedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<GoodsReceiptItemViewModel> Items { get; set; } = [];

    public int TotalQuantity => Items.Sum(item => item.Quantity);
    public bool CanEdit => Status is GoodsReceiptStatus.Draft or GoodsReceiptStatus.Pending;
    public bool CanSubmit => Status == GoodsReceiptStatus.Draft;
    public bool CanApprove => Status is GoodsReceiptStatus.Draft or GoodsReceiptStatus.Pending;
    public bool CanCancel => Status is GoodsReceiptStatus.Draft or GoodsReceiptStatus.Pending;
    public bool CanDelete => Status is GoodsReceiptStatus.Draft or GoodsReceiptStatus.Pending;
}

public sealed class GoodsReceiptItemViewModel
{
    public long Id { get; set; }
    public long ProductVariantId { get; set; }
    public string VariantCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal ImportPrice { get; set; }
    public int CurrentStock { get; set; }
    public decimal LineTotal => ImportPrice * Quantity;
}

public sealed class GoodsReceiptFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp.")]
    [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn nhà cung cấp.")]
    public long? SupplierId { get; set; }

    [Required(ErrorMessage = "Mã phiếu nhập là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Mã phiếu nhập tối đa 50 ký tự.")]
    [RegularExpression(@"^[A-Z0-9][A-Z0-9_-]{2,49}$",
        ErrorMessage = "Mã phiếu chỉ gồm chữ in hoa, số, dấu gạch ngang hoặc gạch dưới.")]
    public string ReceiptCode { get; set; } = string.Empty;

    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;
    public List<InventorySelectOption> SupplierOptions { get; set; } = [];
    public List<InventoryProductVariantOptionViewModel> ProductVariantOptions { get; set; } = [];
    public List<GoodsReceiptItemInputViewModel> Items { get; set; } = [];

    public bool IsPersisted => Id > 0;
    public bool IsLocked => Status is GoodsReceiptStatus.Approved or GoodsReceiptStatus.Cancelled;
    public decimal TotalAmount => Items
        .Where(item => !item.Remove)
        .Sum(item => Math.Max(0, item.Quantity ?? 0) * Math.Max(0, item.ImportPrice ?? 0m));
}

public sealed class GoodsReceiptItemInputViewModel
{
    public long? Id { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn biến thể.")]
    [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn biến thể.")]
    public long? ProductVariantId { get; set; }

    [Required(ErrorMessage = "Số lượng là bắt buộc.")]
    [Range(1, 999999, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int? Quantity { get; set; }

    [Required(ErrorMessage = "Giá nhập là bắt buộc.")]
    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Giá nhập không được âm.")]
    public decimal? ImportPrice { get; set; }

    public bool Remove { get; set; }
}

public sealed class InventoryFilterOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

public sealed class InventorySelectOption
{
    public long Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class InventoryProductVariantOptionViewModel
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }

    public string Text =>
        $"{Code} - {ProductName} ({BrandName}, {CategoryName}) - tồn {CurrentQuantity}";
}

public static class InventoryDisplay
{
    public const int LowStockThreshold = 10;

    public static string GetGoodsReceiptStatusLabel(GoodsReceiptStatus status) => status switch
    {
        GoodsReceiptStatus.Draft => "Bản nháp",
        GoodsReceiptStatus.Pending => "Chờ duyệt",
        GoodsReceiptStatus.Approved => "Đã duyệt",
        GoodsReceiptStatus.Cancelled => "Đã hủy",
        _ => "Không xác định",
    };

    public static string GetGoodsReceiptStatusClass(GoodsReceiptStatus status) => status switch
    {
        GoodsReceiptStatus.Draft => "is-draft",
        GoodsReceiptStatus.Pending => "is-pending",
        GoodsReceiptStatus.Approved => "is-approved",
        GoodsReceiptStatus.Cancelled => "is-cancelled",
        _ => "is-muted",
    };

    public static string GetStockLabel(int quantity) => quantity switch
    {
        <= 0 => "Hết hàng",
        <= LowStockThreshold => "Sắp hết",
        _ => "Ổn định",
    };

    public static string GetStockClass(int quantity) => quantity switch
    {
        <= 0 => "is-out",
        <= LowStockThreshold => "is-low",
        _ => "is-ok",
    };
}
