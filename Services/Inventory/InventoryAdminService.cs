using System.Data;
using System.Security.Claims;
using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.ViewModels.Inventory;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Inventory;

public sealed class InventoryAdminService : IInventoryAdminService
{
    private const int StockPageSize = 20;
    private const int ReceiptPageSize = 12;
    private const decimal MaxReceiptAmount = 9999999999999999m;

    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IInventoryLedgerService _inventoryLedger;

    public InventoryAdminService(
        ApplicationDbContext db,
        IHttpContextAccessor httpContextAccessor,
        IInventoryLedgerService inventoryLedger)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _inventoryLedger = inventoryLedger;
    }

    private sealed record ReceiptItemCandidate(int Index, GoodsReceiptItemInputViewModel Item);

    public async Task<InventoryIndexViewModel> GetIndexAsync(
        InventoryIndexQuery query,
        CancellationToken ct = default)
    {
        var stockPage = Math.Max(1, query.StockPage);
        var receiptPage = Math.Max(1, query.ReceiptPage);

        var allVariants = _db.ProductVariants.AsNoTracking();
        var stockQuery = ApplyStockFilters(allVariants, query);
        var receiptQuery = ApplyReceiptFilters(_db.GoodsReceipts.AsNoTracking(), query);
        var normalizedReceiptStatus = NormalizeReceiptStatus(query.ReceiptStatus);

        var totalVariantCount = await allVariants.CountAsync(ct);
        var totalStockQuantity = await allVariants.SumAsync(variant => (int?)variant.Quantity, ct) ?? 0;
        var lowStockCount = await allVariants.CountAsync(
            variant => variant.Quantity > 0 && variant.Quantity <= InventoryDisplay.LowStockThreshold,
            ct);
        var outOfStockCount = await allVariants.CountAsync(variant => variant.Quantity <= 0, ct);
        var totalInventoryCost = await allVariants
            .SumAsync(variant => (decimal?)(variant.Quantity * variant.AverageCost), ct) ?? 0m;
        var pendingReceiptCount = await _db.GoodsReceipts
            .AsNoTracking()
            .CountAsync(receipt => receipt.Status == GoodsReceiptStatus.Pending, ct);

        var stockTotalCount = await stockQuery.CountAsync(ct);
        var receiptTotalCount = await receiptQuery.CountAsync(ct);

        var stockRows = await stockQuery
            .OrderBy(variant => variant.Quantity <= 0 ? 0 : variant.Quantity <= InventoryDisplay.LowStockThreshold ? 1 : 2)
            .ThenBy(variant => variant.Quantity)
            .ThenBy(variant => variant.Code)
            .Skip((stockPage - 1) * StockPageSize)
            .Take(StockPageSize)
            .Select(variant => new InventoryStockRowViewModel
            {
                VariantId = variant.Id,
                VariantCode = variant.Code,
                ProductName = variant.Product != null ? variant.Product.Name : "Không rõ sản phẩm",
                BrandName = variant.Product != null && variant.Product.Brand != null
                    ? variant.Product.Brand.Name
                    : "Không rõ thương hiệu",
                CategoryName = variant.Product != null && variant.Product.Category != null
                    ? variant.Product.Category.Name
                    : "Không rõ danh mục",
                Price = variant.Price,
                AverageCost = variant.AverageCost,
                Quantity = variant.Quantity,
                ReservedQuantity = variant.InventoryBalances.Sum(balance => (int?)balance.ReservedQuantity) ?? 0,
                SoldCount = variant.SoldCount,
                IsActive = variant.IsActive,
                LastReceiptAt = variant.GoodReceiptItems
                    .Where(item => item.GoodsReceipt != null &&
                        item.GoodsReceipt.Status == GoodsReceiptStatus.Approved)
                    .Max(item => (DateTime?)(item.GoodsReceipt!.UpdatedAt ?? item.GoodsReceipt.CreatedAt)),
            })
            .ToListAsync(ct);

        var receiptRows = await receiptQuery
            .OrderByDescending(receipt => receipt.CreatedAt)
            .ThenByDescending(receipt => receipt.Id)
            .Skip((receiptPage - 1) * ReceiptPageSize)
            .Take(ReceiptPageSize)
            .Select(receipt => new GoodsReceiptRowViewModel
            {
                Id = receipt.Id,
                ReceiptCode = receipt.ReceiptCode,
                SupplierName = receipt.Supplier != null ? receipt.Supplier.Name : "Không rõ nhà cung cấp",
                FulfillmentLocationName = receipt.FulfillmentLocation != null
                    ? receipt.FulfillmentLocation.Name
                    : "Kho mặc định",
                Status = receipt.Status,
                ItemCount = receipt.GoodReceiptItems.Count,
                TotalQuantity = receipt.GoodReceiptItems.Sum(item => item.Quantity),
                TotalAmount = receipt.TotalAmount,
                CreatedByName = receipt.CreatedByStaff != null ? receipt.CreatedByStaff.FullName : "Không rõ",
                CreatedAt = receipt.CreatedAt,
                UpdatedAt = receipt.UpdatedAt,
            })
            .ToListAsync(ct);

        return new InventoryIndexViewModel
        {
            StockRows = stockRows,
            Receipts = receiptRows,
            SupplierOptions = await BuildSupplierFilterOptionsAsync(query.SupplierId, ct),
            CategoryOptions = await BuildCategoryFilterOptionsAsync(query.CategoryId, ct),
            ReceiptStatusOptions = BuildReceiptStatusOptions(normalizedReceiptStatus),
            Search = query.Search,
            Stock = NormalizeStockFilter(query.Stock),
            ReceiptStatus = normalizedReceiptStatus,
            SupplierId = query.SupplierId,
            CategoryId = query.CategoryId,
            StockPage = stockPage,
            StockPageSize = StockPageSize,
            StockTotalCount = stockTotalCount,
            ReceiptPage = receiptPage,
            ReceiptPageSize = ReceiptPageSize,
            ReceiptTotalCount = receiptTotalCount,
            TotalVariantCount = totalVariantCount,
            TotalStockQuantity = totalStockQuantity,
            LowStockCount = lowStockCount,
            OutOfStockCount = outOfStockCount,
            PendingReceiptCount = pendingReceiptCount,
            TotalInventoryCost = totalInventoryCost,
        };
    }

    public async Task<GoodsReceiptDetailsViewModel?> GetDetailsAsync(
        long id,
        CancellationToken ct = default)
    {
        return await _db.GoodsReceipts
            .AsNoTracking()
            .Where(receipt => receipt.Id == id)
            .Select(receipt => new GoodsReceiptDetailsViewModel
            {
                Id = receipt.Id,
                ReceiptCode = receipt.ReceiptCode,
                SupplierName = receipt.Supplier != null ? receipt.Supplier.Name : "Không rõ nhà cung cấp",
                SupplierPhone = receipt.Supplier != null ? receipt.Supplier.Phone : null,
                SupplierEmail = receipt.Supplier != null ? receipt.Supplier.Email : null,
                FulfillmentLocationName = receipt.FulfillmentLocation != null
                    ? receipt.FulfillmentLocation.Name
                    : "Kho mặc định",
                Status = receipt.Status,
                TotalAmount = receipt.TotalAmount,
                CreatedByName = receipt.CreatedByStaff != null ? receipt.CreatedByStaff.FullName : "Không rõ",
                ApprovedByName = receipt.ApprovedByStaff != null ? receipt.ApprovedByStaff.FullName : null,
                CreatedAt = receipt.CreatedAt,
                UpdatedAt = receipt.UpdatedAt,
                Items = receipt.GoodReceiptItems
                    .OrderBy(item => item.Id)
                    .Select(item => new GoodsReceiptItemViewModel
                    {
                        Id = item.Id,
                        ProductVariantId = item.ProductVariantId,
                        VariantCode = item.ProductVariant != null ? item.ProductVariant.Code : "N/A",
                        ProductName = item.ProductVariant != null && item.ProductVariant.Product != null
                            ? item.ProductVariant.Product.Name
                            : "Không rõ sản phẩm",
                        BrandName = item.ProductVariant != null &&
                            item.ProductVariant.Product != null &&
                            item.ProductVariant.Product.Brand != null
                                ? item.ProductVariant.Product.Brand.Name
                                : "Không rõ thương hiệu",
                        CategoryName = item.ProductVariant != null &&
                            item.ProductVariant.Product != null &&
                            item.ProductVariant.Product.Category != null
                                ? item.ProductVariant.Product.Category.Name
                                : "Không rõ danh mục",
                        Quantity = item.Quantity,
                        ImportPrice = item.ImportPrice,
                        CurrentStock = item.ProductVariant != null ? item.ProductVariant.Quantity : 0,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<GoodsReceiptFormViewModel> GetCreateFormAsync(
        long? variantId = null,
        CancellationToken ct = default)
    {
        var form = new GoodsReceiptFormViewModel
        {
            ReceiptCode = await GenerateReceiptCodeAsync(ct),
            FulfillmentLocationId = await _inventoryLedger.ResolveDefaultLocationIdAsync(ct),
            Status = GoodsReceiptStatus.Draft,
            Items =
            [
                new GoodsReceiptItemInputViewModel
                {
                    ProductVariantId = variantId,
                    Quantity = variantId.HasValue ? 1 : null,
                    ImportPrice = 0m,
                },
            ],
        };

        return await PrepareFormAsync(form, ct);
    }

    public async Task<GoodsReceiptFormViewModel?> GetEditFormAsync(
        long id,
        CancellationToken ct = default)
    {
        var receipt = await _db.GoodsReceipts
            .AsNoTracking()
            .Include(item => item.GoodReceiptItems)
            .FirstOrDefaultAsync(receipt => receipt.Id == id, ct);

        if (receipt is null)
        {
            return null;
        }

        var form = new GoodsReceiptFormViewModel
        {
            Id = receipt.Id,
            SupplierId = receipt.SupplierId,
            FulfillmentLocationId = receipt.FulfillmentLocationId,
            ReceiptCode = receipt.ReceiptCode,
            Status = receipt.Status,
            Items = receipt.GoodReceiptItems
                .OrderBy(item => item.Id)
                .Select(item => new GoodsReceiptItemInputViewModel
                {
                    Id = item.Id,
                    ProductVariantId = item.ProductVariantId,
                    Quantity = item.Quantity,
                    ImportPrice = item.ImportPrice,
                })
                .ToList(),
        };

        return await PrepareFormAsync(form, ct);
    }

    public async Task<GoodsReceiptFormViewModel> PrepareFormAsync(
        GoodsReceiptFormViewModel form,
        CancellationToken ct = default)
    {
        form.SupplierOptions = await BuildSupplierOptionsAsync(form.SupplierId, ct);
        form.FulfillmentLocationId ??= await _inventoryLedger.ResolveDefaultLocationIdAsync(ct);
        form.FulfillmentLocationOptions = await BuildFulfillmentLocationOptionsAsync(form.FulfillmentLocationId, ct);
        form.ProductVariantOptions = await BuildProductVariantOptionsAsync(
            form.Items
                .Where(item => item.ProductVariantId.HasValue)
                .Select(item => item.ProductVariantId!.Value)
                .Distinct()
                .ToArray(),
            ct);

        if (form.Items.Count == 0)
        {
            form.Items.Add(new GoodsReceiptItemInputViewModel());
        }

        return form;
    }

    public async Task<GoodsReceiptSaveResult> CreateAsync(
        GoodsReceiptFormViewModel form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);
        form = await PrepareFormAsync(form, ct);

        var errors = await ValidateFormAsync(form, existingId: null, allowedItemIds: null, ct);
        if (errors.Count > 0)
        {
            return GoodsReceiptSaveResult.Failed(form, errors);
        }

        var operatorStaffId = await ResolveOperatorStaffIdAsync(ct);
        if (!operatorStaffId.HasValue)
        {
            return GoodsReceiptSaveResult.Failed(
                form,
                [new InventoryValidationError(string.Empty, "Không tìm thấy tài khoản nhân sự để tạo phiếu nhập.")]);
        }

        var now = DateTime.UtcNow;
        var selectedItems = GetPersistableItemCandidates(form)
            .Select(candidate => candidate.Item)
            .ToList();

        var entity = new GoodsReceipt
        {
            SupplierId = form.SupplierId!.Value,
            FulfillmentLocationId = form.FulfillmentLocationId,
            ReceiptCode = form.ReceiptCode,
            Status = GoodsReceiptStatus.Draft,
            CreatedBy = operatorStaffId.Value,
            CreatedAt = now,
            TotalAmount = CalculateTotal(selectedItems),
        };

        foreach (var item in selectedItems)
        {
            entity.GoodReceiptItems.Add(new GoodReceiptItem
            {
                ProductVariantId = item.ProductVariantId!.Value,
                Quantity = item.Quantity!.Value,
                ImportPrice = item.ImportPrice!.Value,
                CreatedAt = now,
            });
        }

        _db.GoodsReceipts.Add(entity);
        await _db.SaveChangesAsync(ct);

        form.Id = entity.Id;
        form.Status = entity.Status;
        return GoodsReceiptSaveResult.Success(
            form,
            $"Đã tạo phiếu nhập \"{entity.ReceiptCode}\" thành công.");
    }

    public async Task<GoodsReceiptSaveResult> UpdateAsync(
        long id,
        GoodsReceiptFormViewModel form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);

        var entity = await _db.GoodsReceipts
            .Include(receipt => receipt.GoodReceiptItems)
            .FirstOrDefaultAsync(receipt => receipt.Id == id, ct);

        if (entity is null)
        {
            return GoodsReceiptSaveResult.Failed(
                await PrepareFormAsync(form, ct),
                [new InventoryValidationError(string.Empty, "Không tìm thấy phiếu nhập.")]);
        }

        form.Id = entity.Id;
        form.Status = entity.Status;
        form = await PrepareFormAsync(form, ct);

        if (entity.Status is GoodsReceiptStatus.Approved or GoodsReceiptStatus.Cancelled)
        {
            return GoodsReceiptSaveResult.Failed(
                form,
                [new InventoryValidationError(string.Empty, "Phiếu nhập đã duyệt hoặc đã hủy không thể chỉnh sửa.")]);
        }

        var allowedItemIds = entity.GoodReceiptItems.Select(item => item.Id).ToHashSet();
        var errors = await ValidateFormAsync(form, existingId: id, allowedItemIds, ct);
        if (errors.Count > 0)
        {
            return GoodsReceiptSaveResult.Failed(form, errors);
        }

        var selectedItems = GetPersistableItemCandidates(form)
            .Select(candidate => candidate.Item)
            .ToList();

        entity.SupplierId = form.SupplierId!.Value;
        entity.FulfillmentLocationId = form.FulfillmentLocationId;
        entity.ReceiptCode = form.ReceiptCode;
        entity.TotalAmount = CalculateTotal(selectedItems);
        entity.UpdatedAt = DateTime.UtcNow;

        ApplyReceiptItems(entity, selectedItems);
        await _db.SaveChangesAsync(ct);

        return GoodsReceiptSaveResult.Success(
            form,
            $"Đã cập nhật phiếu nhập \"{entity.ReceiptCode}\" thành công.");
    }

    public async Task<GoodsReceiptActionResult> SubmitAsync(
        long id,
        CancellationToken ct = default)
    {
        var receipt = await _db.GoodsReceipts.FirstOrDefaultAsync(item => item.Id == id, ct);
        if (receipt is null)
        {
            return GoodsReceiptActionResult.NotFound();
        }

        if (receipt.Status != GoodsReceiptStatus.Draft)
        {
            return GoodsReceiptActionResult.Failed("Chỉ phiếu nháp mới có thể chuyển sang chờ duyệt.");
        }

        receipt.Status = GoodsReceiptStatus.Pending;
        receipt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return GoodsReceiptActionResult.Success($"Phiếu nhập \"{receipt.ReceiptCode}\" đã chuyển sang chờ duyệt.");
    }

    public async Task<GoodsReceiptActionResult> ApproveAsync(
        long id,
        CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var receipt = await _db.GoodsReceipts
                .Include(item => item.GoodReceiptItems)
                    .ThenInclude(item => item.ProductVariant)
                .FirstOrDefaultAsync(item => item.Id == id, ct);

            if (receipt is null)
            {
                return GoodsReceiptActionResult.NotFound();
            }

            if (receipt.Status != GoodsReceiptStatus.Pending)
            {
                return GoodsReceiptActionResult.Failed("Chỉ phiếu đang chờ duyệt mới có thể duyệt.");
            }

            if (receipt.GoodReceiptItems.Count == 0)
            {
                return GoodsReceiptActionResult.Failed("Phiếu nhập cần có ít nhất một dòng hàng trước khi duyệt.");
            }

            var operatorStaffId = await ResolveOperatorStaffIdAsync(ct);
            if (!operatorStaffId.HasValue)
            {
                return GoodsReceiptActionResult.Failed("Không tìm thấy tài khoản nhân sự để duyệt phiếu nhập.");
            }

            foreach (var group in receipt.GoodReceiptItems.GroupBy(item => item.ProductVariantId))
            {
                var variant = group.First().ProductVariant;
                if (variant is null)
                {
                    return GoodsReceiptActionResult.Failed("Phiếu nhập có biến thể không còn tồn tại.");
                }

                var incomingQuantity = group.Sum(item => item.Quantity);
                if (variant.Quantity > int.MaxValue - incomingQuantity)
                {
                    return GoodsReceiptActionResult.Failed(
                        $"Số tồn của SKU {variant.Code} vượt giới hạn hệ thống nếu duyệt phiếu này.");
                }
            }

            var now = DateTime.UtcNow;

            await _inventoryLedger.ApplyReceiptApprovalAsync(
                receipt,
                receipt.FulfillmentLocationId,
                now,
                ct);

            receipt.Status = GoodsReceiptStatus.Approved;
            receipt.ApprovedBy = operatorStaffId.Value;
            receipt.TotalAmount = receipt.GoodReceiptItems.Sum(item => item.Quantity * item.ImportPrice);
            receipt.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return GoodsReceiptActionResult.Success(
                $"Đã duyệt phiếu nhập \"{receipt.ReceiptCode}\" và cập nhật tồn kho.");
        });
    }

    public async Task<GoodsReceiptActionResult> CancelAsync(
        long id,
        CancellationToken ct = default)
    {
        var receipt = await _db.GoodsReceipts.FirstOrDefaultAsync(item => item.Id == id, ct);
        if (receipt is null)
        {
            return GoodsReceiptActionResult.NotFound();
        }

        if (receipt.Status == GoodsReceiptStatus.Approved)
        {
            return GoodsReceiptActionResult.Failed("Phiếu nhập đã duyệt không thể hủy vì tồn kho đã được cập nhật.");
        }

        if (receipt.Status == GoodsReceiptStatus.Cancelled)
        {
            return GoodsReceiptActionResult.Failed("Phiếu nhập này đã được hủy trước đó.");
        }

        receipt.Status = GoodsReceiptStatus.Cancelled;
        receipt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return GoodsReceiptActionResult.Success($"Đã hủy phiếu nhập \"{receipt.ReceiptCode}\".");
    }

    public async Task<GoodsReceiptActionResult> DeleteAsync(
        long id,
        CancellationToken ct = default)
    {
        var receipt = await _db.GoodsReceipts
            .Include(item => item.GoodReceiptItems)
            .FirstOrDefaultAsync(item => item.Id == id, ct);

        if (receipt is null)
        {
            return GoodsReceiptActionResult.NotFound();
        }

        if (receipt.Status is GoodsReceiptStatus.Approved or GoodsReceiptStatus.Cancelled)
        {
            return GoodsReceiptActionResult.Failed("Chỉ có thể xóa phiếu nháp hoặc phiếu chờ duyệt.");
        }

        _db.GoodReceiptItems.RemoveRange(receipt.GoodReceiptItems);
        _db.GoodsReceipts.Remove(receipt);
        await _db.SaveChangesAsync(ct);

        return GoodsReceiptActionResult.Success($"Đã xóa phiếu nhập \"{receipt.ReceiptCode}\".");
    }

    private static IQueryable<ProductVariant> ApplyStockFilters(
        IQueryable<ProductVariant> query,
        InventoryIndexQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            query = query.Where(variant =>
                variant.Code.Contains(term) ||
                (variant.Product != null &&
                    (variant.Product.Name.Contains(term) ||
                     (variant.Product.Brand != null && variant.Product.Brand.Name.Contains(term)) ||
                     (variant.Product.Category != null && variant.Product.Category.Name.Contains(term)))));
        }

        if (filters.CategoryId is > 0)
        {
            query = query.Where(variant =>
                variant.Product != null &&
                variant.Product.CategoryId == filters.CategoryId.Value);
        }

        query = NormalizeStockFilter(filters.Stock) switch
        {
            "out-of-stock" => query.Where(variant => variant.Quantity <= 0),
            "low-stock" => query.Where(variant =>
                variant.Quantity > 0 && variant.Quantity <= InventoryDisplay.LowStockThreshold),
            "in-stock" => query.Where(variant => variant.Quantity > 0),
            _ => query,
        };

        return query;
    }

    private static IQueryable<GoodsReceipt> ApplyReceiptFilters(
        IQueryable<GoodsReceipt> query,
        InventoryIndexQuery filters)
    {
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            query = query.Where(receipt =>
                receipt.ReceiptCode.Contains(term) ||
                (receipt.Supplier != null && receipt.Supplier.Name.Contains(term)) ||
                receipt.GoodReceiptItems.Any(item =>
                    item.ProductVariant != null &&
                    (item.ProductVariant.Code.Contains(term) ||
                     (item.ProductVariant.Product != null &&
                        item.ProductVariant.Product.Name.Contains(term)))));
        }

        if (filters.SupplierId is > 0)
        {
            query = query.Where(receipt => receipt.SupplierId == filters.SupplierId.Value);
        }

        if (TryParseReceiptStatus(filters.ReceiptStatus, out var status))
        {
            query = query.Where(receipt => receipt.Status == status);
        }

        return query;
    }

    private async Task<List<InventoryFilterOption>> BuildSupplierFilterOptionsAsync(
        long? selectedId,
        CancellationToken ct)
    {
        return await _db.Suppliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .Select(supplier => new InventoryFilterOption
            {
                Value = supplier.Id.ToString(),
                Text = supplier.Name,
                Selected = selectedId.HasValue && supplier.Id == selectedId.Value,
            })
            .ToListAsync(ct);
    }

    private async Task<List<InventoryFilterOption>> BuildCategoryFilterOptionsAsync(
        long? selectedId,
        CancellationToken ct)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(category => category.Products.Any(product => product.ProductVariants.Any()))
            .OrderBy(category => category.Name)
            .Select(category => new InventoryFilterOption
            {
                Value = category.Id.ToString(),
                Text = category.Name,
                Selected = selectedId.HasValue && category.Id == selectedId.Value,
            })
            .ToListAsync(ct);
    }

    private static List<InventoryFilterOption> BuildReceiptStatusOptions(string? selectedValue) =>
        Enum.GetValues<GoodsReceiptStatus>()
            .Select(status => new InventoryFilterOption
            {
                Value = status.ToString(),
                Text = InventoryDisplay.GetGoodsReceiptStatusLabel(status),
                Selected = string.Equals(selectedValue, status.ToString(), StringComparison.OrdinalIgnoreCase),
            })
            .ToList();

    private async Task<List<InventorySelectOption>> BuildSupplierOptionsAsync(
        long? selectedId,
        CancellationToken ct)
    {
        return await _db.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.IsActive || (selectedId.HasValue && supplier.Id == selectedId.Value))
            .OrderByDescending(supplier => supplier.IsActive)
            .ThenBy(supplier => supplier.Name)
            .Select(supplier => new InventorySelectOption
            {
                Id = supplier.Id,
                Text = supplier.IsActive ? supplier.Name : supplier.Name + " (đã tạm ngưng)",
                IsActive = supplier.IsActive,
            })
            .ToListAsync(ct);
    }

    private async Task<List<InventorySelectOption>> BuildFulfillmentLocationOptionsAsync(
        long? selectedId,
        CancellationToken ct)
    {
        return await _db.FulfillmentLocations
            .AsNoTracking()
            .Where(location => location.IsActive || (selectedId.HasValue && location.Id == selectedId.Value))
            .OrderByDescending(location => location.IsDefault)
            .ThenByDescending(location => location.IsActive)
            .ThenBy(location => location.Name)
            .Select(location => new InventorySelectOption
            {
                Id = location.Id,
                Text = location.IsDefault ? location.Name + " (mặc định)" : location.Name,
                IsActive = location.IsActive,
            })
            .ToListAsync(ct);
    }

    private async Task<List<InventoryProductVariantOptionViewModel>> BuildProductVariantOptionsAsync(
        IReadOnlyCollection<long> selectedIds,
        CancellationToken ct)
    {
        return await _db.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.IsActive || selectedIds.Contains(variant.Id))
            .OrderBy(variant => variant.Product != null ? variant.Product.Name : string.Empty)
            .ThenBy(variant => variant.Code)
            .Select(variant => new InventoryProductVariantOptionViewModel
            {
                Id = variant.Id,
                Code = variant.Code,
                ProductName = variant.Product != null ? variant.Product.Name : "Không rõ sản phẩm",
                BrandName = variant.Product != null && variant.Product.Brand != null
                    ? variant.Product.Brand.Name
                    : "Không rõ thương hiệu",
                CategoryName = variant.Product != null && variant.Product.Category != null
                    ? variant.Product.Category.Name
                    : "Không rõ danh mục",
                CurrentQuantity = variant.Quantity,
                Price = variant.Price,
                IsActive = variant.IsActive,
            })
            .ToListAsync(ct);
    }

    private async Task<List<InventoryValidationError>> ValidateFormAsync(
        GoodsReceiptFormViewModel form,
        long? existingId,
        IReadOnlySet<long>? allowedItemIds,
        CancellationToken ct)
    {
        var errors = new List<InventoryValidationError>();

        if (string.IsNullOrWhiteSpace(form.ReceiptCode))
        {
            errors.Add(new InventoryValidationError(nameof(form.ReceiptCode), "Mã phiếu nhập là bắt buộc."));
        }
        else if (form.ReceiptCode.Length > 50)
        {
            errors.Add(new InventoryValidationError(nameof(form.ReceiptCode), "Mã phiếu nhập tối đa 50 ký tự."));
        }
        else if (!IsValidReceiptCode(form.ReceiptCode))
        {
            errors.Add(new InventoryValidationError(
                nameof(form.ReceiptCode),
                "Mã phiếu chỉ gồm chữ in hoa, số, dấu gạch ngang hoặc gạch dưới."));
        }
        else if (await _db.GoodsReceipts.AnyAsync(
                     receipt => receipt.ReceiptCode == form.ReceiptCode &&
                         (!existingId.HasValue || receipt.Id != existingId.Value),
                     ct))
        {
            errors.Add(new InventoryValidationError(nameof(form.ReceiptCode), "Mã phiếu nhập đã tồn tại."));
        }

        if (!form.SupplierId.HasValue)
        {
            errors.Add(new InventoryValidationError(nameof(form.SupplierId), "Vui lòng chọn nhà cung cấp."));
        }
        else
        {
            var supplier = await _db.Suppliers
                .AsNoTracking()
                .Where(item => item.Id == form.SupplierId.Value)
                .Select(item => new { item.IsActive })
                .FirstOrDefaultAsync(ct);

            if (supplier is null)
            {
                errors.Add(new InventoryValidationError(nameof(form.SupplierId), "Nhà cung cấp không tồn tại."));
            }
            else if (!supplier.IsActive)
            {
                errors.Add(new InventoryValidationError(nameof(form.SupplierId), "Nhà cung cấp đang tạm ngưng."));
            }
        }

        if (form.FulfillmentLocationId.HasValue)
        {
            var location = await _db.FulfillmentLocations
                .AsNoTracking()
                .Where(item => item.Id == form.FulfillmentLocationId.Value)
                .Select(item => new { item.IsActive })
                .FirstOrDefaultAsync(ct);

            if (location is null)
            {
                errors.Add(new InventoryValidationError(
                    nameof(form.FulfillmentLocationId),
                    "Điểm nhập kho không tồn tại."));
            }
            else if (!location.IsActive)
            {
                errors.Add(new InventoryValidationError(
                    nameof(form.FulfillmentLocationId),
                    "Điểm nhập kho đang tạm ngưng."));
            }
        }

        var itemCandidates = GetPersistableItemCandidates(form);
        if (itemCandidates.Count == 0)
        {
            errors.Add(new InventoryValidationError(string.Empty, "Phiếu nhập cần ít nhất một dòng hàng."));
            return errors;
        }

        ValidateItemIds(itemCandidates, allowedItemIds, errors);
        ValidateDuplicateVariants(itemCandidates, errors);
        await ValidateItemVariantsAsync(itemCandidates, errors, ct);
        ValidateItemNumbers(itemCandidates, errors);

        var total = CalculateTotal(itemCandidates.Select(candidate => candidate.Item));
        if (total > MaxReceiptAmount)
        {
            errors.Add(new InventoryValidationError(string.Empty, "Tổng tiền phiếu nhập vượt giới hạn hệ thống."));
        }

        return errors;
    }

    private static void ValidateItemIds(
        IEnumerable<ReceiptItemCandidate> candidates,
        IReadOnlySet<long>? allowedItemIds,
        ICollection<InventoryValidationError> errors)
    {
        if (allowedItemIds is null)
        {
            return;
        }

        foreach (var candidate in candidates)
        {
            if (candidate.Item.Id.HasValue && !allowedItemIds.Contains(candidate.Item.Id.Value))
            {
                errors.Add(new InventoryValidationError(
                    $"{nameof(GoodsReceiptFormViewModel.Items)}[{candidate.Index}].{nameof(GoodsReceiptItemInputViewModel.Id)}",
                    "Dòng phiếu nhập không hợp lệ."));
            }
        }
    }

    private static void ValidateDuplicateVariants(
        IEnumerable<ReceiptItemCandidate> candidates,
        ICollection<InventoryValidationError> errors)
    {
        foreach (var group in candidates
                     .Where(candidate => candidate.Item.ProductVariantId.HasValue)
                     .GroupBy(candidate => candidate.Item.ProductVariantId!.Value)
                     .Where(group => group.Count() > 1))
        {
            foreach (var duplicate in group.Skip(1))
            {
                errors.Add(new InventoryValidationError(
                    $"{nameof(GoodsReceiptFormViewModel.Items)}[{duplicate.Index}].{nameof(GoodsReceiptItemInputViewModel.ProductVariantId)}",
                    "SKU này đã có trong phiếu nhập."));
            }
        }
    }

    private async Task ValidateItemVariantsAsync(
        IEnumerable<ReceiptItemCandidate> candidates,
        ICollection<InventoryValidationError> errors,
        CancellationToken ct)
    {
        var candidateList = candidates.ToList();
        var variantIds = candidateList
            .Where(candidate => candidate.Item.ProductVariantId.HasValue)
            .Select(candidate => candidate.Item.ProductVariantId!.Value)
            .Distinct()
            .ToArray();

        var variants = await _db.ProductVariants
            .AsNoTracking()
            .Where(variant => variantIds.Contains(variant.Id))
            .Select(variant => new { variant.Id, variant.IsActive })
            .ToDictionaryAsync(variant => variant.Id, ct);

        foreach (var candidate in candidateList)
        {
            var fieldName = $"{nameof(GoodsReceiptFormViewModel.Items)}[{candidate.Index}].{nameof(GoodsReceiptItemInputViewModel.ProductVariantId)}";
            if (!candidate.Item.ProductVariantId.HasValue)
            {
                errors.Add(new InventoryValidationError(fieldName, "Vui lòng chọn biến thể."));
                continue;
            }

            if (!variants.TryGetValue(candidate.Item.ProductVariantId.Value, out var variant))
            {
                errors.Add(new InventoryValidationError(fieldName, "Biến thể không tồn tại."));
            }
            else if (!variant.IsActive)
            {
                errors.Add(new InventoryValidationError(fieldName, "Biến thể đang tắt, không thể nhập thêm hàng."));
            }
        }
    }

    private static void ValidateItemNumbers(
        IEnumerable<ReceiptItemCandidate> candidates,
        ICollection<InventoryValidationError> errors)
    {
        foreach (var candidate in candidates)
        {
            if (!candidate.Item.Quantity.HasValue || candidate.Item.Quantity.Value <= 0)
            {
                errors.Add(new InventoryValidationError(
                    $"{nameof(GoodsReceiptFormViewModel.Items)}[{candidate.Index}].{nameof(GoodsReceiptItemInputViewModel.Quantity)}",
                    "Số lượng phải lớn hơn 0."));
            }

            if (!candidate.Item.ImportPrice.HasValue || candidate.Item.ImportPrice.Value < 0)
            {
                errors.Add(new InventoryValidationError(
                    $"{nameof(GoodsReceiptFormViewModel.Items)}[{candidate.Index}].{nameof(GoodsReceiptItemInputViewModel.ImportPrice)}",
                    "Giá nhập không được âm."));
            }
        }
    }

    private static List<ReceiptItemCandidate> GetPersistableItemCandidates(GoodsReceiptFormViewModel form) =>
        form.Items
            .Select((item, index) => new ReceiptItemCandidate(index, item))
            .Where(candidate => !candidate.Item.Remove)
            .Where(candidate => HasItemValue(candidate.Item))
            .ToList();

    private static bool HasItemValue(GoodsReceiptItemInputViewModel item) =>
        item.Id.HasValue ||
        item.ProductVariantId.HasValue ||
        item.Quantity.HasValue ||
        item.ImportPrice.HasValue;

    private static decimal CalculateTotal(IEnumerable<GoodsReceiptItemInputViewModel> items) =>
        items.Sum(item => (item.Quantity ?? 0) * (item.ImportPrice ?? 0m));

    private void ApplyReceiptItems(
        GoodsReceipt receipt,
        IReadOnlyCollection<GoodsReceiptItemInputViewModel> selectedItems)
    {
        var selectedById = selectedItems
            .Where(item => item.Id.HasValue)
            .ToDictionary(item => item.Id!.Value);
        var existingItems = receipt.GoodReceiptItems.ToList();
        var now = DateTime.UtcNow;

        foreach (var existing in existingItems)
        {
            if (!selectedById.TryGetValue(existing.Id, out var selected))
            {
                _db.GoodReceiptItems.Remove(existing);
                receipt.GoodReceiptItems.Remove(existing);
                continue;
            }

            existing.ProductVariantId = selected.ProductVariantId!.Value;
            existing.Quantity = selected.Quantity!.Value;
            existing.ImportPrice = selected.ImportPrice!.Value;
            existing.UpdatedAt = now;
        }

        foreach (var selected in selectedItems.Where(item => !item.Id.HasValue))
        {
            receipt.GoodReceiptItems.Add(new GoodReceiptItem
            {
                GoodsReceiptId = receipt.Id,
                ProductVariantId = selected.ProductVariantId!.Value,
                Quantity = selected.Quantity!.Value,
                ImportPrice = selected.ImportPrice!.Value,
                CreatedAt = now,
            });
        }
    }

    private async Task<string> GenerateReceiptCodeAsync(CancellationToken ct)
    {
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneHelper.GetVietnamTimeZone()).Date;
        var prefix = $"GR-{today:yyyyMMdd}-";
        var nextNumber = await _db.GoodsReceipts
            .AsNoTracking()
            .CountAsync(receipt => receipt.ReceiptCode.StartsWith(prefix), ct) + 1;

        string code;
        do
        {
            code = $"{prefix}{nextNumber:0000}";
            nextNumber += 1;
        }
        while (await _db.GoodsReceipts.AnyAsync(receipt => receipt.ReceiptCode == code, ct));

        return code;
    }

    private async Task<long?> ResolveOperatorStaffIdAsync(CancellationToken ct)
    {
        var claimValue = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(AppClaimTypes.UserId);

        if (long.TryParse(claimValue, out var currentStaffId) &&
            await _db.Staff.AnyAsync(staff => staff.Id == currentStaffId && staff.IsActive, ct))
        {
            return currentStaffId;
        }

        return null;
    }

    private static bool TryParseReceiptStatus(string? value, out GoodsReceiptStatus status) =>
        Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);

    private static string? NormalizeReceiptStatus(string? value) =>
        TryParseReceiptStatus(value, out var status) ? status.ToString() : null;

    private static string? NormalizeStockFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "in-stock" => "in-stock",
            "low-stock" => "low-stock",
            "out-of-stock" => "out-of-stock",
            _ => null,
        };
    }

    private static bool IsValidReceiptCode(string value) =>
        value.Length is >= 3 and <= 50 &&
        char.IsLetterOrDigit(value[0]) &&
        value.All(character =>
            char.IsUpper(character) ||
            char.IsDigit(character) ||
            character is '-' or '_');

    private static void NormalizeForm(GoodsReceiptFormViewModel form)
    {
        form.ReceiptCode = string.IsNullOrWhiteSpace(form.ReceiptCode)
            ? string.Empty
            : form.ReceiptCode.Trim().ToUpperInvariant();
    }

}
