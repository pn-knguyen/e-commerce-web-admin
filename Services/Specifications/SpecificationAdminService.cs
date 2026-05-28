using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.ViewModels.Specifications;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Specifications;

public sealed class SpecificationAdminService : ISpecificationAdminService
{
    private const int DefaultPageSize = 30;
    private readonly ApplicationDbContext _db;

    public SpecificationAdminService(ApplicationDbContext db) => _db = db;

    // ── Index ──────────────────────────────────────────────────────────────

    public async Task<SpecificationIndexViewModel> GetIndexAsync(
        SpecificationIndexQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);

        var dbQuery = _db.Specifications
            .Include(s => s.CategorySpecifications)
            .Include(s => s.ProductSpecifications)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(s => s.Key.Contains(term) || s.Name.Contains(term));
        }

        var all = await dbQuery.OrderBy(s => s.Name).ToListAsync(ct);
        var pageItems = all.Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToList();

        var rows = pageItems.Select(s => new SpecificationRowViewModel
        {
            Id = s.Id,
            Key = s.Key,
            Name = s.Name,
            Unit = s.Unit,
            Icon = s.Icon,
            CategoryCount = s.CategorySpecifications.Count,
            ProductCount = s.ProductSpecifications.Count,
        }).ToList();

        return new SpecificationIndexViewModel
        {
            Specifications = rows,
            Search = query.Search,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = all.Count,
            TotalCategoryUsageCount = all.Sum(s => s.CategorySpecifications.Count),
            TotalProductUsageCount = all.Sum(s => s.ProductSpecifications.Count),
        };
    }

    // ── Create ─────────────────────────────────────────────────────────────

    public Task<SpecificationFormViewModel> GetCreateFormAsync(CancellationToken ct = default)
        => Task.FromResult(new SpecificationFormViewModel());

    public async Task<SpecSaveResult> CreateAsync(SpecificationFormViewModel form, CancellationToken ct = default)
    {
        NormalizeForm(form);
        var errors = await ValidateAsync(form, existingId: null, ct);
        if (errors.Count > 0) return SpecSaveResult.Failed(form, errors);

        var entity = new Specification
        {
            Key = form.Key,
            Name = form.Name,
            Unit = form.Unit,
            Icon = form.Icon,
        };

        _db.Specifications.Add(entity);
        await _db.SaveChangesAsync(ct);
        form.Id = entity.Id;
        return SpecSaveResult.Success(form, $"Đã tạo thông số \"{entity.Name}\" thành công.");
    }

    // ── Edit ───────────────────────────────────────────────────────────────

    public async Task<SpecificationFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Specifications.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (entity is null) return null;

        return new SpecificationFormViewModel
        {
            Id = entity.Id,
            Key = entity.Key,
            Name = entity.Name,
            Unit = entity.Unit,
            Icon = entity.Icon,
        };
    }

    public async Task<SpecSaveResult> UpdateAsync(long id, SpecificationFormViewModel form, CancellationToken ct = default)
    {
        NormalizeForm(form);

        var entity = await _db.Specifications.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (entity is null)
            return SpecSaveResult.Failed(form,
                new[] { new SpecValidationError(string.Empty, "Không tìm thấy thông số.") });

        var errors = await ValidateAsync(form, existingId: id, ct);
        if (errors.Count > 0) return SpecSaveResult.Failed(form, errors);

        entity.Key = form.Key;
        entity.Name = form.Name;
        entity.Unit = form.Unit;
        entity.Icon = form.Icon;

        await _db.SaveChangesAsync(ct);
        return SpecSaveResult.Success(form, $"Đã cập nhật thông số \"{entity.Name}\" thành công.");
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    public async Task<SpecDeleteResult> DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Specifications
            .Include(s => s.CategorySpecifications)
            .Include(s => s.ProductSpecifications)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (entity is null) return SpecDeleteResult.NotFound();

        if (entity.CategorySpecifications.Count > 0)
            return SpecDeleteResult.Failed(
                $"Không thể xoá \"{entity.Name}\" vì đang được gán cho {entity.CategorySpecifications.Count} danh mục.");

        if (entity.ProductSpecifications.Count > 0)
            return SpecDeleteResult.Failed(
                $"Không thể xoá \"{entity.Name}\" vì đang được dùng bởi {entity.ProductSpecifications.Count} sản phẩm.");

        _db.Specifications.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return SpecDeleteResult.Success($"Đã xoá thông số \"{entity.Name}\" thành công.");
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<List<SpecValidationError>> ValidateAsync(
        SpecificationFormViewModel form, long? existingId, CancellationToken ct)
    {
        var errors = new List<SpecValidationError>();

        if (await _db.Specifications.AnyAsync(
                s => s.Key == form.Key && (!existingId.HasValue || s.Id != existingId.Value), ct))
        {
            errors.Add(new SpecValidationError(nameof(form.Key), $"Key \"{form.Key}\" đã tồn tại."));
        }

        return errors;
    }

    private static void NormalizeForm(SpecificationFormViewModel form)
    {
        form.Key = form.Key.Trim().ToLowerInvariant().Replace(' ', '_');
        form.Name = form.Name.Trim();
        form.Unit = string.IsNullOrWhiteSpace(form.Unit) ? null : form.Unit.Trim();
        form.Icon = string.IsNullOrWhiteSpace(form.Icon) ? null : form.Icon.Trim();
    }
}
