using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Services.Ratings;
using e_commerce_web_admin.ViewModels.Ratings;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("Ratings", Permissions.View)]
public sealed class RatingsController : Controller
{
    private readonly IRatingAdminService _ratingService;

    public RatingsController(IRatingAdminService ratingService)
        => _ratingService = ratingService;

    public async Task<IActionResult> Index(
        string? search,
        string? status,
        int? stars,
        int page = 1,
        CancellationToken ct = default)
    {
        var viewModel = await _ratingService.GetIndexAsync(
            new RatingIndexQuery
            {
                Search = search,
                Status = status,
                Stars = stars,
                Page = page,
            },
            ct);

        return View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Ratings", Permissions.Approve)]
    public async Task<IActionResult> ToggleApproval(long id, CancellationToken ct)
    {
        var result = await _ratingService.ToggleApprovalAsync(id, ct);
        return result is null
            ? NotFound()
            : Ok(new { isApproved = result.IsApproved, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RbacAuthorize("Ratings", Permissions.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _ratingService.DeleteAsync(id, ct);
        if (!result.Found)
        {
            return NotFound();
        }

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
