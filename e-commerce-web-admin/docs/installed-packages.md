# Các gói NuGet đã cài đặt

Tài liệu này ghi lại các gói NuGet cần thiết cho dự án ASP.NET Core MVC kết hợp SQL Server, kèm mục đích sử dụng của từng gói.

## Thông tin dự án

- Framework: `.NET 10`
- Kiểu ứng dụng: `ASP.NET Core MVC`
- Database dự kiến: `SQL Server`
- ORM: `Entity Framework Core`
- File cấu hình package: `e-commerce-web-admin.csproj`

## Danh sách gói

| Gói | Version | Mục đích |
| --- | --- | --- |
| `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.8` | Cho phép Entity Framework Core kết nối và làm việc với SQL Server. Gói này cung cấp SQL Server provider để cấu hình `UseSqlServer(...)` trong `DbContext`. |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.8` | Hỗ trợ các tác vụ design-time của EF Core, đặc biệt khi tạo migration, scaffold database, và build model snapshot. Thường cần khi chạy `dotnet ef migrations add`. |
| `Microsoft.EntityFrameworkCore.Tools` | `10.0.8` | Cung cấp công cụ EF Core cho Package Manager Console/Visual Studio và hỗ trợ các lệnh migration, update database. |
| `Microsoft.VisualStudio.Web.CodeGeneration.Design` | `10.0.2` | Hỗ trợ scaffold MVC controller, Razor view, và CRUD screen từ model. Hữu ích khi tạo nhanh trang quản trị sản phẩm, danh mục, đơn hàng. |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | `10.0.8` | Tích hợp ASP.NET Core Identity với Entity Framework Core để lưu user, role, claim, login token vào SQL Server. Phù hợp cho hệ thống admin có đăng nhập và phân quyền. |
| `Microsoft.AspNetCore.Identity.UI` | `10.0.8` | Cung cấp UI mặc định cho các tính năng Identity như đăng nhập, đăng ký, quản lý tài khoản. Có thể scaffold ra để tùy biến giao diện sau. |
| `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` | `10.0.8` | Hỗ trợ hiển thị lỗi liên quan EF Core trong môi trường development, ví dụ lỗi migration chưa được áp dụng. |
| `AutoMapper` | `16.1.1` | Hỗ trợ map dữ liệu giữa Entity, ViewModel và DTO. Package này thay cho `AutoMapper.Extensions.Microsoft.DependencyInjection` vì extension cũ kéo theo `AutoMapper 12.0.1` có cảnh báo bảo mật. |
| `FluentValidation.AspNetCore` | `11.3.1` | Hỗ trợ validate dữ liệu đầu vào bằng FluentValidation, phù hợp khi rule validation phức tạp hơn DataAnnotations. |
| `NuGet.Packaging` | `7.6.0` | Direct reference để ép dependency graph dùng phiên bản không còn cảnh báo bảo mật thay cho bản transitive `6.12.1` từ gói scaffold. |
| `NuGet.Protocol` | `7.6.0` | Direct reference để ép dependency graph dùng phiên bản không còn cảnh báo bảo mật thay cho bản transitive `6.12.1` từ gói scaffold. |

## Lệnh cài đặt

Nếu cần cài lại trên một máy khác, chạy các lệnh sau trong thư mục chứa file `.csproj`:

```powershell
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Identity.UI
dotnet add package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
dotnet add package AutoMapper --version 16.1.1
dotnet add package FluentValidation.AspNetCore
dotnet add package NuGet.Packaging --version 7.6.0
dotnet add package NuGet.Protocol --version 7.6.0
```

## Cách các gói này được dùng trong dự án

### 1. Kết nối SQL Server

Gói `Microsoft.EntityFrameworkCore.SqlServer` sẽ được dùng khi đăng ký `DbContext` trong `Program.cs`:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

Connection string nên đặt trong `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ECommerceWebAdmin;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### 2. Tạo và cập nhật database bằng migration

Sau khi có model và `ApplicationDbContext`, có thể tạo migration:

