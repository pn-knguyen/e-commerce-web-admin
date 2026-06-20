using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Services.Products;
using e_commerce_web_admin.ViewModels.Products;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Products", Permissions.View)]
public sealed class ProductsController : Controller
{
    private readonly IProductAdminService _productService;

    public ProductsController(IProductAdminService productService)
        => _productService = productService;

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? featured,
        long? brandId,
        long? categoryId,
        int page = 1,
        CancellationToken ct = default)
    {
        var viewModel = await _productService.GetIndexAsync(
            new ProductIndexQuery
            {
                Search = search,
                Status = status,
                Featured = featured,
                BrandId = brandId,
                CategoryId = categoryId,
                Page = page,
            },
            ct);

        return View(viewModel);
    }

    [RbacAuthorize("Products", Permissions.Create)]
    public async Task<IActionResult> Create(CancellationToken ct)
        => View(await _productService.GetCreateFormAsync(ct));

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Products", Permissions.Create)]
    public async Task<IActionResult> Create(ProductFormViewModel viewModel, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(await _productService.PrepareFormAsync(viewModel, ct));
        }

        var result = await _productService.CreateAsync(viewModel, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [RbacAuthorize("Products", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var viewModel = await _productService.GetEditFormAsync(id, ct);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Products", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id, ProductFormViewModel viewModel, CancellationToken ct)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(await _productService.PrepareFormAsync(viewModel, ct));
        }

        var result = await _productService.UpdateAsync(id, viewModel, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Products", Permissions.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _productService.DeleteAsync(id, ct);
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
        var result = await _productService.CheckDeleteAsync(id, ct);
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
    [RbacAuthorize("Products", Permissions.Edit)]
    public async Task<IActionResult> ToggleActive(long id, CancellationToken ct)
    {
        var result = await _productService.ToggleActiveAsync(id, ct);
        return result is null ? NotFound() : Ok(new { isActive = result.Value });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Products", Permissions.Edit)]
    public async Task<IActionResult> ToggleFeatured(long id, CancellationToken ct)
    {
        var result = await _productService.ToggleFeaturedAsync(id, ct);
        return result is null ? NotFound() : Ok(new { isFeatured = result.Value });
    }

    private void AddErrors(IEnumerable<ProductValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }
    }
}
