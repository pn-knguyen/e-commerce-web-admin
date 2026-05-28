using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Services.Uploads;
using e_commerce_web_admin.ViewModels.Categories;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Categories;

public sealed class CategoryAdminService : ICategoryAdminService
{
    private const int DefaultPageSize = 50;
    private const string CategoryImageFolder = "categories";
    private readonly ApplicationDbContext _db;
    private readonly IImageUploadService _imageUploadService;

    public CategoryAdminService(ApplicationDbContext db, IImageUploadService imageUploadService)
    {
        _db = db;
        _imageUploadService = imageUploadService;
    }

    public async Task<CategoryIndexViewModel> GetIndexAsync(
        CategoryIndexQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var allQuery = _db.Categories
            .Include(category => category.Parent)
            .Include(category => category.Children)
            .Include(category => category.Products)
            .AsSplitQuery()
            .AsNoTracking();

        if (query.Status == "active")
        {
            allQuery = allQuery.Where(category => category.IsActive);
        }
        else if (query.Status == "inactive")
        {
            allQuery = allQuery.Where(category => !category.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            allQuery = allQuery.Where(category => category.Name.Contains(search) || category.Slug.Contains(search));
        }

        var allCategories = await allQuery.ToListAsync(cancellationToken);
        var ordered = BuildTreeOrder(allCategories);
        var productCountByCategoryId = BuildDescendantProductCounts(allCategories);
        var totalCount = ordered.Count;
        var pageItems = ordered
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToList();

        var rows = pageItems.Select(entry => new CategoryRowViewModel
        {
            Id = entry.Category.Id,
            Name = entry.Category.Name,
            Slug = entry.Category.Slug,
            ImagePath = entry.Category.ImagePath,
            ParentId = entry.Category.ParentId,
            ParentName = entry.Category.Parent?.Name ?? string.Empty,
            Position = entry.Category.Position,
            IsActive = entry.Category.IsActive,
            ProductCount = productCountByCategoryId.GetValueOrDefault(entry.Category.Id),
            ChildCount = entry.Category.Children.Count,
            Depth = entry.Depth,
            CreatedAt = entry.Category.CreatedAt,
        }).ToList();

        return new CategoryIndexViewModel
        {
            Categories = rows,
            Search = query.Search,
            Status = query.Status,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            ActiveCount = ordered.Count(entry => entry.Category.IsActive),
            InactiveCount = ordered.Count(entry => !entry.Category.IsActive),
            TotalProductCount = allCategories.Sum(category => category.Products.Count),
        };
    }

    public async Task<CategoryFormViewModel> GetCreateFormAsync(CancellationToken cancellationToken = default)
    {
        return new CategoryFormViewModel
        {
            IsActive = true,
            Position = await _db.Categories.CountAsync(cancellationToken) + 1,
            ParentOptions = await BuildParentOptionsAsync(excludeId: null, cancellationToken),
        };
    }

    public async Task<CategoryFormViewModel?> GetEditFormAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return new CategoryFormViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            ParentId = entity.ParentId,
            Description = entity.Description,
            ImagePath = entity.ImagePath,
            Position = entity.Position,
            IsActive = entity.IsActive,
            ParentOptions = await BuildParentOptionsAsync(excludeId: id, cancellationToken),
        };
    }

    public async Task<CategoryFormViewModel> PrepareFormAsync(
        CategoryFormViewModel form,
        long? excludeId,
        CancellationToken cancellationToken = default)
    {
        form.ParentOptions = await BuildParentOptionsAsync(excludeId, cancellationToken);
        return form;
    }

    public async Task<CategorySaveResult> CreateAsync(
        CategoryFormViewModel form,
        CancellationToken cancellationToken = default)
    {
        NormalizeForm(form);

        var errors = await ValidateFormAsync(form, existingId: null, cancellationToken);
        if (errors.Count > 0)
        {
            return CategorySaveResult.Failed(
                await PrepareFormAsync(form, excludeId: null, cancellationToken),
                errors);
        }

        var uploadError = await UploadImageIfNeededAsync(form, cancellationToken);
        if (uploadError is not null)
        {
            return CategorySaveResult.Failed(
                await PrepareFormAsync(form, excludeId: null, cancellationToken),
                new[] { uploadError });
        }

        var entity = new Category
        {
            Name = form.Name,
            Slug = form.Slug,
            ParentId = form.ParentId,
            Description = form.Description,
            ImagePath = form.ImagePath,
            Position = form.Position,
            IsActive = form.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Categories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        form.Id = entity.Id;

        return CategorySaveResult.Success(form, $"Đã tạo danh mục \"{entity.Name}\" thành công.");
    }

    public async Task<CategorySaveResult> UpdateAsync(
        long id,
        CategoryFormViewModel form,
        CancellationToken cancellationToken = default)
    {
        NormalizeForm(form);

        var entity = await _db.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (entity is null)
        {
            return CategorySaveResult.Failed(
                await PrepareFormAsync(form, excludeId: id, cancellationToken),
                new[] { new CategoryValidationError(string.Empty, "Không tìm thấy danh mục.") });
        }

        var errors = await ValidateFormAsync(form, existingId: id, cancellationToken);
        if (errors.Count > 0)
        {
            return CategorySaveResult.Failed(
                await PrepareFormAsync(form, excludeId: id, cancellationToken),
                errors);
        }

        var uploadError = await UploadImageIfNeededAsync(form, cancellationToken);
        if (uploadError is not null)
        {
            return CategorySaveResult.Failed(
                await PrepareFormAsync(form, excludeId: id, cancellationToken),
                new[] { uploadError });
        }

        entity.Name = form.Name;
        entity.Slug = form.Slug;
        entity.ParentId = form.ParentId;
        entity.Description = form.Description;
        entity.ImagePath = form.ImagePath;
        entity.Position = form.Position;
        entity.IsActive = form.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return CategorySaveResult.Success(form, $"Đã cập nhật danh mục \"{entity.Name}\" thành công.");
    }

    public async Task<CategoryDeleteResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Categories
            .Include(category => category.Children)
            .Include(category => category.Products)
            .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);

        if (entity is null)
        {
            return CategoryDeleteResult.NotFound();
        }

        if (entity.Children.Count > 0)
        {
            return CategoryDeleteResult.Failed(
                $"Không thể xoá \"{entity.Name}\" vì có {entity.Children.Count} danh mục con. Hãy xoá hoặc chuyển danh mục con trước.");
        }

        if (entity.Products.Count > 0)
        {
            return CategoryDeleteResult.Failed(
                $"Không thể xoá \"{entity.Name}\" vì có {entity.Products.Count} sản phẩm đang thuộc danh mục này.");
        }

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return CategoryDeleteResult.Success($"Đã xoá danh mục \"{entity.Name}\" thành công.");
    }

    public async Task<CategoryToggleResult?> ToggleActiveAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new CategoryToggleResult(entity.IsActive);
    }

    private async Task<List<CategoryValidationError>> ValidateFormAsync(
        CategoryFormViewModel form,
        long? existingId,
        CancellationToken cancellationToken)
    {
        var errors = new List<CategoryValidationError>();

        if (await _db.Categories.AnyAsync(category =>
                category.Slug == form.Slug && (!existingId.HasValue || category.Id != existingId.Value),
                cancellationToken))
        {
            errors.Add(new CategoryValidationError(nameof(form.Slug), "Slug đã tồn tại, hãy dùng slug khác."));
        }

        if (form.ParentId.HasValue)
        {
            if (existingId.HasValue && form.ParentId.Value == existingId.Value)
            {
                errors.Add(new CategoryValidationError(nameof(form.ParentId), "Không thể chọn chính mình làm danh mục cha."));
            }
            else if (!await _db.Categories.AnyAsync(category => category.Id == form.ParentId.Value, cancellationToken))
            {
                errors.Add(new CategoryValidationError(nameof(form.ParentId), "Danh mục cha không tồn tại."));
            }
            else if (existingId.HasValue && await IsDescendantAsync(existingId.Value, form.ParentId.Value, cancellationToken))
            {
                errors.Add(new CategoryValidationError(nameof(form.ParentId), "Không thể chọn danh mục con làm danh mục cha."));
            }
        }

        return errors;
    }

    private async Task<CategoryValidationError?> UploadImageIfNeededAsync(
        CategoryFormViewModel form,
        CancellationToken cancellationToken)
    {
        if (form.ImageFile is null || form.ImageFile.Length <= 0)
        {
            return null;
        }

        var uploadResult = await _imageUploadService.UploadAsync(
            form.ImageFile,
            CategoryImageFolder,
            cancellationToken);

        if (!uploadResult.Succeeded)
        {
            return new CategoryValidationError(
                nameof(form.ImageFile),
                uploadResult.ErrorMessage ?? "Không thể tải ảnh lên Cloudinary.");
        }

        form.ImagePath = uploadResult.SecureUrl;
        return null;
    }

    private async Task<List<CategorySelectItem>> BuildParentOptionsAsync(
        long? excludeId,
        CancellationToken cancellationToken)
    {
        var all = await _db.Categories
            .AsNoTracking()
            .OrderBy(category => category.ParentId == null ? 0 : 1)
            .ThenBy(category => category.ParentId)
            .ThenBy(category => category.Position)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);

        var result = new List<CategorySelectItem>();
        AppendParentOptions(all, parentId: null, depth: 0, excludeId, result);
        return result;
    }

    private async Task<bool> IsDescendantAsync(
        long ancestorId,
        long candidateId,
        CancellationToken cancellationToken)
    {
        var allChildren = await _db.Categories
            .AsNoTracking()
            .Select(category => new { category.Id, category.ParentId })
            .ToListAsync(cancellationToken);

        var visited = new HashSet<long>();
        var queue = new Queue<long>();
        queue.Enqueue(ancestorId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var child in allChildren.Where(category => category.ParentId == current))
            {
                if (child.Id == candidateId)
                {
                    return true;
                }

                queue.Enqueue(child.Id);
            }
        }

        return false;
    }

    private static IReadOnlyList<(Category Category, int Depth)> BuildTreeOrder(List<Category> categories)
    {
        var result = new List<(Category Category, int Depth)>();
        var categoryIds = categories.Select(category => category.Id).ToHashSet();
        var rootCategories = categories
            .Where(category => category.ParentId is null || !categoryIds.Contains(category.ParentId.Value))
            .OrderBy(category => category.Position)
            .ThenBy(category => category.Name);

        foreach (var category in rootCategories)
        {
            AppendTree(category, depth: 0, categories, result);
        }

        return result;
    }

    private static void AppendTree(
        Category category,
        int depth,
        List<Category> all,
        List<(Category Category, int Depth)> result)
    {
        result.Add((category, depth));

        var children = all
            .Where(child => child.ParentId == category.Id)
            .OrderBy(child => child.Position)
            .ThenBy(child => child.Name);

        foreach (var child in children)
        {
            AppendTree(child, depth + 1, all, result);
        }
    }

    private static Dictionary<long, int> BuildDescendantProductCounts(List<Category> categories)
    {
        var childrenByParentId = categories
            .Where(category => category.ParentId.HasValue)
            .GroupBy(category => category.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var result = new Dictionary<long, int>();

        foreach (var category in categories)
        {
            CountProducts(category, childrenByParentId, result);
        }

        return result;
    }

    private static int CountProducts(
        Category category,
        IReadOnlyDictionary<long, List<Category>> childrenByParentId,
        Dictionary<long, int> result)
    {
        if (result.TryGetValue(category.Id, out var cachedCount))
        {
            return cachedCount;
        }

        var count = category.Products.Count;
        if (childrenByParentId.TryGetValue(category.Id, out var children))
        {
            foreach (var child in children)
            {
                count += CountProducts(child, childrenByParentId, result);
            }
        }

        result[category.Id] = count;
        return count;
    }

    private static void AppendParentOptions(
        List<Category> all,
        long? parentId,
        int depth,
        long? excludeId,
        List<CategorySelectItem> result)
    {
        var children = all
            .Where(category => category.ParentId == parentId)
            .OrderBy(category => category.Position)
            .ThenBy(category => category.Name);

        foreach (var category in children)
        {
            if (category.Id == excludeId)
            {
                continue;
            }

            var prefix = depth == 0 ? string.Empty : $"{new string('-', depth * 2)} ";
            result.Add(new CategorySelectItem
            {
                Id = category.Id,
                Label = prefix + category.Name,
                Depth = depth,
            });

            AppendParentOptions(all, category.Id, depth + 1, excludeId, result);
        }
    }

    private static void NormalizeForm(CategoryFormViewModel form)
    {
        form.Name = form.Name.Trim();
        form.Slug = string.IsNullOrWhiteSpace(form.Slug)
            ? GenerateSlug(form.Name)
            : GenerateSlug(form.Slug);
        form.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        form.ImagePath = string.IsNullOrWhiteSpace(form.ImagePath) ? null : form.ImagePath.Trim();
    }

    private static string GenerateSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(capacity: normalized.Length);

        foreach (var character in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (character == '\u0111')
            {
                builder.Append('d');
            }
            else if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(character);
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                builder.Append('-');
            }
        }

        return Regex.Replace(builder.ToString(), "-+", "-").Trim('-');
    }
}
