using e_commerce_web_admin.ViewModels.Categories;

namespace e_commerce_web_admin.Services.Categories;

public interface ICategoryAdminService
{
    Task<CategoryIndexViewModel> GetIndexAsync(CategoryIndexQuery query, CancellationToken cancellationToken = default);
    Task<CategoryFormViewModel> GetCreateFormAsync(CancellationToken cancellationToken = default);
    Task<CategoryFormViewModel?> GetEditFormAsync(long id, CancellationToken cancellationToken = default);
    Task<CategoryFormViewModel> PrepareFormAsync(CategoryFormViewModel form, long? excludeId, CancellationToken cancellationToken = default);
    Task<CategorySaveResult> CreateAsync(CategoryFormViewModel form, CancellationToken cancellationToken = default);
    Task<CategorySaveResult> UpdateAsync(long id, CategoryFormViewModel form, CancellationToken cancellationToken = default);
    Task<CategoryDeleteResult> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<CategoryToggleResult?> ToggleActiveAsync(long id, CancellationToken cancellationToken = default);
}
