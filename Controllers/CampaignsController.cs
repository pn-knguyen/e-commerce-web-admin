namespace e_commerce_web_admin.Controllers;

public sealed class CampaignsController : CrudPageControllerBase
{
    protected override string ModuleName => "Chiến dịch";
    protected override string ModuleDescription => "Quản lý chiến dịch bán hàng, slug, loại chiến dịch và thời gian chạy.";
    protected override string ManagementGroup => "Marketing";
}
