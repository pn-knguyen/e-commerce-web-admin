namespace e_commerce_web_admin.Controllers;

public sealed class AttributeOptionsController : CrudPageControllerBase
{
    protected override string ModuleName => "Giá trị thuộc tính";
    protected override string ModuleDescription => "Quản lý các giá trị lựa chọn của từng thuộc tính biến thể.";
    protected override string ManagementGroup => "Cấu hình biến thể";
}
