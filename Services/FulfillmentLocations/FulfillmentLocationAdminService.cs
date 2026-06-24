using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.ViewModels.FulfillmentLocations;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.FulfillmentLocations;

public sealed class FulfillmentLocationAdminService : IFulfillmentLocationAdminService
{
    private const int DefaultPageSize = 20;

    private readonly ApplicationDbContext _db;

    public FulfillmentLocationAdminService(ApplicationDbContext db) => _db = db;

    public async Task<FulfillmentLocationIndexViewModel> GetIndexAsync(
        FulfillmentLocationIndexQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var dbQuery = _db.FulfillmentLocations.AsNoTracking();

        if (query.Status == "active")
        {
            dbQuery = dbQuery.Where(location => location.IsActive);
        }
        else if (query.Status == "inactive")
        {
            dbQuery = dbQuery.Where(location => !location.IsActive);
        }
        else if (query.Status == "default")
        {
            dbQuery = dbQuery.Where(location => location.IsDefault);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(location =>
                location.Name.Contains(term) ||
                location.ContactName.Contains(term) ||
                location.Phone.Contains(term) ||
                location.ProvinceName.Contains(term) ||
                (location.DistrictName != null && location.DistrictName.Contains(term)) ||
                location.WardName.Contains(term) ||
                location.DetailAddress.Contains(term) ||
                (location.FormattedAddress != null && location.FormattedAddress.Contains(term)));
        }

        var totalCount = await dbQuery.CountAsync(ct);
        var activeCount = await _db.FulfillmentLocations.AsNoTracking().CountAsync(location => location.IsActive, ct);
        var inactiveCount = await _db.FulfillmentLocations.AsNoTracking().CountAsync(location => !location.IsActive, ct);
        var defaultCount = await _db.FulfillmentLocations.AsNoTracking().CountAsync(location => location.IsDefault, ct);
        var shipmentCount = await _db.Shipments.AsNoTracking().CountAsync(ct);

        var rows = await dbQuery
            .OrderByDescending(location => location.IsDefault)
            .ThenByDescending(location => location.IsActive)
            .ThenBy(location => location.Name)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(location => new FulfillmentLocationRowViewModel
            {
                Id = location.Id,
                Name = location.Name,
                ContactName = location.ContactName,
                Phone = location.Phone,
                ProvinceName = location.ProvinceName,
                DistrictCode = location.DistrictCode,
                DistrictName = location.DistrictName,
                WardName = location.WardName,
                DetailAddress = location.DetailAddress,
                Address = BuildAddress(location.DetailAddress, location.WardName, location.DistrictName, location.ProvinceName),
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                IsDefault = location.IsDefault,
                IsActive = location.IsActive,
                ShipmentCount = location.Shipments.Count,
                CreatedAt = location.CreatedAt,
            })
            .ToListAsync(ct);

        return new FulfillmentLocationIndexViewModel
        {
            Locations = rows,
            Search = query.Search,
            Status = query.Status,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount,
            DefaultCount = defaultCount,
            ShipmentCount = shipmentCount,
        };
    }

    public async Task<FulfillmentLocationFormViewModel> GetCreateFormAsync(CancellationToken ct = default)
    {
        var hasActiveDefault = await _db.FulfillmentLocations
            .AsNoTracking()
            .AnyAsync(location => location.IsActive && location.IsDefault, ct);

        return new FulfillmentLocationFormViewModel
        {
            IsActive = true,
            IsDefault = !hasActiveDefault,
        };
    }

    public async Task<FulfillmentLocationFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.FulfillmentLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(location => location.Id == id, ct);

