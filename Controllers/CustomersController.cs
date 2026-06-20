using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Services.Customers;
using e_commerce_web_admin.ViewModels.Customers;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Customers", Permissions.View)]
public sealed class CustomersController(ICustomerAdminService customerService) : Controller
{
    public async Task<IActionResult> Index(
        [FromQuery] CustomerIndexQuery query,
        CancellationToken ct = default)
    {
        return View(await customerService.GetIndexAsync(query, ct));
    }

    public async Task<IActionResult> Details(long id, CancellationToken ct = default)
    {
        var customer = await customerService.GetDetailsAsync(id, ct);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Customers", Permissions.Edit)]
    public async Task<IActionResult> ToggleActive(long id, CancellationToken ct = default)
    {
        var result = await customerService.ToggleActiveAsync(id, ct);
        if (!result.Found)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }
}
