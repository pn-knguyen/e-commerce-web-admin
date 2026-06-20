using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Services.Orders;
using e_commerce_web_admin.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Orders", Permissions.View)]
public sealed class OrdersController : Controller
{
    private readonly IOrderAdminService _orderService;

    public OrdersController(IOrderAdminService orderService)
        => _orderService = orderService;

    public async Task<IActionResult> Index(
        string? search,
        string? dateRange,
        string? orderStatus,
        string? paymentStatus,
        long? paymentMethodId,
        int page = 1,
        CancellationToken ct = default)
    {
        var viewModel = await _orderService.GetIndexAsync(
            new OrderIndexQuery
            {
                Search = search,
                DateRange = dateRange,
                OrderStatus = orderStatus,
                PaymentStatus = paymentStatus,
                PaymentMethodId = paymentMethodId,
                Page = page,
            },
            ct);

        return View(viewModel);
    }

    public async Task<IActionResult> Details(long id, CancellationToken ct)
    {
        var viewModel = await _orderService.GetDetailsAsync(id, ct);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Orders", Permissions.Approve)]
    public async Task<IActionResult> UpdateStatus(
        long id,
        OrderStatusUpdateViewModel form,
        CancellationToken ct)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        var result = await _orderService.UpdateStatusAsync(id, form, ct);
        if (!result.Found)
        {
            return NotFound();
        }

        if (!result.Succeeded)
        {
            AddErrors(result.Errors);

            var viewModel = await _orderService.GetDetailsAsync(id, ct);
            if (viewModel is null)
            {
                return NotFound();
            }

            viewModel.StatusForm = form;
            return View("Details", viewModel);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    private void AddErrors(IEnumerable<OrderValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }
    }
}
