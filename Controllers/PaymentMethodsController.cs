namespace e_commerce_web_admin.Controllers;

public sealed class PaymentMethodsController : CrudPageControllerBase
{
    protected override string ModuleName => "Phương thức thanh toán";
    protected override string ModuleDescription => "Quản lý phương thức thanh toán, mô tả và trạng thái sử dụng.";
    protected override string ManagementGroup => "Đơn hàng";
}
