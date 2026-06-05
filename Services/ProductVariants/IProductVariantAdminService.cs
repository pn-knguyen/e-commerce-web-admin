using e_commerce_web_admin.ViewModels.ProductVariants;

namespace e_commerce_web_admin.Services.ProductVariants;

public interface IProductVariantAdminService
{
    Task<ProductVariantIndexViewModel> GetIndexAsync(
        ProductVariantIndexQuery query,
        CancellationToken ct = default);

    Task<ProductVariantFormViewModel> GetCreateFormAsync(
        long? productId = null,
        CancellationToken ct = default);

    Task<ProductVariantFormViewModel?> GetEditFormAsync(
        long id,
        CancellationToken ct = default);

    Task<ProductVariantFormViewModel> PrepareFormAsync(
        ProductVariantFormViewModel form,
        CancellationToken ct = default);

    Task<ProductVariantSaveResult> CreateAsync(
        ProductVariantFormViewModel form,
        CancellationToken ct = default);

    Task<ProductVariantSaveResult> UpdateAsync(
        long id,
        ProductVariantFormViewModel form,
        CancellationToken ct = default);

    Task<ProductVariantDeleteCheckResult> CheckDeleteAsync(
        long id,
        CancellationToken ct = default);

    Task<ProductVariantDeleteResult> DeleteAsync(
        long id,
        CancellationToken ct = default);

    Task<ProductVariantToggleResult?> ToggleActiveAsync(
        long id,
        CancellationToken ct = default);

    Task<ProductVariantToggleResult?> SetDefaultAsync(
        long id,
        CancellationToken ct = default);
}
