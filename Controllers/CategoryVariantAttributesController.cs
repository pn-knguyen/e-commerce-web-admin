using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Services.CategoryVariantAttributes;
using e_commerce_web_admin.ViewModels.CategoryVariantAttributes;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Categories", Permissions.View)]
public sealed class CategoryVariantAttributesController : Controller
{
    private readonly ICvaAdminService _service;

    public CategoryVariantAttributesController(ICvaAdminService service) => _service = service;

    // GET /CategoryVariantAttributes?categoryId={id}
    public async Task<IActionResult> Index(
        long categoryId, string? search, int page = 1, CancellationToken ct = default)
    {
        var vm = await _service.GetIndexAsync(
            categoryId, new CvaIndexQuery { Search = search, Page = page }, ct);

        return vm is null ? NotFound() : View(vm);
    }

    // POST /CategoryVariantAttributes/Assign
    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Categories", Permissions.Edit)]
    public async Task<IActionResult> Assign(CvaAssignViewModel form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Index), new { categoryId = form.CategoryId });
        }

        var result = await _service.AssignAsync(form, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { categoryId = form.CategoryId });
    }

    // POST /CategoryVariantAttributes/Remove
    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Categories", Permissions.Delete)]
    public async Task<IActionResult> Remove(long categoryId, long attributeId, CancellationToken ct)
    {
        var result = await _service.RemoveAsync(categoryId, attributeId, ct);

        if (!result.Found) return NotFound();

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { categoryId });
    }
}
