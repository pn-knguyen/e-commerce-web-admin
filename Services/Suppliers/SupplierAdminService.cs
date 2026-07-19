using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.ViewModels.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Suppliers;

public sealed class SupplierAdminService : ISupplierAdminService
{
    private const int DefaultPageSize = 30;

    private readonly ApplicationDbContext _db;

    public SupplierAdminService(ApplicationDbContext db) => _db = db;

    public async Task<SupplierIndexViewModel> GetIndexAsync(
        SupplierIndexQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var dbQuery = _db.Suppliers.AsNoTracking();

        if (query.Status == "active")
        {
            dbQuery = dbQuery.Where(supplier => supplier.IsActive);
        }
        else if (query.Status == "inactive")
        {
            dbQuery = dbQuery.Where(supplier => !supplier.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(supplier =>
                supplier.Name.Contains(term) ||
                (supplier.Phone != null && supplier.Phone.Contains(term)) ||
                (supplier.Email != null && supplier.Email.Contains(term)) ||
                (supplier.Address != null && supplier.Address.Contains(term)));
        }

        var summary = await dbQuery
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                ActiveCount = group.Count(supplier => supplier.IsActive),
                InactiveCount = group.Count(supplier => !supplier.IsActive),
                TotalGoodsReceiptCount = group.Sum(supplier => supplier.GoodsReceipts.Count),
            })
            .FirstOrDefaultAsync(ct);

        var rows = await dbQuery
            .OrderBy(supplier => supplier.Name)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(supplier => new SupplierRowViewModel
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Phone = supplier.Phone,
                Email = supplier.Email,
                Address = supplier.Address,
                IsActive = supplier.IsActive,
                GoodsReceiptCount = supplier.GoodsReceipts.Count,
                CreatedAt = supplier.CreatedAt,
            })
            .ToListAsync(ct);

        return new SupplierIndexViewModel
        {
            Suppliers = rows,
            Search = query.Search,
            Status = query.Status,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = summary?.TotalCount ?? 0,
            ActiveCount = summary?.ActiveCount ?? 0,
            InactiveCount = summary?.InactiveCount ?? 0,
            TotalGoodsReceiptCount = summary?.TotalGoodsReceiptCount ?? 0,
        };
    }

    public Task<SupplierFormViewModel> GetCreateFormAsync(CancellationToken ct = default)
        => Task.FromResult(new SupplierFormViewModel { IsActive = true });

    public async Task<SupplierFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(supplier => supplier.Id == id, ct);

        if (entity is null)
        {
            return null;
        }

        return new SupplierFormViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Phone = entity.Phone,
            Email = entity.Email,
            Address = entity.Address,
            IsActive = entity.IsActive,
        };
    }

    public async Task<SupplierSaveResult> CreateAsync(
        SupplierFormViewModel form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);

        var errors = await ValidateFormAsync(form, existingId: null, ct);
        if (errors.Count > 0)
        {
            return SupplierSaveResult.Failed(form, errors);
        }

        var entity = new Supplier
        {
            Name = form.Name,
            Phone = form.Phone,
            Email = form.Email,
            Address = form.Address,
            IsActive = form.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Suppliers.Add(entity);
        await _db.SaveChangesAsync(ct);

        form.Id = entity.Id;
        return SupplierSaveResult.Success(
            form,
            $"Đã tạo nhà cung cấp \"{entity.Name}\" thành công.");
    }

    public async Task<SupplierSaveResult> UpdateAsync(
        long id,
        SupplierFormViewModel form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);

        var entity = await _db.Suppliers.FirstOrDefaultAsync(supplier => supplier.Id == id, ct);
        if (entity is null)
        {
            return SupplierSaveResult.Failed(
                form,
                [new SupplierValidationError(string.Empty, "Không tìm thấy nhà cung cấp.")]);
        }

        var errors = await ValidateFormAsync(form, existingId: id, ct);
        if (errors.Count > 0)
        {
            return SupplierSaveResult.Failed(form, errors);
        }

        entity.Name = form.Name;
        entity.Phone = form.Phone;
        entity.Email = form.Email;
        entity.Address = form.Address;
        entity.IsActive = form.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return SupplierSaveResult.Success(
            form,
            $"Đã cập nhật nhà cung cấp \"{entity.Name}\" thành công.");
    }

    public async Task<SupplierDeleteCheckResult> CheckDeleteAsync(
        long id,
        CancellationToken ct = default)
    {
        var supplier = await _db.Suppliers
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Name,
                GoodsReceiptCount = item.GoodsReceipts.Count,
            })
            .FirstOrDefaultAsync(ct);

        if (supplier is null)
        {
            return SupplierDeleteCheckResult.NotFound();
        }

        var blockers = BuildDeleteBlockers(supplier.GoodsReceiptCount);
        return blockers.Count == 0
            ? SupplierDeleteCheckResult.Allowed(supplier.Name)
            : SupplierDeleteCheckResult.Blocked(supplier.Name, blockers);
    }

    public async Task<SupplierDeleteResult> DeleteAsync(long id, CancellationToken ct = default)
    {
        var deleteCheck = await CheckDeleteAsync(id, ct);
        if (!deleteCheck.Found)
        {
            return SupplierDeleteResult.NotFound();
        }

        if (!deleteCheck.CanDelete)
        {
            return SupplierDeleteResult.Failed(deleteCheck.Message);
        }

        var entity = await _db.Suppliers.FirstOrDefaultAsync(supplier => supplier.Id == id, ct);
        if (entity is null)
        {
            return SupplierDeleteResult.NotFound();
        }

        _db.Suppliers.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return SupplierDeleteResult.Success(
            $"Đã xóa nhà cung cấp \"{entity.Name}\" thành công.");
    }

    public async Task<SupplierToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Suppliers.FirstOrDefaultAsync(supplier => supplier.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new SupplierToggleResult(entity.IsActive);
    }

    private async Task<List<SupplierValidationError>> ValidateFormAsync(
        SupplierFormViewModel form,
        long? existingId,
        CancellationToken ct)
    {
        var errors = new List<SupplierValidationError>();

        if (await _db.Suppliers.AnyAsync(
                supplier => supplier.Name == form.Name && (!existingId.HasValue || supplier.Id != existingId.Value),
                ct))
        {
            errors.Add(new SupplierValidationError(
                nameof(form.Name),
                $"Tên nhà cung cấp \"{form.Name}\" đã tồn tại."));
        }

        if (string.IsNullOrWhiteSpace(form.Phone))
        {
            errors.Add(new SupplierValidationError(
                nameof(form.Phone),
                "Số điện thoại là bắt buộc."));
        }
        else if (form.Phone.Length != 10 || form.Phone.Any(character => !char.IsDigit(character)))
        {
            errors.Add(new SupplierValidationError(
                nameof(form.Phone),
                "Số điện thoại phải gồm đúng 10 chữ số."));
        }

        if (string.IsNullOrWhiteSpace(form.Address))
        {
            errors.Add(new SupplierValidationError(
                nameof(form.Address),
                "Địa chỉ là bắt buộc."));
        }

        return errors;
    }

    private static List<string> BuildDeleteBlockers(int goodsReceiptCount)
    {
        var blockers = new List<string>();
        if (goodsReceiptCount > 0)
        {
            blockers.Add($"{goodsReceiptCount} phiếu nhập");
        }

        return blockers;
    }

    private static void NormalizeForm(SupplierFormViewModel form)
    {
        form.Name = form.Name.Trim();
        form.Phone = string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim();
        form.Email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim();
        form.Address = string.IsNullOrWhiteSpace(form.Address) ? null : form.Address.Trim();
    }
}
