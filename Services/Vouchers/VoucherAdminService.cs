using System.Text.RegularExpressions;
using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.Models.Validation;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Vouchers;

public sealed class VoucherAdminService : IVoucherAdminService
{
    private const int DefaultPageSize = 30;

    private static readonly Regex CodeRegex = new(
        VoucherValidationRules.CodePattern,
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

    public async Task<VoucherIndexResult> GetIndexAsync(
        VoucherIndexRequest query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var now = VoucherDateTime.UtcNow();
        var searchQuery = _db.Vouchers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            searchQuery = searchQuery.Where(voucher =>
                voucher.Code.Contains(search) ||
                (voucher.Description != null && voucher.Description.Contains(search)));
        }

        var filteredQuery = ApplyStatusFilter(searchQuery, query.Status, now);

        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var activeCount = await filteredQuery.CountAsync(voucher => voucher.IsActive, cancellationToken);
        var inactiveCount = await filteredQuery.CountAsync(voucher => !voucher.IsActive, cancellationToken);
        var runningCount = await filteredQuery.CountAsync(voucher =>
            voucher.IsActive &&
            voucher.StartDate <= now &&
            voucher.EndDate >= now &&
            (!voucher.MaxUses.HasValue || voucher.UsedCount < voucher.MaxUses.Value),
            cancellationToken);
        var upcomingCount = await filteredQuery.CountAsync(
            voucher => voucher.IsActive && voucher.StartDate > now,
            cancellationToken);
        var expiredCount = await filteredQuery.CountAsync(voucher => voucher.EndDate < now, cancellationToken);
        var exhaustedCount = await filteredQuery.CountAsync(
            voucher => voucher.MaxUses.HasValue && voucher.UsedCount >= voucher.MaxUses.Value,
            cancellationToken);
        var totalUsedCount = await filteredQuery.SumAsync(voucher => (int?)voucher.UsedCount, cancellationToken) ?? 0;

        var pageItems = await filteredQuery
            .OrderByDescending(voucher => voucher.IsActive)
            .ThenByDescending(voucher => voucher.Priority)
            .ThenByDescending(voucher => voucher.StartDate)
            .ThenBy(voucher => voucher.Code)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
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

        var rows = pageItems.Select(item =>
        {
            return new VoucherListItem
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
                StartDateUtc = item.StartDate,
                EndDateUtc = item.EndDate,
                Priority = item.Priority,
                IsActive = item.IsActive,
                OrderCount = item.OrderCount,
                UsageCount = item.UsageCount,
                AssignedUserCount = item.AssignedUserCount,
                TargetCount = item.TargetCount,
                StatusKey = ResolveStatusKey(item, now),
            };
        }).ToList();

