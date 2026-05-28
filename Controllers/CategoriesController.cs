using e_commerce_web_admin.Services.Categories;
using e_commerce_web_admin.ViewModels.Categories;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public sealed class CategoriesController : Controller
{
    private readonly ICategoryAdminService _categoryService;

    public CategoriesController(ICategoryAdminService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var viewModel = await _categoryService.GetIndexAsync(
            new CategoryIndexQuery
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
        return View(await _categoryService.GetCreateFormAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CategoryFormViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(await _categoryService.PrepareFormAsync(viewModel, excludeId: null, cancellationToken));
        }

        var result = await _categoryService.CreateAsync(viewModel, cancellationToken);
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
        var viewModel = await _categoryService.GetEditFormAsync(id, cancellationToken);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        CategoryFormViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(await _categoryService.PrepareFormAsync(viewModel, excludeId: id, cancellationToken));
        }

        var result = await _categoryService.UpdateAsync(id, viewModel, cancellationToken);
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
        var result = await _categoryService.DeleteAsync(id, cancellationToken);
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
        var result = await _categoryService.ToggleActiveAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(new { isActive = result.IsActive });
    }

    private void AddValidationErrors(IEnumerable<CategoryValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }
    }
}
