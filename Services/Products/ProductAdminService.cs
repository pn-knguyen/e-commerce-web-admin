using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Services.Categories;
using e_commerce_web_admin.ViewModels.Products;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Products;

public sealed class ProductAdminService : IProductAdminService
{
    private const int DefaultPageSize = 30;

    private readonly ApplicationDbContext _db;
    private readonly ICategoryHierarchyService _categoryHierarchy;

    public ProductAdminService(
        ApplicationDbContext db,
        ICategoryHierarchyService categoryHierarchy)
    {
        _db = db;
        _categoryHierarchy = categoryHierarchy;
    }

    private sealed class ProductIndexItem
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string BrandName { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public int VariantCount { get; init; }
        public int ViewsCount { get; init; }
        public int TotalSoldCount { get; init; }
        public decimal RatingAverage { get; init; }
        public int RatingCount { get; init; }
        public bool IsActive { get; init; }
        public bool IsFeatured { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    public async Task<ProductIndexViewModel> GetIndexAsync(
        ProductIndexQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var dbQuery = _db.Products.AsNoTracking();

        if (query.Status == "active")
        {
            dbQuery = dbQuery.Where(product => product.IsActive);
        }
        else if (query.Status == "inactive")
        {
            dbQuery = dbQuery.Where(product => !product.IsActive);
        }

        if (query.Featured == "featured")
        {
            dbQuery = dbQuery.Where(product => product.IsFeatured);
        }
        else if (query.Featured == "normal")
        {
            dbQuery = dbQuery.Where(product => !product.IsFeatured);
        }

        if (query.BrandId.HasValue)
        {
            dbQuery = dbQuery.Where(product => product.BrandId == query.BrandId.Value);
        }

        if (query.CategoryId.HasValue)
        {
            var categoryIds = await _categoryHierarchy.GetSelfAndDescendantIdsAsync(
                query.CategoryId.Value,
                ct);
            dbQuery = categoryIds.Count == 0
                ? dbQuery.Where(product => false)
                : dbQuery.Where(product => categoryIds.Contains(product.CategoryId));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(product =>
                product.Name.Contains(term) ||
                product.Slug.Contains(term) ||
                product.Brand!.Name.Contains(term) ||
                product.Category!.Name.Contains(term));
        }

        var totalCount = await dbQuery.CountAsync(ct);
        var activeCount = await dbQuery.CountAsync(product => product.IsActive, ct);
        var inactiveCount = await dbQuery.CountAsync(product => !product.IsActive, ct);
        var featuredCount = await dbQuery.CountAsync(product => product.IsFeatured, ct);
        var totalVariantCount = totalCount == 0
            ? 0
            : await dbQuery.SumAsync(product => product.ProductVariants.Count, ct);

        var items = await dbQuery
            .OrderByDescending(product => product.CreatedAt)
            .ThenBy(product => product.Name)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(product => new ProductIndexItem
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                BrandName = product.Brand!.Name,
                CategoryName = product.Category!.Name,
                VariantCount = product.ProductVariants.Count,
                ViewsCount = product.ViewsCount,
                TotalSoldCount = product.TotalSoldCount,
                RatingAverage = product.RatingAverage,
                RatingCount = product.RatingCount,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                CreatedAt = product.CreatedAt,
            })
            .ToListAsync(ct);

        return new ProductIndexViewModel
        {
            Products = items.Select(MapRow).ToList(),
            BrandOptions = await BuildBrandOptionsAsync(ct),
            CategoryOptions = await BuildCategoryOptionsAsync(ct),
            Search = query.Search,
            Status = query.Status,
            Featured = query.Featured,
            BrandId = query.BrandId,
            CategoryId = query.CategoryId,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount,
            FeaturedCount = featuredCount,
            TotalVariantCount = totalVariantCount,
        };
    }

    public async Task<ProductFormViewModel> GetCreateFormAsync(CancellationToken ct = default)
    {
        return await PrepareFormAsync(new ProductFormViewModel { IsActive = true }, ct);
    }

    public async Task<ProductFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Products
            .AsNoTracking()
            .Include(product => product.ProductSpecifications)
            .FirstOrDefaultAsync(product => product.Id == id, ct);

        if (entity is null)
        {
            return null;
        }

        return await PrepareFormAsync(new ProductFormViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            BrandId = entity.BrandId,
            CategoryId = entity.CategoryId,
            Description = entity.Description,
            IsActive = entity.IsActive,
            IsFeatured = entity.IsFeatured,
            Specifications = entity.ProductSpecifications
                .Select(item => new ProductSpecificationInputViewModel
                {
                    CategoryId = entity.CategoryId,
                    SpecificationId = item.SpecificationId,
                    Value = item.Value,
                    SortOrder = item.SortOrder,
                    IsHighlight = item.IsHighlight,
                })
                .ToList(),
        }, ct);
    }

    public async Task<ProductFormViewModel> PrepareFormAsync(
        ProductFormViewModel form,
        CancellationToken ct = default)
    {
        form.BrandOptions = await BuildBrandOptionsAsync(ct);
        form.CategoryOptions = await BuildCategoryOptionsAsync(ct);
        form.Specifications = await BuildSpecificationInputsAsync(form.Specifications, ct);
        return form;
    }

    public async Task<ProductSaveResult> CreateAsync(
        ProductFormViewModel form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);
        form = await PrepareFormAsync(form, ct);

        var errors = await ValidateFormAsync(form, existingId: null, ct);
        if (errors.Count > 0)
        {
            return ProductSaveResult.Failed(form, errors);
        }

        var entity = new Product
        {
            BrandId = form.BrandId!.Value,
            CategoryId = form.CategoryId!.Value,
            Name = form.Name,
            Slug = form.Slug!,
            Description = form.Description,
            IsActive = form.IsActive,
            IsFeatured = form.IsFeatured,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var specification in GetSelectedSpecificationInputs(form))
        {
            entity.ProductSpecifications.Add(new ProductSpecification
            {
                SpecificationId = specification.SpecificationId,
                Value = specification.Value!,
                SortOrder = specification.SortOrder,
                IsHighlight = specification.IsHighlight,
            });
        }

        _db.Products.Add(entity);
        await _db.SaveChangesAsync(ct);

        form.Id = entity.Id;
        return ProductSaveResult.Success(form, $"Đã tạo sản phẩm \"{entity.Name}\" thành công.");
    }

    public async Task<ProductSaveResult> UpdateAsync(
        long id,
        ProductFormViewModel form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);

        var entity = await _db.Products
            .Include(product => product.ProductSpecifications)
            .FirstOrDefaultAsync(product => product.Id == id, ct);
        if (entity is null)
        {
            return ProductSaveResult.Failed(
                await PrepareFormAsync(form, ct),
                new[] { new ProductValidationError(string.Empty, "Không tìm thấy sản phẩm.") });
        }

        form = await PrepareFormAsync(form, ct);

        var errors = await ValidateFormAsync(form, existingId: id, ct);
        if (errors.Count > 0)
        {
            return ProductSaveResult.Failed(form, errors);
        }

        entity.BrandId = form.BrandId!.Value;
        entity.CategoryId = form.CategoryId!.Value;
        entity.Name = form.Name;
        entity.Slug = form.Slug!;
        entity.Description = form.Description;
        entity.IsActive = form.IsActive;
        entity.IsFeatured = form.IsFeatured;
        entity.UpdatedAt = DateTime.UtcNow;
        ApplyProductSpecifications(entity, GetSelectedSpecificationInputs(form));

        await _db.SaveChangesAsync(ct);
        return ProductSaveResult.Success(form, $"Đã cập nhật sản phẩm \"{entity.Name}\" thành công.");
    }

    public async Task<ProductDeleteCheckResult> CheckDeleteAsync(long id, CancellationToken ct = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Name,
                VariantCount = item.ProductVariants.Count,
                SpecificationCount = item.ProductSpecifications.Count,
                ImageCount = item.ProductVariants.Sum(variant => variant.ProductVariantImages.Count),
            })
            .FirstOrDefaultAsync(ct);

        if (product is null)
        {
            return ProductDeleteCheckResult.NotFound();
        }

        var blockers = BuildDeleteBlockers(
            product.VariantCount,
            product.SpecificationCount,
            product.ImageCount);

        return blockers.Count == 0
            ? ProductDeleteCheckResult.Allowed(product.Name)
            : ProductDeleteCheckResult.Blocked(product.Name, blockers);
    }

    public async Task<ProductDeleteResult> DeleteAsync(long id, CancellationToken ct = default)
    {
        var deleteCheck = await CheckDeleteAsync(id, ct);
        if (!deleteCheck.Found)
        {
            return ProductDeleteResult.NotFound();
        }

        if (!deleteCheck.CanDelete)
        {
            return ProductDeleteResult.Failed(deleteCheck.Message);
        }

        var entity = await _db.Products.FirstOrDefaultAsync(product => product.Id == id, ct);
        if (entity is null)
        {
            return ProductDeleteResult.NotFound();
        }

        _db.Products.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return ProductDeleteResult.Success($"Đã xoá sản phẩm \"{entity.Name}\" thành công.");
    }

    public async Task<ProductToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(product => product.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new ProductToggleResult(entity.IsActive);
    }

    public async Task<ProductToggleResult?> ToggleFeaturedAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(product => product.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.IsFeatured = !entity.IsFeatured;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new ProductToggleResult(entity.IsFeatured);
    }

    private async Task<List<ProductValidationError>> ValidateFormAsync(
        ProductFormViewModel form,
        long? existingId,
        CancellationToken ct)
    {
        var errors = new List<ProductValidationError>();

        if (await _db.Products.AnyAsync(
                product => product.Slug == form.Slug && (!existingId.HasValue || product.Id != existingId.Value),
                ct))
        {
            errors.Add(new ProductValidationError(nameof(form.Slug), "Slug đã tồn tại, hãy dùng slug khác."));
        }

        if (!form.BrandId.HasValue)
        {
            errors.Add(new ProductValidationError(nameof(form.BrandId), "Vui lòng chọn thương hiệu."));
        }
        else if (!await _db.Brands.AnyAsync(brand => brand.Id == form.BrandId.Value, ct))
        {
            errors.Add(new ProductValidationError(nameof(form.BrandId), "Thương hiệu không tồn tại."));
        }

        if (!form.CategoryId.HasValue)
        {
            errors.Add(new ProductValidationError(nameof(form.CategoryId), "Vui lòng chọn danh mục."));
            return errors;
        }

        var category = await _db.Categories
            .Where(item => item.Id == form.CategoryId.Value)
            .Select(item => new
            {
                item.Id,
                ChildCount = item.Children.Count,
            })
            .FirstOrDefaultAsync(ct);

        if (category is null)
        {
            errors.Add(new ProductValidationError(nameof(form.CategoryId), "Danh mục không tồn tại."));
        }
        else if (category.ChildCount > 0)
        {
            errors.Add(new ProductValidationError(
                nameof(form.CategoryId),
                "Chỉ chọn danh mục con cuối cùng, không chọn danh mục cha còn chứa danh mục con."));
        }

        if (category is not null && category.ChildCount == 0)
        {
            foreach (var item in form.Specifications.Select((specification, index) => new { specification, index }))
            {
                if (item.specification.CategoryId != form.CategoryId.Value || !item.specification.IsRequired)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.specification.Value))
                {
                    errors.Add(new ProductValidationError(
                        $"{nameof(form.Specifications)}[{item.index}].{nameof(ProductSpecificationInputViewModel.Value)}",
                        $"Vui lòng nhập {item.specification.Name}."));
                }
            }
        }

        return errors;
    }

    private async Task<List<ProductSpecificationInputViewModel>> BuildSpecificationInputsAsync(
        IEnumerable<ProductSpecificationInputViewModel> existingValues,
        CancellationToken ct)
    {
        var valueMap = existingValues
            .GroupBy(item => new { item.CategoryId, item.SpecificationId })
            .ToDictionary(
                group => group.Key,
                group => group.Last());

        var categories = await _categoryHierarchy.GetNodesAsync(ct);

        var categorySpecifications = await _db.CategorySpecifications
            .AsNoTracking()
            .Include(item => item.Specification)
            .OrderBy(item => item.CategoryId)
            .ThenBy(item => item.GroupName)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Specification!.Name)
            .ToListAsync(ct);

        return _categoryHierarchy.ResolveEffectiveAssignments(
                categories,
                categorySpecifications,
                assignment => assignment.CategoryId,
                assignment => assignment.SpecificationId)
            .OrderBy(item => item.CategoryId)
            .ThenBy(item => item.Assignment.GroupName)
            .ThenBy(item => item.Assignment.SortOrder)
            .ThenBy(item => item.Assignment.Specification!.Name)
            .Select(item =>
            {
                var assignment = item.Assignment;
                valueMap.TryGetValue(
                    new { item.CategoryId, assignment.SpecificationId },
                    out var existing);

                return new ProductSpecificationInputViewModel
                {
                    CategoryId = item.CategoryId,
                    SpecificationId = assignment.SpecificationId,
                    Key = assignment.Specification!.Key,
                    Name = assignment.Specification.Name,
                    Unit = assignment.Specification.Unit,
                    GroupName = assignment.GroupName,
                    IsRequired = assignment.IsRequired,
                    SortOrder = assignment.SortOrder,
                    Value = string.IsNullOrWhiteSpace(existing?.Value) ? null : existing.Value.Trim(),
                    IsHighlight = existing?.IsHighlight ?? false,
                };
            })
            .ToList();
    }

    private static List<ProductSpecificationInputViewModel> GetSelectedSpecificationInputs(ProductFormViewModel form)
    {
        if (!form.CategoryId.HasValue)
        {
            return [];
        }

        return form.Specifications
            .Where(item => item.CategoryId == form.CategoryId.Value)
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .Select(item =>
            {
                item.Value = item.Value!.Trim();
                return item;
            })
            .ToList();
    }

    private void ApplyProductSpecifications(
        Product product,
        IReadOnlyCollection<ProductSpecificationInputViewModel> selectedSpecifications)
    {
        var selectedMap = selectedSpecifications.ToDictionary(item => item.SpecificationId);
        var existingItems = product.ProductSpecifications.ToList();

        foreach (var existing in existingItems)
        {
            if (!selectedMap.TryGetValue(existing.SpecificationId, out var selected))
            {
                _db.ProductSpecifications.Remove(existing);
                product.ProductSpecifications.Remove(existing);
                continue;
            }

            existing.Value = selected.Value!;
            existing.SortOrder = selected.SortOrder;
            existing.IsHighlight = selected.IsHighlight;
        }

        var existingIds = existingItems.Select(item => item.SpecificationId).ToHashSet();
        foreach (var selected in selectedSpecifications.Where(item => !existingIds.Contains(item.SpecificationId)))
        {
            product.ProductSpecifications.Add(new ProductSpecification
            {
                ProductId = product.Id,
                SpecificationId = selected.SpecificationId,
                Value = selected.Value!,
                SortOrder = selected.SortOrder,
                IsHighlight = selected.IsHighlight,
            });
        }
    }

    private async Task<List<ProductSelectItem>> BuildBrandOptionsAsync(CancellationToken ct)
    {
        return await _db.Brands
            .AsNoTracking()
            .OrderBy(brand => brand.Name)
            .Select(brand => new ProductSelectItem
            {
                Id = brand.Id,
                Label = brand.Name,
                IsActive = brand.IsActive,
            })
            .ToListAsync(ct);
    }

    private async Task<List<ProductCategorySelectItem>> BuildCategoryOptionsAsync(CancellationToken ct)
    {
        var categories = await _db.Categories
            .AsNoTracking()
            .Select(category => new CategoryOptionSource
            {
                Id = category.Id,
                Name = category.Name,
                ParentId = category.ParentId,
                Position = category.Position,
                IsActive = category.IsActive,
                HasChildren = category.Children.Any(),
            })
            .ToListAsync(ct);

        var result = new List<ProductCategorySelectItem>();
        AppendCategoryOptions(categories, parentId: null, depth: 0, result);
        return result;
    }

    private static ProductRowViewModel MapRow(ProductIndexItem item)
        => new()
        {
            Id = item.Id,
            Name = item.Name,
            Slug = item.Slug,
            BrandName = item.BrandName,
            CategoryName = item.CategoryName,
            VariantCount = item.VariantCount,
            ViewsCount = item.ViewsCount,
            TotalSoldCount = item.TotalSoldCount,
            RatingAverage = item.RatingAverage,
            RatingCount = item.RatingCount,
            IsActive = item.IsActive,
            IsFeatured = item.IsFeatured,
            CreatedAt = item.CreatedAt,
        };

    private static List<string> BuildDeleteBlockers(
        int variantCount,
        int specificationCount,
        int imageCount)
    {
        var blockers = new List<string>();

        if (variantCount > 0)
        {
            blockers.Add($"{variantCount} biến thể");
        }

        if (specificationCount > 0)
        {
            blockers.Add($"{specificationCount} thông số kỹ thuật");
        }

        if (imageCount > 0)
        {
            blockers.Add($"{imageCount} ảnh sản phẩm");
        }

        return blockers;
    }

    private static void AppendCategoryOptions(
        List<CategoryOptionSource> categories,
        long? parentId,
        int depth,
        List<ProductCategorySelectItem> result)
    {
        var children = categories
            .Where(category => category.ParentId == parentId)
            .OrderBy(category => category.Position)
            .ThenBy(category => category.Name);

        foreach (var category in children)
        {
            var prefix = depth == 0 ? string.Empty : $"{new string('-', depth * 2)} ";
            result.Add(new ProductCategorySelectItem
            {
                Id = category.Id,
                Label = prefix + category.Name,
                Depth = depth,
                IsActive = category.IsActive,
                HasChildren = category.HasChildren,
            });

            AppendCategoryOptions(categories, category.Id, depth + 1, result);
        }
    }

    private static void NormalizeForm(ProductFormViewModel form)
    {
        form.Name = form.Name.Trim();
        form.Slug = string.IsNullOrWhiteSpace(form.Slug)
            ? GenerateSlug(form.Name)
            : GenerateSlug(form.Slug);
        form.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();

        foreach (var specification in form.Specifications)
        {
            specification.Value = string.IsNullOrWhiteSpace(specification.Value)
                ? null
                : specification.Value.Trim();
        }
    }

    private static string GenerateSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

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

    private sealed class CategoryOptionSource
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public long? ParentId { get; init; }
        public int Position { get; init; }
        public bool IsActive { get; init; }
        public bool HasChildren { get; init; }
    }
}
