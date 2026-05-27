namespace e_commerce_web_admin.Controllers;

public sealed class BrandsController : CrudPageControllerBase
{
    protected override string ModuleName => "Thương hiệu";
    protected override string ModuleDescription => "Quản lý thương hiệu, slug, hình ảnh và trạng thái hiển thị.";
    protected override string ManagementGroup => "Danh mục sản phẩm";
}
