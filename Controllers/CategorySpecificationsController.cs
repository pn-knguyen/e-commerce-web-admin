using e_commerce_web_admin.Services.CategorySpecifications;
using e_commerce_web_admin.ViewModels.CategorySpecifications;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public sealed class CategorySpecificationsController : Controller
{
    private readonly ICategorySpecAdminService _service;

    public CategorySpecificationsController(ICategorySpecAdminService service)
        => _service = service;

    // GET /CategorySpecifications?categoryId={id}
    public async Task<IActionResult> Index(
        long categoryId, string? search, int page = 1, CancellationToken ct = default)
    {
        var vm = await _service.GetIndexAsync(
            categoryId, new CategorySpecIndexQuery { Search = search, Page = page }, ct);

        return vm is null ? NotFound() : View(vm);
    }

    // POST /CategorySpecifications/Assign
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(CategorySpecAssignViewModel form, CancellationToken ct)
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

    // POST /CategorySpecifications/Update
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(CategorySpecUpdateViewModel form, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(form, ct);
        return Ok(new { result.Succeeded, result.Message });
    }

    // POST /CategorySpecifications/Remove
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(long categoryId, long specId, CancellationToken ct)
    {
        var result = await _service.RemoveAsync(categoryId, specId, ct);

        if (!result.Found) return NotFound();

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { categoryId });
    }
}
