using e_commerce_web_admin.ViewModels.Vouchers;

namespace e_commerce_web_admin.Services.Vouchers;

public interface IVoucherAdminService
{
    Task<VoucherIndexViewModel> GetIndexAsync(VoucherIndexQuery query, CancellationToken cancellationToken = default);
    Task<VoucherFormViewModel> GetCreateFormAsync(CancellationToken cancellationToken = default);
    Task<VoucherFormViewModel?> GetEditFormAsync(long id, CancellationToken cancellationToken = default);
    Task<VoucherFormViewModel> PrepareFormAsync(VoucherFormViewModel form, CancellationToken cancellationToken = default);
    Task<VoucherSaveResult> CreateAsync(VoucherFormViewModel form, CancellationToken cancellationToken = default);
    Task<VoucherSaveResult> UpdateAsync(long id, VoucherFormViewModel form, CancellationToken cancellationToken = default);
    Task<VoucherDeleteResult> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<VoucherToggleResult?> ToggleActiveAsync(long id, CancellationToken cancellationToken = default);
}
