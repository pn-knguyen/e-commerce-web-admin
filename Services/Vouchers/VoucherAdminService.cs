using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.ViewModels.Vouchers;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Vouchers;

public sealed class VoucherAdminService : IVoucherAdminService
{
    private const int DefaultPageSize = 30;

    private readonly ApplicationDbContext _db;

    public VoucherAdminService(ApplicationDbContext db)
    {
        _db = db;
    }

    private sealed class VoucherIndexItem
    {
        public long Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DiscountType DiscountType { get; init; }
        public decimal DiscountValue { get; init; }
        public decimal MinOrderValue { get; init; }
        public decimal? MaxDiscountValue { get; init; }
        public int? MaxUses { get; init; }
        public int? MaxUsesPerUser { get; init; }
        public int UsedCount { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public int Priority { get; init; }
        public bool IsActive { get; init; }
        public int OrderCount { get; init; }
        public int UsageCount { get; init; }
        public int AssignedUserCount { get; init; }
        public int TargetCount { get; init; }
    }

    public async Task<VoucherIndexViewModel> GetIndexAsync(
        VoucherIndexQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var now = DateTime.UtcNow;
        var dbQuery = _db.Vouchers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            dbQuery = dbQuery.Where(voucher =>
                voucher.Code.Contains(search) ||
                (voucher.Description != null && voucher.Description.Contains(search)));
        }

        dbQuery = ApplyStatusFilter(dbQuery, query.Status, now);

        var all = await dbQuery
            .OrderByDescending(voucher => voucher.IsActive)
            .ThenByDescending(voucher => voucher.Priority)
            .ThenByDescending(voucher => voucher.StartDate)
            .ThenBy(voucher => voucher.Code)
            .Select(voucher => new VoucherIndexItem
            {
                Id = voucher.Id,
                Code = voucher.Code,
                Description = voucher.Description,
                DiscountType = voucher.DiscountType,
                DiscountValue = voucher.DiscountValue,
                MinOrderValue = voucher.MinOrderValue,
                MaxDiscountValue = voucher.MaxDiscountValue,
                MaxUses = voucher.MaxUses,
                MaxUsesPerUser = voucher.MaxUsesPerUser,
                UsedCount = voucher.UsedCount,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                Priority = voucher.Priority,
                IsActive = voucher.IsActive,
                OrderCount = voucher.Orders.Count,
                UsageCount = voucher.VoucherUsages.Count,
                AssignedUserCount = voucher.VoucherUsers.Count,
                TargetCount = voucher.VoucherTargets.Count,
            })
            .ToListAsync(cancellationToken);

        var totalCount = all.Count;
        var pageItems = all.Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToList();
        var rows = pageItems.Select(item =>
        {
            var status = ResolveStatus(item, now);
            return new VoucherRowViewModel
            {
                Id = item.Id,
                Code = item.Code,
                Description = item.Description,
                DiscountType = item.DiscountType,
                DiscountValue = item.DiscountValue,
                MinOrderValue = item.MinOrderValue,
                MaxDiscountValue = item.MaxDiscountValue,
                MaxUses = item.MaxUses,
                MaxUsesPerUser = item.MaxUsesPerUser,
                UsedCount = item.UsedCount,
                StartDate = item.StartDate,
                EndDate = item.EndDate,
                Priority = item.Priority,
                IsActive = item.IsActive,
                OrderCount = item.OrderCount,
                UsageCount = item.UsageCount,
                AssignedUserCount = item.AssignedUserCount,
                TargetCount = item.TargetCount,
                StatusKey = status.Key,
                StatusLabel = status.Label,
            };
        }).ToList();

        return new VoucherIndexViewModel
        {
            Vouchers = rows,
            Search = query.Search,
            Status = query.Status,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            ActiveCount = all.Count(item => item.IsActive),
            InactiveCount = all.Count(item => !item.IsActive),
            RunningCount = all.Count(item => IsRunning(item, now)),
            UpcomingCount = all.Count(item => item.IsActive && item.StartDate > now),
            ExpiredCount = all.Count(item => item.EndDate < now),
            ExhaustedCount = all.Count(IsExhausted),
            TotalUsedCount = all.Sum(item => item.UsedCount),
        };
    }

