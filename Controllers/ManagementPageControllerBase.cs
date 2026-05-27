using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace e_commerce_web_admin.Controllers;

public abstract class ManagementPageControllerBase : Controller
{
    protected abstract string ModuleName { get; }
    protected abstract string ModuleDescription { get; }
    protected virtual string ManagementGroup => "Quản trị";

    public virtual IActionResult Index()
    {
        SetPageContext("Danh sách", "Index");
        ViewData["CanCreate"] = false;
        return View();
    }

    public virtual IActionResult Details(long id)
    {
        SetPageContext("Chi tiết", "Details", id);
        ViewData["CanCreate"] = false;
        return View();
    }

    protected void SetPageContext(string pageAction, string pageActionName, long? entityId = null)
    {
        ViewData["Title"] = $"{pageAction} {ModuleName}";
        ViewData["ModuleName"] = ModuleName;
        ViewData["ModuleDescription"] = ModuleDescription;
        ViewData["ManagementGroup"] = ManagementGroup;
        ViewData["CrudAction"] = pageActionName;
        ViewData["EntityId"] = entityId;
    }

    protected IActionResult BackendNotImplemented()
    {
        return StatusCode(
            StatusCodes.Status501NotImplemented,
            "Backend nghiệp vụ sẽ được triển khai ở service/viewmodel riêng, không xử lý trực tiếp trong view.");
    }
}
