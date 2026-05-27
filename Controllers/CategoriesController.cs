namespace e_commerce_web_admin.Controllers;

public sealed class CategoriesController : CrudPageControllerBase
{
    protected override string ModuleName => "Danh mục";
    protected override string ModuleDescription => "Quản lý cây danh mục cha con, vị trí và trạng thái hiển thị.";
    protected override string ManagementGroup => "Danh mục sản phẩm";
}
