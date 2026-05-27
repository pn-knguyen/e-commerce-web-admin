namespace e_commerce_web_admin.Controllers;

public sealed class SpecificationsController : CrudPageControllerBase
{
    protected override string ModuleName => "Thông số kỹ thuật";
    protected override string ModuleDescription => "Quản lý bộ thông số dùng chung như màn hình, RAM, pin, chất liệu.";
    protected override string ManagementGroup => "Cấu hình sản phẩm";
}
