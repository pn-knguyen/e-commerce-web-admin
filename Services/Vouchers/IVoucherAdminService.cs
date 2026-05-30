namespace e_commerce_web_admin.Services.Vouchers;

public interface IVoucherAdminService
{
    Task<VoucherIndexResult> GetIndexAsync(VoucherIndexRequest query, CancellationToken cancellationToken = default);
    VoucherFormData GetCreateForm();
    Task<VoucherFormData?> GetEditFormAsync(long id, CancellationToken cancellationToken = default);
    Task<VoucherSaveResult> CreateAsync(VoucherFormData form, CancellationToken cancellationToken = default);
    Task<VoucherSaveResult> UpdateAsync(long id, VoucherFormData form, CancellationToken cancellationToken = default);
    Task<VoucherDeleteResult> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<VoucherToggleResult?> ToggleActiveAsync(long id, CancellationToken cancellationToken = default);
}
