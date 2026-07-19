using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Services.Inventory;
using e_commerce_web_admin.ViewModels.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("GoodsReceipts", Permissions.View)]
public sealed class InventoryController : Controller
{
    private readonly IInventoryAdminService _inventoryService;

    public InventoryController(IInventoryAdminService inventoryService)
        => _inventoryService = inventoryService;

    public async Task<IActionResult> Index(
        string? search,
        string? stock,
        long? categoryId,
        int stockPage = 1,
        CancellationToken ct = default)
    {
        var viewModel = await _inventoryService.GetInventoryIndexAsync(
            new InventoryIndexQuery
            {
                Search = search,
                Stock = stock,
                CategoryId = categoryId,
                StockPage = stockPage,
            },
            ct);

        return View(viewModel);
    }
}
