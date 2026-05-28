using e_commerce_web_admin.ViewModels.Brands;

namespace e_commerce_web_admin.Services.Brands;

public interface IBrandAdminService
{
    Task<BrandIndexViewModel> GetIndexAsync(BrandIndexQuery query, CancellationToken ct = default);
    Task<BrandFormViewModel> GetCreateFormAsync(CancellationToken ct = default);
    Task<BrandFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default);
    Task<BrandSaveResult> CreateAsync(BrandFormViewModel form, CancellationToken ct = default);
    Task<BrandSaveResult> UpdateAsync(long id, BrandFormViewModel form, CancellationToken ct = default);
    Task<BrandDeleteResult> DeleteAsync(long id, CancellationToken ct = default);
    Task<BrandToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default);
}
