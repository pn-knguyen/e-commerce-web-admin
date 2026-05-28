using e_commerce_web_admin.ViewModels.CategorySpecifications;

namespace e_commerce_web_admin.Services.CategorySpecifications;

public interface ICategorySpecAdminService
{
    Task<CategorySpecIndexViewModel?> GetIndexAsync(
        long categoryId, CategorySpecIndexQuery query, CancellationToken ct = default);

    Task<CategorySpecSaveResult> AssignAsync(
        CategorySpecAssignViewModel form, CancellationToken ct = default);

    Task<CategorySpecSaveResult> UpdateAsync(
        CategorySpecUpdateViewModel form, CancellationToken ct = default);

    Task<CategorySpecRemoveResult> RemoveAsync(
        long categoryId, long specId, CancellationToken ct = default);
}
