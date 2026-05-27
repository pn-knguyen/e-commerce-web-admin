namespace e_commerce_web_admin.Controllers;

public sealed class CategorySpecificationsController : CrudPageControllerBase
{
    protected override string ModuleName => "Thông số theo danh mục";
    protected override string ModuleDescription => "Cấu hình thông số bắt buộc, thứ tự và nhóm hiển thị cho từng danh mục.";
    protected override string ManagementGroup => "Cấu hình sản phẩm";
}
