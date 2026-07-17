using e_commerce_web_admin.Filters;
using e_commerce_web_admin.Models.Constants;
using e_commerce_web_admin.Services.ProfitReports;
using e_commerce_web_admin.ViewModels.ProfitReports;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

[RbacAuthorize("ProfitReports", Permissions.View)]
public sealed class ProfitReportsController : Controller
{
    private readonly IProfitReportService _profitReportService;

    public ProfitReportsController(IProfitReportService profitReportService)
        => _profitReportService = profitReportService;

    public async Task<IActionResult> Index(string? period, CancellationToken ct = default)
    {
        var viewModel = await _profitReportService.GetReportAsync(
            new ProfitReportQuery { Period = period },
            ct);

        return View(viewModel);
    }
}