```powershell
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Trong đó:

- `Microsoft.EntityFrameworkCore.Design` giúp EF Core đọc được cấu trúc project tại design-time.
- `Microsoft.EntityFrameworkCore.Tools` hỗ trợ các lệnh migration/update database.

### 3. Đăng nhập và phân quyền admin

Với web admin e-commerce, nên dùng Identity để quản lý:

- Tài khoản admin.
- Mật khẩu đã hash.
- Role như `Admin`, `Staff`, `Manager`.
- Phân quyền truy cập controller/action.

Gói liên quan:

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.AspNetCore.Identity.UI`

### 4. Scaffold nhanh controller và view CRUD

Gói `Microsoft.VisualStudio.Web.CodeGeneration.Design` hỗ trợ tạo nhanh controller và view từ model, ví dụ:

```powershell
dotnet aspnet-codegenerator controller `
  -name ProductsController `
  -m Product `
  -dc ApplicationDbContext `
  --relativeFolderPath Controllers `
  --useDefaultLayout `
  --referenceScriptLibraries
```

Chức năng này hữu ích khi làm nhanh các màn hình quản trị:

- Sản phẩm.
- Biến thể sản phẩm.
- Danh mục.
- Thương hiệu.
- Đơn hàng.
- Voucher.
- Promotion.

### 5. Map dữ liệu giữa Entity, ViewModel và DTO

Gói `AutoMapper` giúp map dữ liệu giữa Entity, ViewModel và DTO. Với các phiên bản AutoMapper mới, package extension DI cũ không còn cần thiết cho dự án này.

```csharp
builder.Services.AddAutoMapper(typeof(Program));
```

Trong web admin e-commerce, AutoMapper thường dùng để:

- Map `Product` sang `ProductViewModel`.
- Map form tạo/sửa sản phẩm sang Entity.
- Tránh viết lặp lại nhiều đoạn gán dữ liệu thủ công.

### 6. Validate form bằng FluentValidation

Gói `FluentValidation.AspNetCore` giúp viết rule validation rõ ràng hơn khi DataAnnotations không đủ linh hoạt.

Ví dụ validator cho form sản phẩm:

```csharp
public class ProductViewModelValidator : AbstractValidator<ProductViewModel>
{
    public ProductViewModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0);
    }
}
```

FluentValidation phù hợp cho các rule như:

- Validate giá bán, giá khuyến mãi.
- Validate tồn kho.
- Validate ngày bắt đầu/kết thúc voucher.
- Validate dữ liệu biến thể sản phẩm.

## Trạng thái warning package

Các warning package trước đó đã được xử lý:

| Package có warning trước đó | Nguyên nhân | Cách xử lý |
| --- | --- | --- |
| `AutoMapper 12.0.1` | Bị kéo theo bởi `AutoMapper.Extensions.Microsoft.DependencyInjection 12.0.1` và có cảnh báo `NU1903` mức cao. | Gỡ `AutoMapper.Extensions.Microsoft.DependencyInjection`, cài trực tiếp `AutoMapper 16.1.1`. |
| `NuGet.Packaging 6.12.1` | Dependency gián tiếp của công cụ scaffold, có cảnh báo `NU1901` mức thấp. | Thêm direct reference `NuGet.Packaging 7.6.0`. |
| `NuGet.Protocol 6.12.1` | Dependency gián tiếp của công cụ scaffold, có cảnh báo `NU1901` mức thấp. | Thêm direct reference `NuGet.Protocol 7.6.0`. |

Kết quả kiểm tra hiện tại:

```powershell
dotnet build --no-restore
```

Kết quả: `0 Warning(s), 0 Error(s)`.

```powershell
dotnet list package --vulnerable --include-transitive
```

Kết quả: project không còn vulnerable package theo nguồn NuGet hiện tại.

## Gói chưa cần cài ngay

Một số gói có thể cần sau này, nhưng chưa nên cài khi chưa có nhu cầu cụ thể:

| Gói | Khi nào cần |
| --- | --- |
| `Microsoft.AspNetCore.Authentication.Google` | Khi cần đăng nhập bằng Google. |
| `Microsoft.AspNetCore.Authentication.Facebook` | Khi cần đăng nhập bằng Facebook. |
| `Serilog.AspNetCore` | Khi cần logging nâng cao ra file, console, hoặc hệ thống giám sát. |
