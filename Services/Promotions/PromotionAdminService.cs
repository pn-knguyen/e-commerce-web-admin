using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.Models.Validation;
using e_commerce_web_admin.ViewModels.Promotions;
using Microsoft.EntityFrameworkCore;

namespace e_commerce_web_admin.Services.Promotions;

public sealed class PromotionAdminService : IPromotionAdminService
{
    private const int DefaultPageSize = 30;

    private readonly ApplicationDbContext _db;

    public PromotionAdminService(ApplicationDbContext db)
        => _db = db;

    private sealed class PromotionIndexItem
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int Priority { get; init; }
        public bool IsActive { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public decimal MinOrderValue { get; init; }
        public decimal? MaxDiscountValue { get; init; }
        public int? UsageLimit { get; init; }
        public int UsedCount { get; init; }
        public int TargetCount { get; init; }
        public int RuleCount { get; init; }
        public PromotionActionType ActionType { get; init; }
        public decimal DiscountValue { get; init; }
        public int BuyQuantity { get; init; }
        public int GetQuantity { get; init; }
        public string? GiftVariantLabel { get; init; }
    }

    public async Task<PromotionIndexResult> GetIndexAsync(
        PromotionIndexRequest query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var now = PromotionDateTime.UtcNow();
        var searchQuery = _db.Promotions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            searchQuery = searchQuery.Where(promotion =>
                promotion.Name.Contains(search) ||
                (promotion.Description != null && promotion.Description.Contains(search)));
        }

        var filteredQuery = ApplyStatusFilter(searchQuery, query.Status, now);

