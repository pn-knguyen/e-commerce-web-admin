using e_commerce_web_admin.Data;
using e_commerce_web_admin.ViewModels.Attributes;
using Microsoft.EntityFrameworkCore;
using AttributeEntity = e_commerce_web_admin.Models.Entities.Attribute;

namespace e_commerce_web_admin.Services.Attributes;

public sealed class AttributeAdminService : IAttributeAdminService
{
    private const int DefaultPageSize = 30;
    private readonly ApplicationDbContext _db;

    public AttributeAdminService(ApplicationDbContext db) => _db = db;

    // ── Index ──────────────────────────────────────────────────────────────

    public async Task<AttributeIndexViewModel> GetIndexAsync(
        AttributeIndexQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);

        // Base query without heavy includes — use projections for efficiency
        var baseQuery = _db.Attributes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            baseQuery = baseQuery.Where(a => a.Code.Contains(term) || a.Name.Contains(term));
        }

        var totalCount = await baseQuery.CountAsync(ct);

        // Stats (single pass via SQL aggregation, not in-memory)
        var stats = await _db.Attributes.AsNoTracking().Select(a => new
        {
            TotalOptions  = (int)_db.AttributeOptions.Count(o => o.AttributeId == a.Id),
            TotalCategory = (int)_db.CategoryVariantAttributes.Count(c => c.AttributeId == a.Id),
        }).ToListAsync(ct);

        // Paginated rows with lightweight projection
        var rows = await baseQuery
            .OrderBy(a => a.Name)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(a => new AttributeRowViewModel
            {
                Id             = a.Id,
                Code           = a.Code,
                Name           = a.Name,
                OptionCount    = a.AttributeOptions.Count(),
                CategoryCount  = a.CategoryVariantAttributes.Count(),
                VariantUsageCount = a.AttributeOptions
                    .SelectMany(o => o.VariantAttributes)
                    .Count(),
            })
            .ToListAsync(ct);

        return new AttributeIndexViewModel
        {
            Attributes            = rows,
            Search                = query.Search,
            Page                  = page,
            PageSize              = DefaultPageSize,
            TotalCount            = totalCount,
            TotalOptionCount      = stats.Sum(s => s.TotalOptions),
            TotalCategoryUsageCount = stats.Sum(s => s.TotalCategory),
            TotalVariantUsageCount = rows.Sum(r => r.VariantUsageCount),
        };
    }

    // ── Create ─────────────────────────────────────────────────────────────

    public Task<AttributeFormViewModel> GetCreateFormAsync(CancellationToken ct = default)
        => Task.FromResult(new AttributeFormViewModel());

    public async Task<AttrSaveResult> CreateAsync(
        AttributeFormViewModel form, CancellationToken ct = default)
    {
        NormalizeForm(form);
        var errors = await ValidateFormAsync(form, existingId: null, ct);
        if (errors.Count > 0) return AttrSaveResult.Failed(errors);

        var entity = new AttributeEntity { Code = form.Code, Name = form.Name };
        _db.Attributes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return AttrSaveResult.Success($"Đã tạo thuộc tính \"{entity.Name}\" thành công.");
    }

    // ── Edit ───────────────────────────────────────────────────────────────

    public async Task<AttributeEditViewModel?> GetEditViewAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Attributes
            .Include(a => a.AttributeOptions)
                .ThenInclude(o => o.VariantAttributes)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (entity is null) return null;

        return new AttributeEditViewModel
        {
            Form = new AttributeFormViewModel
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
            },
            Options = MapToOptionsViewModel(entity),
        };
    }

    public async Task<AttrSaveResult> UpdateAsync(
        long id, AttributeFormViewModel form, CancellationToken ct = default)
    {
        var entity = await _db.Attributes.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null)
            return AttrSaveResult.Failed([new AttrValidationError(string.Empty, "Không tìm thấy thuộc tính.")]);

        // Code is immutable after creation — only update Name
        form.Name = form.Name.Trim();
        entity.Name = form.Name;
        await _db.SaveChangesAsync(ct);
        return AttrSaveResult.Success($"Đã cập nhật thuộc tính \"{entity.Name}\" thành công.");
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    public async Task<AttrDeleteResult> DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Attributes
            .Include(a => a.AttributeOptions)
            .Include(a => a.CategoryVariantAttributes)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (entity is null) return AttrDeleteResult.NotFound();

        if (entity.CategoryVariantAttributes.Count > 0)
            return AttrDeleteResult.Failed(
                $"Không thể xoá \"{entity.Name}\" vì đang được gán cho {entity.CategoryVariantAttributes.Count} danh mục.");

        if (entity.AttributeOptions.Count > 0)
            return AttrDeleteResult.Failed(
                $"Không thể xoá \"{entity.Name}\" vì còn {entity.AttributeOptions.Count} giá trị (option). Xoá hết option trước.");

        _db.Attributes.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return AttrDeleteResult.Success($"Đã xoá thuộc tính \"{entity.Name}\" thành công.");
    }

    // ── Options ────────────────────────────────────────────────────────────

    public async Task<AttributeOptionsViewModel?> GetOptionsAsync(
        long attributeId, CancellationToken ct = default)
    {
        var entity = await _db.Attributes
            .Include(a => a.AttributeOptions)
                .ThenInclude(o => o.VariantAttributes)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attributeId, ct);

        return entity is null ? null : MapToOptionsViewModel(entity);
    }

    public async Task<AttrOptionSaveResult> AddOptionAsync(
        AttributeOptionFormViewModel form, CancellationToken ct = default)
    {
        NormalizeOptionForm(form);

        var attrExists = await _db.Attributes.AnyAsync(a => a.Id == form.AttributeId, ct);
        if (!attrExists)
            return AttrOptionSaveResult.Failed("Không tìm thấy thuộc tính.");

        var duplicate = await _db.AttributeOptions.AnyAsync(
            o => o.AttributeId == form.AttributeId && o.Value == form.Value, ct);
        if (duplicate)
            return AttrOptionSaveResult.Failed($"Giá trị \"{form.Value}\" đã tồn tại trong thuộc tính này.");

        var option = new Models.Entities.AttributeOption
        {
            AttributeId = form.AttributeId,
            Value = form.Value,
            Label = form.Label,
        };
        _db.AttributeOptions.Add(option);
        await _db.SaveChangesAsync(ct);
        return AttrOptionSaveResult.Success(option.Id, $"Đã thêm option \"{option.Label}\" thành công.");
    }

    public async Task<AttrOptionSaveResult> UpdateOptionAsync(
        AttributeOptionUpdateViewModel form, CancellationToken ct = default)
    {
        var option = await _db.AttributeOptions.FirstOrDefaultAsync(o => o.Id == form.Id, ct);
        if (option is null)
            return AttrOptionSaveResult.Failed("Không tìm thấy option.");

        option.Label = form.Label.Trim();
        await _db.SaveChangesAsync(ct);
        return AttrOptionSaveResult.Success(option.Id, $"Đã cập nhật option thành công.");
    }

    public async Task<AttrOptionDeleteResult> DeleteOptionAsync(long optionId, CancellationToken ct = default)
    {
        var option = await _db.AttributeOptions
            .Include(o => o.VariantAttributes)
            .FirstOrDefaultAsync(o => o.Id == optionId, ct);

        if (option is null) return AttrOptionDeleteResult.NotFound();

        if (option.VariantAttributes.Count > 0)
            return AttrOptionDeleteResult.Failed(
                $"Không thể xoá \"{option.Label}\" vì đang được dùng bởi {option.VariantAttributes.Count} biến thể sản phẩm.");

        _db.AttributeOptions.Remove(option);
        await _db.SaveChangesAsync(ct);
        return AttrOptionDeleteResult.Success($"Đã xoá option \"{option.Label}\" thành công.");
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<List<AttrValidationError>> ValidateFormAsync(
        AttributeFormViewModel form, long? existingId, CancellationToken ct)
    {
        var errors = new List<AttrValidationError>();

        if (await _db.Attributes.AnyAsync(
                a => a.Code == form.Code && (!existingId.HasValue || a.Id != existingId.Value), ct))
        {
            errors.Add(new AttrValidationError(nameof(form.Code), $"Code \"{form.Code}\" đã tồn tại."));
        }

        return errors;
    }

    private static void NormalizeForm(AttributeFormViewModel form)
    {
        // Code: lowercase, spaces → underscore, trim
        form.Code = form.Code.Trim().ToLowerInvariant().Replace(' ', '_');
        form.Name = form.Name.Trim();
    }

    private static void NormalizeOptionForm(AttributeOptionFormViewModel form)
    {
        form.Value = form.Value.Trim().ToLowerInvariant().Replace(' ', '_');
        form.Label = form.Label.Trim();
    }

    private static AttributeOptionsViewModel MapToOptionsViewModel(AttributeEntity entity)
        => new()
        {
            AttributeId = entity.Id,
            AttributeName = entity.Name,
            AttributeCode = entity.Code,
            Options = entity.AttributeOptions
                .OrderBy(o => o.Label)
                .Select(o => new AttributeOptionRowViewModel
                {
                    Id = o.Id,
                    AttributeId = o.AttributeId,
                    Value = o.Value,
                    Label = o.Label,
                    VariantUsageCount = o.VariantAttributes.Count,
                })
                .ToList(),
        };
}
