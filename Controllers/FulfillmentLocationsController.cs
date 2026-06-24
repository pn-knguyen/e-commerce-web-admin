using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Services.FulfillmentLocations;
using e_commerce_web_admin.Services.Shipping.Providers;
using e_commerce_web_admin.ViewModels.FulfillmentLocations;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("FulfillmentLocations", Permissions.View)]
public sealed class FulfillmentLocationsController : Controller
{
    private readonly IFulfillmentLocationAdminService _locationService;
    private readonly IShippingProviderGateway _shippingProvider;

    public FulfillmentLocationsController(
        IFulfillmentLocationAdminService locationService,
        IShippingProviderGateway shippingProvider)
    {
        _locationService = locationService;
        _shippingProvider = shippingProvider;
    }

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        int page = 1,
        CancellationToken ct = default)
    {
        var viewModel = await _locationService.GetIndexAsync(
            new FulfillmentLocationIndexQuery
            {
                Search = search,
                Status = status,
                Page = page,
            },
            ct);

        return View(viewModel);
    }

    [RbacAuthorize("FulfillmentLocations", Permissions.Create)]
    public async Task<IActionResult> Create(CancellationToken ct)
        => View(await _locationService.GetCreateFormAsync(ct));

    [HttpGet]
    public async Task<IActionResult> GhnProvinces(CancellationToken ct)
    {
        var result = await _shippingProvider.GetProvincesAsync(ct);
        return result.Succeeded
            ? Ok(new
            {
                items = result.Items
                    .OrderBy(item => item.Name)
                    .Select(item => new
                    {
                        id = item.Id,
                        name = item.Name,
                        code = item.Code,
                    }),
            })
            : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpGet]
    public async Task<IActionResult> GhnDistricts(
        int provinceId,
        string? purpose,
        CancellationToken ct)
    {
        var result = await _shippingProvider.GetDistrictsAsync(provinceId, ct);
        return result.Succeeded
            ? Ok(new
            {
                items = result.Items
                    .Where(item => IsSupportedAddress(item.Status, item.SupportType, purpose))
                    .OrderBy(item => item.Name)
                    .Select(item => new
                    {
                        id = item.Id,
                        provinceId = item.ProvinceId,
                        name = item.Name,
                        supportType = item.SupportType,
                        status = item.Status,
                    }),
            })
            : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpGet]
    public async Task<IActionResult> GhnWards(
        int districtId,
        string? purpose,
        CancellationToken ct)
    {
        var result = await _shippingProvider.GetWardsAsync(districtId, ct);
        return result.Succeeded
            ? Ok(new
            {
                items = result.Items
                    .Where(item => IsSupportedAddress(item.Status, item.SupportType, purpose))
                    .OrderBy(item => item.Name)
                    .Select(item => new
                    {
                        code = item.Code,
                        districtId = item.DistrictId,
                        name = item.Name,
                        supportType = item.SupportType,
                        status = item.Status,
                    }),
            })
            : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("FulfillmentLocations", Permissions.Create)]
    public async Task<IActionResult> Create(FulfillmentLocationFormViewModel viewModel, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _locationService.CreateAsync(viewModel, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [RbacAuthorize("FulfillmentLocations", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var viewModel = await _locationService.GetEditFormAsync(id, ct);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("FulfillmentLocations", Permissions.Edit)]
    public async Task<IActionResult> Edit(long id, FulfillmentLocationFormViewModel viewModel, CancellationToken ct)
    {
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var result = await _locationService.UpdateAsync(id, viewModel, ct);
        if (!result.Succeeded)
        {
            AddErrors(result.Errors);
            return View(result.Form);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("FulfillmentLocations", Permissions.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _locationService.DeleteAsync(id, ct);
        if (!result.Found)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("FulfillmentLocations", Permissions.Edit)]
    public async Task<IActionResult> SetDefault(long id, CancellationToken ct)
    {
        var result = await _locationService.SetDefaultAsync(id, ct);
        if (!result.Found)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("FulfillmentLocations", Permissions.Edit)]
    public async Task<IActionResult> ToggleActive(long id, CancellationToken ct)
    {
        var result = await _locationService.ToggleActiveAsync(id, ct);
        return result is null ? NotFound() : Ok(new { isActive = result.IsActive, isDefault = result.IsDefault });
    }

    private void AddErrors(IEnumerable<FulfillmentLocationValidationError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(error.FieldName, error.Message);
        }
    }

    private static bool IsSupportedAddress(int? status, int? supportType, string? purpose)
    {
        if (status == 2 || supportType == 0)
        {
            return false;
        }

        return purpose?.Trim().ToLowerInvariant() switch
        {
            "pickup" => supportType is null or 1 or 3,
            "delivery" => supportType is null or 2 or 3,
            _ => true,
        };
    }
}
