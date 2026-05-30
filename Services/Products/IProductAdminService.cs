using e_commerce_web_admin.ViewModels.Products;

namespace e_commerce_web_admin.Services.Products;

public interface IProductAdminService
{
    Task<ProductIndexViewModel> GetIndexAsync(ProductIndexQuery query, CancellationToken ct = default);
    Task<ProductFormViewModel> GetCreateFormAsync(CancellationToken ct = default);
    Task<ProductFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default);
    Task<ProductFormViewModel> PrepareFormAsync(ProductFormViewModel form, CancellationToken ct = default);
    Task<ProductSaveResult> CreateAsync(ProductFormViewModel form, CancellationToken ct = default);
    Task<ProductSaveResult> UpdateAsync(long id, ProductFormViewModel form, CancellationToken ct = default);
    Task<ProductDeleteCheckResult> CheckDeleteAsync(long id, CancellationToken ct = default);
    Task<ProductDeleteResult> DeleteAsync(long id, CancellationToken ct = default);
    Task<ProductToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default);
    Task<ProductToggleResult?> ToggleFeaturedAsync(long id, CancellationToken ct = default);
}
