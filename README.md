# TechStore E-Commerce Admin

<!-- markdownlint-disable MD013 MD033 -->

<p align="center">
  <img src="docs/readme-images/techstore-admin-logo.svg" alt="TechStore Admin Logo" width="132" />
</p>

<p align="center">
  Hệ thống quản trị thương mại điện tử hiện đại cho sản phẩm, đơn hàng, kho hàng, vận chuyển, marketing, khách hàng, nhân sự và phân quyền.
</p>

<p align="center">
  <a href="#hình-ảnh-giao-diện"><strong>Xem giao diện</strong></a>
  ·
  <a href="#chức-năng-chính"><strong>Chức năng</strong></a>
  ·
  <a href="#kiến-trúc-tổng-quan"><strong>Kiến trúc</strong></a>
  ·
  <a href="#hướng-dẫn-clone-về-để-chạy"><strong>Clone & chạy</strong></a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/ASP.NET_Core-MVC-5C2D91?style=flat-square&logo=dotnet&logoColor=white" alt="ASP.NET Core MVC" />
  <img src="https://img.shields.io/badge/SQL_Server-EF_Core-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white" alt="SQL Server EF Core" />
  <img src="https://img.shields.io/badge/Razor-Admin_UI-0F766E?style=flat-square&logo=html5&logoColor=white" alt="Razor Admin UI" />
  <img src="https://img.shields.io/badge/SignalR-Realtime-111827?style=flat-square" alt="SignalR Realtime" />
</p>

<p align="center">
  <img src="docs/readme-images/01-dashboard.jpg" alt="TechStore Admin Dashboard" width="100%" />
</p>

<table>
  <tr>
    <td align="center"><strong>20+</strong><br />module quản trị</td>
    <td align="center"><strong>23</strong><br />ảnh giao diện</td>
    <td align="center"><strong>RBAC</strong><br />phân quyền theo module</td>
    <td align="center"><strong>GHN</strong><br />vận chuyển tích hợp</td>
  </tr>
</table>

## Giới Thiệu

TechStore E-Commerce Admin là hệ thống quản trị cho website bán thiết bị điện tử. Ứng dụng được xây dựng bằng ASP.NET Core MVC/Razor, Entity Framework Core, SQL Server và ASP.NET Core Identity. Mục tiêu của hệ thống là gom toàn bộ nghiệp vụ vận hành thương mại điện tử vào một trang admin thống nhất: sản phẩm, biến thể, tồn kho, đơn hàng, vận chuyển GHN, khách hàng, tin nhắn, marketing, nhân sự và phân quyền.

README này mô tả đầy đủ web admin đã làm, kèm ảnh chụp giao diện từ database demo của dự án. Bộ ảnh không dùng dữ liệu thật từ database local.

## Mục Lục

