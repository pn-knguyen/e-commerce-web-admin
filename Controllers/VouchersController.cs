using e_commerce_web_admin.Services.Vouchers;
using e_commerce_web_admin.ViewModels.Vouchers;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

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
        var viewModel = await _voucherService.GetIndexAsync(
            new VoucherIndexQuery
            {
                Search = search,
                Status = status,
                Page = page,
            },
            cancellationToken);

        return View(viewModel);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await _voucherService.GetCreateFormAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        VoucherFormViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(await _voucherService.PrepareFormAsync(viewModel, cancellationToken));
        }

        var result = await _voucherService.CreateAsync(viewModel, cancellationToken);
        if (!result.Succeeded)
        {
            AddValidationErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var viewModel = await _voucherService.GetEditFormAsync(id, cancellationToken);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
            return View(await _voucherService.PrepareFormAsync(viewModel, cancellationToken));
        }

        var result = await _voucherService.UpdateAsync(id, viewModel, cancellationToken);
        if (!result.Succeeded)
        {
            AddValidationErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
    public async Task<IActionResult> ToggleActive(long id, CancellationToken cancellationToken)
    {
        var result = await _voucherService.ToggleActiveAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(new { isActive = result.IsActive });
    }

    private void AddValidationErrors(IEnumerable<VoucherValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }
    }
}
