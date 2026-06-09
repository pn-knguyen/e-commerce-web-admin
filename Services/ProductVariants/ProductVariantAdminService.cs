using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.Services.Categories;
using e_commerce_web_admin.Services.Uploads;
using e_commerce_web_admin.ViewModels.ProductVariants;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.ProductVariants;

public sealed class ProductVariantAdminService : IProductVariantAdminService
{
    private const int DefaultPageSize = 30;
    private const string ProductVariantImageFolder = "product-variants";

    private readonly ApplicationDbContext _db;
    private readonly ICategoryHierarchyService _categoryHierarchy;
    private readonly IImageUploadService _imageUploadService;

    public ProductVariantAdminService(
        ApplicationDbContext db,
        ICategoryHierarchyService categoryHierarchy,
        IImageUploadService imageUploadService)
    {
        _db = db;
        _categoryHierarchy = categoryHierarchy;
        _imageUploadService = imageUploadService;
    }

    private sealed class ProductSnapshot
    {
        public long Id { get; init; }
        public long CategoryId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string BrandName { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
    }

    public async Task<ProductVariantIndexViewModel> GetIndexAsync(
        ProductVariantIndexQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var dbQuery = _db.ProductVariants.AsNoTracking();

        if (query.Status == "active")
        {
            dbQuery = dbQuery.Where(variant => variant.IsActive);
        }
        else if (query.Status == "inactive")
        {
            dbQuery = dbQuery.Where(variant => !variant.IsActive);
        }

        if (query.Stock == "in-stock")
        {
            dbQuery = dbQuery.Where(variant => variant.Quantity > 0);
        }
        else if (query.Stock == "out-of-stock")
        {
            dbQuery = dbQuery.Where(variant => variant.Quantity <= 0);
        }

        if (query.ProductId.HasValue)
        {
            dbQuery = dbQuery.Where(variant => variant.ProductId == query.ProductId.Value);
        }

        if (query.CategoryId.HasValue)
        {
            dbQuery = dbQuery.Where(variant =>
                variant.Product != null &&
                variant.Product.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(variant =>
                variant.Code.Contains(term) ||
                (variant.Product != null &&
                    (variant.Product.Name.Contains(term) ||
                     variant.Product.Slug.Contains(term) ||
                     (variant.Product.Brand != null && variant.Product.Brand.Name.Contains(term)) ||
                     (variant.Product.Category != null && variant.Product.Category.Name.Contains(term)))));
        }

        var totalCount = await dbQuery.CountAsync(ct);
        var activeCount = await dbQuery.CountAsync(variant => variant.IsActive, ct);
        var inactiveCount = await dbQuery.CountAsync(variant => !variant.IsActive, ct);
        var outOfStockCount = await dbQuery.CountAsync(variant => variant.Quantity <= 0, ct);
        var totalImageCount = totalCount == 0
            ? 0
            : await dbQuery.SumAsync(variant => variant.ProductVariantImages.Count, ct);

        var entities = await dbQuery
            .Include(variant => variant.Product)
                .ThenInclude(product => product!.Brand)
            .Include(variant => variant.Product)
                .ThenInclude(product => product!.Category)
            .Include(variant => variant.VariantAttributes)
                .ThenInclude(item => item.AttributeOption)
                    .ThenInclude(option => option!.Attribute)
            .Include(variant => variant.ProductVariantImages)
            .OrderByDescending(variant => variant.CreatedAt)
            .ThenBy(variant => variant.Code)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToListAsync(ct);

        return new ProductVariantIndexViewModel
        {
            Variants = entities.Select(MapRow).ToList(),
            ProductOptions = await BuildProductOptionsAsync(ct),
            CategoryOptions = await BuildCategoryOptionsAsync(ct),
            Search = query.Search,
            Status = query.Status,
            Stock = query.Stock,
            ProductId = query.ProductId,
            CategoryId = query.CategoryId,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount,
            OutOfStockCount = outOfStockCount,
            TotalImageCount = totalImageCount,
        };
    }

    public async Task<ProductVariantFormViewModel> GetCreateFormAsync(
        long? productId = null,
        CancellationToken ct = default)
    {
        return await PrepareFormAsync(
            new ProductVariantFormViewModel
            {
                ProductId = productId,
                Quantity = 0,
                IsActive = true,
            },
            ct);
    }

    public async Task<ProductVariantFormViewModel?> GetEditFormAsync(
        long id,
        CancellationToken ct = default)
    {
        var entity = await _db.ProductVariants
            .AsNoTracking()
            .Include(variant => variant.Product)
                .ThenInclude(product => product!.Brand)
            .Include(variant => variant.Product)
                .ThenInclude(product => product!.Category)
            .Include(variant => variant.VariantAttributes)
                .ThenInclude(item => item.AttributeOption)
            .Include(variant => variant.ProductVariantImages)
            .FirstOrDefaultAsync(variant => variant.Id == id, ct);

        if (entity is null)
        {
            return null;
        }

        var form = new ProductVariantFormViewModel
        {
            Id = entity.Id,
            ProductId = entity.ProductId,
            IsProductLocked = true,
            ProductName = entity.Product?.Name,
            ProductMeta = BuildProductMeta(entity.Product?.Brand?.Name, entity.Product?.Category?.Name),
            Code = entity.Code,
            Price = entity.Price,
            Quantity = entity.Quantity,
            ColorName = entity.ColorName,
            ColorHex = entity.ColorHex,
            IsDefault = entity.IsDefault,
            IsActive = entity.IsActive,
            Attributes = entity.VariantAttributes
                .Where(item => item.AttributeOption is not null)
                .Select(item => new ProductVariantAttributeInputViewModel
                {
                    CategoryId = entity.Product?.CategoryId ?? 0,
                    AttributeId = item.AttributeOption!.AttributeId,
                    SelectedOptionId = item.AttributeOptionId,
                })
                .ToList(),
            Images = entity.ProductVariantImages
                .OrderBy(image => image.Position)
                .ThenBy(image => image.Id)
                .Select(image => new ProductVariantImageInputViewModel
                {
                    Id = image.Id,
                    ImagePath = image.ImagePath,
                    AltText = image.AltText,
                    Position = image.Position,
                })
                .ToList(),
        };

        return await PrepareFormAsync(form, ct);
    }

    public async Task<ProductVariantFormViewModel> PrepareFormAsync(
        ProductVariantFormViewModel form,
        CancellationToken ct = default)
    {
        form.ProductOptions = await BuildProductOptionsAsync(ct);
        form.Attributes = await BuildAttributeInputsAsync(form.Attributes, ct);

        if (form.ProductId.HasValue)
        {
            var selectedProduct = form.ProductOptions.FirstOrDefault(item => item.Id == form.ProductId.Value);
            if (selectedProduct is not null)
            {
                form.ProductName = selectedProduct.ProductName;
                form.ProductMeta = BuildProductMeta(selectedProduct.BrandName, selectedProduct.CategoryName);
            }
        }

        if (form.Images.Count == 0)
        {
            form.Images.Add(new ProductVariantImageInputViewModel());
        }

        return form;
    }

    public async Task<ProductVariantSaveResult> CreateAsync(
        ProductVariantFormViewModel form,
        CancellationToken ct = default)
    {
        MergeBulkImageFiles(form);
        NormalizeForm(form);
        form = await PrepareFormAsync(form, ct);

        var errors = await ValidateFormAsync(form, existingId: null, ct);
        if (errors.Count > 0)
        {
            return ProductVariantSaveResult.Failed(form, errors);
        }

        var uploadErrors = await UploadVariantImagesAsync(form, ct);
        if (uploadErrors.Count > 0)
        {
            return ProductVariantSaveResult.Failed(form, uploadErrors);
        }

        var hasExistingVariants = await _db.ProductVariants
            .AnyAsync(variant => variant.ProductId == form.ProductId!.Value, ct);

        var entity = new ProductVariant
        {
            ProductId = form.ProductId!.Value,
            Code = form.Code,
            Price = form.Price!.Value,
            Quantity = 0,
            ColorName = form.ColorName,
            ColorHex = form.ColorHex,
            IsDefault = form.IsDefault || !hasExistingVariants,
            IsActive = form.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var selected in GetSelectedAttributeInputs(form))
        {
            entity.VariantAttributes.Add(new VariantAttribute
            {
                AttributeOptionId = selected.SelectedOptionId!.Value,
                CreatedAt = DateTime.UtcNow,
            });
        }

        foreach (var image in GetSelectedImageInputs(form))
        {
            entity.ProductVariantImages.Add(new ProductVariantImage
            {
                ImagePath = image.ImagePath!,
                AltText = image.AltText,
                Position = image.Position!.Value,
            });
        }

        _db.ProductVariants.Add(entity);

        if (entity.IsDefault)
        {
            await ClearSiblingDefaultsAsync(entity.ProductId, entity.Id, ct);
        }

        await _db.SaveChangesAsync(ct);

        form.Id = entity.Id;
        return ProductVariantSaveResult.Success(form, $"Đã tạo biến thể \"{entity.Code}\" thành công.");
    }

    public async Task<ProductVariantSaveResult> UpdateAsync(
        long id,
        ProductVariantFormViewModel form,
        CancellationToken ct = default)
    {
        MergeBulkImageFiles(form);
        NormalizeForm(form);

        var entity = await _db.ProductVariants
            .Include(variant => variant.VariantAttributes)
            .Include(variant => variant.ProductVariantImages)
            .FirstOrDefaultAsync(variant => variant.Id == id, ct);

        if (entity is null)
        {
            return ProductVariantSaveResult.Failed(
                await PrepareFormAsync(form, ct),
                new[] { new ProductVariantValidationError(string.Empty, "Không tìm thấy biến thể sản phẩm.") });
        }

        form.Id = entity.Id;
        form.ProductId = entity.ProductId;
        form.IsProductLocked = true;
        form = await PrepareFormAsync(form, ct);

        var errors = await ValidateFormAsync(form, existingId: id, ct);
        if (errors.Count > 0)
        {
            return ProductVariantSaveResult.Failed(form, errors);
        }

        var uploadErrors = await UploadVariantImagesAsync(form, ct);
        if (uploadErrors.Count > 0)
        {
            return ProductVariantSaveResult.Failed(form, uploadErrors);
        }

        entity.Code = form.Code;
        entity.Price = form.Price!.Value;
        entity.ColorName = form.ColorName;
        entity.ColorHex = form.ColorHex;
        entity.IsDefault = form.IsDefault;
        entity.IsActive = form.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        ApplyVariantAttributes(entity, GetSelectedAttributeInputs(form));
        ApplyVariantImages(entity, GetSelectedImageInputs(form));

        if (entity.IsDefault)
        {
            await ClearSiblingDefaultsAsync(entity.ProductId, entity.Id, ct);
        }

        await _db.SaveChangesAsync(ct);
        return ProductVariantSaveResult.Success(form, $"Đã cập nhật biến thể \"{entity.Code}\" thành công.");
    }

    public async Task<ProductVariantDeleteCheckResult> CheckDeleteAsync(
        long id,
        CancellationToken ct = default)
    {
        var variant = await _db.ProductVariants
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Code,
                CartCount = item.CartItems.Count,
                WishlistCount = item.Wishlists.Count,
                OrderItemCount = item.OrderItems.Count,
                ReceiptItemCount = item.GoodReceiptItems.Count,
                GiftRuleCount = item.GiftPromotionRules.Count,
            })
            .FirstOrDefaultAsync(ct);

