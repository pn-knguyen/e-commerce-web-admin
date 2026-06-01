using e_commerce_web_admin.ViewModels.Suppliers;

namespace e_commerce_web_admin.Services.Suppliers;

public interface ISupplierAdminService
{
    Task<SupplierIndexViewModel> GetIndexAsync(SupplierIndexQuery query, CancellationToken ct = default);
    Task<SupplierFormViewModel> GetCreateFormAsync(CancellationToken ct = default);
    Task<SupplierFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default);
    Task<SupplierSaveResult> CreateAsync(SupplierFormViewModel form, CancellationToken ct = default);
    Task<SupplierSaveResult> UpdateAsync(long id, SupplierFormViewModel form, CancellationToken ct = default);
    Task<SupplierDeleteCheckResult> CheckDeleteAsync(long id, CancellationToken ct = default);
    Task<SupplierDeleteResult> DeleteAsync(long id, CancellationToken ct = default);
    Task<SupplierToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default);
}
