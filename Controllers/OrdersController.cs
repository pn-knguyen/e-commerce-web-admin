using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public sealed class OrdersController : ManagementPageControllerBase
{
    protected override string ModuleName => "Đơn hàng";
    protected override string ModuleDescription => "Theo dõi đơn hàng, xem chi tiết và cập nhật trạng thái xử lý hoặc thanh toán.";
    protected override string ManagementGroup => "Đơn hàng";

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStatus(long id, IFormCollection form)
    {
        return BackendNotImplemented();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePaymentStatus(long id, IFormCollection form)
    {
        return BackendNotImplemented();
    }
}
