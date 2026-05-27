namespace e_commerce_web_admin.Controllers;

public sealed class PromotionsController : CrudPageControllerBase
{
    protected override string ModuleName => "Chương trình khuyến mãi";
    protected override string ModuleDescription => "Quản lý chương trình khuyến mãi, điều kiện đơn hàng và giới hạn áp dụng.";
    protected override string ManagementGroup => "Marketing";
}