        return new VoucherIndexResult
        {
            Vouchers = rows,
            Search = query.Search,
            Status = query.Status,
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount,
            RunningCount = runningCount,
            UpcomingCount = upcomingCount,
            ExpiredCount = expiredCount,
            ExhaustedCount = exhaustedCount,
            TotalUsedCount = totalUsedCount,
        };
    }

    public VoucherFormData GetCreateForm()
    {
        var localNow = VoucherDateTime.ToAdminLocal(VoucherDateTime.UtcNow());
        var startLocal = new DateTime(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            localNow.Hour,
            0,
            0,
            DateTimeKind.Unspecified);
        var endLocal = startLocal.Date.AddDays(30).AddHours(23).AddMinutes(59);

        return new VoucherFormData
        {
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 100000m,
            MinOrderValue = 0m,
            MaxDiscountValue = 100000m,
            StartDateUtc = VoucherDateTime.FromAdminLocal(startLocal),
            EndDateUtc = VoucherDateTime.FromAdminLocal(endLocal),
            Priority = 0,
            IsActive = true,
        };
    }

    public async Task<VoucherFormData?> GetEditFormAsync(
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

        return new VoucherFormData
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
            StartDateUtc = entity.StartDate,
            EndDateUtc = entity.EndDate,
            Priority = entity.Priority,
            IsActive = entity.IsActive,
        };
    }

    public async Task<VoucherSaveResult> CreateAsync(
        VoucherFormData form,
        CancellationToken cancellationToken = default)
    {
        NormalizeForm(form);

        var errors = await ValidateFormAsync(form, existingId: null, usedCount: 0, cancellationToken);
        if (errors.Count > 0)
        {
            return VoucherSaveResult.Failed(form, errors);
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
            StartDate = form.StartDateUtc,
            EndDate = form.EndDateUtc,
            Priority = form.Priority,
            IsActive = form.IsActive,
            CreatedAt = VoucherDateTime.UtcNow(),
        };

        _db.Vouchers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        form.Id = entity.Id;
        return VoucherSaveResult.Success(form, $"Đã tạo voucher \"{entity.Code}\" thành công.");
    }

    public async Task<VoucherSaveResult> UpdateAsync(
        long id,
        VoucherFormData form,
        CancellationToken cancellationToken = default)
    {
        NormalizeForm(form);

        var entity = await _db.Vouchers.FirstOrDefaultAsync(voucher => voucher.Id == id, cancellationToken);
        if (entity is null)
        {
            return VoucherSaveResult.Failed(
                form,
                new[] { new VoucherValidationError(string.Empty, "Không tìm thấy voucher.") });
        }

        var errors = await ValidateFormAsync(form, id, entity.UsedCount, cancellationToken);
        if (errors.Count > 0)
        {
            form.UsedCount = entity.UsedCount;
            return VoucherSaveResult.Failed(form, errors);
        }

        entity.Code = form.Code;
        entity.Description = form.Description;
        entity.DiscountType = form.DiscountType;
        entity.DiscountValue = form.DiscountValue;
        entity.MinOrderValue = form.MinOrderValue;
        entity.MaxDiscountValue = form.MaxDiscountValue;
        entity.MaxUses = form.MaxUses;
        entity.MaxUsesPerUser = form.MaxUsesPerUser;
        entity.StartDate = form.StartDateUtc;
        entity.EndDate = form.EndDateUtc;
        entity.Priority = form.Priority;
        entity.IsActive = form.IsActive;
        entity.UpdatedAt = VoucherDateTime.UtcNow();

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
        entity.UpdatedAt = VoucherDateTime.UtcNow();
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
        VoucherFormData form,
        long? existingId,
        int usedCount,
        CancellationToken cancellationToken)
    {
        var errors = new List<VoucherValidationError>();
        var isCodeValid = ValidateCode(form, errors);

        if (!Enum.IsDefined(form.DiscountType))
        {
            errors.Add(new VoucherValidationError(nameof(form.DiscountType), VoucherValidationMessages.DiscountTypeInvalid));
        }

        if (form.DiscountValue <= 0)
        {
            errors.Add(new VoucherValidationError(nameof(form.DiscountValue), VoucherValidationMessages.DiscountValuePositive));
        }

        if (form.MinOrderValue < 0)
        {
            errors.Add(new VoucherValidationError(nameof(form.MinOrderValue), VoucherValidationMessages.MinOrderNonNegative));
        }

        if (form.MaxDiscountValue.HasValue && form.MaxDiscountValue.Value <= 0)
        {
            errors.Add(new VoucherValidationError(nameof(form.MaxDiscountValue), VoucherValidationMessages.MaxDiscountPositive));
        }

        if (form.MaxUses.HasValue && form.MaxUses.Value <= 0)
        {
            errors.Add(new VoucherValidationError(nameof(form.MaxUses), VoucherValidationMessages.MaxUsesPositive));
        }

        if (form.MaxUsesPerUser.HasValue && form.MaxUsesPerUser.Value <= 0)
        {
            errors.Add(new VoucherValidationError(nameof(form.MaxUsesPerUser), VoucherValidationMessages.MaxUsesPerUserPositive));
        }

        if (form.Priority < VoucherValidationRules.PriorityMin || form.Priority > VoucherValidationRules.PriorityMax)
        {
            errors.Add(new VoucherValidationError(nameof(form.Priority), VoucherValidationMessages.PriorityRange));
        }

        if (form.EndDateUtc <= form.StartDateUtc)
        {
            errors.Add(new VoucherValidationError(nameof(form.EndDateUtc), VoucherValidationMessages.EndDateAfterStart));
        }

        if (form.DiscountType == DiscountType.Percentage &&
            form.DiscountValue > VoucherValidationRules.PercentageDiscountMax)
        {
            errors.Add(new VoucherValidationError(nameof(form.DiscountValue), VoucherValidationMessages.PercentageDiscountMax));
        }

        if (form.DiscountType == DiscountType.FixedAmount &&
            form.MaxDiscountValue.HasValue &&
            form.MaxDiscountValue.Value < form.DiscountValue)
        {
            errors.Add(new VoucherValidationError(nameof(form.MaxDiscountValue), VoucherValidationMessages.FixedMaxDiscount));
        }

        if (form.MaxUses.HasValue && form.MaxUses.Value < usedCount)
        {
            errors.Add(new VoucherValidationError(
                nameof(form.MaxUses),
                string.Format(VoucherValidationMessages.MaxUsesLessThanUsed, usedCount)));
        }

        if (form.MaxUses.HasValue &&
            form.MaxUsesPerUser.HasValue &&
            form.MaxUsesPerUser.Value > form.MaxUses.Value)
        {
            errors.Add(new VoucherValidationError(
                nameof(form.MaxUsesPerUser),
                VoucherValidationMessages.MaxUsesPerUserExceedsMaxUses));
        }

        if (isCodeValid &&
            await _db.Vouchers.AnyAsync(voucher =>
                voucher.Code == form.Code &&
                (!existingId.HasValue || voucher.Id != existingId.Value),
                cancellationToken))
        {
            errors.Add(new VoucherValidationError(nameof(form.Code), VoucherValidationMessages.DuplicateCode));
        }

        return errors;
    }

    private static bool ValidateCode(VoucherFormData form, ICollection<VoucherValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(form.Code))
        {
            errors.Add(new VoucherValidationError(nameof(form.Code), VoucherValidationMessages.CodeRequired));
            return false;
        }

        if (form.Code.Length > VoucherValidationRules.CodeMaxLength)
        {
            errors.Add(new VoucherValidationError(nameof(form.Code), VoucherValidationMessages.CodeMaxLength));
            return false;
        }

        if (!CodeRegex.IsMatch(form.Code))
        {
            errors.Add(new VoucherValidationError(nameof(form.Code), VoucherValidationMessages.CodePattern));
            return false;
        }

        return true;
    }

    private static void NormalizeForm(VoucherFormData form)
    {
        form.Code = form.Code.Trim().ToUpperInvariant();
        form.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        form.StartDateUtc = DateTime.SpecifyKind(form.StartDateUtc, DateTimeKind.Utc);
        form.EndDateUtc = DateTime.SpecifyKind(form.EndDateUtc, DateTimeKind.Utc);
    }

    private static string ResolveStatusKey(VoucherIndexItem voucher, DateTime now)
    {
        if (!voucher.IsActive)
        {
            return "inactive";
        }

        if (IsExhausted(voucher))
        {
            return "exhausted";
        }

        if (voucher.StartDate > now)
        {
            return "upcoming";
        }

        if (voucher.EndDate < now)
        {
            return "expired";
        }

        return "running";
    }

    private static bool IsExhausted(VoucherIndexItem voucher)
    {
        return voucher.MaxUses.HasValue && voucher.UsedCount >= voucher.MaxUses.Value;
    }
}
