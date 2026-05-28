using e_commerce_web_admin.Services.Brands;
using e_commerce_web_admin.ViewModels.Brands;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public sealed class BrandsController : Controller
{
    private readonly IBrandAdminService _brandService;

    public BrandsController(IBrandAdminService brandService)
        => _brandService = brandService;

    // GET /Brands
    public async Task<IActionResult> Index(
        string? search, string? status, int page = 1,
        CancellationToken ct = default)
    {
        var vm = await _brandService.GetIndexAsync(
            new BrandIndexQuery { Search = search, Status = status, Page = page }, ct);
        return View(vm);
    }

    // GET /Brands/Create
    public async Task<IActionResult> Create(CancellationToken ct)
        => View(await _brandService.GetCreateFormAsync(ct));

    // POST /Brands/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BrandFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _brandService.CreateAsync(vm, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // GET /Brands/Edit/{id}
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var vm = await _brandService.GetEditFormAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    // POST /Brands/Edit/{id}
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, BrandFormViewModel vm, CancellationToken ct)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var result = await _brandService.UpdateAsync(id, vm, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // POST /Brands/Delete/{id}
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _brandService.DeleteAsync(id, ct);
        if (!result.Found) return NotFound();

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // POST /Brands/ToggleActive/{id}
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id, CancellationToken ct)
    {
        var result = await _brandService.ToggleActiveAsync(id, ct);
        return result is null ? NotFound() : Ok(new { isActive = result.IsActive });
    }

    private void AddErrors(IEnumerable<BrandValidationError> errors)
    {
        foreach (var e in errors)
            ModelState.AddModelError(e.FieldName, e.Message);
    }
}
