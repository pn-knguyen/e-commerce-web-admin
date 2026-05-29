using e_commerce_web_admin.Services.Attributes;
using e_commerce_web_admin.ViewModels.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;

namespace e_commerce_web_admin.Controllers;

public sealed class AttributesController : Controller
{
    private readonly IAttributeAdminService _service;
    private readonly IAntiforgery _antiforgery;

    public AttributesController(IAttributeAdminService service, IAntiforgery antiforgery)
    {
        _service = service;
        _antiforgery = antiforgery;
    }

    // GET /Attributes
    public async Task<IActionResult> Index(
        string? search, int page = 1, CancellationToken ct = default)
    {
        var vm = await _service.GetIndexAsync(
            new AttributeIndexQuery { Search = search, Page = page }, ct);
        return View(vm);
    }

    // GET /Attributes/Create
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var vm = await _service.GetCreateFormAsync(ct);
        return View(vm);
    }

    // POST /Attributes/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AttributeFormViewModel form, CancellationToken ct)
    {
        var result = await _service.CreateAsync(form, ct);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(e.Field, e.Message);
            return View(form);
        }
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // GET /Attributes/Edit/{id}
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var vm = await _service.GetEditViewAsync(id, ct);
        return vm is null ? NotFound() : View(vm);
    }

    // POST /Attributes/Edit/{id}
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, AttributeFormViewModel form, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, form, ct);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError(e.Field, e.Message);

            // Reload options panel on validation failure
            var vm = await _service.GetEditViewAsync(id, ct);
            if (vm is null) return NotFound();
            vm.Form = form;
            return View(vm);
        }
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /Attributes/Delete/{id}
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // ── Options API (JSON, called by attributes.js) ─────────────────────────

    // GET /Attributes/{id}/Options
    [HttpGet("Attributes/{id:long}/Options")]
    public async Task<IActionResult> Options(long id, CancellationToken ct)
    {
        var vm = await _service.GetOptionsAsync(id, ct);
        return vm is null ? NotFound() : Ok(vm);
    }

    // POST /Attributes/{id}/Options/Add
    [HttpPost("Attributes/{id:long}/Options/Add")]
    public async Task<IActionResult> OptionsAdd(long id, [FromBody] AttributeOptionFormViewModel form, CancellationToken ct)
    {
        if (!await IsValidAntiforgeryAsync()) return Forbid();
        form.AttributeId = id;
        if (!ModelState.IsValid)
            return BadRequest(new { succeeded = false, message = "Dữ liệu không hợp lệ." });

        var result = await _service.AddOptionAsync(form, ct);
        return Ok(new { result.Succeeded, result.Message, result.OptionId });
    }

    // POST /Attributes/Options/{optionId}/Update
    [HttpPost("Attributes/Options/{optionId:long}/Update")]
    public async Task<IActionResult> OptionUpdate(long optionId, [FromBody] AttributeOptionUpdateViewModel form, CancellationToken ct)
    {
        if (!await IsValidAntiforgeryAsync()) return Forbid();
        form.Id = optionId;
        if (!ModelState.IsValid)
            return BadRequest(new { succeeded = false, message = "Dữ liệu không hợp lệ." });

        var result = await _service.UpdateOptionAsync(form, ct);
        return Ok(new { result.Succeeded, result.Message });
    }

    // POST /Attributes/Options/{optionId}/Delete
    [HttpPost("Attributes/Options/{optionId:long}/Delete")]
    public async Task<IActionResult> OptionDelete(long optionId, CancellationToken ct)
    {
        if (!await IsValidAntiforgeryAsync()) return Forbid();
        var result = await _service.DeleteOptionAsync(optionId, ct);
        if (!result.Found) return NotFound();
        return Ok(new { result.Succeeded, result.Message });
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<bool> IsValidAntiforgeryAsync()
    {
        try { await _antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch { return false; }
    }
}
