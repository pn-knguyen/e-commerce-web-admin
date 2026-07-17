using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Inventory;

public sealed class InventoryLedgerService : IInventoryLedgerService
{
    private const string GoodsReceiptItemReference = "GoodsReceiptItem";
    private const string OrderItemReference = "OrderItem";

    private readonly ApplicationDbContext _db;

    public InventoryLedgerService(ApplicationDbContext db)
        => _db = db;

    public async Task<long?> ResolveDefaultLocationIdAsync(CancellationToken ct = default)
        => await _db.FulfillmentLocations
            .AsNoTracking()
            .Where(location => location.IsActive)
            .OrderByDescending(location => location.IsDefault)
            .ThenBy(location => location.Name)
            .Select(location => (long?)location.Id)
            .FirstOrDefaultAsync(ct);

    public async Task ApplyReceiptApprovalAsync(
        GoodsReceipt receipt,
        long? fulfillmentLocationId,
        DateTime now,
        CancellationToken ct = default)
    {
        var locationId = fulfillmentLocationId ?? receipt.FulfillmentLocationId ?? await ResolveDefaultLocationIdAsync(ct);
        receipt.FulfillmentLocationId = locationId;

        foreach (var group in receipt.GoodReceiptItems.GroupBy(item => item.ProductVariantId))
        {
            var variant = group.First().ProductVariant!;
            var incomingQuantity = group.Sum(item => item.Quantity);
            var incomingValue = group.Sum(item => item.Quantity * item.ImportPrice);

            var balance = await GetOrCreateBalanceAsync(variant.Id, locationId, now, ct);
            balance.AverageCost = CalculateWeightedAverage(
                balance.OnHandQuantity,
                balance.AverageCost,
                incomingQuantity,
                incomingValue);
            balance.OnHandQuantity += incomingQuantity;
            balance.UpdatedAt = now;

            variant.AverageCost = CalculateWeightedAverage(
                variant.Quantity,
                variant.AverageCost,
                incomingQuantity,
                incomingValue);
            variant.Quantity += incomingQuantity;
            variant.UpdatedAt = now;
        }

        foreach (var item in receipt.GoodReceiptItems.OrderBy(item => item.Id))
        {
            var lot = new InventoryStockLot
            {
                ProductVariantId = item.ProductVariantId,
                FulfillmentLocationId = locationId,
                GoodReceiptItemId = item.Id,
                LotCode = BuildLotCode(receipt.ReceiptCode, item.Id),
                ReceivedQuantity = item.Quantity,
                RemainingQuantity = item.Quantity,
                UnitCost = item.ImportPrice,
                ReceivedAt = now,
                CreatedAt = now,
            };

            _db.InventoryStockLots.Add(lot);
            _db.InventoryMovements.Add(new InventoryMovement
            {
                ProductVariantId = item.ProductVariantId,
                FulfillmentLocationId = locationId,
                StockLot = lot,
                Type = InventoryMovementType.Receipt,
                QuantityDelta = item.Quantity,
                ReservedQuantityDelta = 0,
                UnitCost = item.ImportPrice,
                TotalCost = item.Quantity * item.ImportPrice,
                ReferenceType = GoodsReceiptItemReference,
                ReferenceId = item.Id,
                Note = $"Goods receipt {receipt.ReceiptCode}",
                OccurredAt = now,
                CreatedAt = now,
            });
        }
    }

    public Task ApplyOrderSaleAsync(Order order, CancellationToken ct = default)
        => ApplyOrderSaleAsync(order, null, ct);

    public async Task ApplyOrderSaleAsync(
        Order order,
        long? fulfillmentLocationId,
        CancellationToken ct = default)
    {
        if (order.OrderStatus != OrderStatus.Completed)
        {
            return;
        }

        await EnsureOrderInventoryLoadedAsync(order, ct);

        var now = DateTime.UtcNow;
        foreach (var orderItem in order.OrderItems.Where(item =>
            item.Quantity > 0 &&
            item.ProductVariant is not null &&
            item.UnitCost <= 0 &&
            item.CostAllocations.Count == 0))
        {
            var remainingQuantity = orderItem.Quantity;
            var totalCost = 0m;

            var lots = await _db.InventoryStockLots
                .Where(lot =>
                    lot.ProductVariantId == orderItem.ProductVariantId &&
                    lot.RemainingQuantity > 0)
                .OrderBy(lot =>
                    fulfillmentLocationId.HasValue &&
                    lot.FulfillmentLocationId == fulfillmentLocationId.Value
                        ? 0
                        : 1)
                .ThenBy(lot => lot.ReceivedAt)
                .ThenBy(lot => lot.Id)
                .ToListAsync(ct);

            foreach (var lot in lots)
            {
                if (remainingQuantity <= 0)
                {
                    break;
                }

                var allocatedQuantity = Math.Min(remainingQuantity, lot.RemainingQuantity);
                lot.RemainingQuantity -= allocatedQuantity;
                lot.UpdatedAt = now;

                var balance = await GetOrCreateBalanceAsync(
                    lot.ProductVariantId,
                    lot.FulfillmentLocationId,
                    now,
                    ct);
                balance.OnHandQuantity = Math.Max(0, balance.OnHandQuantity - allocatedQuantity);
                balance.UpdatedAt = now;

                orderItem.CostAllocations.Add(new OrderItemCostAllocation
                {
                    StockLotId = lot.Id,
                    Quantity = allocatedQuantity,
                    UnitCost = lot.UnitCost,
                    CreatedAt = now,
                });

                _db.InventoryMovements.Add(new InventoryMovement
                {
                    ProductVariantId = lot.ProductVariantId,
                    FulfillmentLocationId = lot.FulfillmentLocationId,
                    StockLotId = lot.Id,
                    Type = InventoryMovementType.Sale,
                    QuantityDelta = -allocatedQuantity,
                    ReservedQuantityDelta = 0,
                    UnitCost = lot.UnitCost,
                    TotalCost = allocatedQuantity * lot.UnitCost,
                    ReferenceType = OrderItemReference,
                    ReferenceId = orderItem.Id,
                    Note = $"Order {order.OrderCode} completed",
                    OccurredAt = now,
                    CreatedAt = now,
                });

                totalCost += allocatedQuantity * lot.UnitCost;
                remainingQuantity -= allocatedQuantity;
            }

            if (remainingQuantity > 0)
            {
                totalCost += remainingQuantity * orderItem.ProductVariant!.AverageCost;
            }

            orderItem.UnitCost = Math.Round(totalCost / orderItem.Quantity, 2, MidpointRounding.AwayFromZero);
        }
    }