        if (variant is null)
        {
            return ProductVariantDeleteCheckResult.NotFound();
        }

        var voucherTargetCount = await _db.VoucherTargets.CountAsync(
            target => target.TargetType == TargetType.ProductVariant && target.TargetId == id,
            ct);
        var promotionTargetCount = await _db.PromotionTargets.CountAsync(
            target => target.TargetType == TargetType.ProductVariant && target.TargetId == id,
            ct);

        var blockers = BuildDeleteBlockers(
            variant.CartCount,
            variant.WishlistCount,
            variant.OrderItemCount,
            variant.ReceiptItemCount,
            variant.GiftRuleCount,
            voucherTargetCount,
            promotionTargetCount);

        return blockers.Count == 0
            ? ProductVariantDeleteCheckResult.Allowed(variant.Code)
            : ProductVariantDeleteCheckResult.Blocked(variant.Code, blockers);
    }

    public async Task<ProductVariantDeleteResult> DeleteAsync(
        long id,
        CancellationToken ct = default)
    {
        var check = await CheckDeleteAsync(id, ct);
        if (!check.Found)
        {
            return ProductVariantDeleteResult.NotFound();
        }

        if (!check.CanDelete)
        {
            return ProductVariantDeleteResult.Failed(check.Message);
        }

        var entity = await _db.ProductVariants
            .Include(variant => variant.VariantAttributes)
            .Include(variant => variant.ProductVariantImages)
            .FirstOrDefaultAsync(variant => variant.Id == id, ct);

        if (entity is null)
        {
            return ProductVariantDeleteResult.NotFound();
        }

        _db.VariantAttributes.RemoveRange(entity.VariantAttributes);
        _db.ProductVariantImages.RemoveRange(entity.ProductVariantImages);
        _db.ProductVariants.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return ProductVariantDeleteResult.Success($"Đã xóa biến thể \"{entity.Code}\".");
    }

