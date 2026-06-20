using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Models.Enums;
using e_commerce_web_admin.Services.Vouchers;
using e_commerce_web_admin.ViewModels.Vouchers;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Vouchers", Permissions.View)]
public sealed class VouchersController : Controller
{
    private readonly IVoucherAdminService _voucherService;

    public VouchersController(IVoucherAdminService voucherService)
    {
        _voucherService = voucherService;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _voucherService.GetIndexAsync(
            new VoucherIndexRequest
            {
                Search = search,
                Status = status,
                Page = page,
            },
            cancellationToken);

        return View(ToIndexViewModel(result));
    }

    [RbacAuthorize("Vouchers", Permissions.Create)]
    public IActionResult Create()
    {
        return View(ToFormViewModel(_voucherService.GetCreateForm()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RbacAuthorize("Vouchers", Permissions.Create)]
    public async Task<IActionResult> Create(
        VoucherFormViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(PrepareForm(viewModel));
        }

        var result = await _voucherService.CreateAsync(ToFormData(viewModel), cancellationToken);
        if (!result.Succeeded)
        {
            AddValidationErrors(result.Errors);
            return View(ToFormViewModel(result.Form));
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [RbacAuthorize("Vouchers", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var form = await _voucherService.GetEditFormAsync(id, cancellationToken);
        return form is null ? NotFound() : View(ToFormViewModel(form));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RbacAuthorize("Vouchers", Permissions.Edit)]
    public async Task<IActionResult> Edit(
        long id,
        VoucherFormViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(PrepareForm(viewModel));
        }

        var result = await _voucherService.UpdateAsync(id, ToFormData(viewModel), cancellationToken);
        if (!result.Succeeded)
        {
            AddValidationErrors(result.Errors);
            return View(ToFormViewModel(result.Form));
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RbacAuthorize("Vouchers", Permissions.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var result = await _voucherService.DeleteAsync(id, cancellationToken);
        if (!result.Found)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RbacAuthorize("Vouchers", Permissions.Edit)]
    public async Task<IActionResult> ToggleActive(long id, CancellationToken cancellationToken)
    {
        var result = await _voucherService.ToggleActiveAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(new { isActive = result.IsActive });
    }

    private static VoucherIndexViewModel ToIndexViewModel(VoucherIndexResult result)
    {
        return new VoucherIndexViewModel
        {
            Vouchers = result.Vouchers.Select(ToRowViewModel).ToList(),
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

    private static VoucherRowViewModel ToRowViewModel(VoucherListItem item)
    {
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
            StartDate = VoucherDateTime.ToAdminLocal(item.StartDateUtc),
            EndDate = VoucherDateTime.ToAdminLocal(item.EndDateUtc),
            Priority = item.Priority,
            IsActive = item.IsActive,
            OrderCount = item.OrderCount,
            UsageCount = item.UsageCount,
            AssignedUserCount = item.AssignedUserCount,
            TargetCount = item.TargetCount,
            StatusKey = item.StatusKey,
            StatusLabel = ResolveStatusLabel(item.StatusKey),
        };
    }

    private static VoucherFormViewModel ToFormViewModel(VoucherFormData form)
    {
        return PrepareForm(new VoucherFormViewModel
        {
            Id = form.Id,
            Code = form.Code,
            Description = form.Description,
            DiscountType = form.DiscountType,
            DiscountValue = form.DiscountValue,
            MinOrderValue = form.MinOrderValue,
            MaxDiscountValue = form.MaxDiscountValue,
            MaxUses = form.MaxUses,
            MaxUsesPerUser = form.MaxUsesPerUser,
            UsedCount = form.UsedCount,
            StartDate = VoucherDateTime.ToAdminLocal(form.StartDateUtc),
            EndDate = VoucherDateTime.ToAdminLocal(form.EndDateUtc),
            Priority = form.Priority,
            IsActive = form.IsActive,
        });
    }

    private static VoucherFormData ToFormData(VoucherFormViewModel viewModel)
    {
        return new VoucherFormData
        {
            Id = viewModel.Id,
            Code = viewModel.Code,
            Description = viewModel.Description,
            DiscountType = viewModel.DiscountType,
            DiscountValue = viewModel.DiscountValue,
            MinOrderValue = viewModel.MinOrderValue,
            MaxDiscountValue = viewModel.MaxDiscountValue,
            MaxUses = viewModel.MaxUses,
            MaxUsesPerUser = viewModel.MaxUsesPerUser,
            UsedCount = viewModel.UsedCount,
            StartDateUtc = VoucherDateTime.FromAdminLocal(viewModel.StartDate),
            EndDateUtc = VoucherDateTime.FromAdminLocal(viewModel.EndDate),
            Priority = viewModel.Priority,
            IsActive = viewModel.IsActive,
        };
    }

    private static VoucherFormViewModel PrepareForm(VoucherFormViewModel viewModel)
    {
        viewModel.DiscountTypeOptions = BuildDiscountTypeOptions();
        return viewModel;
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

    private void AddValidationErrors(IEnumerable<VoucherValidationError> errors)
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
            nameof(VoucherFormData.StartDateUtc) => nameof(VoucherFormViewModel.StartDate),
            nameof(VoucherFormData.EndDateUtc) => nameof(VoucherFormViewModel.EndDate),
            _ => fieldName,
        };
    }

    private static string ResolveStatusLabel(string statusKey)
    {
        return statusKey switch
        {
            "running" => "Đang chạy",
            "upcoming" => "Sắp diễn ra",
            "expired" => "Hết hạn",
            "exhausted" => "Hết lượt",
            _ => "Tạm tắt",
        };
    }
}
