using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Orders;

public static class OrderFifoCostHelper
{
    public static async Task<FifoCostResult> ApplyStatusChangeAsync(
        ApplicationDbContext db,
        Order order,
        OrderStatus from,
        OrderStatus to,
        CancellationToken ct = default)
    {
        if (from == to)
        {
            return FifoCostResult.Success();
        }

        if (to == OrderStatus.Completed)
        {
            return await AllocateCompletedOrderAsync(db, order, ct);
        }

        if (to is OrderStatus.Cancelled or OrderStatus.Returned)
        {
            await ReleaseOrderAllocationsAsync(db, order, ct);
            OrderInventoryHelper.ApplyInventoryChange(order, from, to);
        }

        return FifoCostResult.Success();
    }

    private static async Task<FifoCostResult> AllocateCompletedOrderAsync(
        ApplicationDbContext db,
        Order order,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var orderItemIds = order.OrderItems.Select(item => item.Id).ToArray();
        var existingAllocatedOrderItemIds = await db.OrderItemCostAllocations
            .AsNoTracking()
            .Where(item => orderItemIds.Contains(item.OrderItemId) && item.ReleasedAt == null)
            .Select(item => item.OrderItemId)
            .Distinct()
            .ToListAsync(ct);
        var allocatedSet = existingAllocatedOrderItemIds.ToHashSet();

        foreach (var item in order.OrderItems.OrderBy(item => item.Id))
        {
            if (allocatedSet.Contains(item.Id))
            {
                continue;
            }

            var requiredQuantity = item.Quantity;
            var batches = await db.InventoryBatches
                .Where(batch =>
                    batch.ProductVariantId == item.ProductVariantId &&
                    batch.QuantityRemaining > 0)
                .OrderBy(batch => batch.ReceivedAt)
                .ThenBy(batch => batch.Id)
                .ToListAsync(ct);

            var availableQuantity = batches.Sum(batch => batch.QuantityRemaining);
            if (availableQuantity < requiredQuantity)
            {
                return FifoCostResult.Failed(
                    $"SKU {item.ProductVariant?.Code ?? item.ProductVariantId.ToString()} không đủ lớp tồn FIFO để ghi nhận giá vốn. Cần {requiredQuantity}, còn {availableQuantity}.");
            }

            foreach (var batch in batches)
            {
                if (requiredQuantity == 0)
                {
                    break;
                }

                var allocatedQuantity = Math.Min(requiredQuantity, batch.QuantityRemaining);
                batch.QuantityRemaining -= allocatedQuantity;
                batch.UpdatedAt = now;
                db.OrderItemCostAllocations.Add(new OrderItemCostAllocation
                {
                    OrderItemId = item.Id,
                    InventoryBatchId = batch.Id,
                    Quantity = allocatedQuantity,
                    UnitCost = batch.UnitCost,
                    CreatedAt = now,
                });
                requiredQuantity -= allocatedQuantity;
            }
        }

        return FifoCostResult.Success();
    }

    private static async Task ReleaseOrderAllocationsAsync(
        ApplicationDbContext db,
        Order order,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var orderItemIds = order.OrderItems.Select(item => item.Id).ToArray();
        var allocations = await db.OrderItemCostAllocations
            .Include(item => item.InventoryBatch)
            .Where(item => orderItemIds.Contains(item.OrderItemId) && item.ReleasedAt == null)
            .ToListAsync(ct);

        foreach (var allocation in allocations)
        {
            allocation.ReleasedAt = now;
            if (allocation.InventoryBatch is not null)
            {
                allocation.InventoryBatch.QuantityRemaining += allocation.Quantity;
                allocation.InventoryBatch.UpdatedAt = now;
            }
        }
    }
}

public sealed class FifoCostResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static FifoCostResult Success() => new() { Succeeded = true };

    public static FifoCostResult Failed(string message) => new()
    {
        Succeeded = false,
        ErrorMessage = message,
    };
}