    public async Task<ProductVariantToggleResult?> ToggleActiveAsync(
        long id,
        CancellationToken ct = default)
    {
        var entity = await _db.ProductVariants.FirstOrDefaultAsync(variant => variant.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new ProductVariantToggleResult(entity.IsActive);
    }

    public async Task<ProductVariantToggleResult?> SetDefaultAsync(
        long id,
        CancellationToken ct = default)
    {
        var entity = await _db.ProductVariants.FirstOrDefaultAsync(variant => variant.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.IsDefault = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await ClearSiblingDefaultsAsync(entity.ProductId, entity.Id, ct);
        await _db.SaveChangesAsync(ct);
        return new ProductVariantToggleResult(entity.IsDefault);
    }

    private async Task<List<ProductVariantProductOptionViewModel>> BuildProductOptionsAsync(CancellationToken ct)
    {
        return await _db.Products
            .AsNoTracking()
            .Include(product => product.Brand)
            .Include(product => product.Category)
            .OrderBy(product => product.Name)
            .Select(product => new ProductVariantProductOptionViewModel
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                ProductName = product.Name,
                BrandName = product.Brand != null ? product.Brand.Name : "Không có thương hiệu",
                CategoryName = product.Category != null ? product.Category.Name : "Không có danh mục",
                Label = product.Name + " - " +
                    (product.Brand != null ? product.Brand.Name : "Không có thương hiệu") + " - " +
                    (product.Category != null ? product.Category.Name : "Không có danh mục"),
                IsActive = product.IsActive,
            })
            .ToListAsync(ct);
    }

    private async Task<List<ProductVariantCategoryOptionViewModel>> BuildCategoryOptionsAsync(CancellationToken ct)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(category => category.Products.Any())
            .OrderBy(category => category.Name)
            .Select(category => new ProductVariantCategoryOptionViewModel
            {
                Id = category.Id,
                Label = category.Name,
            })
            .ToListAsync(ct);
    }

