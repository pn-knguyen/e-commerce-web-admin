using e_commerce_web_admin.ViewModels.Promotions;

namespace e_commerce_web_admin.Services.Promotions;

public interface IPromotionAdminService
{
    Task<PromotionIndexResult> GetIndexAsync(PromotionIndexRequest query, CancellationToken ct = default);
    PromotionFormData GetCreateForm();
    Task<PromotionFormData?> GetEditFormAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<PromotionGiftVariantOption>> GetGiftVariantOptionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PromotionTargetOption>> GetTargetOptionsAsync(CancellationToken ct = default);
    Task<PromotionSaveResult> CreateAsync(PromotionFormData form, CancellationToken ct = default);
    Task<PromotionSaveResult> UpdateAsync(long id, PromotionFormData form, CancellationToken ct = default);
    Task<PromotionDeleteResult> DeleteAsync(long id, CancellationToken ct = default);
    Task<PromotionToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default);
}