- [Công nghệ sử dụng](#công-nghệ-sử-dụng)
- [Kiến trúc tổng quan](#kiến-trúc-tổng-quan)
- [Chức năng chính](#chức-năng-chính)
- [Hình ảnh giao diện](#hình-ảnh-giao-diện)
- [Cấu trúc thư mục](#cấu-trúc-thư-mục)
- [Cấu hình quan trọng](#cấu-hình-quan-trọng)
- [Hướng dẫn clone về để chạy](#hướng-dẫn-clone-về-để-chạy)

## Điểm Nổi Bật

<table>
  <tr>
    <td width="25%">
      <h3>Vận hành tập trung</h3>
      <p>Quản lý sản phẩm, SKU, kho, đơn hàng, khách hàng và marketing trong một giao diện admin thống nhất.</p>
    </td>
    <td width="25%">
      <h3>Dữ liệu rõ ràng</h3>
      <p>Service layer tách khỏi Razor, ViewModel riêng, EF Core migration đầy đủ và backend luôn validate lại dữ liệu.</p>
    </td>
    <td width="25%">
      <h3>Phân quyền chi tiết</h3>
      <p>RBAC theo module và hành động: xem, tạo, sửa, xóa, duyệt. Role Admin có toàn quyền hệ thống.</p>
    </td>
    <td width="25%">
      <h3>Realtime & tích hợp</h3>
      <p>SignalR cho tin nhắn khách hàng, Cloudinary cho ảnh và GHN cho báo giá, tạo vận đơn, đồng bộ trạng thái.</p>
    </td>
  </tr>
</table>

## Công Nghệ Sử Dụng

| Nhóm | Công nghệ |
| --- | --- |
| Backend | ASP.NET Core MVC, Razor Views, C# |
| Runtime | .NET 10 |
| Database | SQL Server |
| ORM | Entity Framework Core, EF Core Migrations |
| Xác thực | ASP.NET Core Identity, Cookie Authentication |
| Phân quyền | Role based access control, permission claim theo module |
| Realtime | SignalR cho tin nhắn khách hàng |
| Vận chuyển | Tích hợp Giao Hàng Nhanh |
| Upload ảnh | Cloudinary |
| Frontend admin | Razor, Tailwind CDN, Bootstrap assets, jQuery, Lucide Icons |
| Tối ưu | Response Compression, MemoryCache, lazy loading assets, background worker |

## Kiến Trúc Tổng Quan

Ứng dụng đi theo mô hình MVC có service layer rõ ràng. Controller chỉ nhận request, kiểm tra quyền và điều hướng. Service xử lý nghiệp vụ, truy vấn database, validate dữ liệu và trả ViewModel/DTO cho Razor render.

```mermaid
flowchart LR
    A["Admin Browser"] --> B["Razor View + CSS + JavaScript"]
    B --> C["MVC Controller"]
    C --> D["Service Layer"]
    D --> E["Entity Framework Core"]
    E --> F[("SQL Server")]
    D --> G["Cloudinary Upload"]
    D --> H["Giao Hàng Nhanh API"]
    I["Customer Web"] --> J["SignalR Hub"]
    J --> D
```

Các nguyên tắc chính:

- Controller mỏng, nghiệp vụ nằm trong service.
- ViewModel tách khỏi Entity để giao diện không phụ thuộc trực tiếp vào EF Core.
- JavaScript chỉ hỗ trợ trải nghiệm nhập liệu, backend vẫn validate lại trước khi lưu.
- RBAC kiểm tra theo module và quyền, riêng role `Admin` được truy cập toàn bộ hệ thống.
- Các module lớn như đơn hàng, vận chuyển, tồn kho và báo cáo lãi có service riêng để dễ bảo trì.

## Chức Năng Chính

| Module | Đường dẫn | Nội dung |
| --- | --- | --- |
| Bảng điều khiển | `/Dashboard` | KPI doanh thu, đơn hàng, khách hàng, sản phẩm, biểu đồ hoạt động và bộ lọc thời gian, nhóm hàng, trạng thái. |
| Báo cáo lãi | `/ProfitReports` | Báo cáo lãi gộp, giá vốn, biên lãi, giá trị tồn kho và lãi dự kiến theo dữ liệu đơn hoàn tất, đã thanh toán. |
| Khách hàng | `/Customers` | Danh sách khách hàng, trạng thái hoạt động, thông tin liên hệ, chi tiết lịch sử mua hàng. |
| Tin nhắn khách hàng | `/CustomerMessages` | Workspace theo dõi hội thoại support và AI, realtime bằng SignalR, phân biệt luồng khách hỏi admin và khách hỏi AI. |
| Nhân sự | `/Staff` | Quản lý tài khoản staff/admin, trạng thái hoạt động, vai trò và bảo vệ không khóa admin cuối cùng. |
| Phân quyền | `/Roles` | Tạo, sửa, xóa vai trò và gán quyền theo từng module như Products.View, Orders.Approve, Vouchers.Edit. |
| Thương hiệu | `/Brands` | Quản lý thương hiệu, ảnh đại diện, slug, trạng thái bật/tắt. |
| Danh mục | `/Categories` | Quản lý cây danh mục, danh mục cha/con, ảnh, slug, trạng thái. |
| Sản phẩm | `/Products` | Quản lý sản phẩm gốc, thương hiệu, danh mục, mô tả, nổi bật, trạng thái và thông số kỹ thuật. |
| Biến thể sản phẩm | `/ProductVariants` | Quản lý SKU, giá bán, màu, ảnh theo biến thể, thuộc tính biến thể và trạng thái mặc định. |
| Thông số kỹ thuật | `/Specifications` | Quản lý danh sách thông số kỹ thuật dùng cho sản phẩm. |
| Thuộc tính biến thể | `/Attributes` | Quản lý thuộc tính tạo biến thể như dung lượng, kích thước, bộ xử lý. |
| Nhà cung cấp | `/Suppliers` | Quản lý nhà cung cấp, số điện thoại, email, địa chỉ và trạng thái hoạt động. |
| Tồn kho | `/Inventory` | Theo dõi tồn kho, giá vốn trung bình, cảnh báo số lượng và dữ liệu phục vụ báo cáo lợi nhuận. |
| Phiếu nhập | `/GoodsReceipts` | Tạo, sửa, duyệt phiếu nhập, cập nhật sổ kho và giá vốn cho biến thể. |
| Điểm lấy hàng | `/FulfillmentLocations` | Quản lý kho/điểm lấy hàng, địa chỉ GHN, điểm mặc định và trạng thái hoạt động. |
| Đơn hàng | `/Orders` | Quản lý danh sách, chi tiết, trạng thái đơn hàng, thanh toán và thao tác vận chuyển. |
| Phương thức thanh toán | `/PaymentMethods` | Quản lý phương thức thanh toán, bật/tắt và kiểm soát xóa khi đã phát sinh đơn. |
| Đánh giá | `/Ratings` | Duyệt, ẩn/hiện hoặc xóa đánh giá sản phẩm của khách hàng. |
| Voucher | `/Vouchers` | Quản lý mã giảm giá, thời gian hiệu lực, điều kiện sử dụng và trạng thái. |
| Chiến dịch | `/Campaigns` | Màn hình tập trung điều hướng các hoạt động marketing. |
| Khuyến mãi | `/Promotions` | Quản lý chương trình giảm giá theo thời gian, trạng thái và dữ liệu sản phẩm áp dụng. |

## Hình Ảnh Giao Diện

Toàn bộ ảnh dưới đây được chụp từ giao diện web admin với dữ liệu demo, giúp người đọc nắm nhanh hệ thống trước khi clone về chạy.

<table>
  <tr>
    <td width="50%" align="center">
      <img src="docs/readme-images/00-login.jpg" alt="Màn hình đăng nhập admin" width="100%" />
      <br /><strong>Đăng nhập admin</strong>
    </td>
    <td width="50%" align="center">
      <img src="docs/readme-images/01-dashboard.jpg" alt="Bảng điều khiển admin" width="100%" />
      <br /><strong>Bảng điều khiển</strong>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/readme-images/02-profit-reports.jpg" alt="Báo cáo lãi" width="100%" />
      <br /><strong>Báo cáo lãi</strong>
    </td>
    <td width="50%" align="center">
      <img src="docs/readme-images/03-customers.jpg" alt="Quản lý khách hàng" width="100%" />
      <br /><strong>Quản lý khách hàng</strong>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/readme-images/04-customer-messages.jpg" alt="Tin nhắn khách hàng" width="100%" />
      <br /><strong>Tin nhắn khách hàng</strong>
    </td>
    <td width="50%" align="center">
      <img src="docs/readme-images/05-staff.jpg" alt="Quản lý nhân sự" width="100%" />
      <br /><strong>Quản lý nhân sự</strong>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/readme-images/06-roles.jpg" alt="Phân quyền" width="100%" />
      <br /><strong>Phân quyền</strong>
    </td>
    <td width="50%" align="center">
      <img src="docs/readme-images/07-brands.jpg" alt="Quản lý thương hiệu" width="100%" />
      <br /><strong>Thương hiệu</strong>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/readme-images/08-categories.jpg" alt="Quản lý danh mục" width="100%" />
      <br /><strong>Danh mục</strong>
    </td>
    <td width="50%" align="center">
      <img src="docs/readme-images/09-products.jpg" alt="Quản lý sản phẩm" width="100%" />
      <br /><strong>Sản phẩm</strong>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/readme-images/10-product-variants.jpg" alt="Quản lý biến thể sản phẩm" width="100%" />
      <br /><strong>Biến thể sản phẩm</strong>
    </td>
    <td width="50%" align="center">
      <img src="docs/readme-images/11-specifications.jpg" alt="Quản lý thông số kỹ thuật" width="100%" />
      <br /><strong>Thông số kỹ thuật</strong>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/readme-images/12-attributes.jpg" alt="Quản lý thuộc tính biến thể" width="100%" />
      <br /><strong>Thuộc tính biến thể</strong>
    </td>
    <td width="50%" align="center">
      <img src="docs/readme-images/13-suppliers.jpg" alt="Quản lý nhà cung cấp" width="100%" />
      <br /><strong>Nhà cung cấp</strong>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/readme-images/14-inventory.jpg" alt="Quản lý tồn kho" width="100%" />
      <br /><strong>Tồn kho</strong>
    </td>
    <td width="50%" align="center">
      <img src="docs/readme-images/15-goods-receipts.jpg" alt="Quản lý phiếu nhập" width="100%" />
      <br /><strong>Phiếu nhập</strong>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/readme-images/16-fulfillment-locations.jpg" alt="Quản lý điểm lấy hàng" width="100%" />
      <br /><strong>Điểm lấy hàng</strong>
    </td>
    <td width="50%" align="center">
      <img src="docs/readme-images/17-orders.jpg" alt="Quản lý đơn hàng" width="100%" />
      <br /><strong>Đơn hàng</strong>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/readme-images/18-payment-methods.jpg" alt="Quản lý phương thức thanh toán" width="100%" />
      <br /><strong>Phương thức thanh toán</strong>
    </td>
    <td width="50%" align="center">
      <img src="docs/readme-images/19-ratings.jpg" alt="Quản lý đánh giá" width="100%" />
      <br /><strong>Đánh giá</strong>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="docs/readme-images/20-vouchers.jpg" alt="Quản lý voucher" width="100%" />
      <br /><strong>Voucher</strong>
    </td>
    <td width="50%" align="center">
      <img src="docs/readme-images/21-campaigns.jpg" alt="Quản lý chiến dịch" width="100%" />
      <br /><strong>Chiến dịch</strong>
    </td>
  </tr>
  <tr>
    <td colspan="2" align="center">
      <img src="docs/readme-images/22-promotions.jpg" alt="Quản lý khuyến mãi" width="100%" />
      <br /><strong>Khuyến mãi</strong>
    </td>
  </tr>
</table>

## Cấu Trúc Thư Mục

```text
e-commerce-web-admin/
├── Controllers/                 MVC controllers cho từng module admin
├── Data/                        ApplicationDbContext, factory và seed data
├── Filters/                     RBAC authorization filter
├── Hubs/                        SignalR hub cho tin nhắn khách hàng
├── Integrations/GiaoHangNhanh/  Client, options và model tích hợp GHN
├── Migrations/                  EF Core migrations cho SQL Server
├── Models/                      Entity, enum, constant và validation rule
├── Services/                    Business service theo từng module
├── ViewModels/                  ViewModel/DTO phục vụ Razor views
├── Views/                       Razor UI
├── wwwroot/css/                 CSS riêng theo module
├── wwwroot/js/                  JavaScript riêng theo module
├── docs/                        Tài liệu kỹ thuật, UML và ảnh README
└── Program.cs                   Đăng ký DI, middleware, auth, routing
```

## Cấu Hình Quan Trọng

Các cấu hình chính nằm trong `appsettings.example.json`:

| Section | Ý nghĩa |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Chuỗi kết nối SQL Server. |
| `Cloudinary` | Thông tin upload ảnh thương hiệu, danh mục, sản phẩm và biến thể. |
| `GiaoHangNhanh` | Token, ShopId, endpoint, cấu hình báo giá, tạo vận đơn, hủy và đồng bộ trạng thái. |
| `CustomerMessages:AllowedCustomerOrigins` | Danh sách origin của web customer được phép kết nối realtime. |
| `CustomerMessages:Jwt` | Issuer, audience và signing key cho luồng tin nhắn customer/admin/AI. |

Không đưa token Cloudinary, GHN hoặc signing key thật vào README hay commit công khai. Khi chạy local, ưu tiên dùng `appsettings.Development.json` hoặc `dotnet user-secrets`.

## Hướng Dẫn Clone Về Để Chạy

### 1. Chuẩn Bị Môi Trường

Cài các công cụ sau:

- Git.
- .NET SDK 10.
- SQL Server hoặc SQL Server LocalDB.
- SQL Server Management Studio hoặc `sqlcmd`.

### 2. Clone Source Code

```powershell
git clone <repository-url>
cd e-commerce-web-admin
```

### 3. Restore Package Và Tool

```powershell
dotnet restore
dotnet tool restore
```

### 4. Cấu Hình Database

Tạo file cấu hình local từ file mẫu:

```powershell
copy appsettings.example.json appsettings.Development.json
```

Sửa `ConnectionStrings:DefaultConnection` trong `appsettings.Development.json`. Ví dụ dùng LocalDB:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ECommerceAdmin;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Nếu chưa cấu hình GHN hoặc Cloudinary, có thể để GHN tắt khi chạy local:

```json
{
  "GiaoHangNhanh": {
    "Enabled": false,
    "EnableBackgroundStatusSync": false
  }
}
```

### 5. Tạo Database Bằng Migration

```powershell
dotnet ef database update
```

### 6. Tạo Tài Khoản Admin Local Lần Đầu

Sau khi migration xong, chạy script sau trong đúng database vừa tạo. Tài khoản demo local là:

```text
Username: readme-admin
Password: Admin@123456
```

Chỉ dùng tài khoản này cho development. Khi đưa lên môi trường thật, hãy đổi mật khẩu hoặc tạo admin riêng.

```sql
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;

DECLARE @hash nvarchar(max) =
    N'AQAAAAIAAYagAAAAEIJVjsSYciD+RpA2IAjyFCuK7kxlb6edpR5qGv2I4wHB9m7flDORbE3vwei/ZdmfnQ==';

IF NOT EXISTS (SELECT 1 FROM staff_roles WHERE NormalizedName = N'ADMIN')
BEGIN
    INSERT INTO staff_roles (Name, NormalizedName, ConcurrencyStamp)
    VALUES (N'Admin', N'ADMIN', CONVERT(nvarchar(36), NEWID()));
END;

DECLARE @adminRoleId bigint =
    (SELECT Id FROM staff_roles WHERE NormalizedName = N'ADMIN');

IF NOT EXISTS (SELECT 1 FROM staff WHERE NormalizedUserName = N'README-ADMIN')
BEGIN
    INSERT INTO staff (
        FullName, IsActive, AvatarImage, CreatedAt, UpdatedAt,
        UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
        LockoutEnabled, AccessFailedCount
    )
    VALUES (
        N'Quản trị Demo README', 1, NULL, SYSUTCDATETIME(), NULL,
        N'readme-admin', N'README-ADMIN',
        N'readme-admin@ecommerce.local', N'README-ADMIN@ECOMMERCE.LOCAL',
        1, @hash, CONVERT(nvarchar(36), NEWID()), CONVERT(nvarchar(36), NEWID()),
        N'0900000000', 0, 0, 1, 0
    );
END;

DECLARE @staffId bigint =
    (SELECT Id FROM staff WHERE NormalizedUserName = N'README-ADMIN');

IF NOT EXISTS (
    SELECT 1
    FROM staff_user_roles
    WHERE UserId = @staffId AND RoleId = @adminRoleId
)
BEGIN
    INSERT INTO staff_user_roles (UserId, RoleId)
    VALUES (@staffId, @adminRoleId);
END;
```

### 7. Chạy Web Admin

```powershell
dotnet run --launch-profile http
```

Mở trình duyệt:

```text
http://localhost:5081
```

Đăng nhập bằng tài khoản admin local đã tạo ở bước 6. Sau khi vào hệ thống, có thể tạo thêm nhân sự, vai trò và cấu hình quyền tại các màn hình `Quản lý nhân sự` và `Phân quyền`.