    public Task<VoucherFormViewModel> GetCreateFormAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return Task.FromResult(PrepareForm(new VoucherFormViewModel
        {
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 100000m,
            MinOrderValue = 0m,
            MaxDiscountValue = 100000m,
            StartDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(now.Year, now.Month, now.Day, 23, 59, 0, DateTimeKind.Utc).AddDays(30),
            Priority = 0,
            IsActive = true,
        }));
    }

    public async Task<VoucherFormViewModel?> GetEditFormAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Vouchers
            .AsNoTracking()
            .FirstOrDefaultAsync(voucher => voucher.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return PrepareForm(new VoucherFormViewModel
        {
            Id = entity.Id,
            Code = entity.Code,
            Description = entity.Description,
            DiscountType = entity.DiscountType,
            DiscountValue = entity.DiscountValue,
            MinOrderValue = entity.MinOrderValue,
            MaxDiscountValue = entity.MaxDiscountValue,
            MaxUses = entity.MaxUses,
            MaxUsesPerUser = entity.MaxUsesPerUser,
            UsedCount = entity.UsedCount,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Priority = entity.Priority,
            IsActive = entity.IsActive,
        });
    }

    public Task<VoucherFormViewModel> PrepareFormAsync(
        VoucherFormViewModel form,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PrepareForm(form));
    }

    public async Task<VoucherSaveResult> CreateAsync(
        VoucherFormViewModel form,
        CancellationToken cancellationToken = default)
    {
        NormalizeForm(form);

        var errors = await ValidateFormAsync(form, existingId: null, usedCount: 0, cancellationToken);
        if (errors.Count > 0)
        {
            return VoucherSaveResult.Failed(PrepareForm(form), errors);
        }

        var entity = new Voucher
        {
            Code = form.Code,
            Description = form.Description,
            DiscountType = form.DiscountType,
            DiscountValue = form.DiscountValue,
            MinOrderValue = form.MinOrderValue,
            MaxDiscountValue = form.MaxDiscountValue,
            MaxUses = form.MaxUses,
            MaxUsesPerUser = form.MaxUsesPerUser,
            StartDate = form.StartDate,
            EndDate = form.EndDate,
            Priority = form.Priority,
            IsActive = form.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Vouchers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        form.Id = entity.Id;
        return VoucherSaveResult.Success(form, $"Đã tạo voucher \"{entity.Code}\" thành công.");
    }

    public async Task<VoucherSaveResult> UpdateAsync(
        long id,
        VoucherFormViewModel form,
        CancellationToken cancellationToken = default)
    {
        NormalizeForm(form);

        var entity = await _db.Vouchers.FirstOrDefaultAsync(voucher => voucher.Id == id, cancellationToken);
        if (entity is null)
        {
            return VoucherSaveResult.Failed(
                PrepareForm(form),
                new[] { new VoucherValidationError(string.Empty, "Không tìm thấy voucher.") });
        }

        var errors = await ValidateFormAsync(form, id, entity.UsedCount, cancellationToken);
        if (errors.Count > 0)
        {
            form.UsedCount = entity.UsedCount;
            return VoucherSaveResult.Failed(PrepareForm(form), errors);
        }

        entity.Code = form.Code;
        entity.Description = form.Description;
        entity.DiscountType = form.DiscountType;
        entity.DiscountValue = form.DiscountValue;
        entity.MinOrderValue = form.MinOrderValue;
        entity.MaxDiscountValue = form.MaxDiscountValue;
        entity.MaxUses = form.MaxUses;
        entity.MaxUsesPerUser = form.MaxUsesPerUser;
        entity.StartDate = form.StartDate;
        entity.EndDate = form.EndDate;
        entity.Priority = form.Priority;
        entity.IsActive = form.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        form.UsedCount = entity.UsedCount;
        return VoucherSaveResult.Success(form, $"Đã cập nhật voucher \"{entity.Code}\" thành công.");
    }

    public async Task<VoucherDeleteResult> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Vouchers
            .Include(voucher => voucher.Orders)
            .Include(voucher => voucher.VoucherUsages)
            .Include(voucher => voucher.VoucherUsers)
            .Include(voucher => voucher.VoucherTargets)
            .FirstOrDefaultAsync(voucher => voucher.Id == id, cancellationToken);

        if (entity is null)
        {
            return VoucherDeleteResult.NotFound();
        }

        if (entity.Orders.Count > 0 || entity.VoucherUsages.Count > 0 || entity.UsedCount > 0)
        {
            return VoucherDeleteResult.Failed(
                $"Không thể xoá \"{entity.Code}\" vì voucher đã phát sinh đơn hàng hoặc lượt sử dụng.");
        }

        if (entity.VoucherUsers.Any(user => user.UsedCount > 0))
        {
            return VoucherDeleteResult.Failed(
                $"Không thể xoá \"{entity.Code}\" vì đã có khách hàng sử dụng voucher này.");
        }

        if (entity.VoucherUsers.Count > 0)
        {
            _db.VoucherUsers.RemoveRange(entity.VoucherUsers);
        }

        if (entity.VoucherTargets.Count > 0)
        {
            _db.VoucherTargets.RemoveRange(entity.VoucherTargets);
        }

        _db.Vouchers.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return VoucherDeleteResult.Success($"Đã xoá voucher \"{entity.Code}\" thành công.");
    }

    public async Task<VoucherToggleResult?> ToggleActiveAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Vouchers.FirstOrDefaultAsync(voucher => voucher.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new VoucherToggleResult(entity.IsActive);
    }

    private static IQueryable<Voucher> ApplyStatusFilter(
        IQueryable<Voucher> query,
        string? status,
        DateTime now)
    {
        return status switch
        {
            "active" => query.Where(voucher => voucher.IsActive),
            "inactive" => query.Where(voucher => !voucher.IsActive),
            "running" => query.Where(voucher =>
                voucher.IsActive &&
                voucher.StartDate <= now &&
                voucher.EndDate >= now &&
                (!voucher.MaxUses.HasValue || voucher.UsedCount < voucher.MaxUses.Value)),
            "upcoming" => query.Where(voucher => voucher.IsActive && voucher.StartDate > now),
            "expired" => query.Where(voucher => voucher.EndDate < now),
            "exhausted" => query.Where(voucher => voucher.MaxUses.HasValue && voucher.UsedCount >= voucher.MaxUses.Value),
            _ => query,
        };
    }

    private async Task<List<VoucherValidationError>> ValidateFormAsync(
        VoucherFormViewModel form,
        long? existingId,
        int usedCount,
        CancellationToken cancellationToken)
    {
        var errors = new List<VoucherValidationError>();

        if (!Enum.IsDefined(form.DiscountType))
        {
            errors.Add(new VoucherValidationError(nameof(form.DiscountType), "Loại giảm giá không hợp lệ."));
        }

        if (await _db.Vouchers.AnyAsync(voucher =>
                voucher.Code == form.Code &&
                (!existingId.HasValue || voucher.Id != existingId.Value),
                cancellationToken))
        {
            errors.Add(new VoucherValidationError(nameof(form.Code), "Mã voucher đã tồn tại, hãy dùng mã khác."));
        }

        if (form.EndDate <= form.StartDate)
        {
            errors.Add(new VoucherValidationError(nameof(form.EndDate), "Ngày kết thúc phải sau ngày bắt đầu."));
        }

        if (form.DiscountType == DiscountType.Percentage && form.DiscountValue > 100)
        {
            errors.Add(new VoucherValidationError(nameof(form.DiscountValue), "Giảm theo phần trăm không được vượt quá 100%."));
        }

        if (form.DiscountType == DiscountType.FixedAmount &&
            form.MaxDiscountValue.HasValue &&
            form.MaxDiscountValue.Value < form.DiscountValue)
        {
            errors.Add(new VoucherValidationError(
                nameof(form.MaxDiscountValue),
                "Mức giảm tối đa không được nhỏ hơn giá trị giảm cố định."));
        }

        if (form.MaxUses.HasValue && form.MaxUses.Value < usedCount)
        {
            errors.Add(new VoucherValidationError(
                nameof(form.MaxUses),
                $"Tổng lượt dùng không được nhỏ hơn số lượt đã dùng ({usedCount})."));
        }

        if (form.MaxUses.HasValue &&
            form.MaxUsesPerUser.HasValue &&
            form.MaxUsesPerUser.Value > form.MaxUses.Value)
        {
            errors.Add(new VoucherValidationError(
                nameof(form.MaxUsesPerUser),
                "Lượt dùng mỗi khách không được lớn hơn tổng lượt dùng."));
        }

        return errors;
    }

    private static VoucherFormViewModel PrepareForm(VoucherFormViewModel form)
    {
        form.DiscountTypeOptions = BuildDiscountTypeOptions();
        return form;
    }

    private static List<VoucherDiscountTypeOption> BuildDiscountTypeOptions()
    {
        return new List<VoucherDiscountTypeOption>
        {
            new()
            {
                Value = DiscountType.FixedAmount.ToString(),
                Label = "Số tiền cố định",
                Hint = "Giảm trực tiếp theo VND",
            },
            new()
            {
                Value = DiscountType.Percentage.ToString(),
                Label = "Theo phần trăm",
                Hint = "Giảm theo phần trăm giá trị đơn hàng",
            },
        };
    }

    private static void NormalizeForm(VoucherFormViewModel form)
    {
        form.Code = form.Code.Trim().ToUpperInvariant();
        form.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
    }

    private static (string Key, string Label) ResolveStatus(VoucherIndexItem voucher, DateTime now)
    {
        if (!voucher.IsActive)
        {
            return ("inactive", "Tạm tắt");
        }

        if (IsExhausted(voucher))
        {
            return ("exhausted", "Hết lượt");
        }

        if (voucher.StartDate > now)
        {
            return ("upcoming", "Sắp diễn ra");
        }

        if (voucher.EndDate < now)
        {
            return ("expired", "Hết hạn");
        }

        return ("running", "Đang chạy");
    }

    private static bool IsRunning(VoucherIndexItem voucher, DateTime now)
    {
        return voucher.IsActive &&
            voucher.StartDate <= now &&
            voucher.EndDate >= now &&
            !IsExhausted(voucher);
    }

    private static bool IsExhausted(VoucherIndexItem voucher)
    {
        return voucher.MaxUses.HasValue && voucher.UsedCount >= voucher.MaxUses.Value;
    }
}