        var summary = await filteredQuery
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                ActiveCount = group.Count(promotion => promotion.IsActive),
                InactiveCount = group.Count(promotion => !promotion.IsActive),
                RunningCount = group.Count(promotion =>
                    promotion.IsActive &&
                    promotion.StartDate <= now &&
                    promotion.EndDate >= now &&
                    (!promotion.UsageLimit.HasValue || promotion.UsedCount < promotion.UsageLimit.Value)),
                UpcomingCount = group.Count(promotion => promotion.IsActive && promotion.StartDate > now),
                ExpiredCount = group.Count(promotion => promotion.EndDate < now),
                ExhaustedCount = group.Count(promotion =>
                    promotion.UsageLimit.HasValue && promotion.UsedCount >= promotion.UsageLimit.Value),
                TotalUsedCount = group.Sum(promotion => (int?)promotion.UsedCount) ?? 0,
            })
            .FirstOrDefaultAsync(ct);

        var pageItems = await filteredQuery
            .OrderByDescending(promotion => promotion.IsActive)
            .ThenByDescending(promotion => promotion.Priority)
            .ThenByDescending(promotion => promotion.StartDate)
            .ThenBy(promotion => promotion.Name)
            .Skip((page - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .Select(promotion => new PromotionIndexItem
            {
                Id = promotion.Id,
                Name = promotion.Name,
                Description = promotion.Description,
                Priority = promotion.Priority,
                IsActive = promotion.IsActive,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                MinOrderValue = promotion.MinOrderValue,
                MaxDiscountValue = promotion.MaxDiscountValue,
                UsageLimit = promotion.UsageLimit,
                UsedCount = promotion.UsedCount,
                TargetCount = promotion.PromotionTargets.Count,
                RuleCount = promotion.PromotionRules.Count,
                ActionType = promotion.PromotionRules
                    .OrderBy(rule => rule.Id)
                    .Select(rule => (PromotionActionType?)rule.ActionType)
                    .FirstOrDefault() ?? PromotionActionType.DiscountOrder,
                DiscountValue = promotion.PromotionRules
                    .OrderBy(rule => rule.Id)
                    .Select(rule => (decimal?)rule.DiscountValue)
                    .FirstOrDefault() ?? 0m,
                BuyQuantity = promotion.PromotionRules
                    .OrderBy(rule => rule.Id)
                    .Select(rule => (int?)rule.BuyQuantity)
                    .FirstOrDefault() ?? 0,
                GetQuantity = promotion.PromotionRules
                    .OrderBy(rule => rule.Id)
                    .Select(rule => (int?)rule.GetQuantity)
                    .FirstOrDefault() ?? 0,
                GiftVariantLabel = promotion.PromotionRules
                    .OrderBy(rule => rule.Id)
                    .Select(rule => rule.GiftProductVariant != null && rule.GiftProductVariant.Product != null
                        ? rule.GiftProductVariant.Product.Name + " - " + rule.GiftProductVariant.Code
                        : null)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var rows = pageItems.Select(item => new PromotionListItem
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Priority = item.Priority,
            IsActive = item.IsActive,
            StartDateUtc = item.StartDate,
            EndDateUtc = item.EndDate,
            MinOrderValue = item.MinOrderValue,
            MaxDiscountValue = item.MaxDiscountValue,
            UsageLimit = item.UsageLimit,
            UsedCount = item.UsedCount,
            TargetCount = item.TargetCount,
            RuleCount = item.RuleCount,
            ActionType = item.ActionType,
            DiscountValue = item.DiscountValue,
            BuyQuantity = item.BuyQuantity,
            GetQuantity = item.GetQuantity,
            GiftVariantLabel = item.GiftVariantLabel,
            StatusKey = ResolveStatusKey(item, now),
        }).ToList();

        return new PromotionIndexResult
        {
            Promotions = rows,
            Search = query.Search,
            Status = NormalizeStatus(query.Status),
            Page = page,
            PageSize = DefaultPageSize,
            TotalCount = summary?.TotalCount ?? 0,
            ActiveCount = summary?.ActiveCount ?? 0,
            InactiveCount = summary?.InactiveCount ?? 0,
            RunningCount = summary?.RunningCount ?? 0,
            UpcomingCount = summary?.UpcomingCount ?? 0,
            ExpiredCount = summary?.ExpiredCount ?? 0,
            ExhaustedCount = summary?.ExhaustedCount ?? 0,
            TotalUsedCount = summary?.TotalUsedCount ?? 0,
        };
    }

    public PromotionFormData GetCreateForm()
    {
        var localNow = PromotionDateTime.ToAdminLocal(PromotionDateTime.UtcNow());
        var startLocal = new DateTime(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            localNow.Hour,
            0,
            0,
            DateTimeKind.Unspecified);
        var endLocal = startLocal.Date.AddDays(30).AddHours(23).AddMinutes(59);

        return new PromotionFormData
        {
            ActionType = PromotionActionType.DiscountOrder,
            DiscountValue = 100000m,
            BuyQuantity = 1,
            GetQuantity = 0,
            MinOrderValue = 0m,
            MaxDiscountValue = 100000m,
            StartDateUtc = PromotionDateTime.FromAdminLocal(startLocal),
            EndDateUtc = PromotionDateTime.FromAdminLocal(endLocal),
            Priority = 0,
            TargetType = TargetType.Category,
            IsActive = true,
        };
    }

    public async Task<PromotionFormData?> GetEditFormAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Promotions
            .AsNoTracking()
            .Include(promotion => promotion.PromotionTargets)
            .Include(promotion => promotion.PromotionRules)
            .FirstOrDefaultAsync(promotion => promotion.Id == id, ct);

        if (entity is null)
        {
            return null;
        }

        var rule = entity.PromotionRules.OrderBy(item => item.Id).FirstOrDefault();
        var selectedTargetType = entity.PromotionTargets
            .OrderBy(target => target.TargetType)
            .ThenBy(target => target.TargetId)
            .Select(target => (TargetType?)target.TargetType)
            .FirstOrDefault();

        var targetType = selectedTargetType ?? TargetType.Category;

        return new PromotionFormData
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Priority = entity.Priority,
            IsActive = entity.IsActive,
            StartDateUtc = entity.StartDate,
            EndDateUtc = entity.EndDate,
            MinOrderValue = entity.MinOrderValue,
            MaxDiscountValue = entity.MaxDiscountValue,
            UsageLimit = entity.UsageLimit,
            UsedCount = entity.UsedCount,
            TargetType = targetType,
            TargetIds = entity.PromotionTargets
                .Where(target => target.TargetType == targetType)
                .OrderBy(target => target.TargetId)
                .Select(target => target.TargetId)
                .ToList(),
            RuleId = rule?.Id,
            GiftProductVariantId = rule?.GiftProductVariantId,
            ActionType = rule?.ActionType ?? PromotionActionType.DiscountOrder,
            DiscountValue = rule?.DiscountValue ?? 0m,
            BuyQuantity = rule?.BuyQuantity ?? 1,
            GetQuantity = rule?.GetQuantity ?? 0,
        };
    }

    public async Task<IReadOnlyList<PromotionGiftVariantOption>> GetGiftVariantOptionsAsync(CancellationToken ct = default)
    {
        return await _db.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.IsActive && variant.Product != null && variant.Product.IsActive)
            .OrderBy(variant => variant.Product!.Name)
            .ThenBy(variant => variant.Code)
            .Select(variant => new PromotionGiftVariantOption
            {
                Value = variant.Id,
                Text = variant.Product != null
                    ? variant.Product.Name + " - " + variant.Code
                    : variant.Code,
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PromotionTargetOption>> GetTargetOptionsAsync(CancellationToken ct = default)
    {
        var options = new List<PromotionTargetOption>();

        options.AddRange(await _db.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new PromotionTargetOption
            {
                TargetType = TargetType.Category,
                Value = category.Id,
                Text = category.Name,
            })
            .ToListAsync(ct));

        options.AddRange(await _db.Brands
            .AsNoTracking()
            .Where(brand => brand.IsActive)
            .OrderBy(brand => brand.Name)
            .Select(brand => new PromotionTargetOption
            {
                TargetType = TargetType.Brand,
                Value = brand.Id,
                Text = brand.Name,
            })
            .ToListAsync(ct));

        options.AddRange(await _db.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .Select(product => new PromotionTargetOption
            {
                TargetType = TargetType.Product,
                Value = product.Id,
                Text = product.Name,
            })
            .ToListAsync(ct));

        options.AddRange(await _db.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.IsActive && variant.Product != null && variant.Product.IsActive)
            .OrderBy(variant => variant.Product!.Name)
            .ThenBy(variant => variant.Code)
            .Select(variant => new PromotionTargetOption
            {
                TargetType = TargetType.ProductVariant,
                Value = variant.Id,
                Text = variant.Product != null
                    ? variant.Product.Name + " - " + variant.Code
                    : variant.Code,
            })
            .ToListAsync(ct));

        return options;
    }

    public async Task<PromotionSaveResult> CreateAsync(
        PromotionFormData form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);

        var errors = await ValidateFormAsync(form, usedCount: 0, ct);
        if (errors.Count > 0)
        {
            return PromotionSaveResult.Failed(form, errors);
        }

        var entity = new Promotion
        {
            Name = form.Name,
            Description = form.Description,
            Priority = form.Priority,
            IsActive = form.IsActive,
            StartDate = form.StartDateUtc,
            EndDate = form.EndDateUtc,
            MinOrderValue = form.MinOrderValue,
            MaxDiscountValue = form.MaxDiscountValue,
            UsageLimit = form.UsageLimit,
            CreatedAt = PromotionDateTime.UtcNow(),
        };

        foreach (var target in BuildTargets(form))
        {
            entity.PromotionTargets.Add(target);
        }

        entity.PromotionRules.Add(BuildRule(form));

        _db.Promotions.Add(entity);
        await _db.SaveChangesAsync(ct);

        form.Id = entity.Id;
        return PromotionSaveResult.Success(form, $"Đã tạo khuyến mãi \"{entity.Name}\" thành công.");
    }

    public async Task<PromotionSaveResult> UpdateAsync(
        long id,
        PromotionFormData form,
        CancellationToken ct = default)
    {
        NormalizeForm(form);

        var entity = await _db.Promotions
            .Include(promotion => promotion.PromotionRules)
            .Include(promotion => promotion.PromotionTargets)
            .FirstOrDefaultAsync(promotion => promotion.Id == id, ct);

        if (entity is null)
        {
            return PromotionSaveResult.Failed(
                form,
                new[] { new PromotionValidationError(string.Empty, "Không tìm thấy khuyến mãi.") });
        }

        var errors = await ValidateFormAsync(form, entity.UsedCount, ct);
        if (errors.Count > 0)
        {
            form.UsedCount = entity.UsedCount;
            return PromotionSaveResult.Failed(form, errors);
        }

        entity.Name = form.Name;
        entity.Description = form.Description;
        entity.Priority = form.Priority;
        entity.IsActive = form.IsActive;
        entity.StartDate = form.StartDateUtc;
        entity.EndDate = form.EndDateUtc;
        entity.MinOrderValue = form.MinOrderValue;
        entity.MaxDiscountValue = form.MaxDiscountValue;
        entity.UsageLimit = form.UsageLimit;
        entity.UpdatedAt = PromotionDateTime.UtcNow();

        SyncPromotionTargets(entity, form);

        var rule = entity.PromotionRules.OrderBy(item => item.Id).FirstOrDefault();
        if (rule is null)
        {
            entity.PromotionRules.Add(BuildRule(form));
        }
        else
        {
            ApplyRule(rule, form);
        }

        await _db.SaveChangesAsync(ct);

        form.UsedCount = entity.UsedCount;
        return PromotionSaveResult.Success(form, $"Đã cập nhật khuyến mãi \"{entity.Name}\" thành công.");
    }

    public async Task<PromotionDeleteResult> DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Promotions
            .Include(promotion => promotion.PromotionTargets)
            .Include(promotion => promotion.PromotionRules)
            .FirstOrDefaultAsync(promotion => promotion.Id == id, ct);

        if (entity is null)
        {
            return PromotionDeleteResult.NotFound();
        }

        if (entity.UsedCount > 0)
        {
            return PromotionDeleteResult.Failed(
                $"Không thể xoá \"{entity.Name}\" vì khuyến mãi đã phát sinh lượt sử dụng.");
        }

        if (entity.PromotionTargets.Count > 0)
        {
            _db.PromotionTargets.RemoveRange(entity.PromotionTargets);
        }

        if (entity.PromotionRules.Count > 0)
        {
            _db.PromotionRules.RemoveRange(entity.PromotionRules);
        }

        _db.Promotions.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return PromotionDeleteResult.Success($"Đã xoá khuyến mãi \"{entity.Name}\" thành công.");
    }

    public async Task<PromotionToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.Promotions.FirstOrDefaultAsync(promotion => promotion.Id == id, ct);
        if (entity is null)
        {
            return null;
        }

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = PromotionDateTime.UtcNow();
        await _db.SaveChangesAsync(ct);

        return new PromotionToggleResult(entity.IsActive);
    }

    private static IQueryable<Promotion> ApplyStatusFilter(
        IQueryable<Promotion> query,
        string? status,
        DateTime now)
    {
        return NormalizeStatus(status) switch
        {
            "active" => query.Where(promotion => promotion.IsActive),
            "inactive" => query.Where(promotion => !promotion.IsActive),
            "running" => query.Where(promotion =>
                promotion.IsActive &&
                promotion.StartDate <= now &&
                promotion.EndDate >= now &&
                (!promotion.UsageLimit.HasValue || promotion.UsedCount < promotion.UsageLimit.Value)),
            "upcoming" => query.Where(promotion => promotion.IsActive && promotion.StartDate > now),
            "expired" => query.Where(promotion => promotion.EndDate < now),
            "exhausted" => query.Where(promotion =>
                promotion.UsageLimit.HasValue && promotion.UsedCount >= promotion.UsageLimit.Value),
            _ => query,
        };
    }

    private async Task<List<PromotionValidationError>> ValidateFormAsync(
        PromotionFormData form,
        int usedCount,
        CancellationToken ct)
    {
        var errors = new List<PromotionValidationError>();

        if (string.IsNullOrWhiteSpace(form.Name))
        {
            errors.Add(new PromotionValidationError(nameof(form.Name), PromotionValidationMessages.NameRequired));
        }
        else if (form.Name.Length > PromotionValidationRules.NameMaxLength)
        {
            errors.Add(new PromotionValidationError(nameof(form.Name), PromotionValidationMessages.NameMaxLength));
        }

        if (form.Description is { Length: > PromotionValidationRules.DescriptionMaxLength })
        {
            errors.Add(new PromotionValidationError(
                nameof(form.Description),
                PromotionValidationMessages.DescriptionMaxLength));
        }

        if (form.MinOrderValue < 0)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.MinOrderValue),
                PromotionValidationMessages.MinOrderNonNegative));
        }

        if (form.MaxDiscountValue.HasValue && form.MaxDiscountValue.Value <= 0)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.MaxDiscountValue),
                PromotionValidationMessages.MaxDiscountPositive));
        }

        if (form.UsageLimit.HasValue && form.UsageLimit.Value <= 0)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.UsageLimit),
                PromotionValidationMessages.UsageLimitPositive));
        }

        if (form.UsageLimit.HasValue && form.UsageLimit.Value < usedCount)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.UsageLimit),
                string.Format(PromotionValidationMessages.UsageLimitLessThanUsed, usedCount)));
        }

        if (form.Priority < PromotionValidationRules.PriorityMin || form.Priority > PromotionValidationRules.PriorityMax)
        {
            errors.Add(new PromotionValidationError(nameof(form.Priority), PromotionValidationMessages.PriorityRange));
        }

        if (form.EndDateUtc <= form.StartDateUtc)
        {
            errors.Add(new PromotionValidationError(nameof(form.EndDateUtc), PromotionValidationMessages.EndDateAfterStart));
        }

        await ValidateTargetsAsync(form, errors, ct);

        if (!Enum.IsDefined(form.ActionType))
        {
            errors.Add(new PromotionValidationError(nameof(form.ActionType), PromotionValidationMessages.ActionTypeInvalid));
            return errors;
        }

        ValidateRuleNumbers(form, errors);
        await ValidateGiftVariantAsync(form, errors, ct);

        return errors;
    }

    private async Task ValidateTargetsAsync(
        PromotionFormData form,
        ICollection<PromotionValidationError> errors,
        CancellationToken ct)
    {
        if (!IsSupportedTargetType(form.TargetType))
        {
            errors.Add(new PromotionValidationError(nameof(form.TargetType), PromotionValidationMessages.TargetTypeInvalid));
            return;
        }

        if (form.TargetIds.Count == 0)
        {
            errors.Add(new PromotionValidationError(nameof(form.TargetIds), PromotionValidationMessages.TargetRequired));
            return;
        }

        if (form.TargetIds.Any(targetId => targetId <= 0))
        {
            errors.Add(new PromotionValidationError(nameof(form.TargetIds), PromotionValidationMessages.TargetInvalid));
            return;
        }

        var targetCount = await CountTargetMatchesAsync(form.TargetType, form.TargetIds, ct);
        if (targetCount != form.TargetIds.Count)
        {
            errors.Add(new PromotionValidationError(nameof(form.TargetIds), PromotionValidationMessages.TargetInvalid));
        }
    }

    private Task<int> CountTargetMatchesAsync(
        TargetType targetType,
        IReadOnlyCollection<long> targetIds,
        CancellationToken ct)
    {
        return targetType switch
        {
            TargetType.Category => _db.Categories.CountAsync(
                category => category.IsActive && targetIds.Contains(category.Id),
                ct),
            TargetType.Brand => _db.Brands.CountAsync(
                brand => brand.IsActive && targetIds.Contains(brand.Id),
                ct),
            TargetType.Product => _db.Products.CountAsync(
                product => product.IsActive && targetIds.Contains(product.Id),
                ct),
            TargetType.ProductVariant => _db.ProductVariants.CountAsync(
                variant => variant.IsActive &&
                    variant.Product != null &&
                    variant.Product.IsActive &&
                    targetIds.Contains(variant.Id),
                ct),
            _ => Task.FromResult(0),
        };
    }

    private static void ValidateRuleNumbers(
        PromotionFormData form,
        ICollection<PromotionValidationError> errors)
    {
        if (form.DiscountValue < 0)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.DiscountValue),
                PromotionValidationMessages.DiscountValueNonNegative));
        }
        else if (form.ActionType is PromotionActionType.DiscountOrder or PromotionActionType.DiscountProduct &&
            form.DiscountValue <= 0)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.DiscountValue),
                PromotionValidationMessages.DiscountValuePositive));
        }

        if (form.BuyQuantity <= 0)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.BuyQuantity),
                PromotionValidationMessages.BuyQuantityPositive));
        }

        if (form.GetQuantity < 0)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.GetQuantity),
                PromotionValidationMessages.GetQuantityNonNegative));
        }

        if (form.ActionType == PromotionActionType.GiftProduct && form.GetQuantity <= 0)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.GetQuantity),
                PromotionValidationMessages.GiftQuantityPositive));
        }

        if (form.ActionType == PromotionActionType.BuyXGetY &&
            form.DiscountValue <= 0 &&
            form.GetQuantity <= 0)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.DiscountValue),
                PromotionValidationMessages.BuyXGetYRequiresBenefit));
        }

        if (form.ActionType is PromotionActionType.DiscountOrder or PromotionActionType.DiscountProduct or PromotionActionType.BuyXGetY &&
            form.DiscountValue > 0 &&
            form.MaxDiscountValue.HasValue &&
            form.MaxDiscountValue.Value < form.DiscountValue)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.MaxDiscountValue),
                PromotionValidationMessages.MaxDiscountLessThanDiscount));
        }
    }

    private async Task ValidateGiftVariantAsync(
        PromotionFormData form,
        ICollection<PromotionValidationError> errors,
        CancellationToken ct)
    {
        if (form.ActionType != PromotionActionType.GiftProduct)
        {
            return;
        }

        if (!form.GiftProductVariantId.HasValue)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.GiftProductVariantId),
                PromotionValidationMessages.GiftVariantRequired));
            return;
        }

        var exists = await _db.ProductVariants.AnyAsync(
            variant => variant.Id == form.GiftProductVariantId.Value,
            ct);

        if (!exists)
        {
            errors.Add(new PromotionValidationError(
                nameof(form.GiftProductVariantId),
                PromotionValidationMessages.GiftVariantInvalid));
        }
    }

    private static IEnumerable<PromotionTarget> BuildTargets(PromotionFormData form)
    {
        return form.TargetIds.Select(targetId => new PromotionTarget
        {
            TargetType = form.TargetType,
            TargetId = targetId,
        });
    }

    private void SyncPromotionTargets(Promotion entity, PromotionFormData form)
    {
        var requestedTargets = form.TargetIds
            .Select(targetId => (form.TargetType, TargetId: targetId))
            .ToHashSet();
        var staleTargets = entity.PromotionTargets
            .Where(target => !requestedTargets.Contains((target.TargetType, target.TargetId)))
            .ToList();

        foreach (var target in staleTargets)
        {
            entity.PromotionTargets.Remove(target);
        }

        if (staleTargets.Count > 0)
        {
            _db.PromotionTargets.RemoveRange(staleTargets);
        }

        var existingTargets = entity.PromotionTargets
            .Select(target => (target.TargetType, target.TargetId))
            .ToHashSet();

        foreach (var targetId in form.TargetIds)
        {
            if (!existingTargets.Contains((form.TargetType, targetId)))
            {
                entity.PromotionTargets.Add(new PromotionTarget
                {
                    TargetType = form.TargetType,
                    TargetId = targetId,
                });
            }
        }
    }

    private static PromotionRule BuildRule(PromotionFormData form)
    {
        var rule = new PromotionRule();
        ApplyRule(rule, form);
        return rule;
    }

    private static void ApplyRule(PromotionRule rule, PromotionFormData form)
    {
        rule.ActionType = form.ActionType;
        rule.DiscountValue = form.DiscountValue;
        rule.BuyQuantity = form.BuyQuantity;
        rule.GetQuantity = form.GetQuantity;
        rule.GiftProductVariantId = form.ActionType == PromotionActionType.GiftProduct
            ? form.GiftProductVariantId
            : null;
    }

    private static void NormalizeForm(PromotionFormData form)
    {
        form.Name = form.Name.Trim();
        form.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        form.TargetIds = form.TargetIds.Distinct().ToList();
        form.StartDateUtc = DateTime.SpecifyKind(form.StartDateUtc, DateTimeKind.Utc);
        form.EndDateUtc = DateTime.SpecifyKind(form.EndDateUtc, DateTimeKind.Utc);

        if (form.ActionType != PromotionActionType.GiftProduct)
        {
            form.GiftProductVariantId = null;
        }
    }

    private static bool IsSupportedTargetType(TargetType targetType)
    {
        return targetType is TargetType.Category
            or TargetType.Brand
            or TargetType.Product
            or TargetType.ProductVariant;
    }

    private static string? NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "active" => "active",
            "inactive" => "inactive",
            "running" => "running",
            "upcoming" => "upcoming",
            "expired" => "expired",
            "exhausted" => "exhausted",
            _ => null,
        };
    }

    private static string ResolveStatusKey(PromotionIndexItem promotion, DateTime now)
    {
        if (!promotion.IsActive)
        {
            return "inactive";
        }

        if (promotion.UsageLimit.HasValue && promotion.UsedCount >= promotion.UsageLimit.Value)
        {
            return "exhausted";
        }

        if (promotion.StartDate > now)
        {
            return "upcoming";
        }

        if (promotion.EndDate < now)
        {
            return "expired";
        }

        return "running";
    }
}
