using e_commerce_web_admin.ViewModels.Ratings;

namespace e_commerce_web_admin.Services.Ratings;

public interface IRatingAdminService
{
    Task<RatingIndexViewModel> GetIndexAsync(RatingIndexQuery query, CancellationToken ct = default);
    Task<RatingToggleResult?> ToggleApprovalAsync(long id, CancellationToken ct = default);
    Task<RatingDeleteResult> DeleteAsync(long id, CancellationToken ct = default);
}
