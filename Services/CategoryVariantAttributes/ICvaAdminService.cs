using e_commerce_web_admin.ViewModels.CategoryVariantAttributes;

namespace e_commerce_web_admin.Services.CategoryVariantAttributes;

public interface ICvaAdminService
{
    Task<CvaIndexViewModel?> GetIndexAsync(long categoryId, CvaIndexQuery query, CancellationToken ct = default);
    Task<CvaSaveResult> AssignAsync(CvaAssignViewModel form, CancellationToken ct = default);
    Task<CvaRemoveResult> RemoveAsync(long categoryId, long attributeId, CancellationToken ct = default);
}
