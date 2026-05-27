namespace e_commerce_web_admin.Controllers;

public sealed class ProductsController : CrudPageControllerBase
{
    protected override string ModuleName => "Sản phẩm";
    protected override string ModuleDescription => "Quản lý thông tin sản phẩm gốc, thương hiệu, danh mục và nội dung mô tả.";
    protected override string ManagementGroup => "Danh mục sản phẩm";
}
