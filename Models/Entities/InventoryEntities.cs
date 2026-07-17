using e_commerce_web_admin.Models.Enums;

namespace e_commerce_web_admin.Models.Entities;

public class Supplier
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
}

public class GoodsReceipt
{
    public long Id { get; set; }
    public long SupplierId { get; set; }
    public long? FulfillmentLocationId { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;
    public long CreatedBy { get; set; }
    public long? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Supplier? Supplier { get; set; }
    public FulfillmentLocation? FulfillmentLocation { get; set; }
    public Staff? CreatedByStaff { get; set; }
    public Staff? ApprovedByStaff { get; set; }
    public ICollection<GoodReceiptItem> GoodReceiptItems { get; set; } = new List<GoodReceiptItem>();
}

public class GoodReceiptItem
{
    public long Id { get; set; }
    public long GoodsReceiptId { get; set; }
    public long ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal ImportPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public GoodsReceipt? GoodsReceipt { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public ICollection<InventoryStockLot> InventoryStockLots { get; set; } = new List<InventoryStockLot>();
}

public class InventoryStockLot
{
    public long Id { get; set; }
    public long ProductVariantId { get; set; }
    public long? FulfillmentLocationId { get; set; }
    public long? GoodReceiptItemId { get; set; }
    public string LotCode { get; set; } = string.Empty;
    public int ReceivedQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ProductVariant? ProductVariant { get; set; }
    public FulfillmentLocation? FulfillmentLocation { get; set; }
    public GoodReceiptItem? GoodReceiptItem { get; set; }
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    public ICollection<OrderItemCostAllocation> OrderItemCostAllocations { get; set; } = new List<OrderItemCostAllocation>();
}

public class InventoryBalance
{
    public long Id { get; set; }
    public long ProductVariantId { get; set; }
    public long? FulfillmentLocationId { get; set; }
    public int OnHandQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public decimal AverageCost { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];

    public int AvailableQuantity => OnHandQuantity - ReservedQuantity;

    public ProductVariant? ProductVariant { get; set; }
    public FulfillmentLocation? FulfillmentLocation { get; set; }
}

public class InventoryMovement
{
    public long Id { get; set; }
    public long ProductVariantId { get; set; }
    public long? FulfillmentLocationId { get; set; }
    public long? StockLotId { get; set; }
    public InventoryMovementType Type { get; set; }
    public int QuantityDelta { get; set; }
    public int ReservedQuantityDelta { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public long? ReferenceId { get; set; }
    public string? Note { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ProductVariant? ProductVariant { get; set; }
    public FulfillmentLocation? FulfillmentLocation { get; set; }
    public InventoryStockLot? StockLot { get; set; }
}

public class OrderItemCostAllocation
{
    public long Id { get; set; }
    public long OrderItemId { get; set; }
    public long? StockLotId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OrderItem? OrderItem { get; set; }
    public InventoryStockLot? StockLot { get; set; }
}
