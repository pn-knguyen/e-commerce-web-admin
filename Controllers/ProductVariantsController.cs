namespace e_commerce_web_admin.Controllers;

public sealed class ProductVariantsController : CrudPageControllerBase
{
    protected override string ModuleName => "Biến thể sản phẩm";
    protected override string ModuleDescription => "Quản lý mã biến thể thực tế, giá bán, tồn kho và trạng thái mặc định.";
    protected override string ManagementGroup => "Danh mục sản phẩm";
}
