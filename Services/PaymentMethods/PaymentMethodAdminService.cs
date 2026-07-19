using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.ViewModels.PaymentMethods;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.PaymentMethods;

public sealed class PaymentMethodAdminService : IPaymentMethodAdminService
{
    private const int DefaultPageSize = 30;

    private readonly ApplicationDbContext _db;

    public PaymentMethodAdminService(ApplicationDbContext db) => _db = db;

    public async Task<PaymentMethodIndexViewModel> GetIndexAsync(
        PaymentMethodIndexQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var dbQuery = _db.PaymentMethods.AsNoTracking();

        if (query.Status == "active")
        {
            dbQuery = dbQuery.Where(method => method.IsActive);
        }
        else if (query.Status == "inactive")
        {
            dbQuery = dbQuery.Where(method => !method.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(method =>
                method.Name.Contains(term) ||
                (method.Description != null && method.Description.Contains(term)));
        }

        var summary = await dbQuery
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                ActiveCount = group.Count(method => method.IsActive),
                InactiveCount = group.Count(method => !method.IsActive),
                TotalOrderUsageCount = group.Sum(method => method.Orders.Count),
            })
            .FirstOrDefaultAsync(ct);

        var rows = await dbQuery
            .OrderBy(method => method.Name)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(method => new PaymentMethodRowViewModel
            {
                Id = method.Id,
                Name = method.Name,
                Description = method.Description,
                IsActive = method.IsActive,
                OrderCount = method.Orders.Count,
            })
            .ToListAsync(ct);

        return new PaymentMethodIndexViewModel
        {
            PaymentMethods = rows,
            Search = query.Search,
            Status = query.Status,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = summary?.TotalCount ?? 0,
            ActiveCount = summary?.ActiveCount ?? 0,
            InactiveCount = summary?.InactiveCount ?? 0,
            TotalOrderUsageCount = summary?.TotalOrderUsageCount ?? 0,
        };
    }

    public Task<PaymentMethodFormViewModel> GetCreateFormAsync(CancellationToken ct = default)
        => Task.FromResult(new PaymentMethodFormViewModel { IsActive = true });

    public async Task<PaymentMethodFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.PaymentMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(method => method.Id == id, ct);

        if (entity is null)
        {
            return null;
        }

        return new PaymentMethodFormViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
        };
    }

    public async Task<PaymentMethodSaveResult> CreateAsync(
        PaymentMethodFormViewModel form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);

        var errors = await ValidateFormAsync(form, existingId: null, ct);
        if (errors.Count > 0)
        {
            return PaymentMethodSaveResult.Failed(form, errors);
        }

        var entity = new PaymentMethod
        {
            Name = form.Name,
            Description = form.Description,
            IsActive = form.IsActive,
        };

        _db.PaymentMethods.Add(entity);
        await _db.SaveChangesAsync(ct);

        form.Id = entity.Id;
        return PaymentMethodSaveResult.Success(
            form,
            $"Đã tạo phương thức thanh toán \"{entity.Name}\" thành công.");
    }

    public async Task<PaymentMethodSaveResult> UpdateAsync(
        long id,
        PaymentMethodFormViewModel form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);

        var entity = await _db.PaymentMethods.FirstOrDefaultAsync(method => method.Id == id, ct);
        if (entity is null)
        {
            return PaymentMethodSaveResult.Failed(
                form,
                [new PaymentMethodValidationError(string.Empty, "Không tìm thấy phương thức thanh toán.")]);
        }

        var errors = await ValidateFormAsync(form, existingId: id, ct);
        if (errors.Count > 0)
        {
            return PaymentMethodSaveResult.Failed(form, errors);
        }

        entity.Name = form.Name;
        entity.Description = form.Description;
        entity.IsActive = form.IsActive;

        await _db.SaveChangesAsync(ct);
        return PaymentMethodSaveResult.Success(
            form,
            $"Đã cập nhật phương thức thanh toán \"{entity.Name}\" thành công.");
    }

    public async Task<PaymentMethodDeleteCheckResult> CheckDeleteAsync(
        long id,
        CancellationToken ct = default)
    {
        var method = await _db.PaymentMethods
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Name,
                OrderCount = item.Orders.Count,
            })
            .FirstOrDefaultAsync(ct);

        if (method is null)
        {
            return PaymentMethodDeleteCheckResult.NotFound();
        }

        var blockers = BuildDeleteBlockers(method.OrderCount);
        return blockers.Count == 0
            ? PaymentMethodDeleteCheckResult.Allowed(method.Name)
            : PaymentMethodDeleteCheckResult.Blocked(method.Name, blockers);
    }

    public async Task<PaymentMethodDeleteResult> DeleteAsync(long id, CancellationToken ct = default)
    {
        var deleteCheck = await CheckDeleteAsync(id, ct);
        if (!deleteCheck.Found)
        {
            return PaymentMethodDeleteResult.NotFound();
        }

        if (!deleteCheck.CanDelete)
        {
            return PaymentMethodDeleteResult.Failed(deleteCheck.Message);
        }

        var entity = await _db.PaymentMethods.FirstOrDefaultAsync(method => method.Id == id, ct);
        if (entity is null)
        {
            return PaymentMethodDeleteResult.NotFound();
        }

        _db.PaymentMethods.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return PaymentMethodDeleteResult.Success(
            $"Đã xóa phương thức thanh toán \"{entity.Name}\" thành công.");
    }

    public async Task<PaymentMethodToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.PaymentMethods.FirstOrDefaultAsync(method => method.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.IsActive = !entity.IsActive;
        await _db.SaveChangesAsync(ct);

        return new PaymentMethodToggleResult(entity.IsActive);
    }

    private async Task<List<PaymentMethodValidationError>> ValidateFormAsync(
        PaymentMethodFormViewModel form,
        long? existingId,
        CancellationToken ct)
    {
        var errors = new List<PaymentMethodValidationError>();

        if (await _db.PaymentMethods.AnyAsync(
                method => method.Name == form.Name && (!existingId.HasValue || method.Id != existingId.Value),
                ct))
        {
            errors.Add(new PaymentMethodValidationError(
                nameof(form.Name),
                $"Tên phương thức thanh toán \"{form.Name}\" đã tồn tại."));
        }

        return errors;
    }

    private static List<string> BuildDeleteBlockers(int orderCount)
    {
        var blockers = new List<string>();
        if (orderCount > 0)
        {
            blockers.Add($"{orderCount} đơn hàng");
        }

        return blockers;
    }

    private static void NormalizeForm(PaymentMethodFormViewModel form)
    {
        form.Name = form.Name.Trim();
        form.Description = string.IsNullOrWhiteSpace(form.Description)
            ? null
            : form.Description.Trim();
    }
}
