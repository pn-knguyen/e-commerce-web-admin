namespace e_commerce_web_admin.Models.Constants;

/// <summary>Các claim type dùng trong Cookie Authentication.</summary>
public static class AppClaimTypes
{
    public const string UserId   = "uid";
    public const string UserRole = "role";
    public const string FullName = "fullname";
    public const string Email    = "email";
    public const string Avatar   = "avatar";
}

public static class StaffClaimTypes
{
    public const string Permission = "permission";
}

public static class StaffRoleNames
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
}

public static class PaymentMethodIds
{
    public const long CashOnDelivery = 1;
}

/// <summary>Tên permission hợp lệ trong hệ thống RBAC.</summary>
public static class Permissions
{
    public const string View    = "View";
    public const string Create  = "Create";
    public const string Edit    = "Edit";
    public const string Delete  = "Delete";
    public const string Approve = "Approve";

    public static readonly string[] All = [View, Create, Edit, Delete, Approve];

    public static string Build(string module, string permission) => $"{module}.{permission}";
}

public static class PermissionModules
{
    public static readonly string[] All =
    [
        "Dashboard",
        "Customers",
        "Staff",
        "Roles",
        "Brands",
        "Categories",
        "Products",
        "ProductVariants",
        "Specifications",
        "Attributes",
        "Suppliers",
        "GoodsReceipts",
        "FulfillmentLocations",
        "Orders",
        "PaymentMethods",
        "Ratings",
        "Vouchers",
        "Promotions",
    ];

    public static readonly IReadOnlyDictionary<string, string> DisplayNames = new Dictionary<string, string>
    {
        ["Dashboard"] = "Bảng điều khiển",
        ["Customers"] = "Khách hàng",
        ["Staff"] = "Nhân sự quản trị",
        ["Roles"] = "Vai trò và quyền",
        ["Brands"] = "Thương hiệu",
        ["Categories"] = "Danh mục",
        ["Products"] = "Sản phẩm",
        ["ProductVariants"] = "Biến thể sản phẩm",
        ["Specifications"] = "Thông số kỹ thuật",
        ["Attributes"] = "Thuộc tính biến thể",
        ["Suppliers"] = "Nhà cung cấp",
        ["GoodsReceipts"] = "Quản lý tồn kho",
        ["FulfillmentLocations"] = "Điểm lấy hàng",
        ["Orders"] = "Đơn hàng",
        ["PaymentMethods"] = "Phương thức thanh toán",
        ["Ratings"] = "Đánh giá",
        ["Vouchers"] = "Voucher",
        ["Promotions"] = "Khuyến mãi",
    };
}
