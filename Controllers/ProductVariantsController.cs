using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Services.ProductVariants;
using e_commerce_web_admin.ViewModels.ProductVariants;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("ProductVariants", Permissions.View)]
public sealed class ProductVariantsController : Controller
{
    private readonly IProductVariantAdminService _variantService;

    public ProductVariantsController(IProductVariantAdminService variantService)
        => _variantService = variantService;

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? stock,
        long? productId,
        long? categoryId,
        int page = 1,
        CancellationToken ct = default)
    {
        var viewModel = await _variantService.GetIndexAsync(
            new ProductVariantIndexQuery
            {
                Search = search,
                Status = status,
                Stock = stock,
                ProductId = productId,
                CategoryId = categoryId,
                Page = page,
            },
            ct);

        return View(viewModel);
    }

    [RbacAuthorize("ProductVariants", Permissions.Create)]
    public async Task<IActionResult> Create(long? productId, CancellationToken ct)
        => View(await _variantService.GetCreateFormAsync(productId, ct));

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("ProductVariants", Permissions.Create)]
    public async Task<IActionResult> Create(ProductVariantFormViewModel viewModel, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(await _variantService.PrepareFormAsync(viewModel, ct));
        }

        var result = await _variantService.CreateAsync(viewModel, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index), new { productId = result.Form.ProductId });
    }

    [RbacAuthorize("ProductVariants", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var viewModel = await _variantService.GetEditFormAsync(id, ct);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("ProductVariants", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id, ProductVariantFormViewModel viewModel, CancellationToken ct)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            viewModel.IsProductLocked = true;
            return View(await _variantService.PrepareFormAsync(viewModel, ct));
        }

        var result = await _variantService.UpdateAsync(id, viewModel, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index), new { productId = result.Form.ProductId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("ProductVariants", Permissions.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _variantService.DeleteAsync(id, ct);
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
        var result = await _variantService.CheckDeleteAsync(id, ct);
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
    [RbacAuthorize("ProductVariants", Permissions.Edit)]
    public async Task<IActionResult> ToggleActive(long id, CancellationToken ct)
    {
        var result = await _variantService.ToggleActiveAsync(id, ct);
        return result is null ? NotFound() : Ok(new { isActive = result.Value });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("ProductVariants", Permissions.Edit)]
    public async Task<IActionResult> SetDefault(long id, CancellationToken ct)
    {
        var result = await _variantService.SetDefaultAsync(id, ct);
        return result is null ? NotFound() : Ok(new { isDefault = result.Value });
    }

    private void AddErrors(IEnumerable<ProductVariantValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }
    }
}
