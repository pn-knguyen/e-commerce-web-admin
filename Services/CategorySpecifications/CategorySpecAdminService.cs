using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Services.Categories;
using e_commerce_web_admin.ViewModels.CategorySpecifications;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.CategorySpecifications;

public sealed class CategorySpecAdminService : ICategorySpecAdminService
{
    private const int DefaultPageSize = 30;
    private const int MaxGroupNameLength = 120;
    private const int MaxSortOrder = 9999;

    private readonly ApplicationDbContext _db;
    private readonly ICategoryHierarchyService _categoryHierarchy;

    public CategorySpecAdminService(
        ApplicationDbContext db,
        ICategoryHierarchyService categoryHierarchy)
    {
        _db = db;
        _categoryHierarchy = categoryHierarchy;
    }

    public async Task<CategorySpecIndexViewModel?> GetIndexAsync(
        long categoryId,
        CategorySpecIndexQuery query,
        CancellationToken ct = default)
    {
        var category = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == categoryId, ct);

        if (category is null)
        {
            return null;
        }

        var page = Math.Max(1, query.Page);
        var assignments = await _db.CategorySpecifications
            .AsNoTracking()
            .Include(item => item.Specification)
            .Where(item => item.CategoryId == categoryId)
            .ToListAsync(ct);

