using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Services.Categories;
using e_commerce_web_admin.ViewModels.CategoryVariantAttributes;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.CategoryVariantAttributes;

public sealed class CvaAdminService : ICvaAdminService
{
    private const int DefaultPageSize = 30;
    private readonly ApplicationDbContext _db;
    private readonly ICategoryHierarchyService _categoryHierarchy;

    public CvaAdminService(
        ApplicationDbContext db,
        ICategoryHierarchyService categoryHierarchy)
    {
        _db = db;
        _categoryHierarchy = categoryHierarchy;
    }

    // ── Index ──────────────────────────────────────────────────────────────

    public async Task<CvaIndexViewModel?> GetIndexAsync(
        long categoryId, CvaIndexQuery query, CancellationToken ct = default)
    {
        var category = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null) return null;

        var page = Math.Max(1, query.Page);

        var assignedQuery = _db.CategoryVariantAttributes
            .Where(cva =>
                cva.CategoryId == categoryId &&
                cva.Attribute!.Code != CatalogAttributeCodes.Color)
            .AsNoTracking();

        var assignedAttributeIdsQuery = assignedQuery.Select(cva => cva.AttributeId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            assignedQuery = assignedQuery.Where(cva =>
                cva.Attribute!.Name.Contains(term) ||
                cva.Attribute.Code.Contains(term));
        }

        var totalAssigned = await assignedQuery.CountAsync(ct);
        var categoryIds = await _categoryHierarchy.GetSelfAndDescendantIdsAsync(categoryId, ct);

        var pageItems = await assignedQuery
            .OrderBy(cva => cva.Attribute!.Name)
            .ThenBy(cva => cva.Attribute!.Code)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(cva => new CvaRowViewModel
            {
                AttributeId = cva.AttributeId,
                Code = cva.Attribute!.Code,
                Name = cva.Attribute.Name,
                OptionCount = cva.Attribute.AttributeOptions.Count(),
                VariantUsageCount = cva.Attribute.AttributeOptions
                    .SelectMany(o => o.VariantAttributes)
                    .Where(va => categoryIds.Contains(va.ProductVariant!.Product!.CategoryId))
                    .Count(),
                AssignedAt = cva.CreatedAt,
            })
            .ToListAsync(ct);

        // Attributes chưa gán (dành cho dropdown assign)
        var available = await _db.Attributes
            .Where(a =>
                a.Code != CatalogAttributeCodes.Color &&
                !assignedAttributeIdsQuery.Contains(a.Id))
            .OrderBy(a => a.Name)
            .Select(a => new CvaAvailableOption
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                OptionCount = a.AttributeOptions.Count(),
            })
            .AsNoTracking()
            .ToListAsync(ct);

        return new CvaIndexViewModel
        {
            CategoryId = categoryId,
            CategoryName = category.Name,
            CategorySlug = category.Slug,
            AssignedAttributes = pageItems,
            AvailableAttributes = available,
            Search = query.Search,
            Page = page,
            PageSize = DefaultPageSize,
            TotalAssigned = totalAssigned,
        };
    }

    // ── Assign ─────────────────────────────────────────────────────────────

    public async Task<CvaSaveResult> AssignAsync(
        CvaAssignViewModel form, CancellationToken ct = default)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == form.CategoryId, ct);
        if (!categoryExists)
            return new CvaSaveResult(false, "Không tìm thấy danh mục.");

        var attribute = await _db.Attributes.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == form.AttributeId, ct);
        if (attribute is null)
            return new CvaSaveResult(false, "Không tìm thấy thuộc tính.");

        if (attribute.Code == CatalogAttributeCodes.Color)
        {
            return new CvaSaveResult(
                false,
                "Màu sắc được quản lý trực tiếp trên biến thể sản phẩm.");
        }

        var alreadyAssigned = await _db.CategoryVariantAttributes.AnyAsync(
            cva => cva.CategoryId == form.CategoryId && cva.AttributeId == form.AttributeId, ct);

        if (alreadyAssigned)
            return new CvaSaveResult(false,
                $"Thuộc tính \"{attribute.Name}\" đã được gán cho danh mục này.");

        _db.CategoryVariantAttributes.Add(new CategoryVariantAttribute
        {
            CategoryId = form.CategoryId,
            AttributeId = form.AttributeId,
        });

        await _db.SaveChangesAsync(ct);
        return new CvaSaveResult(true, $"Đã gán thuộc tính \"{attribute.Name}\" thành công.");
    }

    // ── Remove ─────────────────────────────────────────────────────────────

    public async Task<CvaRemoveResult> RemoveAsync(
        long categoryId, long attributeId, CancellationToken ct = default)
    {
        var entity = await _db.CategoryVariantAttributes
            .Include(cva => cva.Attribute)
            .FirstOrDefaultAsync(
                cva => cva.CategoryId == categoryId && cva.AttributeId == attributeId, ct);

        if (entity is null) return CvaRemoveResult.NotFound();

        var categoryIds = await _categoryHierarchy.GetSelfAndDescendantIdsAsync(categoryId, ct);
        var inUse = await _db.VariantAttributes
            .AnyAsync(va =>
                va.AttributeOption!.AttributeId == attributeId &&
                categoryIds.Contains(va.ProductVariant!.Product!.CategoryId), ct);

        if (inUse)
            return CvaRemoveResult.Blocked(
                $"Không thể bỏ gán \"{entity.Attribute!.Name}\" vì đang được sử dụng bởi biến thể sản phẩm trong danh mục này.");

        _db.CategoryVariantAttributes.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return CvaRemoveResult.Ok($"Đã bỏ gán thuộc tính \"{entity.Attribute!.Name}\" thành công.");
    }
}