    private async Task<List<ProductVariantAttributeInputViewModel>> BuildAttributeInputsAsync(
        IEnumerable<ProductVariantAttributeInputViewModel> existingValues,
        CancellationToken ct)
    {
        var valueMap = existingValues
            .GroupBy(item => new { item.CategoryId, item.AttributeId })
            .ToDictionary(group => group.Key, group => group.Last());

        var categories = await _categoryHierarchy.GetNodesAsync(ct);

        var categoryAttributes = await _db.CategoryVariantAttributes
            .AsNoTracking()
            .Where(item => item.Attribute!.Code != CatalogAttributeCodes.Color)
            .Include(item => item.Attribute)
                .ThenInclude(attribute => attribute!.AttributeOptions)
            .OrderBy(item => item.CategoryId)
            .ThenBy(item => item.Attribute!.Name)
            .ToListAsync(ct);

        return _categoryHierarchy.ResolveEffectiveAssignments(
                categories,
                categoryAttributes,
                assignment => assignment.CategoryId,
                assignment => assignment.AttributeId)
            .OrderBy(item => item.CategoryId)
            .ThenBy(item => item.Assignment.Attribute!.Name)
            .Select(item =>
            {
                var assignment = item.Assignment;
                valueMap.TryGetValue(new { item.CategoryId, assignment.AttributeId }, out var existing);

                return new ProductVariantAttributeInputViewModel
                {
                    CategoryId = item.CategoryId,
                    AttributeId = assignment.AttributeId,
                    AttributeCode = assignment.Attribute!.Code,
                    AttributeName = assignment.Attribute.Name,
                    SelectedOptionId = existing?.SelectedOptionId,
                    Options = assignment.Attribute.AttributeOptions
                        .OrderBy(option => option.Label)
                        .Select(option => new ProductVariantAttributeOptionViewModel
                        {
                            Id = option.Id,
                            Value = option.Value,
                            Label = option.Label,
                        })
                        .ToList(),
                };
            })
            .ToList();
    }

