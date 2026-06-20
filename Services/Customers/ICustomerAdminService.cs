using e_commerce_web_admin.ViewModels.Customers;

namespace e_commerce_web_admin.Services.Customers;

public interface ICustomerAdminService
{
    Task<CustomerIndexViewModel> GetIndexAsync(
        CustomerIndexQuery query,
        CancellationToken ct = default);

    Task<CustomerDetailsViewModel?> GetDetailsAsync(
        long id,
        CancellationToken ct = default);

    Task<CustomerActionResult> ToggleActiveAsync(
        long id,
        CancellationToken ct = default);
}
