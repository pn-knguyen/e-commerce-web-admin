using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.ViewModels.CategorySpecifications;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.CategorySpecifications;

public sealed class CategorySpecAdminService : ICategorySpecAdminService
{
    private const int DefaultPageSize = 30;
    private readonly ApplicationDbContext _db;

    public CategorySpecAdminService(ApplicationDbContext db) => _db = db;

    // ── Index ──────────────────────────────────────────────────────────────

    public async Task<CategorySpecIndexViewModel?> GetIndexAsync(
        long categoryId, CategorySpecIndexQuery query, CancellationToken ct = default)
    {
        var category = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null) return null;

        var page = Math.Max(1, query.Page);

        // Specs đã gán
        var allCategoryAssignments = await _db.CategorySpecifications
            .Include(cs => cs.Specification)
            .Where(cs => cs.CategoryId == categoryId)
            .AsNoTracking()
            .ToListAsync(ct);

        var assignedQuery = allCategoryAssignments.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            assignedQuery = assignedQuery.Where(cs =>
                cs.Specification!.Name.Contains(term) ||
                cs.Specification!.Key.Contains(term));
        }

        var allAssigned = assignedQuery
            .OrderBy(cs => cs.SortOrder)
            .ThenBy(cs => cs.Specification!.Name)
            .ToList();

        // Đếm usage của từng spec trong category này (qua Product)
        var categoryProductIds = await _db.Products
            .Where(p => p.CategoryId == categoryId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var usageMap = await _db.ProductSpecifications
            .Where(ps => categoryProductIds.Contains(ps.ProductId))
            .GroupBy(ps => ps.SpecificationId)
            .Select(g => new { SpecId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SpecId, x => x.Count, ct);

        var pageItems = allAssigned
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToList();

        var rows = pageItems.Select(cs => new CategorySpecRowViewModel
        {
            SpecificationId = cs.SpecificationId,
            Key = cs.Specification!.Key,
            Name = cs.Specification.Name,
            Unit = cs.Specification.Unit,
            GroupName = cs.GroupName,
            IsRequired = cs.IsRequired,
            SortOrder = cs.SortOrder,
            ProductUsageCount = usageMap.GetValueOrDefault(cs.SpecificationId),
        }).ToList();

        // Specs chưa gán
        var assignedIds = allCategoryAssignments.Select(cs => cs.SpecificationId).ToHashSet();
        var available = await _db.Specifications.AsNoTracking()
            .Where(s => !assignedIds.Contains(s.Id))
            .OrderBy(s => s.Name)
            .Select(s => new AvailableSpecOption
            {
                Id = s.Id,
                Key = s.Key,
                Name = s.Name,
                Unit = s.Unit,
            })
            .ToListAsync(ct);

        return new CategorySpecIndexViewModel
        {
            CategoryId = categoryId,
            CategoryName = category.Name,
            CategorySlug = category.Slug,
            AssignedSpecs = rows,
            AvailableSpecs = available,
            AssignForm = new CategorySpecAssignViewModel { CategoryId = categoryId },
            Search = query.Search,
            Page = page,
            PageSize = DefaultPageSize,
            TotalAssigned = allAssigned.Count,
        };
    }

    // ── Assign (upsert) ────────────────────────────────────────────────────

    public async Task<CategorySpecSaveResult> AssignAsync(
        CategorySpecAssignViewModel form, CancellationToken ct = default)
    {
        var categoryExists = await _db.Categories.AsNoTracking()
            .AnyAsync(c => c.Id == form.CategoryId, ct);

        if (!categoryExists)
            return new CategorySpecSaveResult(false, "Không tìm thấy danh mục.");

        var spec = await _db.Specifications.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == form.SpecificationId, ct);

        if (spec is null)
            return new CategorySpecSaveResult(false, "Không tìm thấy thông số.");

        var existing = await _db.CategorySpecifications
            .FirstOrDefaultAsync(
                cs => cs.CategoryId == form.CategoryId && cs.SpecificationId == form.SpecificationId, ct);

        if (existing is not null)
            return new CategorySpecSaveResult(false, $"Thông số \"{spec.Name}\" đã được gán cho danh mục này.");

        _db.CategorySpecifications.Add(new CategorySpecification
        {
            CategoryId = form.CategoryId,
            SpecificationId = form.SpecificationId,
            GroupName = string.IsNullOrWhiteSpace(form.GroupName) ? null : form.GroupName.Trim(),
            IsRequired = form.IsRequired,
            SortOrder = form.SortOrder,
        });

        await _db.SaveChangesAsync(ct);
        return new CategorySpecSaveResult(true, $"Đã gán thông số \"{spec.Name}\" thành công.");
    }

    // ── Update ─────────────────────────────────────────────────────────────

    public async Task<CategorySpecSaveResult> UpdateAsync(
        CategorySpecUpdateViewModel form, CancellationToken ct = default)
    {
        var entity = await _db.CategorySpecifications
            .FirstOrDefaultAsync(
                cs => cs.CategoryId == form.CategoryId && cs.SpecificationId == form.SpecificationId, ct);

        if (entity is null)
            return new CategorySpecSaveResult(false, "Không tìm thấy liên kết thông số - danh mục.");

        entity.GroupName = string.IsNullOrWhiteSpace(form.GroupName) ? null : form.GroupName.Trim();
        entity.IsRequired = form.IsRequired;
        entity.SortOrder = form.SortOrder;

        await _db.SaveChangesAsync(ct);
        return new CategorySpecSaveResult(true, "Đã cập nhật thành công.");
    }

    // ── Remove ─────────────────────────────────────────────────────────────

    public async Task<CategorySpecRemoveResult> RemoveAsync(
        long categoryId, long specId, CancellationToken ct = default)
    {
        var entity = await _db.CategorySpecifications
            .Include(cs => cs.Specification)
            .FirstOrDefaultAsync(
                cs => cs.CategoryId == categoryId && cs.SpecificationId == specId, ct);

        if (entity is null)
            return new CategorySpecRemoveResult(false, false, "Không tìm thấy liên kết.");

        // Kiểm tra có sản phẩm trong danh mục này đang dùng spec không
        var inUse = await _db.ProductSpecifications.AnyAsync(
            ps => ps.SpecificationId == specId &&
                  _db.Products.Any(p => p.Id == ps.ProductId && p.CategoryId == categoryId), ct);

        if (inUse)
            return new CategorySpecRemoveResult(true, false,
                $"Không thể bỏ gán \"{entity.Specification!.Name}\" vì đang được dùng bởi sản phẩm trong danh mục.");

        _db.CategorySpecifications.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return new CategorySpecRemoveResult(true, true,
            $"Đã bỏ gán thông số \"{entity.Specification!.Name}\" thành công.");
    }
}
