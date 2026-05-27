namespace e_commerce_web_admin.Controllers;

public sealed class CategoryVariantAttributesController : CrudPageControllerBase
{
    protected override string ModuleName => "Thuộc tính theo danh mục";
    protected override string ModuleDescription => "Cấu hình thuộc tính nào được dùng để tạo biến thể cho từng danh mục.";
    protected override string ManagementGroup => "Cấu hình biến thể";
}
