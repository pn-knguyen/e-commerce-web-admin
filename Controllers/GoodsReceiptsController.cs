using e_commerce_web_admin.Services.Inventory;
using e_commerce_web_admin.ViewModels.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public sealed class GoodsReceiptsController : Controller
{
    private readonly IInventoryAdminService _inventoryService;

    public GoodsReceiptsController(IInventoryAdminService inventoryService)
        => _inventoryService = inventoryService;

    public async Task<IActionResult> Index(
        string? search,
        string? stock,
        string? receiptStatus,
        long? supplierId,
        long? categoryId,
        int stockPage = 1,
        int receiptPage = 1,
        CancellationToken ct = default)
    {
        var viewModel = await _inventoryService.GetIndexAsync(
            new InventoryIndexQuery
            {
                Search = search,
                Stock = stock,
                ReceiptStatus = receiptStatus,
                SupplierId = supplierId,
                CategoryId = categoryId,
                StockPage = stockPage,
                ReceiptPage = receiptPage,
            },
            ct);

        return View(viewModel);
    }

    public async Task<IActionResult> Details(long id, CancellationToken ct)
    {
        var viewModel = await _inventoryService.GetDetailsAsync(id, ct);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    public async Task<IActionResult> Create(long? variantId, CancellationToken ct)
        => View(await _inventoryService.GetCreateFormAsync(variantId, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoodsReceiptFormViewModel viewModel, CancellationToken ct)
    {
        var result = await _inventoryService.CreateAsync(viewModel, ct);
        if (!result.Succeeded)
        {
            ModelState.Clear();
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.Form.Id });
    }

    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var viewModel = await _inventoryService.GetEditFormAsync(id, ct);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, GoodsReceiptFormViewModel viewModel, CancellationToken ct)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        var result = await _inventoryService.UpdateAsync(id, viewModel, ct);
        if (!result.Succeeded)
        {
            ModelState.Clear();
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(long id, CancellationToken ct)
        => await HandleReceiptActionAsync(
            id,
            _inventoryService.SubmitAsync,
            redirectToDetails: true,
            ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
        => await HandleReceiptActionAsync(
            id,
            _inventoryService.ApproveAsync,
            redirectToDetails: true,
            ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => await HandleReceiptActionAsync(
            id,
            _inventoryService.CancelAsync,
            redirectToDetails: true,
            ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => await HandleReceiptActionAsync(
            id,
            _inventoryService.DeleteAsync,
            redirectToDetails: false,
            ct);

    private async Task<IActionResult> HandleReceiptActionAsync(
        long id,
        Func<long, CancellationToken, Task<GoodsReceiptActionResult>> action,
        bool redirectToDetails,
        CancellationToken ct)
    {
        var result = await action(id, ct);
        if (!result.Found)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

        return redirectToDetails
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(Index));
    }

    private void AddErrors(IEnumerable<InventoryValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }
    }
}