    public async Task ApplyOrderStatusChangeAsync(
        Order order,
        OrderStatus from,
        OrderStatus to,
        CancellationToken ct = default)
    {
        if (from == to || to is not (OrderStatus.Cancelled or OrderStatus.Returned))
        {
            return;
        }

        await EnsureOrderInventoryLoadedAsync(order, ct);

        var now = DateTime.UtcNow;
        var items = order.OrderItems
            .Where(item => item.ProductVariant is not null)
            .GroupBy(item => item.ProductVariantId)
            .Select(group => new
            {
                Variant = group.First().ProductVariant!,
                Quantity = group.Sum(item => item.Quantity),
            })
            .ToList();

        foreach (var item in items)
        {
            item.Variant.Quantity += item.Quantity;
            item.Variant.SoldCount = Math.Max(0, item.Variant.SoldCount - item.Quantity);
            item.Variant.UpdatedAt = now;

            if (item.Variant.Product is not null)
            {
                item.Variant.Product.TotalSoldCount = Math.Max(0, item.Variant.Product.TotalSoldCount - item.Quantity);
                item.Variant.Product.UpdatedAt = now;
            }
        }

        foreach (var orderItem in order.OrderItems)
        {
            foreach (var allocation in orderItem.CostAllocations)
            {
                if (allocation.StockLot is null)
                {
                    continue;
                }

                allocation.StockLot.RemainingQuantity += allocation.Quantity;
                allocation.StockLot.UpdatedAt = now;

                var balance = await GetOrCreateBalanceAsync(
                    allocation.StockLot.ProductVariantId,
                    allocation.StockLot.FulfillmentLocationId,
                    now,
                    ct);
                balance.AverageCost = CalculateWeightedAverage(
                    balance.OnHandQuantity,
                    balance.AverageCost,
                    allocation.Quantity,
                    allocation.Quantity * allocation.UnitCost);
                balance.OnHandQuantity += allocation.Quantity;
                balance.UpdatedAt = now;

                _db.InventoryMovements.Add(new InventoryMovement
                {
                    ProductVariantId = allocation.StockLot.ProductVariantId,
                    FulfillmentLocationId = allocation.StockLot.FulfillmentLocationId,
                    StockLotId = allocation.StockLotId,
                    Type = InventoryMovementType.Return,
                    QuantityDelta = allocation.Quantity,
                    ReservedQuantityDelta = 0,
                    UnitCost = allocation.UnitCost,
                    TotalCost = allocation.Quantity * allocation.UnitCost,
                    ReferenceType = OrderItemReference,
                    ReferenceId = orderItem.Id,
                    Note = $"Order {order.OrderCode} moved to {to}",
                    OccurredAt = now,
                    CreatedAt = now,
                });
            }
        }
    }

    private async Task EnsureOrderInventoryLoadedAsync(Order order, CancellationToken ct)
    {
        await _db.Entry(order).Collection(item => item.OrderItems).Query()
            .Include(item => item.ProductVariant)
                .ThenInclude(variant => variant!.Product)
            .Include(item => item.CostAllocations)
                .ThenInclude(allocation => allocation.StockLot)
            .LoadAsync(ct);
    }

    private async Task<InventoryBalance> GetOrCreateBalanceAsync(
        long productVariantId,
        long? fulfillmentLocationId,
        DateTime now,
        CancellationToken ct)
    {
        var balance = await _db.InventoryBalances.FirstOrDefaultAsync(item =>
            item.ProductVariantId == productVariantId &&
            item.FulfillmentLocationId == fulfillmentLocationId,
            ct);

        if (balance is not null)
        {
            return balance;
        }

        balance = new InventoryBalance
        {
            ProductVariantId = productVariantId,
            FulfillmentLocationId = fulfillmentLocationId,
            OnHandQuantity = 0,
            ReservedQuantity = 0,
            AverageCost = 0m,
            UpdatedAt = now,
        };

        _db.InventoryBalances.Add(balance);
        return balance;
    }

    private static decimal CalculateWeightedAverage(
        int oldQuantity,
        decimal oldAverageCost,
        int incomingQuantity,
        decimal incomingValue)
    {
        var nextQuantity = oldQuantity + incomingQuantity;
        if (nextQuantity <= 0)
        {
            return 0m;
        }

        var oldValue = oldQuantity * oldAverageCost;
        return Math.Round((oldValue + incomingValue) / nextQuantity, 2, MidpointRounding.AwayFromZero);
    }

    private static string BuildLotCode(string receiptCode, long receiptItemId)
        => $"{receiptCode}-{receiptItemId}".Length <= 80
            ? $"{receiptCode}-{receiptItemId}"
            : $"{receiptCode[..Math.Min(receiptCode.Length, 60)]}-{receiptItemId}";
}
