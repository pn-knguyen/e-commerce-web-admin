using e_commerce_web_admin.Services.PaymentMethods;
using e_commerce_web_admin.ViewModels.PaymentMethods;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public sealed class PaymentMethodsController : Controller
{
    private readonly IPaymentMethodAdminService _paymentMethodService;

    public PaymentMethodsController(IPaymentMethodAdminService paymentMethodService)
        => _paymentMethodService = paymentMethodService;

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        int page = 1,
        CancellationToken ct = default)
    {
        var viewModel = await _paymentMethodService.GetIndexAsync(
            new PaymentMethodIndexQuery
            {
                Search = search,
                Status = status,
                Page = page,
            },
            ct);

        return View(viewModel);
    }

    public async Task<IActionResult> Create(CancellationToken ct)
        => View(await _paymentMethodService.GetCreateFormAsync(ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentMethodFormViewModel viewModel, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _paymentMethodService.CreateAsync(viewModel, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var viewModel = await _paymentMethodService.GetEditFormAsync(id, ct);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, PaymentMethodFormViewModel viewModel, CancellationToken ct)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _paymentMethodService.UpdateAsync(id, viewModel, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _paymentMethodService.DeleteAsync(id, ct);
        if (!result.Found)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckDelete(long id, CancellationToken ct)
    {
        var result = await _paymentMethodService.CheckDeleteAsync(id, ct);
        if (!result.Found)
        {
            return NotFound();
        }

        return Ok(new
        {
            canDelete = result.CanDelete,
            message = result.Message,
            blockers = result.Blockers,
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id, CancellationToken ct)
    {
        var result = await _paymentMethodService.ToggleActiveAsync(id, ct);
        return result is null ? NotFound() : Ok(new { isActive = result.Value });
    }

    private void AddErrors(IEnumerable<PaymentMethodValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }
    }
}
