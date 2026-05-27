namespace e_commerce_web_admin.Controllers;

public sealed class AttributesController : CrudPageControllerBase
{
    protected override string ModuleName => "Thuộc tính biến thể";
    protected override string ModuleDescription => "Quản lý thuộc tính tạo biến thể như màu sắc, dung lượng, kích thước.";
    protected override string ManagementGroup => "Cấu hình biến thể";
}
