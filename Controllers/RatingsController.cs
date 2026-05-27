using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public sealed class RatingsController : ManagementPageControllerBase
{
    protected override string ModuleName => "Đánh giá";
    protected override string ModuleDescription => "Xem, duyệt hoặc từ chối đánh giá sản phẩm từ người mua.";
    protected override string ManagementGroup => "Đơn hàng";

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Approve(long id)
    {
        return BackendNotImplemented();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reject(long id)
    {
        return BackendNotImplemented();
    }
}
