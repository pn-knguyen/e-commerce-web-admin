using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Services.Specifications;
using e_commerce_web_admin.ViewModels.Specifications;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Specifications", Permissions.View)]
public sealed class SpecificationsController : Controller
{
    private readonly ISpecificationAdminService _specService;

    public SpecificationsController(ISpecificationAdminService specService)
        => _specService = specService;

    // GET /Specifications
    public async Task<IActionResult> Index(
        string? search, int page = 1, CancellationToken ct = default)
    {
        var vm = await _specService.GetIndexAsync(
            new SpecificationIndexQuery { Search = search, Page = page }, ct);
        return View(vm);
    }

    // GET /Specifications/Create
    [RbacAuthorize("Specifications", Permissions.Create)]
    public async Task<IActionResult> Create(CancellationToken ct)
        => View(await _specService.GetCreateFormAsync(ct));

    // POST /Specifications/Create
    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Specifications", Permissions.Create)]
    public async Task<IActionResult> Create(SpecificationFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _specService.CreateAsync(vm, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // GET /Specifications/Edit/{id}
    [RbacAuthorize("Specifications", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var vm = await _specService.GetEditFormAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    // POST /Specifications/Edit/{id}
    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Specifications", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id, SpecificationFormViewModel vm, CancellationToken ct)
    {
        if (id != vm.Id) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var result = await _specService.UpdateAsync(id, vm, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // POST /Specifications/Delete/{id}
    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Specifications", Permissions.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _specService.DeleteAsync(id, ct);
        if (!result.Found) return NotFound();

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private void AddErrors(IEnumerable<SpecValidationError> errors)
    {
        foreach (var e in errors)
            ModelState.AddModelError(e.FieldName, e.Message);
    }
}
