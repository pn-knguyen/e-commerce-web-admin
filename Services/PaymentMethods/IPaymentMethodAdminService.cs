using e_commerce_web_admin.ViewModels.PaymentMethods;

namespace e_commerce_web_admin.Services.PaymentMethods;

public interface IPaymentMethodAdminService
{
    Task<PaymentMethodIndexViewModel> GetIndexAsync(PaymentMethodIndexQuery query, CancellationToken ct = default);
    Task<PaymentMethodFormViewModel> GetCreateFormAsync(CancellationToken ct = default);
    Task<PaymentMethodFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default);
    Task<PaymentMethodSaveResult> CreateAsync(PaymentMethodFormViewModel form, CancellationToken ct = default);
    Task<PaymentMethodSaveResult> UpdateAsync(long id, PaymentMethodFormViewModel form, CancellationToken ct = default);
    Task<PaymentMethodDeleteCheckResult> CheckDeleteAsync(long id, CancellationToken ct = default);
    Task<PaymentMethodDeleteResult> DeleteAsync(long id, CancellationToken ct = default);
    Task<PaymentMethodToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default);
}
