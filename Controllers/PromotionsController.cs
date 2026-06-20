using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.Services.Promotions;
using e_commerce_web_admin.ViewModels.Promotions;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Promotions", Permissions.View)]
public sealed class PromotionsController : Controller
{
    private readonly IPromotionAdminService _promotionService;

    public PromotionsController(IPromotionAdminService promotionService)
        => _promotionService = promotionService;

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        int page = 1,
        CancellationToken ct = default)
    {
        var result = await _promotionService.GetIndexAsync(
            new PromotionIndexRequest
            {
                Search = search,
                Status = status,
                Page = page,
            },
            ct);

        return View(ToIndexViewModel(result));
    }

    [RbacAuthorize("Promotions", Permissions.Create)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var viewModel = ToFormViewModel(_promotionService.GetCreateForm());
        return View(await PrepareFormAsync(viewModel, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Promotions", Permissions.Create)]
    public async Task<IActionResult> Create(
        PromotionFormViewModel viewModel,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(await PrepareFormAsync(viewModel, ct));
        }

        var result = await _promotionService.CreateAsync(ToFormData(viewModel), ct);
        if (!result.Succeeded)
        {
            AddValidationErrors(result.Errors);
            return View(await PrepareFormAsync(ToFormViewModel(result.Form), ct));
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [RbacAuthorize("Promotions", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var form = await _promotionService.GetEditFormAsync(id, ct);
        return form is null
            ? NotFound()
            : View(await PrepareFormAsync(ToFormViewModel(form), ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Promotions", Permissions.Edit)]
    public async Task<IActionResult> Edit(
        long id,
        PromotionFormViewModel viewModel,
        CancellationToken ct)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(await PrepareFormAsync(viewModel, ct));
        }

        var result = await _promotionService.UpdateAsync(id, ToFormData(viewModel), ct);
        if (!result.Succeeded)
        {
            AddValidationErrors(result.Errors);
            return View(await PrepareFormAsync(ToFormViewModel(result.Form), ct));
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Promotions", Permissions.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _promotionService.DeleteAsync(id, ct);
        if (!result.Found)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Promotions", Permissions.Edit)]
    public async Task<IActionResult> ToggleActive(long id, CancellationToken ct)
    {
        var result = await _promotionService.ToggleActiveAsync(id, ct);
        return result is null ? NotFound() : Ok(new { isActive = result.IsActive });
    }

    private static PromotionIndexViewModel ToIndexViewModel(PromotionIndexResult result)
    {
        return new PromotionIndexViewModel
        {
            Promotions = result.Promotions.Select(ToRowViewModel).ToList(),
            Search = result.Search,
            Status = result.Status,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            ActiveCount = result.ActiveCount,
            InactiveCount = result.InactiveCount,
            RunningCount = result.RunningCount,
            UpcomingCount = result.UpcomingCount,
            ExpiredCount = result.ExpiredCount,
            ExhaustedCount = result.ExhaustedCount,
            TotalUsedCount = result.TotalUsedCount,
        };
    }

    private static PromotionRowViewModel ToRowViewModel(PromotionListItem item)
    {
        return new PromotionRowViewModel
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Priority = item.Priority,
            IsActive = item.IsActive,
            StartDate = PromotionDateTime.ToAdminLocal(item.StartDateUtc),
            EndDate = PromotionDateTime.ToAdminLocal(item.EndDateUtc),
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
            StatusKey = item.StatusKey,
            StatusLabel = PromotionDisplay.GetStatusLabel(item.StatusKey),
        };
    }

    private static PromotionFormViewModel ToFormViewModel(PromotionFormData form)
    {
        return new PromotionFormViewModel
        {
            Id = form.Id,
            Name = form.Name,
            Description = form.Description,
            Priority = form.Priority,
            IsActive = form.IsActive,
            StartDate = PromotionDateTime.ToAdminLocal(form.StartDateUtc),
            EndDate = PromotionDateTime.ToAdminLocal(form.EndDateUtc),
            MinOrderValue = form.MinOrderValue,
            MaxDiscountValue = form.MaxDiscountValue,
            UsageLimit = form.UsageLimit,
            UsedCount = form.UsedCount,
            TargetType = form.TargetType,
            TargetIds = form.TargetIds.ToList(),
            RuleId = form.RuleId,
            GiftProductVariantId = form.GiftProductVariantId,
            ActionType = form.ActionType,
            DiscountValue = form.DiscountValue,
            BuyQuantity = form.BuyQuantity,
            GetQuantity = form.GetQuantity,
        };
    }

    private static PromotionFormData ToFormData(PromotionFormViewModel viewModel)
    {
        return new PromotionFormData
        {
            Id = viewModel.Id,
            Name = viewModel.Name,
            Description = viewModel.Description,
            Priority = viewModel.Priority ?? 0,
            IsActive = viewModel.IsActive,
            StartDateUtc = PromotionDateTime.FromAdminLocal(viewModel.StartDate ?? DateTime.MinValue),
            EndDateUtc = PromotionDateTime.FromAdminLocal(viewModel.EndDate ?? DateTime.MinValue),
            MinOrderValue = viewModel.MinOrderValue ?? 0m,
            MaxDiscountValue = viewModel.MaxDiscountValue,
            UsageLimit = viewModel.UsageLimit,
            UsedCount = viewModel.UsedCount,
            TargetType = viewModel.TargetType,
            TargetIds = viewModel.TargetIds.ToList(),
            RuleId = viewModel.RuleId,
            GiftProductVariantId = viewModel.GiftProductVariantId,
            ActionType = viewModel.ActionType,
            DiscountValue = viewModel.DiscountValue ?? 0m,
            BuyQuantity = viewModel.BuyQuantity ?? 0,
            GetQuantity = viewModel.GetQuantity ?? 0,
        };
    }

    private async Task<PromotionFormViewModel> PrepareFormAsync(
        PromotionFormViewModel viewModel,
        CancellationToken ct)
    {
        viewModel.ActionTypeOptions = BuildActionTypeOptions();
        viewModel.TargetTypeOptions = BuildTargetTypeOptions();
        viewModel.TargetOptions = await _promotionService.GetTargetOptionsAsync(ct);
        viewModel.GiftVariantOptions = await _promotionService.GetGiftVariantOptionsAsync(ct);
        return viewModel;
    }

    private static List<PromotionTargetTypeOption> BuildTargetTypeOptions()
    {
        return new List<PromotionTargetTypeOption>
        {
            new()
            {
                Value = TargetType.Category.ToString(),
                Label = PromotionDisplay.GetTargetTypeLabel(TargetType.Category),
                Hint = "Áp dụng cho toàn bộ sản phẩm thuộc danh mục được chọn",
            },
            new()
            {
                Value = TargetType.Brand.ToString(),
                Label = PromotionDisplay.GetTargetTypeLabel(TargetType.Brand),
                Hint = "Áp dụng cho sản phẩm thuộc thương hiệu được chọn",
            },
            new()
            {
                Value = TargetType.Product.ToString(),
                Label = PromotionDisplay.GetTargetTypeLabel(TargetType.Product),
                Hint = "Áp dụng cho từng sản phẩm cụ thể",
            },
            new()
            {
                Value = TargetType.ProductVariant.ToString(),
                Label = PromotionDisplay.GetTargetTypeLabel(TargetType.ProductVariant),
                Hint = "Áp dụng chính xác cho biến thể/SKU được chọn",
            },
        };
    }

    private static List<PromotionActionTypeOption> BuildActionTypeOptions()
    {
        return new List<PromotionActionTypeOption>
        {
            new()
            {
                Value = PromotionActionType.DiscountOrder.ToString(),
                Label = PromotionDisplay.GetActionTypeLabel(PromotionActionType.DiscountOrder),
                Hint = "Giảm trực tiếp trên tổng đơn hàng",
            },
            new()
            {
                Value = PromotionActionType.DiscountProduct.ToString(),
                Label = PromotionDisplay.GetActionTypeLabel(PromotionActionType.DiscountProduct),
                Hint = "Giảm trên sản phẩm thuộc phạm vi áp dụng",
            },
            new()
            {
                Value = PromotionActionType.BuyXGetY.ToString(),
                Label = PromotionDisplay.GetActionTypeLabel(PromotionActionType.BuyXGetY),
                Hint = "Mua số lượng X để nhận ưu đãi Y",
            },
            new()
            {
                Value = PromotionActionType.GiftProduct.ToString(),
                Label = PromotionDisplay.GetActionTypeLabel(PromotionActionType.GiftProduct),
                Hint = "Tặng biến thể sản phẩm được chọn",
            },
        };
    }

    private void AddValidationErrors(IEnumerable<PromotionValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(ToViewModelFieldName(error.FieldName), error.Message);
        }
    }

    private static string ToViewModelFieldName(string fieldName)
    {
        return fieldName switch
        {
            nameof(PromotionFormData.StartDateUtc) => nameof(PromotionFormViewModel.StartDate),
            nameof(PromotionFormData.EndDateUtc) => nameof(PromotionFormViewModel.EndDate),
            _ => fieldName,
        };
    }
}
