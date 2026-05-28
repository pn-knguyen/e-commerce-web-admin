using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Services.Uploads;
using e_commerce_web_admin.ViewModels.Brands;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Brands;

public sealed class BrandAdminService : IBrandAdminService
{
    private const int DefaultPageSize = 30;
    private const string BrandImageFolder = "brands";

    private readonly ApplicationDbContext _db;
    private readonly IImageUploadService _imageUploadService;

    public BrandAdminService(ApplicationDbContext db, IImageUploadService imageUploadService)
    {
        _db = db;
        _imageUploadService = imageUploadService;
    }

    private sealed class BrandIndexItem
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string? ImagePath { get; init; }
        public bool IsActive { get; init; }
        public int ProductCount { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    // ── Index ──────────────────────────────────────────────────────────────

    public async Task<BrandIndexViewModel> GetIndexAsync(BrandIndexQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);

        var dbQuery = _db.Brands.AsNoTracking();

        if (query.Status == "active")
            dbQuery = dbQuery.Where(b => b.IsActive);
        else if (query.Status == "inactive")
            dbQuery = dbQuery.Where(b => !b.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(b => b.Name.Contains(term) || b.Slug.Contains(term));
        }

        var all = await dbQuery
            .OrderBy(b => b.Name)
            .Select(b => new BrandIndexItem
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                ImagePath = b.ImagePath,
                IsActive = b.IsActive,
                ProductCount = b.Products.Count,
                CreatedAt = b.CreatedAt,
            })
            .ToListAsync(ct);

        var totalCount = all.Count;
        var pageItems = all.Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToList();

        var rows = pageItems.Select(b => new BrandRowViewModel
        {
            Id = b.Id,
            Name = b.Name,
            Slug = b.Slug,
            ImagePath = b.ImagePath,
            IsActive = b.IsActive,
            ProductCount = b.ProductCount,
            CreatedAt = b.CreatedAt,
        }).ToList();

        return new BrandIndexViewModel
        {
            Brands = rows,
            Search = query.Search,
            Status = query.Status,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            ActiveCount = all.Count(b => b.IsActive),
            InactiveCount = all.Count(b => !b.IsActive),
            TotalProductCount = all.Sum(b => b.ProductCount),
        };
    }

    // ── Create ─────────────────────────────────────────────────────────────

    public Task<BrandFormViewModel> GetCreateFormAsync(CancellationToken ct = default)
        => Task.FromResult(new BrandFormViewModel { IsActive = true });

    public async Task<BrandSaveResult> CreateAsync(BrandFormViewModel form, CancellationToken ct = default)
    {
        NormalizeForm(form);

        var errors = await ValidateAsync(form, existingId: null, ct);
        if (errors.Count > 0)
            return BrandSaveResult.Failed(form, errors);

        var uploadError = await UploadImageIfNeededAsync(form, ct);
        if (uploadError is not null)
            return BrandSaveResult.Failed(form, new[] { uploadError });

        var entity = new Brand
        {
            Name = form.Name,
            Slug = form.Slug,
            Description = form.Description,
            ImagePath = form.ImagePath,
            IsActive = form.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Brands.Add(entity);
        await _db.SaveChangesAsync(ct);

        form.Id = entity.Id;
        return BrandSaveResult.Success(form, $"Đã tạo thương hiệu \"{entity.Name}\" thành công.");
    }

    // ── Edit ───────────────────────────────────────────────────────────────

    public async Task<BrandFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Brands.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (entity is null) return null;

        return new BrandFormViewModel
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            Description = entity.Description,
            ImagePath = entity.ImagePath,
            IsActive = entity.IsActive,
        };
    }

    public async Task<BrandSaveResult> UpdateAsync(long id, BrandFormViewModel form, CancellationToken ct = default)
    {
        NormalizeForm(form);

        var entity = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (entity is null)
            return BrandSaveResult.Failed(form,
                new[] { new BrandValidationError(string.Empty, "Không tìm thấy thương hiệu.") });

        var errors = await ValidateAsync(form, existingId: id, ct);
        if (errors.Count > 0)
            return BrandSaveResult.Failed(form, errors);

        var uploadError = await UploadImageIfNeededAsync(form, ct);
        if (uploadError is not null)
            return BrandSaveResult.Failed(form, new[] { uploadError });

        entity.Name = form.Name;
        entity.Slug = form.Slug;
        entity.Description = form.Description;
        entity.ImagePath = form.ImagePath;
        entity.IsActive = form.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return BrandSaveResult.Success(form, $"Đã cập nhật thương hiệu \"{entity.Name}\" thành công.");
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    public async Task<BrandDeleteResult> DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (entity is null) return BrandDeleteResult.NotFound();

        if (entity.Products.Count > 0)
            return BrandDeleteResult.Failed(
                $"Không thể xoá \"{entity.Name}\" vì có {entity.Products.Count} sản phẩm đang dùng thương hiệu này.");

        _db.Brands.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return BrandDeleteResult.Success($"Đã xoá thương hiệu \"{entity.Name}\" thành công.");
    }

    // ── Toggle ─────────────────────────────────────────────────────────────

    public async Task<BrandToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (entity is null) return null;

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new BrandToggleResult(entity.IsActive);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<List<BrandValidationError>> ValidateAsync(
        BrandFormViewModel form, long? existingId, CancellationToken ct)
    {
        var errors = new List<BrandValidationError>();

        if (await _db.Brands.AnyAsync(
                b => b.Slug == form.Slug && (!existingId.HasValue || b.Id != existingId.Value), ct))
        {
            errors.Add(new BrandValidationError(nameof(form.Slug), "Slug đã tồn tại, hãy dùng slug khác."));
        }

        return errors;
    }

    private async Task<BrandValidationError?> UploadImageIfNeededAsync(
        BrandFormViewModel form, CancellationToken ct)
    {
        if (form.ImageFile is null || form.ImageFile.Length <= 0)
            return null;

        var result = await _imageUploadService.UploadAsync(form.ImageFile, BrandImageFolder, ct);
        if (!result.Succeeded)
            return new BrandValidationError(nameof(form.ImageFile),
                result.ErrorMessage ?? "Không thể tải ảnh lên Cloudinary.");

        form.ImagePath = result.SecureUrl;
        return null;
    }

    private static void NormalizeForm(BrandFormViewModel form)
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
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc == UnicodeCategory.NonSpacingMark) continue;
            if (ch == '\u0111') { sb.Append('d'); continue; }
            if (ch is >= 'a' and <= 'z' or >= '0' and <= '9') { sb.Append(ch); continue; }
            if (char.IsWhiteSpace(ch) || ch is '-' or '_') sb.Append('-');
        }

        return Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
    }
}
