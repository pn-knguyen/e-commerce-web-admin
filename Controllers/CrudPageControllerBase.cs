using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public abstract class CrudPageControllerBase : Controller
{
    protected abstract string ModuleName { get; }
    protected abstract string ModuleDescription { get; }
    protected virtual string ManagementGroup => "Quản trị";

    public virtual IActionResult Index()
    {
        SetPageContext("Danh sách", "Index");
        return View();
    }

    public virtual IActionResult Details(long id)
    {
        SetPageContext("Chi tiết", "Details", id);
        return View();
    }

    public virtual IActionResult Create()
    {
        SetPageContext("Thêm mới", "Create");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual IActionResult Create(IFormCollection form)
    {
        return BackendNotImplemented();
    }

    public virtual IActionResult Edit(long id)
    {
        SetPageContext("Cập nhật", "Edit", id);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public virtual IActionResult Edit(long id, IFormCollection form)
    {
        return BackendNotImplemented();
    }

    public virtual IActionResult Delete(long id)
    {
        SetPageContext("Xóa", "Delete", id);
        return View();
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public virtual IActionResult DeleteConfirmed(long id)
    {
        return BackendNotImplemented();
    }

    protected void SetPageContext(string pageAction, string crudAction, long? entityId = null)
    {
        ViewData["Title"] = $"{pageAction} {ModuleName}";
        ViewData["ModuleName"] = ModuleName;
        ViewData["ModuleDescription"] = ModuleDescription;
        ViewData["ManagementGroup"] = ManagementGroup;
        ViewData["CrudAction"] = crudAction;
        ViewData["EntityId"] = entityId;
    }

    protected IActionResult BackendNotImplemented()
    {
        return StatusCode(
            StatusCodes.Status501NotImplemented,
            "Backend CRUD sẽ được triển khai ở service/viewmodel riêng, không xử lý trực tiếp trong view.");
    }
}
