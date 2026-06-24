using e_commerce_web_admin.ViewModels.FulfillmentLocations;

namespace e_commerce_web_admin.Services.FulfillmentLocations;

public interface IFulfillmentLocationAdminService
{
    Task<FulfillmentLocationIndexViewModel> GetIndexAsync(
        FulfillmentLocationIndexQuery query,
        CancellationToken ct = default);
    Task<FulfillmentLocationFormViewModel> GetCreateFormAsync(CancellationToken ct = default);
    Task<FulfillmentLocationFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default);
    Task<FulfillmentLocationSaveResult> CreateAsync(
        FulfillmentLocationFormViewModel form,
        CancellationToken ct = default);
    Task<FulfillmentLocationSaveResult> UpdateAsync(
        long id,
        FulfillmentLocationFormViewModel form,
        CancellationToken ct = default);
    Task<FulfillmentLocationActionResult> DeleteAsync(long id, CancellationToken ct = default);
    Task<FulfillmentLocationActionResult> SetDefaultAsync(long id, CancellationToken ct = default);
    Task<FulfillmentLocationToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default);
}
