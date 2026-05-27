using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public sealed class UsersController : ManagementPageControllerBase
{
    protected override string ModuleName => "Người dùng";
    protected override string ModuleDescription => "Xem tài khoản, cập nhật vai trò và khóa hoặc mở hoạt động của người dùng.";
    protected override string ManagementGroup => "Người dùng";

    public IActionResult Edit(long id)
    {
        SetPageContext("Cập nhật", "Edit", id);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(long id, IFormCollection form)
    {
        return BackendNotImplemented();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Lock(long id)
    {
        return BackendNotImplemented();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Unlock(long id)
    {
        return BackendNotImplemented();
    }
}
