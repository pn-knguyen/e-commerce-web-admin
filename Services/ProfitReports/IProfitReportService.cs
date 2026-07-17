using e_commerce_web_admin.ViewModels.ProfitReports;

namespace e_commerce_web_admin.Services.ProfitReports;

public interface IProfitReportService
{
    Task<ProfitReportViewModel> GetReportAsync(ProfitReportQuery query, CancellationToken ct = default);
}
