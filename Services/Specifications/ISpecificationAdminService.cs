using e_commerce_web_admin.ViewModels.Specifications;

namespace e_commerce_web_admin.Services.Specifications;

public interface ISpecificationAdminService
{
    Task<SpecificationIndexViewModel> GetIndexAsync(SpecificationIndexQuery query, CancellationToken ct = default);
    Task<SpecificationFormViewModel> GetCreateFormAsync(CancellationToken ct = default);
    Task<SpecificationFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default);
    Task<SpecSaveResult> CreateAsync(SpecificationFormViewModel form, CancellationToken ct = default);
    Task<SpecSaveResult> UpdateAsync(long id, SpecificationFormViewModel form, CancellationToken ct = default);
    Task<SpecDeleteResult> DeleteAsync(long id, CancellationToken ct = default);
}
