using e_commerce_web_admin.Services.Suppliers;
using e_commerce_web_admin.ViewModels.Suppliers;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public sealed class SuppliersController : Controller
{
    private readonly ISupplierAdminService _supplierService;

    public SuppliersController(ISupplierAdminService supplierService)
        => _supplierService = supplierService;

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        int page = 1,
        CancellationToken ct = default)
    {
        var viewModel = await _supplierService.GetIndexAsync(
            new SupplierIndexQuery
            {
                Search = search,
                Status = status,
                Page = page,
            },
            ct);

        return View(viewModel);
    }

    public async Task<IActionResult> Create(CancellationToken ct)
        => View(await _supplierService.GetCreateFormAsync(ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierFormViewModel viewModel, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _supplierService.CreateAsync(viewModel, ct);
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
        var viewModel = await _supplierService.GetEditFormAsync(id, ct);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, SupplierFormViewModel viewModel, CancellationToken ct)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _supplierService.UpdateAsync(id, viewModel, ct);
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
        var result = await _supplierService.DeleteAsync(id, ct);
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
        var result = await _supplierService.CheckDeleteAsync(id, ct);
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
        var result = await _supplierService.ToggleActiveAsync(id, ct);
        return result is null ? NotFound() : Ok(new { isActive = result.IsActive });
    }

    private void AddErrors(IEnumerable<SupplierValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }
    }
}