    private async Task<ProductSnapshot?> GetProductSnapshotAsync(long productId, CancellationToken ct)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => new ProductSnapshot
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                BrandName = product.Brand != null ? product.Brand.Name : "Không có thương hiệu",
                CategoryName = product.Category != null ? product.Category.Name : "Không có danh mục",
            })
            .FirstOrDefaultAsync(ct);
    }

    private async Task<List<ProductVariantValidationError>> ValidateFormAsync(
        ProductVariantFormViewModel form,
        long? existingId,
        CancellationToken ct)
    {
        var errors = new List<ProductVariantValidationError>();

        if (!form.ProductId.HasValue)
        {
            errors.Add(new ProductVariantValidationError(nameof(form.ProductId), "Vui lòng chọn sản phẩm."));
            return errors;
        }

        var product = await GetProductSnapshotAsync(form.ProductId.Value, ct);
        if (product is null)
        {
            errors.Add(new ProductVariantValidationError(nameof(form.ProductId), "Sản phẩm không tồn tại."));
            return errors;
        }

        if (string.IsNullOrWhiteSpace(form.Code))
        {
            errors.Add(new ProductVariantValidationError(nameof(form.Code), "Mã biến thể là bắt buộc."));
        }
        else if (await _db.ProductVariants.AnyAsync(
                     variant => variant.Code == form.Code &&
                         (!existingId.HasValue || variant.Id != existingId.Value),
                     ct))
        {
            errors.Add(new ProductVariantValidationError(nameof(form.Code), "Mã biến thể đã tồn tại."));
        }

        if (!form.Price.HasValue || form.Price.Value < 0)
        {
            errors.Add(new ProductVariantValidationError(nameof(form.Price), "Giá bán không được âm."));
        }

        ValidateColorInputs(form, errors);
        ValidateAttributeInputs(form, product.CategoryId, errors);
        ValidateImageInputs(form, errors);

        if (!errors.Any(error => error.FieldName.StartsWith(nameof(form.Attributes), StringComparison.Ordinal)))
        {
            await ValidateDuplicateAttributeCombinationAsync(form, existingId, errors, ct);
        }

        return errors;
    }

    private static void ValidateColorInputs(
        ProductVariantFormViewModel form,
        ICollection<ProductVariantValidationError> errors)
    {
        var hasColorName = !string.IsNullOrWhiteSpace(form.ColorName);
        var hasColorHex = !string.IsNullOrWhiteSpace(form.ColorHex);

        if (hasColorName && form.ColorName!.Length > 120)
        {
            errors.Add(new ProductVariantValidationError(
                nameof(form.ColorName),
                "Tên màu tối đa 120 ký tự."));
        }

        if (hasColorHex && !IsValidColorHex(form.ColorHex!))
        {
            errors.Add(new ProductVariantValidationError(
                nameof(form.ColorHex),
                "Mã màu phải đúng định dạng #RRGGBB."));
        }

        if (hasColorName != hasColorHex)
        {
            var fieldName = hasColorName ? nameof(form.ColorHex) : nameof(form.ColorName);
            errors.Add(new ProductVariantValidationError(
                fieldName,
                "Vui lòng nhập đầy đủ tên màu và mã màu."));
        }
    }

    private static void ValidateAttributeInputs(
        ProductVariantFormViewModel form,
        long categoryId,
        ICollection<ProductVariantValidationError> errors)
    {
        var categoryAttributes = form.Attributes
            .Select((attribute, index) => new { attribute, index })
            .Where(item => item.attribute.CategoryId == categoryId)
            .ToList();

        foreach (var item in categoryAttributes)
        {
            var fieldName = $"{nameof(form.Attributes)}[{item.index}].{nameof(ProductVariantAttributeInputViewModel.SelectedOptionId)}";
            if (!item.attribute.SelectedOptionId.HasValue)
            {
                errors.Add(new ProductVariantValidationError(
                    fieldName,
                    $"Vui lòng chọn {item.attribute.AttributeName}."));
                continue;
            }

            if (!item.attribute.Options.Any(option => option.Id == item.attribute.SelectedOptionId.Value))
            {
                errors.Add(new ProductVariantValidationError(
                    fieldName,
                    $"{item.attribute.AttributeName} không có giá trị đã chọn."));
            }
        }
    }

    private static void ValidateImageInputs(
        ProductVariantFormViewModel form,
        ICollection<ProductVariantValidationError> errors)
    {
        foreach (var item in form.Images.Select((image, index) => new { image, index }))
        {
            if (item.image.Remove)
            {
                continue;
            }

            var hasAnyValue =
                item.image.Id.HasValue ||
                !string.IsNullOrWhiteSpace(item.image.ImagePath) ||
                (item.image.ImageFile is not null && item.image.ImageFile.Length > 0) ||
                !string.IsNullOrWhiteSpace(item.image.AltText);

            if (!hasAnyValue)
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(item.image.ImagePath) &&
                (item.image.ImageFile is null || item.image.ImageFile.Length <= 0))
            {
                errors.Add(new ProductVariantValidationError(
                    $"{nameof(form.Images)}[{item.index}].{nameof(ProductVariantImageInputViewModel.ImageFile)}",
                    "Vui lòng chọn ảnh để tải lên."));
            }
            else if (!string.IsNullOrWhiteSpace(item.image.ImagePath) && item.image.ImagePath.Length > 500)
            {
                errors.Add(new ProductVariantValidationError(
                    $"{nameof(form.Images)}[{item.index}].{nameof(ProductVariantImageInputViewModel.ImagePath)}",
                    "Đường dẫn ảnh tối đa 500 ký tự."));
            }

            if (!string.IsNullOrWhiteSpace(item.image.AltText) && item.image.AltText.Length > 255)
            {
                errors.Add(new ProductVariantValidationError(
                    $"{nameof(form.Images)}[{item.index}].{nameof(ProductVariantImageInputViewModel.AltText)}",
                    "Alt text tối đa 255 ký tự."));
            }

            if (item.image.Position < 0)
            {
                errors.Add(new ProductVariantValidationError(
                    $"{nameof(form.Images)}[{item.index}].{nameof(ProductVariantImageInputViewModel.Position)}",
                    "Thứ tự ảnh không được âm."));
            }
        }
    }

    private async Task<List<ProductVariantValidationError>> UploadVariantImagesAsync(
        ProductVariantFormViewModel form,
        CancellationToken ct)
    {
        var errors = new List<ProductVariantValidationError>();

        foreach (var item in form.Images.Select((image, index) => new { image, index }))
        {
            if (item.image.Remove ||
                item.image.ImageFile is null ||
                item.image.ImageFile.Length <= 0)
            {
                continue;
            }

            var uploadResult = await _imageUploadService.UploadAsync(
                item.image.ImageFile,
                ProductVariantImageFolder,
                ct);

            if (!uploadResult.Succeeded)
            {
                errors.Add(new ProductVariantValidationError(
                    $"{nameof(form.Images)}[{item.index}].{nameof(ProductVariantImageInputViewModel.ImageFile)}",
                    "Không thể tải ảnh biến thể lên Cloudinary. Vui lòng kiểm tra cấu hình hoặc chọn ảnh khác."));
                continue;
            }

            item.image.ImagePath = uploadResult.SecureUrl;
        }

        return errors;
    }

    private async Task ValidateDuplicateAttributeCombinationAsync(
        ProductVariantFormViewModel form,
        long? existingId,
        ICollection<ProductVariantValidationError> errors,
        CancellationToken ct)
    {
        var selectedOptionIds = GetSelectedAttributeInputs(form)
            .Select(item => item.SelectedOptionId!.Value)
            .OrderBy(id => id)
            .ToArray();
        var colorName = NormalizeComparable(form.ColorName);
        var colorHex = NormalizeComparable(form.ColorHex);

        if (selectedOptionIds.Length == 0 && colorName.Length == 0 && colorHex.Length == 0)
        {
            return;
        }

        var existingVariants = await _db.ProductVariants
            .AsNoTracking()
            .Where(variant =>
                variant.ProductId == form.ProductId!.Value &&
                (!existingId.HasValue || variant.Id != existingId.Value))
            .Include(variant => variant.VariantAttributes)
                .ThenInclude(item => item.AttributeOption)
                .ThenInclude(option => option!.Attribute)
            .ToListAsync(ct);

        var selectedSet = selectedOptionIds.ToHashSet();
        var duplicate = existingVariants.FirstOrDefault(variant =>
        {
            var existingSet = variant.VariantAttributes
                .Where(item => item.AttributeOption?.Attribute?.Code != CatalogAttributeCodes.Color)
                .Select(item => item.AttributeOptionId)
                .ToHashSet();

            return existingSet.Count == selectedSet.Count &&
                existingSet.SetEquals(selectedSet) &&
                NormalizeComparable(variant.ColorName) == colorName &&
                NormalizeComparable(variant.ColorHex) == colorHex;
        });

        if (duplicate is not null)
        {
            errors.Add(new ProductVariantValidationError(
                string.Empty,
                $"Tổ hợp thuộc tính này đã tồn tại ở biến thể \"{duplicate.Code}\"."));
        }
    }

    private static List<ProductVariantAttributeInputViewModel> GetSelectedAttributeInputs(
        ProductVariantFormViewModel form)
    {
        if (!form.ProductId.HasValue)
        {
            return [];
        }

        var productCategoryId = form.ProductOptions
            .FirstOrDefault(product => product.Id == form.ProductId.Value)
            ?.CategoryId;

        if (!productCategoryId.HasValue)
        {
            return [];
        }

        return form.Attributes
            .Where(item => item.CategoryId == productCategoryId.Value)
            .Where(item => item.SelectedOptionId.HasValue)
            .ToList();
    }

    private static List<ProductVariantImageInputViewModel> GetSelectedImageInputs(
        ProductVariantFormViewModel form)
    {
        var nextPosition = 1;
        return form.Images
            .Where(item => !item.Remove)
            .Where(HasPersistableImageValue)
            .Select(item =>
            {
                item.ImagePath = item.ImagePath!.Trim();
                item.AltText = string.IsNullOrWhiteSpace(item.AltText) ? null : item.AltText.Trim();
                item.Position = item.Position.HasValue ? item.Position.Value : nextPosition;
                nextPosition = Math.Max(nextPosition + 1, item.Position.Value + 1);
                return item;
            })
            .OrderBy(item => item.Position)
            .ToList();
    }

    private static bool HasPersistableImageValue(ProductVariantImageInputViewModel image)
    {
        return !string.IsNullOrWhiteSpace(image.ImagePath);
    }

    private void ApplyVariantAttributes(
        ProductVariant variant,
        IReadOnlyCollection<ProductVariantAttributeInputViewModel> selectedAttributes)
    {
        var selectedIds = selectedAttributes
            .Select(item => item.SelectedOptionId!.Value)
            .ToHashSet();
        var existingItems = variant.VariantAttributes.ToList();

        foreach (var existing in existingItems)
        {
            if (selectedIds.Contains(existing.AttributeOptionId))
            {
                continue;
            }

            _db.VariantAttributes.Remove(existing);
            variant.VariantAttributes.Remove(existing);
        }

        var existingIds = existingItems.Select(item => item.AttributeOptionId).ToHashSet();
        foreach (var selectedId in selectedIds.Where(id => !existingIds.Contains(id)))
        {
            variant.VariantAttributes.Add(new VariantAttribute
            {
                ProductVariantId = variant.Id,
                AttributeOptionId = selectedId,
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    private void ApplyVariantImages(
        ProductVariant variant,
        IReadOnlyCollection<ProductVariantImageInputViewModel> selectedImages)
    {
        var selectedById = selectedImages
            .Where(image => image.Id.HasValue)
            .ToDictionary(image => image.Id!.Value);
        var existingItems = variant.ProductVariantImages.ToList();

        foreach (var existing in existingItems)
        {
            if (!selectedById.TryGetValue(existing.Id, out var selected))
            {
                _db.ProductVariantImages.Remove(existing);
                variant.ProductVariantImages.Remove(existing);
                continue;
            }
            existing.ImagePath = selected.ImagePath!;
            existing.AltText = selected.AltText;
            existing.Position = selected.Position!.Value;
        }

        foreach (var selected in selectedImages.Where(image => !image.Id.HasValue))
        {
            variant.ProductVariantImages.Add(new ProductVariantImage
            {
                ProductVariantId = variant.Id,
                ImagePath = selected.ImagePath!,
                AltText = selected.AltText,
                Position = selected.Position!.Value,
            });
        }
    }

    private async Task ClearSiblingDefaultsAsync(
        long productId,
        long currentVariantId,
        CancellationToken ct)
    {
        var siblings = await _db.ProductVariants
            .Where(variant =>
                variant.ProductId == productId &&
                variant.Id != currentVariantId &&
                variant.IsDefault)
            .ToListAsync(ct);

        foreach (var sibling in siblings)
        {
            sibling.IsDefault = false;
            sibling.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static ProductVariantRowViewModel MapRow(ProductVariant variant)
    {
        return new ProductVariantRowViewModel
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            ProductName = variant.Product?.Name ?? "Không rõ sản phẩm",
            ProductSlug = variant.Product?.Slug ?? string.Empty,
            BrandName = variant.Product?.Brand?.Name ?? "Không rõ thương hiệu",
            CategoryName = variant.Product?.Category?.Name ?? "Không rõ danh mục",
            Code = variant.Code,
            Price = variant.Price,
            SoldCount = variant.SoldCount,
            Quantity = variant.Quantity,
            ColorName = variant.ColorName,
            ColorHex = variant.ColorHex,
            IsDefault = variant.IsDefault,
            IsActive = variant.IsActive,
            AttributeSummary = BuildAttributeSummary(variant),
            ImageCount = variant.ProductVariantImages.Count,
            CreatedAt = variant.CreatedAt,
        };
    }

    private static string BuildAttributeSummary(ProductVariant variant)
    {
        var labels = new List<string>();
        if (!string.IsNullOrWhiteSpace(variant.ColorName))
        {
            labels.Add($"Màu: {variant.ColorName}");
        }

        labels.AddRange(variant.VariantAttributes
            .Where(item =>
                item.AttributeOption?.Attribute is not null &&
                item.AttributeOption.Attribute.Code != CatalogAttributeCodes.Color)
            .OrderBy(item => item.AttributeOption!.Attribute!.Name)
            .Select(item => $"{item.AttributeOption!.Attribute!.Name}: {item.AttributeOption.Label}"));

        return labels.Count == 0 ? "Chưa gán thuộc tính" : string.Join(" · ", labels);
    }

    private static bool IsValidColorHex(string value)
    {
        var color = value.Trim();
        return color.Length == 7 && color[0] == '#' && color.Skip(1).All(Uri.IsHexDigit);
    }

    private static string NormalizeComparable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    }

    private static void MergeBulkImageFiles(ProductVariantFormViewModel form)
    {
        foreach (var file in form.BulkImageFiles.Where(file => file.Length > 0))
        {
            form.Images.Add(new ProductVariantImageInputViewModel
            {
                ImageFile = file,
            });
        }

        form.BulkImageFiles.Clear();
    }

    private static string BuildProductMeta(string? brandName, string? categoryName)
    {
        var parts = new[] { brandName, categoryName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0 ? "Chưa có thông tin phân loại" : string.Join(" · ", parts);
    }

    private static List<string> BuildDeleteBlockers(
        int cartCount,
        int wishlistCount,
        int orderItemCount,
        int receiptItemCount,
        int giftRuleCount,
        int voucherTargetCount,
        int promotionTargetCount)
    {
        var blockers = new List<string>();

        if (cartCount > 0)
        {
            blockers.Add($"{cartCount} giỏ hàng");
        }

        if (wishlistCount > 0)
        {
            blockers.Add($"{wishlistCount} lượt yêu thích");
        }

        if (orderItemCount > 0)
        {
            blockers.Add($"{orderItemCount} dòng đơn hàng");
        }

        if (receiptItemCount > 0)
        {
            blockers.Add($"{receiptItemCount} dòng phiếu nhập");
        }

        if (giftRuleCount > 0)
        {
            blockers.Add($"{giftRuleCount} quy tắc quà tặng");
        }

        if (voucherTargetCount > 0)
        {
            blockers.Add($"{voucherTargetCount} phạm vi voucher");
        }

        if (promotionTargetCount > 0)
        {
            blockers.Add($"{promotionTargetCount} phạm vi khuyến mãi");
        }

        return blockers;
    }

    private static void NormalizeForm(ProductVariantFormViewModel form)
    {
        form.Code = string.IsNullOrWhiteSpace(form.Code)
            ? string.Empty
            : form.Code.Trim().ToUpperInvariant();

        form.ColorName = string.IsNullOrWhiteSpace(form.ColorName) ? null : form.ColorName.Trim();
        form.ColorHex = string.IsNullOrWhiteSpace(form.ColorHex) ? null : form.ColorHex.Trim().ToUpperInvariant();

        foreach (var image in form.Images)
        {
            image.ImagePath = string.IsNullOrWhiteSpace(image.ImagePath) ? null : image.ImagePath.Trim();
            image.AltText = string.IsNullOrWhiteSpace(image.AltText) ? null : image.AltText.Trim();
        }
    }
}
