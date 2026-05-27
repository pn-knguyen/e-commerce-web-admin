namespace e_commerce_web_admin.Controllers;

public sealed class VouchersController : CrudPageControllerBase
{
    protected override string ModuleName => "Voucher";
    protected override string ModuleDescription => "Quản lý mã giảm giá, điều kiện áp dụng, giới hạn lượt dùng và thời hạn.";
    protected override string ManagementGroup => "Khuyến mãi";
}