        return entity is null
            ? null
            : new FulfillmentLocationFormViewModel
            {
                Id = entity.Id,
                Name = entity.Name,
                ContactName = entity.ContactName,
                Phone = entity.Phone,
                ProvinceCode = entity.ProvinceCode,
                ProvinceName = entity.ProvinceName,
                DistrictCode = entity.DistrictCode,
                DistrictName = entity.DistrictName,
                WardCode = entity.WardCode,
                WardName = entity.WardName,
                DetailAddress = entity.DetailAddress,
                FormattedAddress = entity.FormattedAddress,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                IsDefault = entity.IsDefault,
                IsActive = entity.IsActive,
            };
    }

    public async Task<FulfillmentLocationSaveResult> CreateAsync(
        FulfillmentLocationFormViewModel form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);

        var errors = await ValidateFormAsync(form, existingId: null, ct);
        if (errors.Count > 0)
        {
            return FulfillmentLocationSaveResult.Failed(form, errors);
        }

        if (form.IsActive && !await HasActiveDefaultAsync(exceptId: null, ct))
        {
            form.IsDefault = true;
        }

        if (form.IsDefault)
        {
            await ClearDefaultAsync(exceptId: null, ct);
        }

        var entity = new FulfillmentLocation
        {
            Name = form.Name,
            ContactName = form.ContactName,
            Phone = form.Phone,
            ProvinceCode = form.ProvinceCode,
            ProvinceName = form.ProvinceName,
            DistrictCode = form.DistrictCode,
            DistrictName = form.DistrictName,
            WardCode = form.WardCode,
            WardName = form.WardName,
            DetailAddress = form.DetailAddress,
            FormattedAddress = form.FormattedAddress,
            Latitude = form.Latitude,
            Longitude = form.Longitude,
            IsDefault = form.IsDefault,
            IsActive = form.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        _db.FulfillmentLocations.Add(entity);
        await _db.SaveChangesAsync(ct);
        form.Id = entity.Id;

        return FulfillmentLocationSaveResult.Success(
            form,
            $"Đã tạo điểm lấy hàng \"{entity.Name}\".");
    }

    public async Task<FulfillmentLocationSaveResult> UpdateAsync(
        long id,
        FulfillmentLocationFormViewModel form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);

        var entity = await _db.FulfillmentLocations.FirstOrDefaultAsync(location => location.Id == id, ct);
        if (entity is null)
        {
            return FulfillmentLocationSaveResult.Failed(
                form,
                [new FulfillmentLocationValidationError(string.Empty, "Không tìm thấy điểm lấy hàng.")]);
        }

        var errors = await ValidateFormAsync(form, existingId: id, ct);
        if (errors.Count > 0)
        {
            return FulfillmentLocationSaveResult.Failed(form, errors);
        }

        if (form.IsDefault)
        {
            await ClearDefaultAsync(id, ct);
        }

        entity.Name = form.Name;
        entity.ContactName = form.ContactName;
        entity.Phone = form.Phone;
        entity.ProvinceCode = form.ProvinceCode;
        entity.ProvinceName = form.ProvinceName;
        entity.DistrictCode = form.DistrictCode;
        entity.DistrictName = form.DistrictName;
        entity.WardCode = form.WardCode;
        entity.WardName = form.WardName;
        entity.DetailAddress = form.DetailAddress;
        entity.FormattedAddress = form.FormattedAddress;
        entity.Latitude = form.Latitude;
        entity.Longitude = form.Longitude;
        entity.IsDefault = form.IsDefault;
        entity.IsActive = form.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await EnsureOneActiveDefaultAsync(ct);

        return FulfillmentLocationSaveResult.Success(
            form,
            $"Đã cập nhật điểm lấy hàng \"{entity.Name}\".");
    }

    public async Task<FulfillmentLocationActionResult> DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.FulfillmentLocations
            .Include(location => location.Shipments)
            .FirstOrDefaultAsync(location => location.Id == id, ct);

        if (entity is null)
        {
            return FulfillmentLocationActionResult.NotFound();
        }

        if (entity.Shipments.Count > 0)
        {
            return FulfillmentLocationActionResult.Failed(
                "Điểm lấy hàng đã có báo giá hoặc vận đơn GHN, không thể xóa. Hãy tắt hoạt động nếu không dùng nữa.");
        }

        var name = entity.Name;
        _db.FulfillmentLocations.Remove(entity);
        await _db.SaveChangesAsync(ct);
        await EnsureOneActiveDefaultAsync(ct);

        return FulfillmentLocationActionResult.Success($"Đã xóa điểm lấy hàng \"{name}\".");
    }

    public async Task<FulfillmentLocationActionResult> SetDefaultAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.FulfillmentLocations.FirstOrDefaultAsync(location => location.Id == id, ct);
        if (entity is null)
        {
            return FulfillmentLocationActionResult.NotFound();
        }

        if (!entity.IsActive)
        {
            return FulfillmentLocationActionResult.Failed("Chỉ điểm lấy hàng đang hoạt động mới được đặt mặc định.");
        }

        await ClearDefaultAsync(id, ct);
        entity.IsDefault = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return FulfillmentLocationActionResult.Success($"Đã đặt \"{entity.Name}\" làm điểm lấy hàng mặc định.");
    }

    public async Task<FulfillmentLocationToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.FulfillmentLocations.FirstOrDefaultAsync(location => location.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.IsActive = !entity.IsActive;
        if (!entity.IsActive)
        {
            entity.IsDefault = false;
        }
        else if (!await HasActiveDefaultAsync(entity.Id, ct))
        {
            entity.IsDefault = true;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await EnsureOneActiveDefaultAsync(ct);

        return new FulfillmentLocationToggleResult(entity.IsActive, entity.IsDefault);
    }

    private async Task<List<FulfillmentLocationValidationError>> ValidateFormAsync(
        FulfillmentLocationFormViewModel form,
        long? existingId,
        CancellationToken ct)
    {
        var errors = new List<FulfillmentLocationValidationError>();

        if (form.IsDefault && !form.IsActive)
        {
            errors.Add(new FulfillmentLocationValidationError(
                nameof(form.IsDefault),
                "Điểm lấy hàng mặc định phải đang hoạt động."));
        }

        if (form.Latitude.HasValue != form.Longitude.HasValue)
        {
            errors.Add(new FulfillmentLocationValidationError(
                nameof(form.Latitude),
                "Vui lòng nhập đủ cả vĩ độ và kinh độ, hoặc bỏ trống cả hai."));
        }

        if (await _db.FulfillmentLocations.AnyAsync(
                location => location.Name == form.Name && (!existingId.HasValue || location.Id != existingId.Value),
                ct))
        {
            errors.Add(new FulfillmentLocationValidationError(
                nameof(form.Name),
                $"Tên điểm lấy hàng \"{form.Name}\" đã tồn tại."));
        }

        return errors;
    }

    private async Task ClearDefaultAsync(long? exceptId, CancellationToken ct)
    {
        var defaults = await _db.FulfillmentLocations
            .Where(location => location.IsDefault && (!exceptId.HasValue || location.Id != exceptId.Value))
            .ToListAsync(ct);

        foreach (var location in defaults)
        {
            location.IsDefault = false;
            location.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task EnsureOneActiveDefaultAsync(CancellationToken ct)
    {
        if (await _db.FulfillmentLocations.AnyAsync(location => location.IsActive && location.IsDefault, ct))
        {
            return;
        }

        var replacement = await _db.FulfillmentLocations
            .Where(location => location.IsActive)
            .OrderBy(location => location.Name)
            .FirstOrDefaultAsync(ct);

        if (replacement is not null)
        {
            replacement.IsDefault = true;
            replacement.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<bool> HasActiveDefaultAsync(long? exceptId, CancellationToken ct) =>
        await _db.FulfillmentLocations.AnyAsync(
            location => location.IsActive &&
                location.IsDefault &&
                (!exceptId.HasValue || location.Id != exceptId.Value),
            ct);

    private static void NormalizeForm(FulfillmentLocationFormViewModel form)
    {
        form.Name = form.Name.Trim();
        form.ContactName = form.ContactName.Trim();
        form.Phone = form.Phone.Trim();
        form.ProvinceCode = NormalizeOptional(form.ProvinceCode);
        form.ProvinceName = form.ProvinceName.Trim();
        form.DistrictCode = NormalizeOptional(form.DistrictCode);
        form.DistrictName = NormalizeOptional(form.DistrictName);
        form.WardCode = NormalizeOptional(form.WardCode);
        form.WardName = form.WardName.Trim();
        form.DetailAddress = form.DetailAddress.Trim();
        form.FormattedAddress = string.IsNullOrWhiteSpace(form.FormattedAddress)
            ? BuildAddress(form.DetailAddress, form.WardName, form.DistrictName, form.ProvinceName)
            : form.FormattedAddress.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string BuildAddress(params string?[] values) =>
        string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));
}