        var assignedQuery = assignments.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            assignedQuery = assignedQuery.Where(item =>
                item.Specification!.Name.Contains(term) ||
                item.Specification.Key.Contains(term) ||
                (!string.IsNullOrWhiteSpace(item.GroupName) && item.GroupName.Contains(term)));
        }

        var assigned = assignedQuery
            .OrderBy(item => string.IsNullOrWhiteSpace(item.GroupName) ? 1 : 0)
            .ThenBy(item => item.GroupName)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Specification!.Name)
            .ToList();

        var categoryIds = await _categoryHierarchy.GetSelfAndDescendantIdsAsync(categoryId, ct);
        var categoryProductIds = await _db.Products
            .AsNoTracking()
            .Where(item => categoryIds.Contains(item.CategoryId))
            .Select(item => item.Id)
            .ToListAsync(ct);

        var usageMap = await _db.ProductSpecifications
            .AsNoTracking()
            .Where(item => categoryProductIds.Contains(item.ProductId))
            .GroupBy(item => item.SpecificationId)
            .Select(group => new { SpecificationId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.SpecificationId, item => item.Count, ct);

        var rows = assigned
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(item => new CategorySpecRowViewModel
            {
                SpecificationId = item.SpecificationId,
                Key = item.Specification!.Key,
                Name = item.Specification.Name,
                Unit = item.Specification.Unit,
                GroupName = item.GroupName,
                IsRequired = item.IsRequired,
                SortOrder = item.SortOrder,
                ProductUsageCount = usageMap.GetValueOrDefault(item.SpecificationId),
            })
            .ToList();

        var assignedIds = assignments.Select(item => item.SpecificationId).ToHashSet();
        var available = await _db.Specifications
            .AsNoTracking()
            .Where(item => !assignedIds.Contains(item.Id))
            .OrderBy(item => item.Name)
            .Select(item => new AvailableSpecOption
            {
                Id = item.Id,
                Key = item.Key,
                Name = item.Name,
                Unit = item.Unit,
            })
            .ToListAsync(ct);

        return new CategorySpecIndexViewModel
        {
            CategoryId = categoryId,
            CategoryName = category.Name,
            CategorySlug = category.Slug,
            AssignedSpecs = rows,
            AvailableSpecs = available,
            AssignForm = BuildAssignForm(categoryId, assignments, available),
            Search = query.Search,
            Page = page,
            PageSize = DefaultPageSize,
            TotalAssigned = assigned.Count,
        };
    }

    public async Task<CategorySpecSaveResult> AssignAsync(
        CategorySpecAssignViewModel form,
        CancellationToken ct = default)
    {
        var categoryExists = await _db.Categories
            .AsNoTracking()
            .AnyAsync(item => item.Id == form.CategoryId, ct);

        if (!categoryExists)
        {
            return new CategorySpecSaveResult(false, "Không tìm thấy danh mục.");
        }

        var selectedItems = NormalizeSelectedItems(form.Items);
        if (selectedItems.Count == 0)
        {
            return new CategorySpecSaveResult(false, "Vui lòng chọn ít nhất một thông số cần gán.");
        }

        var validationError = ValidateSelectedItems(selectedItems);
        if (validationError is not null)
        {
            return new CategorySpecSaveResult(false, validationError);
        }

        var selectedIds = selectedItems.Select(item => item.SpecificationId).ToList();
        if (selectedIds.Distinct().Count() != selectedIds.Count)
        {
            return new CategorySpecSaveResult(false, "Danh sách thông số được chọn bị trùng.");
        }

        var specs = await _db.Specifications
            .AsNoTracking()
            .Where(item => selectedIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, ct);

        if (specs.Count != selectedIds.Count)
        {
            return new CategorySpecSaveResult(false, "Một hoặc nhiều thông số không tồn tại.");
        }

        var existingIds = await _db.CategorySpecifications
            .AsNoTracking()
            .Where(item => item.CategoryId == form.CategoryId && selectedIds.Contains(item.SpecificationId))
            .Select(item => item.SpecificationId)
            .ToListAsync(ct);

        if (existingIds.Count > 0)
        {
            var existingNames = existingIds
                .Select(id => specs.TryGetValue(id, out var spec) ? spec.Name : id.ToString())
                .ToList();
            return new CategorySpecSaveResult(
                false,
                $"Thông số đã được gán trước đó: {string.Join(", ", existingNames)}.");
        }

        foreach (var item in selectedItems)
        {
            _db.CategorySpecifications.Add(new CategorySpecification
            {
                CategoryId = form.CategoryId,
                SpecificationId = item.SpecificationId,
                GroupName = item.GroupName,
                IsRequired = item.IsRequired,
                SortOrder = item.SortOrder,
            });
        }

        await _db.SaveChangesAsync(ct);

        if (selectedItems.Count == 1)
        {
            var specName = specs[selectedItems[0].SpecificationId].Name;
            return new CategorySpecSaveResult(true, $"Đã gán thông số \"{specName}\" thành công.");
        }

        return new CategorySpecSaveResult(true, $"Đã gán {selectedItems.Count} thông số thành công.");
    }

    public async Task<CategorySpecSaveResult> UpdateAsync(
        CategorySpecUpdateViewModel form,
        CancellationToken ct = default)
    {
        var entity = await _db.CategorySpecifications
            .FirstOrDefaultAsync(
                item => item.CategoryId == form.CategoryId && item.SpecificationId == form.SpecificationId,
                ct);

        if (entity is null)
        {
            return new CategorySpecSaveResult(false, "Không tìm thấy liên kết thông số - danh mục.");
        }

        entity.GroupName = string.IsNullOrWhiteSpace(form.GroupName) ? null : form.GroupName.Trim();
        entity.IsRequired = form.IsRequired;
        entity.SortOrder = form.SortOrder;

        await _db.SaveChangesAsync(ct);
        return new CategorySpecSaveResult(true, "Đã cập nhật thành công.");
    }

    public async Task<CategorySpecRemoveResult> RemoveAsync(
        long categoryId,
        long specId,
        CancellationToken ct = default)
    {
        var entity = await _db.CategorySpecifications
            .Include(item => item.Specification)
            .FirstOrDefaultAsync(
                item => item.CategoryId == categoryId && item.SpecificationId == specId,
                ct);

        if (entity is null)
        {
            return new CategorySpecRemoveResult(false, false, "Không tìm thấy liên kết.");
        }

        var categoryIds = await _categoryHierarchy.GetSelfAndDescendantIdsAsync(categoryId, ct);
        var inUse = await _db.ProductSpecifications.AnyAsync(
            productSpec => productSpec.SpecificationId == specId &&
                           _db.Products.Any(product => product.Id == productSpec.ProductId && categoryIds.Contains(product.CategoryId)),
            ct);

        if (inUse)
        {
            return new CategorySpecRemoveResult(
                true,
                false,
                $"Không thể bỏ gán \"{entity.Specification!.Name}\" vì đang được dùng bởi sản phẩm trong danh mục.");
        }

        _db.CategorySpecifications.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return new CategorySpecRemoveResult(
            true,
            true,
            $"Đã bỏ gán thông số \"{entity.Specification!.Name}\" thành công.");
    }

    private static CategorySpecAssignViewModel BuildAssignForm(
        long categoryId,
        IReadOnlyCollection<CategorySpecification> assigned,
        IReadOnlyList<AvailableSpecOption> available)
    {
        var nextSortOrder = assigned.Count == 0 ? 1 : assigned.Max(item => item.SortOrder) + 1;
        return new CategorySpecAssignViewModel
        {
            CategoryId = categoryId,
            Items = available.Select((item, index) => new CategorySpecAssignItemViewModel
            {
                SpecificationId = item.Id,
                SortOrder = Math.Min(nextSortOrder + index, MaxSortOrder),
            }).ToList(),
        };
    }

    private static List<CategorySpecAssignItemViewModel> NormalizeSelectedItems(
        IEnumerable<CategorySpecAssignItemViewModel> items)
    {
        return items
            .Where(item => item.Selected)
            .Select(item => new CategorySpecAssignItemViewModel
            {
                SpecificationId = item.SpecificationId,
                Selected = true,
                GroupName = string.IsNullOrWhiteSpace(item.GroupName) ? null : item.GroupName.Trim(),
                IsRequired = item.IsRequired,
                SortOrder = item.SortOrder,
            })
            .ToList();
    }

    private static string? ValidateSelectedItems(IEnumerable<CategorySpecAssignItemViewModel> items)
    {
        foreach (var item in items)
        {
            if (item.SpecificationId <= 0)
            {
                return "Thông số được chọn không hợp lệ.";
            }

            if (item.GroupName?.Length > MaxGroupNameLength)
            {
                return "Tên nhóm tối đa 120 ký tự.";
            }

            if (item.SortOrder is < 0 or > MaxSortOrder)
            {
                return "Thứ tự phải từ 0 đến 9999.";
            }
        }

        return null;
    }
}
