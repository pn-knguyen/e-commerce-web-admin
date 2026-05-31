# Tài liệu quản lý phương thức thanh toán

Tài liệu này giải thích toàn bộ phần code phục vụ màn hình quản lý **phương thức thanh toán** trong admin. Module này dùng bảng có sẵn `payment_methods`, không tạo migration mới, và được tách theo các lớp rõ ràng:

- Backend nhận request qua controller.
- Service xử lý nghiệp vụ và database.
- ViewModel làm lớp dữ liệu dành riêng cho UI.
- Razor render giao diện.
- JavaScript xử lý tương tác nhỏ ở trình duyệt.
- CSS chứa style riêng của module.

## 1. Danh sách file liên quan

```text
Program.cs
Models/Entities/OrderEntities.cs
Data/ApplicationDbContext.cs
Controllers/PaymentMethodsController.cs
Services/PaymentMethods/IPaymentMethodAdminService.cs
Services/PaymentMethods/PaymentMethodAdminService.cs
Services/PaymentMethods/PaymentMethodServiceResults.cs
ViewModels/PaymentMethods/PaymentMethodViewModels.cs
Views/PaymentMethods/Index.cshtml
Views/PaymentMethods/Create.cshtml
Views/PaymentMethods/Edit.cshtml
Views/PaymentMethods/_Form.cshtml
wwwroot/js/payment-methods.js
wwwroot/css/payment-methods.css
Views/Shared/_AdminLayout.cshtml
Views/Shared/_Layout.cshtml
```

Ý nghĩa từng nhóm:

- `Program.cs`: đăng ký service vào Dependency Injection.
- `Models/Entities/OrderEntities.cs`: khai báo entity `PaymentMethod` và quan hệ với `Order`.
- `Data/ApplicationDbContext.cs`: map entity `PaymentMethod` vào bảng `payment_methods`.
- `Controllers/PaymentMethodsController.cs`: nhận request HTTP, gọi service, trả view hoặc JSON.
- `Services/PaymentMethods/*`: chứa interface, nghiệp vụ, kết quả trả về từ nghiệp vụ.
- `ViewModels/PaymentMethods/*`: chứa dữ liệu mà view cần, không đưa trực tiếp entity EF Core ra UI.
- `Views/PaymentMethods/*`: giao diện danh sách, tạo mới, chỉnh sửa và form dùng chung.
- `wwwroot/js/payment-methods.js`: xử lý toggle trạng thái, kiểm tra xóa, xác nhận xóa, đóng toast.
- `wwwroot/css/payment-methods.css`: style riêng cho bảng, filter, nút trạng thái, nút thao tác và responsive.
- `Views/Shared/_AdminLayout.cshtml`, `_Layout.cshtml`: nơi có link điều hướng tới `/PaymentMethods`.

## 2. Đăng ký service trong Program

File: `Program.cs`

```csharp
using e_commerce_web_admin.Services.PaymentMethods;

builder.Services.AddScoped<IPaymentMethodAdminService, PaymentMethodAdminService>();
```

Giải thích:

- `using e_commerce_web_admin.Services.PaymentMethods;` cho phép `Program.cs` nhìn thấy interface và implementation của module.
- `AddScoped` tạo một instance `PaymentMethodAdminService` cho mỗi request HTTP.
- Controller chỉ phụ thuộc vào `IPaymentMethodAdminService`, không phụ thuộc trực tiếp vào `ApplicationDbContext`.

Lợi ích của cách tách này:

- Controller mỏng, dễ đọc.
- Service gom toàn bộ nghiệp vụ.
- Sau này nếu muốn test hoặc đổi logic xử lý, chỉ cần đổi service mà không đụng nhiều vào controller.

## 3. Entity PaymentMethod

File: `Models/Entities/OrderEntities.cs`

```csharp
public class PaymentMethod
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
```

Giải thích từng thuộc tính:

- `Id`: khóa chính của phương thức thanh toán.
- `Name`: tên phương thức, ví dụ `Thanh toán khi nhận hàng`.
- `Description`: mô tả hoặc ghi chú thêm, có thể null.
- `IsActive`: cho biết phương thức còn được dùng hay không.
- `Orders`: navigation property, thể hiện một phương thức thanh toán có thể được nhiều đơn hàng sử dụng.

Quan hệ với đơn hàng:

```csharp
public class Order
{
    public long PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
}
```

Ý nghĩa:

- Mỗi đơn hàng bắt buộc có `PaymentMethodId`.
- `PaymentMethod` là object liên kết khi EF Core load quan hệ.
- Vì `Order.PaymentMethodId` là khóa ngoại, module xóa phương thức thanh toán phải kiểm tra trước xem phương thức đó đã có đơn hàng dùng chưa.

## 4. Mapping database

File: `Data/ApplicationDbContext.cs`

```csharp
modelBuilder.Entity<PaymentMethod>(entity =>
{
    entity.ToTable("payment_methods");
    entity.Property(method => method.Name).HasMaxLength(120).IsRequired();
    entity.Property(method => method.Description).HasMaxLength(500);
});
```

Giải thích:

- `ToTable("payment_methods")`: map entity `PaymentMethod` vào bảng `payment_methods`.
- `Name` có độ dài tối đa 120 ký tự và bắt buộc nhập.
- `Description` có độ dài tối đa 500 ký tự.

Hiện tại `Name` chưa có unique index ở database. Việc chống trùng tên đang nằm ở service:

```csharp
method => method.Name == form.Name
```

Điều này ổn với thao tác admin thông thường. Nếu cần chặn tuyệt đối cả trường hợp ghi thẳng DB hoặc nhiều request đồng thời, nên bổ sung migration unique index sau:

```csharp
entity.HasIndex(method => method.Name).IsUnique();
```

## 5. ViewModel

File: `ViewModels/PaymentMethods/PaymentMethodViewModels.cs`

ViewModel là lớp dữ liệu riêng cho giao diện. Module này không đưa entity `PaymentMethod` trực tiếp ra view.

### 5.1. PaymentMethodIndexQuery

```csharp
public sealed class PaymentMethodIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}
```

Mục đích:

- Nhận điều kiện lọc từ URL.
- `Search`: từ khóa tìm theo tên hoặc mô tả.
- `Status`: lọc theo `active`, `inactive` hoặc tất cả.
- `Page`: trang hiện tại.

Controller tạo object này từ query string:

```csharp
new PaymentMethodIndexQuery
{
    Search = search,
    Status = status,
    Page = page,
}
```

### 5.2. PaymentMethodIndexViewModel

```csharp
public sealed class PaymentMethodIndexViewModel
{
    public List<PaymentMethodRowViewModel> PaymentMethods { get; set; } = [];
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int TotalOrderUsageCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}
```

Mục đích:

- Chứa dữ liệu cho màn danh sách.
- `PaymentMethods`: các dòng hiển thị trong bảng.
- `Search`, `Status`: giữ lại giá trị filter để render lại input/select.
- `TotalCount`, `ActiveCount`, `InactiveCount`, `TotalOrderUsageCount`: số liệu ở các card thống kê.
- `TotalPages`, `HasPrev`, `HasNext`: phục vụ phân trang.

Ví dụ view dùng `TotalPages`:

```cshtml
@if (Model.TotalPages > 1)
{
    // render pagination
}
```

### 5.3. PaymentMethodRowViewModel

```csharp
public sealed class PaymentMethodRowViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int OrderCount { get; set; }
}
```

Mục đích:

- Đại diện cho một dòng trong danh sách.
- `OrderCount` không nằm trực tiếp trong entity `PaymentMethod`, mà được tính từ quan hệ `Orders.Count`.

View dùng object này khi lặp danh sách:

```cshtml
@foreach (var method in Model.PaymentMethods)
{
    <p class="font-semibold text-slate-800 text-sm truncate">@method.Name</p>
    <span>@method.OrderCount</span>
}
```

### 5.4. PaymentMethodFormViewModel

```csharp
public sealed class PaymentMethodFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tên phương thức thanh toán là bắt buộc.")]
    [StringLength(120, ErrorMessage = "Tên phương thức thanh toán tối đa 120 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
```

Mục đích:

- Dùng chung cho form tạo mới và chỉnh sửa.
- Validation attribute giúp MVC tự kiểm tra dữ liệu trước khi gọi service.
- `Id` dùng để kiểm tra đúng bản ghi khi edit.

Controller kiểm tra validation:

```csharp
if (!ModelState.IsValid)
{
    return View(viewModel);
}
```

## 6. Service interface

File: `Services/PaymentMethods/IPaymentMethodAdminService.cs`

```csharp
public interface IPaymentMethodAdminService
{
    Task<PaymentMethodIndexViewModel> GetIndexAsync(PaymentMethodIndexQuery query, CancellationToken ct = default);
    Task<PaymentMethodFormViewModel> GetCreateFormAsync(CancellationToken ct = default);
    Task<PaymentMethodFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default);
    Task<PaymentMethodSaveResult> CreateAsync(PaymentMethodFormViewModel form, CancellationToken ct = default);
    Task<PaymentMethodSaveResult> UpdateAsync(long id, PaymentMethodFormViewModel form, CancellationToken ct = default);
    Task<PaymentMethodDeleteCheckResult> CheckDeleteAsync(long id, CancellationToken ct = default);
    Task<PaymentMethodDeleteResult> DeleteAsync(long id, CancellationToken ct = default);
    Task<PaymentMethodToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default);
}
```

Mục đích:

- Định nghĩa rõ controller được phép gọi những nghiệp vụ nào.
- Giúp controller không cần biết service xử lý bằng EF Core, API hay cách khác.
- Dễ thay implementation khi test.

Các nhóm nghiệp vụ:

- `GetIndexAsync`: lấy dữ liệu màn danh sách.
- `GetCreateFormAsync`, `GetEditFormAsync`: chuẩn bị form.
- `CreateAsync`, `UpdateAsync`: lưu dữ liệu.
- `CheckDeleteAsync`, `DeleteAsync`: kiểm tra xóa và xóa.
- `ToggleActiveAsync`: bật/tắt trạng thái.

## 7. Service result objects

File: `Services/PaymentMethods/PaymentMethodServiceResults.cs`

Các result object giúp service trả về kết quả có cấu trúc, thay vì ném string rời rạc về controller.

### 7.1. PaymentMethodValidationError

```csharp
public sealed record PaymentMethodValidationError(string FieldName, string Message);
```

Mục đích:

- Đại diện cho lỗi validation nghiệp vụ.
- `FieldName`: tên field trong form, ví dụ `Name`.
- `Message`: nội dung lỗi.

Controller đưa lỗi này vào `ModelState`:

```csharp
ModelState.AddModelError(error.FieldName, error.Message);
```

### 7.2. PaymentMethodSaveResult

```csharp
public sealed class PaymentMethodSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public PaymentMethodFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<PaymentMethodValidationError> Errors { get; init; } = [];
}
```

Mục đích:

- Dùng cho tạo mới và cập nhật.
- `Succeeded`: lưu thành công hay thất bại.
- `Message`: thông báo thành công.
- `Form`: dữ liệu form sau khi normalize hoặc giữ lại dữ liệu lỗi.
- `Errors`: danh sách lỗi nghiệp vụ.

Factory success:

```csharp
public static PaymentMethodSaveResult Success(PaymentMethodFormViewModel form, string message) =>
    new() { Succeeded = true, Form = form, Message = message };
```

Factory failed:

```csharp
public static PaymentMethodSaveResult Failed(
    PaymentMethodFormViewModel form,
    IReadOnlyCollection<PaymentMethodValidationError> errors) =>
    new() { Succeeded = false, Form = form, Errors = errors };
```

Controller dùng như sau:

```csharp
var result = await _paymentMethodService.CreateAsync(viewModel, ct);
if (!result.Succeeded)
{
    AddErrors(result.Errors);
    return View(result.Form);
}
```

### 7.3. PaymentMethodDeleteCheckResult

```csharp
public sealed class PaymentMethodDeleteCheckResult
{
    public bool Found { get; init; }
    public bool CanDelete { get; init; }
    public string MethodName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Blockers { get; init; } = [];
}
```

Mục đích:

- Dùng để kiểm tra một phương thức có thể xóa không.
- `Found`: có tìm thấy bản ghi không.
- `CanDelete`: có được phép xóa không.
- `Blockers`: danh sách dữ liệu liên quan đang chặn xóa, ví dụ `3 đơn hàng`.

Khi được phép xóa:

```csharp
public static PaymentMethodDeleteCheckResult Allowed(string methodName) =>
    new()
    {
        Found = true,
        CanDelete = true,
        MethodName = methodName,
        Message = $"Có thể xóa phương thức thanh toán \"{methodName}\".",
    };
```

Khi bị chặn:

```csharp
public static PaymentMethodDeleteCheckResult Blocked(string methodName, IReadOnlyList<string> blockers) =>
    new()
    {
        Found = true,
        CanDelete = false,
        MethodName = methodName,
        Blockers = blockers,
        Message = $"Không thể xóa \"{methodName}\" vì còn {string.Join(", ", blockers)} liên quan.",
    };
```

JavaScript gọi endpoint `CheckDelete` và nhận JSON từ object này.

### 7.4. PaymentMethodDeleteResult

```csharp
public sealed class PaymentMethodDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
}
```

Mục đích:

- Dùng cho thao tác xóa thật.
- Controller dựa vào `Found` để trả `NotFound`.
- Controller dựa vào `Succeeded` để chọn `TempData["Success"]` hoặc `TempData["Error"]`.

Controller dùng như sau:

```csharp
TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
```

### 7.5. PaymentMethodToggleResult

```csharp
public sealed record PaymentMethodToggleResult(bool Value);
```

Mục đích:

- Trả trạng thái mới sau khi bật/tắt.
- Controller chuyển thành JSON:

```csharp
return result is null ? NotFound() : Ok(new { isActive = result.Value });
```

## 8. Service xử lý nghiệp vụ

File: `Services/PaymentMethods/PaymentMethodAdminService.cs`

Service này là trung tâm nghiệp vụ của module.

```csharp
public sealed class PaymentMethodAdminService : IPaymentMethodAdminService
{
    private const int DefaultPageSize = 30;

    private readonly ApplicationDbContext _db;

    public PaymentMethodAdminService(ApplicationDbContext db) => _db = db;
}
```

Giải thích:

- `DefaultPageSize = 30`: mỗi trang hiển thị 30 phương thức.
- `_db`: DbContext để truy vấn và lưu dữ liệu.
- Constructor nhận `ApplicationDbContext` qua DI.

### 8.1. GetIndexAsync

Code chính:

```csharp
public async Task<PaymentMethodIndexViewModel> GetIndexAsync(
    PaymentMethodIndexQuery query,
    CancellationToken ct = default)
{
    var page = Math.Max(1, query.Page);
    var dbQuery = _db.PaymentMethods.AsNoTracking();
}
```

Giải thích:

- `Math.Max(1, query.Page)` đảm bảo page nhỏ nhất là 1.
- `AsNoTracking()` dùng cho màn đọc dữ liệu, EF Core không cần tracking entity, giúp query nhẹ hơn.

Lọc trạng thái:

```csharp
if (query.Status == "active")
{
    dbQuery = dbQuery.Where(method => method.IsActive);
}
else if (query.Status == "inactive")
{
    dbQuery = dbQuery.Where(method => !method.IsActive);
}
```

Ý nghĩa:

- `active`: chỉ lấy phương thức đang bật.
- `inactive`: chỉ lấy phương thức đang tắt.
- Giá trị khác hoặc rỗng: lấy tất cả.

Tìm kiếm:

```csharp
if (!string.IsNullOrWhiteSpace(query.Search))
{
    var term = query.Search.Trim();
    dbQuery = dbQuery.Where(method =>
        method.Name.Contains(term) ||
        (method.Description != null && method.Description.Contains(term)));
}
```

Ý nghĩa:

- Cắt khoảng trắng đầu/cuối từ khóa.
- Tìm theo `Name`.
- Nếu `Description` không null thì tìm thêm trong mô tả.

Tính thống kê:

```csharp
var totalCount = await dbQuery.CountAsync(ct);
var activeCount = await dbQuery.CountAsync(method => method.IsActive, ct);
var inactiveCount = await dbQuery.CountAsync(method => !method.IsActive, ct);
var totalOrderUsageCount = totalCount == 0
    ? 0
    : await dbQuery.SumAsync(method => method.Orders.Count, ct);
```

Ý nghĩa:

- `totalCount`: tổng số phương thức sau khi lọc.
- `activeCount`: số phương thức đang bật trong tập đã lọc.
- `inactiveCount`: số phương thức đang tắt trong tập đã lọc.
- `totalOrderUsageCount`: tổng số đơn hàng đang sử dụng các phương thức trong tập đã lọc.
- Có check `totalCount == 0` để tránh query sum không cần thiết.

Lấy dữ liệu trang hiện tại:

```csharp
var rows = await dbQuery
    .OrderBy(method => method.Name)
    .Skip((page - 1) * DefaultPageSize)
    .Take(DefaultPageSize)
    .Select(method => new PaymentMethodRowViewModel
    {
        Id = method.Id,
        Name = method.Name,
        Description = method.Description,
        IsActive = method.IsActive,
        OrderCount = method.Orders.Count,
    })
    .ToListAsync(ct);
```

Ý nghĩa:

- Sắp xếp theo tên.
- `Skip` và `Take` để phân trang.
- Chỉ select đúng dữ liệu view cần, không trả nguyên entity.
- `OrderCount = method.Orders.Count` được EF Core dịch thành subquery count.

Trả viewmodel:

```csharp
return new PaymentMethodIndexViewModel
{
    PaymentMethods = rows,
    Search = query.Search,
    Status = query.Status,
    Page = page,
    PageSize = DefaultPageSize,
    TotalCount = totalCount,
    ActiveCount = activeCount,
    InactiveCount = inactiveCount,
    TotalOrderUsageCount = totalOrderUsageCount,
};
```

### 8.2. GetCreateFormAsync

```csharp
public Task<PaymentMethodFormViewModel> GetCreateFormAsync(CancellationToken ct = default)
    => Task.FromResult(new PaymentMethodFormViewModel { IsActive = true });
```

Mục đích:

- Chuẩn bị form tạo mới.
- Mặc định `IsActive = true` để phương thức mới được bật.
- Dùng `Task.FromResult` để giữ cùng kiểu async với các method service khác.

### 8.3. GetEditFormAsync

```csharp
public async Task<PaymentMethodFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default)
{
    var entity = await _db.PaymentMethods
        .AsNoTracking()
        .FirstOrDefaultAsync(method => method.Id == id, ct);

    if (entity is null)
    {
        return null;
    }

    return new PaymentMethodFormViewModel
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        IsActive = entity.IsActive,
    };
}
```

Mục đích:

- Tìm phương thức theo `id`.
- Nếu không có thì trả `null` để controller trả `NotFound`.
- Nếu có thì map entity sang `PaymentMethodFormViewModel`.

Vì đây chỉ là đọc dữ liệu để render form, dùng `AsNoTracking()`.

### 8.4. CreateAsync

```csharp
public async Task<PaymentMethodSaveResult> CreateAsync(
    PaymentMethodFormViewModel form,
    CancellationToken ct = default)
{
    NormalizeForm(form);

    var errors = await ValidateFormAsync(form, existingId: null, ct);
    if (errors.Count > 0)
    {
        return PaymentMethodSaveResult.Failed(form, errors);
    }
}
```

Ý nghĩa đoạn đầu:

- `NormalizeForm(form)`: chuẩn hóa dữ liệu trước khi validate và lưu.
- `existingId: null`: vì đang tạo mới nên không loại trừ bản ghi nào.
- Nếu có lỗi nghiệp vụ thì trả failed result.

Tạo entity:

```csharp
var entity = new PaymentMethod
{
    Name = form.Name,
    Description = form.Description,
    IsActive = form.IsActive,
};
```

Lưu database:

```csharp
_db.PaymentMethods.Add(entity);
await _db.SaveChangesAsync(ct);
```

Cập nhật id và trả thông báo:

```csharp
form.Id = entity.Id;
return PaymentMethodSaveResult.Success(
    form,
    $"Đã tạo phương thức thanh toán \"{entity.Name}\" thành công.");
```

### 8.5. UpdateAsync

```csharp
public async Task<PaymentMethodSaveResult> UpdateAsync(
    long id,
    PaymentMethodFormViewModel form,
    CancellationToken ct = default)
{
    NormalizeForm(form);

    var entity = await _db.PaymentMethods.FirstOrDefaultAsync(method => method.Id == id, ct);
    if (entity is null)
    {
        return PaymentMethodSaveResult.Failed(
            form,
            [new PaymentMethodValidationError(string.Empty, "Không tìm thấy phương thức thanh toán.")]);
    }
}
```

Ý nghĩa:

- Chuẩn hóa form.
- Tìm entity cần cập nhật.
- Nếu không thấy, trả lỗi nghiệp vụ.

Validate khi sửa:

```csharp
var errors = await ValidateFormAsync(form, existingId: id, ct);
if (errors.Count > 0)
{
    return PaymentMethodSaveResult.Failed(form, errors);
}
```

`existingId: id` giúp service không báo trùng với chính bản ghi đang sửa.

Cập nhật entity:

```csharp
entity.Name = form.Name;
entity.Description = form.Description;
entity.IsActive = form.IsActive;

await _db.SaveChangesAsync(ct);
```

Trả kết quả:

```csharp
return PaymentMethodSaveResult.Success(
    form,
    $"Đã cập nhật phương thức thanh toán \"{entity.Name}\" thành công.");
```

### 8.6. CheckDeleteAsync

```csharp
public async Task<PaymentMethodDeleteCheckResult> CheckDeleteAsync(
    long id,
    CancellationToken ct = default)
{
    var method = await _db.PaymentMethods
        .AsNoTracking()
        .Where(item => item.Id == id)
        .Select(item => new
        {
            item.Name,
            OrderCount = item.Orders.Count,
        })
        .FirstOrDefaultAsync(ct);
}
```

Mục đích:

- Kiểm tra phương thức có tồn tại không.
- Tính số đơn hàng đang dùng phương thức này.
- Chỉ select `Name` và `OrderCount`, không load nguyên entity.

Nếu không tìm thấy:

```csharp
if (method is null)
{
    return PaymentMethodDeleteCheckResult.NotFound();
}
```

Tạo danh sách dữ liệu chặn xóa:

```csharp
var blockers = BuildDeleteBlockers(method.OrderCount);
return blockers.Count == 0
    ? PaymentMethodDeleteCheckResult.Allowed(method.Name)
    : PaymentMethodDeleteCheckResult.Blocked(method.Name, blockers);
```

Ý nghĩa:

- Nếu không có blocker thì có thể xóa.
- Nếu có đơn hàng liên quan thì chặn xóa.

### 8.7. DeleteAsync

```csharp
public async Task<PaymentMethodDeleteResult> DeleteAsync(long id, CancellationToken ct = default)
{
    var deleteCheck = await CheckDeleteAsync(id, ct);
    if (!deleteCheck.Found)
    {
        return PaymentMethodDeleteResult.NotFound();
    }

    if (!deleteCheck.CanDelete)
    {
        return PaymentMethodDeleteResult.Failed(deleteCheck.Message);
    }
}
```

Mục đích:

- Luôn check điều kiện trước khi xóa thật.
- Nếu không tìm thấy, trả not found.
- Nếu còn đơn hàng liên quan, không xóa và trả message lỗi.

Xóa thật:

```csharp
var entity = await _db.PaymentMethods.FirstOrDefaultAsync(method => method.Id == id, ct);
if (entity is null)
{
    return PaymentMethodDeleteResult.NotFound();
}

_db.PaymentMethods.Remove(entity);
await _db.SaveChangesAsync(ct);
```

Trả kết quả:

```csharp
return PaymentMethodDeleteResult.Success(
    $"Đã xóa phương thức thanh toán \"{entity.Name}\" thành công.");
```

Lưu ý:

- Có một khoảng rất nhỏ giữa `CheckDeleteAsync` và `SaveChangesAsync`. Nếu đúng lúc đó phát sinh đơn hàng mới dùng phương thức này, DB có thể chặn bằng FK.
- Với admin thông thường, rủi ro này thấp.

### 8.8. ToggleActiveAsync

```csharp
public async Task<PaymentMethodToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default)
{
    var entity = await _db.PaymentMethods.FirstOrDefaultAsync(method => method.Id == id, ct);
    if (entity is null)
    {
        return null;
    }

    entity.IsActive = !entity.IsActive;
    await _db.SaveChangesAsync(ct);

    return new PaymentMethodToggleResult(entity.IsActive);
}
```

Mục đích:

- Tìm phương thức theo id.
- Nếu không có thì trả `null`.
- Đảo trạng thái `IsActive`.
- Lưu database.
- Trả trạng thái mới.

View gọi chức năng này bằng JavaScript qua endpoint:

```text
POST /PaymentMethods/ToggleActive/{id}
```

### 8.9. ValidateFormAsync

```csharp
private async Task<List<PaymentMethodValidationError>> ValidateFormAsync(
    PaymentMethodFormViewModel form,
    long? existingId,
    CancellationToken ct)
{
    var errors = new List<PaymentMethodValidationError>();

    if (await _db.PaymentMethods.AnyAsync(
            method => method.Name == form.Name && (!existingId.HasValue || method.Id != existingId.Value),
            ct))
    {
        errors.Add(new PaymentMethodValidationError(
            nameof(form.Name),
            $"Tên phương thức thanh toán \"{form.Name}\" đã tồn tại."));
    }

    return errors;
}
```

Mục đích:

- Kiểm tra nghiệp vụ không cho trùng tên phương thức.
- Khi tạo mới, `existingId` là null nên chỉ cần thấy tên tồn tại là lỗi.
- Khi sửa, loại trừ chính bản ghi đang sửa bằng điều kiện `method.Id != existingId.Value`.

Điểm cần nhớ:

- Đây là validation ở tầng app, không phải unique index DB.
- Nếu thao tác qua UI bình thường thì đủ dùng.

### 8.10. BuildDeleteBlockers

```csharp
private static List<string> BuildDeleteBlockers(int orderCount)
{
    var blockers = new List<string>();
    if (orderCount > 0)
    {
        blockers.Add($"{orderCount} đơn hàng");
    }

    return blockers;
}
```

Mục đích:

- Gom các dữ liệu liên quan đang chặn xóa.
- Hiện tại chỉ có đơn hàng.
- Sau này nếu `payment_methods` liên kết thêm bảng khác, có thể bổ sung blocker ở đây.

### 8.11. NormalizeForm

```csharp
private static void NormalizeForm(PaymentMethodFormViewModel form)
{
    form.Name = form.Name.Trim();
    form.Description = string.IsNullOrWhiteSpace(form.Description)
        ? null
        : form.Description.Trim();
}
```

Mục đích:

- Xóa khoảng trắng dư ở tên.
- Nếu mô tả rỗng hoặc toàn khoảng trắng, lưu `null`.
- Nếu có mô tả, trim trước khi lưu.

Ví dụ:

```text
"  COD  " -> "COD"
"   "    -> null
```

## 9. Controller

File: `Controllers/PaymentMethodsController.cs`

Controller chỉ điều phối request/response. Nó không chứa query EF Core và không xử lý nghiệp vụ phức tạp.

### 9.1. Constructor

```csharp
private readonly IPaymentMethodAdminService _paymentMethodService;

public PaymentMethodsController(IPaymentMethodAdminService paymentMethodService)
    => _paymentMethodService = paymentMethodService;
```

Mục đích:

- Nhận service qua Dependency Injection.
- Controller phụ thuộc vào interface, không phụ thuộc implementation cụ thể.

### 9.2. Index

```csharp
public async Task<IActionResult> Index(
    string? search,
    string? status,
    int page = 1,
    CancellationToken ct = default)
{
    var viewModel = await _paymentMethodService.GetIndexAsync(
        new PaymentMethodIndexQuery
        {
            Search = search,
            Status = status,
            Page = page,
        },
        ct);

    return View(viewModel);
}
```

Route:

```text
GET /PaymentMethods
GET /PaymentMethods?search=cod&status=active&page=1
```

Mục đích:

- Nhận filter từ query string.
- Gói filter vào `PaymentMethodIndexQuery`.
- Gọi service lấy dữ liệu.
- Trả view `Views/PaymentMethods/Index.cshtml`.

### 9.3. Create GET

```csharp
public async Task<IActionResult> Create(CancellationToken ct)
    => View(await _paymentMethodService.GetCreateFormAsync(ct));
```

Route:

```text
GET /PaymentMethods/Create
```

Mục đích:

- Chuẩn bị form tạo mới.
- Mặc định form có `IsActive = true`.

### 9.4. Create POST

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Create(PaymentMethodFormViewModel viewModel, CancellationToken ct)
{
    if (!ModelState.IsValid)
    {
        return View(viewModel);
    }

    var result = await _paymentMethodService.CreateAsync(viewModel, ct);
    if (!result.Succeeded)
    {
        AddErrors(result.Errors);
        return View(result.Form);
    }

    TempData["Success"] = result.Message;
    return RedirectToAction(nameof(Index));
}
```

Route:

```text
POST /PaymentMethods/Create
```

Giải thích:

- `[HttpPost]`: action nhận request POST.
- `[ValidateAntiForgeryToken]`: chống CSRF.
- `ModelState.IsValid`: kiểm tra validation attribute trong `PaymentMethodFormViewModel`.
- Nếu service báo lỗi nghiệp vụ, đưa lỗi vào `ModelState`.
- Nếu thành công, lưu message vào `TempData["Success"]` và redirect về danh sách.

### 9.5. Edit GET

```csharp
public async Task<IActionResult> Edit(long id, CancellationToken ct)
{
    var viewModel = await _paymentMethodService.GetEditFormAsync(id, ct);
    return viewModel is null ? NotFound() : View(viewModel);
}
```

Route:

```text
GET /PaymentMethods/Edit/{id}
```

Mục đích:

- Lấy dữ liệu bản ghi cần sửa.
- Nếu không tồn tại, trả HTTP 404.
- Nếu tồn tại, render view edit.

### 9.6. Edit POST

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(long id, PaymentMethodFormViewModel viewModel, CancellationToken ct)
{
    if (id != viewModel.Id)
    {
        return BadRequest();
    }

    if (!ModelState.IsValid)
    {
        return View(viewModel);
    }

    var result = await _paymentMethodService.UpdateAsync(id, viewModel, ct);
    if (!result.Succeeded)
    {
        AddErrors(result.Errors);
        return View(result.Form);
    }

    TempData["Success"] = result.Message;
    return RedirectToAction(nameof(Index));
}
```

Route:

```text
POST /PaymentMethods/Edit/{id}
```

Giải thích:

- `id != viewModel.Id` chặn trường hợp route id và hidden input bị lệch.
- Validate form trước.
- Gọi service cập nhật.
- Nếu lỗi, render lại form.
- Nếu thành công, redirect về danh sách.

### 9.7. Delete

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(long id, CancellationToken ct)
{
    var result = await _paymentMethodService.DeleteAsync(id, ct);
    if (!result.Found)
    {
        return NotFound();
    }

    TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
    return RedirectToAction(nameof(Index));
}
```

Route:

```text
POST /PaymentMethods/Delete/{id}
```

Mục đích:

- Chỉ cho xóa bằng POST.
- Có anti-forgery token.
- Nếu không tìm thấy bản ghi thì 404.
- Nếu không xóa được vì còn đơn hàng, show toast lỗi.
- Nếu xóa được, show toast thành công.

### 9.8. CheckDelete

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> CheckDelete(long id, CancellationToken ct)
{
    var result = await _paymentMethodService.CheckDeleteAsync(id, ct);
    if (!result.Found)
    {
        return NotFound();
    }

    return Ok(new
    {
        canDelete = result.CanDelete,
        message = result.Message,
        blockers = result.Blockers,
    });
}
```

Route:

```text
POST /PaymentMethods/CheckDelete/{id}
```

Mục đích:

- Endpoint AJAX để JavaScript kiểm tra trước khi hiện confirm xóa.
- Trả JSON, không trả view.

JSON mẫu:

```json
{
  "canDelete": false,
  "message": "Không thể xóa \"COD\" vì còn 2 đơn hàng liên quan.",
  "blockers": ["2 đơn hàng"]
}
```

### 9.9. ToggleActive

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleActive(long id, CancellationToken ct)
{
    var result = await _paymentMethodService.ToggleActiveAsync(id, ct);
    return result is null ? NotFound() : Ok(new { isActive = result.Value });
}
```

Route:

```text
POST /PaymentMethods/ToggleActive/{id}
```

Mục đích:

- Endpoint AJAX để bật/tắt trạng thái.
- Nếu không tìm thấy bản ghi thì 404.
- Nếu thành công, trả trạng thái mới.

### 9.10. AddErrors

```csharp
private void AddErrors(IEnumerable<PaymentMethodValidationError> errors)
{
    foreach (var error in errors)
    {
        ModelState.AddModelError(error.FieldName, error.Message);
    }
}
```

Mục đích:

- Chuyển lỗi nghiệp vụ từ service sang `ModelState`.
- Razor form có thể hiển thị lỗi bằng `asp-validation-for` hoặc `asp-validation-summary`.

## 10. View danh sách

File: `Views/PaymentMethods/Index.cshtml`

Khai báo model và layout:

```cshtml
@model e_commerce_web_admin.ViewModels.PaymentMethods.PaymentMethodIndexViewModel
@{
    ViewData["Title"] = "Phương thức thanh toán";
    Layout = "~/Views/Shared/_AdminLayout.cshtml";
}
```

Ý nghĩa:

- View nhận `PaymentMethodIndexViewModel`.
- Dùng admin layout.
- Set title cho trang.

Nạp CSS riêng:

```cshtml
@section Styles {
    <link rel="stylesheet" href="~/css/payment-methods.css" asp-append-version="true" />
}
```

`asp-append-version="true"` thêm version hash để tránh cache CSS cũ.

### 10.1. Header và nút thêm

```cshtml
<h1 class="text-2xl font-bold text-slate-800">Phương thức thanh toán</h1>

<a asp-action="Create"
   class="inline-flex items-center gap-2 bg-gradient-to-r from-teal-500 to-teal-600 hover:from-teal-600 hover:to-teal-700
    text-white text-sm font-semibold px-5 py-2.5 rounded-xl transition-all shadow-sm hover:shadow-md hover:-translate-y-0.5">
    <i data-lucide="plus" class="w-4 h-4"></i>
    Thêm phương thức
</a>
```

Mục đích:

- Hiển thị tiêu đề trang.
- Nút `Thêm phương thức` điều hướng tới action `Create`.
- Dùng tone teal để đồng bộ với các trang admin khác.

### 10.2. Toast thông báo

```cshtml
@if (TempData["Success"] != null)
{
    <div id="toastSuccess" class="...">
        <span class="flex-1">@TempData["Success"]</span>
        <button type="button" data-dismiss-target="toastSuccess">
            <i data-lucide="x" class="w-4 h-4"></i>
        </button>
    </div>
}
```

Ý nghĩa:

- Nếu controller set `TempData["Success"]`, view sẽ hiện toast thành công.
- Nút đóng có `data-dismiss-target="toastSuccess"` để JS biết element cần xóa.

Toast lỗi:

```cshtml
@if (TempData["Error"] != null)
{
    <div id="toastError" class="...">
        <span class="flex-1">@TempData["Error"]</span>
    </div>
}
```

### 10.3. Card thống kê

```cshtml
<p class="text-xl font-bold text-slate-800">@Model.TotalCount</p>
<p class="text-xs text-slate-400">Tổng phương thức</p>
```

Các số liệu lấy từ `PaymentMethodIndexViewModel`:

- `Model.TotalCount`: tổng phương thức.
- `Model.ActiveCount`: đang bật.
- `Model.InactiveCount`: đang tắt.
- `Model.TotalOrderUsageCount`: tổng số đơn hàng đang sử dụng.

Ví dụ card số đơn hàng dùng:

```cshtml
<p class="text-xl font-bold text-teal-600">@Model.TotalOrderUsageCount</p>
<p class="text-xs text-slate-400">Đơn hàng dùng</p>
```

### 10.4. Filter form

```cshtml
<form method="get" asp-action="Index" class="payment-filter-grid">
    <input type="text" name="search" value="@Model.Search"
           placeholder="Tìm theo tên hoặc mô tả..." />

    <select name="status">
        <option value="" selected="@(string.IsNullOrEmpty(Model.Status))">Tất cả trạng thái</option>
        <option value="active" selected="@(Model.Status == "active")">Đang bật</option>
        <option value="inactive" selected="@(Model.Status == "inactive")">Đang tắt</option>
    </select>

    <button type="submit">Tìm</button>
</form>
```

Mục đích:

- Dùng GET để filter xuất hiện trên URL.
- `value="@Model.Search"` giữ lại từ khóa sau khi submit.
- `selected` giữ lại trạng thái lọc.

Nút xóa lọc:

```cshtml
@if (!string.IsNullOrEmpty(Model.Search) || !string.IsNullOrEmpty(Model.Status))
{
    <a asp-action="Index">Xóa lọc</a>
}
```

Chỉ hiện khi đang có filter.

### 10.5. Empty state

```cshtml
@if (!Model.PaymentMethods.Any())
{
    <div class="py-24 text-center">
        <p class="text-slate-600 font-semibold text-lg">Không tìm thấy phương thức thanh toán</p>
    </div>
}
```

Mục đích:

- Nếu danh sách rỗng thì không render bảng trống.
- Nếu có search, gợi ý thử từ khóa khác hoặc xóa bộ lọc.
- Nếu chưa có dữ liệu, gợi ý thêm phương thức.

### 10.6. Header bảng

```cshtml
<div class="payment-index-grid px-6 py-3.5 bg-gradient-to-r from-slate-50 to-slate-50/50 border-b border-slate-100">
    <span>Phương thức</span>
    <span>Đơn hàng</span>
    <span>Trạng thái</span>
    <span>Thao tác</span>
</div>
```

Mục đích:

- Sử dụng grid riêng `.payment-index-grid`.
- Giữ các cột đồng bộ giữa header và row.

### 10.7. Render từng dòng

```cshtml
@foreach (var method in Model.PaymentMethods)
{
    <div class="payment-row payment-index-grid px-6 py-3.5 payment-anim" data-id="@method.Id">
        ...
    </div>
}
```

Mục đích:

- Lặp qua danh sách row viewmodel.
- `data-id` giữ id của phương thức trên DOM.

Hiển thị tên và mô tả:

```cshtml
<p class="font-semibold text-slate-800 text-sm truncate">@method.Name</p>
<p class="text-[11px] text-slate-400 truncate">
    @(string.IsNullOrWhiteSpace(method.Description) ? "Chưa có mô tả" : method.Description)
</p>
```

Hiển thị số đơn hàng:

```cshtml
<span class="@(method.OrderCount > 0 ? "bg-teal-100 text-teal-700" : "bg-slate-100 text-slate-400")">
    @method.OrderCount
</span>
```

Nếu có đơn hàng thì badge màu teal, nếu không thì màu slate.

### 10.8. Nút bật/tắt trạng thái

```cshtml
<button type="button" data-payment-toggle data-payment-id="@method.Id"
        class="payment-status-btn @(method.IsActive ? "is-active" : "is-inactive")"
        data-active="@method.IsActive.ToString().ToLower()">
    <span class="dot"></span>
    <span class="status-label">@(method.IsActive ? "Bật" : "Tắt")</span>
</button>
```

Ý nghĩa:

- `data-payment-toggle`: JS dùng để bind click.
- `data-payment-id`: id gửi lên endpoint toggle.
- Class `is-active` hoặc `is-inactive` quyết định style.
- Text hiển thị `Bật` hoặc `Tắt`.

### 10.9. Nút sửa

```cshtml
<a asp-action="Edit" asp-route-id="@method.Id" title="Chỉnh sửa"
   class="payment-action-btn payment-action-edit">
    <i data-lucide="pencil" class="w-3.5 h-3.5"></i>
    Sửa
</a>
```

Mục đích:

- Dẫn tới `/PaymentMethods/Edit/{id}`.
- Dùng tag helper `asp-route-id` để build URL.

### 10.10. Form xóa

```cshtml
<form asp-action="Delete" asp-route-id="@method.Id" method="post" data-payment-delete
      data-payment-id="@method.Id" data-payment-name="@method.Name">
    @Html.AntiForgeryToken()
    <button type="submit" title="Xóa" class="payment-action-btn payment-action-delete">
        <i data-lucide="trash-2" class="w-3.5 h-3.5"></i>
        Xóa
    </button>
</form>
```

Mục đích:

- Xóa bằng POST, không dùng GET.
- Có anti-forgery token.
- `data-payment-delete`: JS bind submit event.
- `data-payment-id`: JS gọi endpoint `CheckDelete`.
- `data-payment-name`: JS hiển thị tên trong confirm.

### 10.11. Pagination

```cshtml
@if (Model.TotalPages > 1)
{
    <div class="px-6 py-4 border-t border-slate-100 flex items-center justify-between bg-slate-50/50">
        Trang <span>@Model.Page</span> / @Model.TotalPages
    </div>
}
```

Nút trang trước:

```cshtml
@if (Model.HasPrev)
{
    <a asp-action="Index"
       asp-route-page="@(Model.Page - 1)"
       asp-route-search="@Model.Search"
       asp-route-status="@Model.Status">
        ...
    </a>
}
```

Nút số trang:

```cshtml
@for (int p = Math.Max(1, Model.Page - 2); p <= Math.Min(Model.TotalPages, Model.Page + 2); p++)
{
    <a asp-action="Index" asp-route-page="@p" asp-route-search="@Model.Search" asp-route-status="@Model.Status">
        @p
    </a>
}
```

Mục đích:

- Giữ lại `search` và `status` khi đổi trang.
- Chỉ hiển thị tối đa vài trang gần trang hiện tại.

### 10.12. Nạp JavaScript

```cshtml
@section Scripts {
    <script src="~/js/payment-methods.js" asp-append-version="true"></script>
}
```

Mục đích:

- Chỉ nạp JS payment methods ở trang danh sách.
- Không làm ảnh hưởng các trang khác.

## 11. View Create

File: `Views/PaymentMethods/Create.cshtml`

Khai báo model:

```cshtml
@model e_commerce_web_admin.ViewModels.PaymentMethods.PaymentMethodFormViewModel
```

Set title và layout:

```cshtml
@{
    ViewData["Title"] = "Thêm phương thức thanh toán";
    Layout = "~/Views/Shared/_AdminLayout.cshtml";
}
```

Nạp CSS riêng:

```cshtml
@section Styles {
    <link rel="stylesheet" href="~/css/payment-methods.css" asp-append-version="true" />
}
```

Breadcrumb:

```cshtml
<nav class="flex items-center gap-2 text-xs text-slate-400 mb-2">
    <a asp-action="Index" class="hover:text-teal-600 transition-colors">Phương thức thanh toán</a>
    <i data-lucide="chevron-right" class="w-3.5 h-3.5"></i>
    <span class="text-slate-600 font-medium">Thêm mới</span>
</nav>
```

Form:

```cshtml
<form asp-action="Create" method="post" class="payment-anim" style="animation-delay:0.1s">
    @Html.AntiForgeryToken()
    <partial name="_Form" model="Model" />
</form>
```

Mục đích:

- Submit về `POST /PaymentMethods/Create`.
- Có anti-forgery token.
- Dùng partial `_Form` để tránh lặp code form với trang edit.

## 12. View Edit

File: `Views/PaymentMethods/Edit.cshtml`

Set title có tên phương thức:

```cshtml
@{
    ViewData["Title"] = $"Sửa phương thức thanh toán - {Model.Name}";
    Layout = "~/Views/Shared/_AdminLayout.cshtml";
}
```

Breadcrumb:

```cshtml
<a asp-action="Index" class="hover:text-teal-600 transition-colors">Phương thức thanh toán</a>
<span class="text-slate-600 font-medium">@Model.Name</span>
```

Form edit:

```cshtml
<form asp-action="Edit" asp-route-id="@Model.Id" method="post" class="payment-anim" style="animation-delay:0.1s">
    @Html.AntiForgeryToken()
    <input type="hidden" asp-for="Id" />
    <partial name="_Form" model="Model" />
</form>
```

Mục đích:

- Submit về `POST /PaymentMethods/Edit/{id}`.
- Hidden `Id` giúp controller kiểm tra route id và form id có khớp không.
- Dùng lại partial `_Form`.

## 13. Partial form

File: `Views/PaymentMethods/_Form.cshtml`

Partial này dùng chung cho create và edit.

Xác định form đang edit hay create:

```cshtml
@{
    var isEdit = Model.Id > 0;
}
```

Mục đích:

- Nếu `Id > 0` thì form đang chỉnh sửa.
- Nếu `Id == 0` thì form đang tạo mới.
- Biến này dùng để đổi text nút submit.

### 13.1. Input tên phương thức

```cshtml
<label asp-for="Name" class="block text-xs font-semibold text-slate-600 mb-1.5">
    Tên phương thức <span class="text-red-500">*</span>
</label>
<input asp-for="Name" class="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm text-slate-800
focus:outline-none focus:ring-2 focus:ring-teal-400 focus:border-transparent transition"
       placeholder="Ví dụ: Thanh toán khi nhận hàng" />
<span asp-validation-for="Name" class="text-xs text-red-500 mt-1 block"></span>
```

Ý nghĩa:

- `asp-for="Name"` bind input với `PaymentMethodFormViewModel.Name`.
- `asp-validation-for="Name"` hiển thị lỗi validation của field này.
- Placeholder gợi ý dữ liệu nhập.

### 13.2. Textarea mô tả

```cshtml
<textarea asp-for="Description" rows="6" class="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm text-slate-800
focus:outline-none focus:ring-2 focus:ring-teal-400 focus:border-transparent transition resize-y"
          placeholder="Ghi chú cách dùng, điều kiện hoặc hướng dẫn thanh toán..."></textarea>
<span asp-validation-for="Description" class="text-xs text-red-500 mt-1 block"></span>
```

Ý nghĩa:

- Bind với `Description`.
- Cho phép resize theo chiều dọc.
- Validation tối đa 500 ký tự.

### 13.3. Toggle IsActive trong form

```cshtml
<input asp-for="IsActive" type="checkbox" class="sr-only peer" />
<div class="w-10 h-6 bg-slate-200 rounded-full peer-checked:bg-teal-500 transition-colors"></div>
<div class="absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform peer-checked:translate-x-4"></div>
```

Mục đích:

- Checkbox thật được ẩn bằng `sr-only`.
- UI toggle dùng Tailwind class `peer-checked`.
- Khi checkbox checked, nền chuyển teal và nút tròn trượt sang phải.

Text:

```cshtml
<span class="text-sm text-slate-700 group-hover:text-slate-900">Cho phép sử dụng khi thanh toán</span>
```

### 13.4. Nút submit và hủy

```cshtml
<button type="submit" class="w-full flex items-center justify-center gap-2
    bg-gradient-to-r from-teal-500 to-teal-600 hover:from-teal-600 hover:to-teal-700
    text-white text-sm font-semibold px-4 py-2.5 rounded-xl transition-all shadow-sm hover:shadow-md">
    <i data-lucide="save" class="w-4 h-4"></i>
    @(isEdit ? "Lưu thay đổi" : "Tạo phương thức")
</button>
```

Mục đích:

- Nếu create, text là `Tạo phương thức`.
- Nếu edit, text là `Lưu thay đổi`.

Nút hủy:

```cshtml
<a asp-action="Index" class="w-full flex items-center justify-center gap-2 border border-slate-200 hover:bg-slate-50
    text-slate-700 text-sm font-medium px-4 py-2.5 rounded-xl transition-colors">
    Hủy bỏ
</a>
```

Mục đích:

- Quay về danh sách.
- Không submit form.

### 13.5. Validation summary

```cshtml
@if (!ViewData.ModelState.IsValid)
{
    <div class="bg-red-50 border border-red-200 rounded-2xl p-4">
        <div asp-validation-summary="ModelOnly" class="text-xs text-red-600 space-y-1"></div>
    </div>
}
```

Mục đích:

- Hiển thị lỗi cấp form.
- Các lỗi gắn với field cụ thể sẽ hiển thị ở `asp-validation-for`.

## 14. JavaScript

File: `wwwroot/js/payment-methods.js`

File này chỉ xử lý tương tác ở trang PaymentMethods. Không chứa nghiệp vụ database.

### 14.1. Khởi tạo khi DOM ready

```javascript
document.addEventListener('DOMContentLoaded', () => {
    bindStatusToggles();
    bindDeleteConfirmation();
    bindToastDismiss();
});
```

Mục đích:

- Chờ HTML load xong.
- Bind click cho nút toggle.
- Bind submit cho form xóa.
- Bind nút đóng toast.

### 14.2. Lấy anti-forgery token

```javascript
function getAntiForgeryToken(scope) {
    return scope?.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? document.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? '';
}
```

Mục đích:

- Lấy token từ form gần nhất hoặc toàn document.
- Token được gửi trong header `RequestVerificationToken`.
- Bắt buộc vì controller có `[ValidateAntiForgeryToken]`.

### 14.3. Bind nút toggle

```javascript
function bindStatusToggles() {
    document.querySelectorAll('[data-payment-toggle]').forEach(button => {
        button.addEventListener('click', () => togglePaymentMethod(button));
    });
}
```

Mục đích:

- Tìm tất cả button có `data-payment-toggle`.
- Khi click, gọi `togglePaymentMethod(button)`.

### 14.4. Toggle trạng thái

```javascript
async function togglePaymentMethod(button) {
    const id = button.dataset.paymentId;
    if (!id) {
        return;
    }

    button.disabled = true;
}
```

Ý nghĩa:

- Lấy id từ `data-payment-id`.
- Nếu thiếu id thì không làm gì.
- Disable button để tránh click liên tục.

Gửi request:

```javascript
const response = await fetch(`/PaymentMethods/ToggleActive/${encodeURIComponent(id)}`, {
    method: 'POST',
    headers: {
        RequestVerificationToken: getAntiForgeryToken(document),
        'X-Requested-With': 'XMLHttpRequest',
    },
});
```

Ý nghĩa:

- Gọi endpoint toggle bằng POST.
- `encodeURIComponent(id)` đảm bảo id an toàn khi đưa vào URL.
- Gửi anti-forgery token.
- Header `X-Requested-With` đánh dấu request AJAX.

Xử lý response:

```javascript
if (!response.ok) {
    throw new Error('Toggle failed');
}

await response.json();
window.location.reload();
```

Ý nghĩa:

- Nếu HTTP không thành công, nhảy vào catch.
- Nếu thành công, reload trang để cập nhật số liệu và trạng thái.

Xử lý lỗi:

```javascript
catch {
    alert('Không thể cập nhật trạng thái phương thức thanh toán. Vui lòng thử lại.');
    button.disabled = false;
}
```

### 14.5. Bind xác nhận xóa

```javascript
function bindDeleteConfirmation() {
    document.querySelectorAll('[data-payment-delete]').forEach(form => {
        form.addEventListener('submit', async event => {
            ...
        });
    });
}
```

Mục đích:

- Tìm tất cả form xóa.
- Chặn submit mặc định để kiểm tra điều kiện xóa trước.

Chống lặp sau khi đã xác nhận:

```javascript
if (form.dataset.deleteChecked === 'true') {
    return;
}
```

Ý nghĩa:

- Sau khi user confirm, code submit lại form.
- Cờ `deleteChecked` giúp submit lần 2 không bị chặn nữa.

Chặn submit lần đầu:

```javascript
event.preventDefault();
```

Lấy tên và disable nút:

```javascript
const name = form.dataset.paymentName || 'phương thức thanh toán này';
const submitButton = form.querySelector('button[type="submit"]');
submitButton?.setAttribute('disabled', 'disabled');
```

Kiểm tra xóa:

```javascript
const result = await checkPaymentMethodDelete(form);

if (!result.canDelete) {
    alert(result.message || `Không thể xóa "${name}" vì còn dữ liệu liên quan.`);
    return;
}
```

Mục đích:

- Gọi server kiểm tra xem còn đơn hàng liên quan không.
- Nếu server báo không được xóa, show message và dừng.

Confirm lần cuối:

```javascript
if (!confirm(`Bạn có chắc muốn xóa phương thức thanh toán "${name}"?\nHành động này không thể hoàn tác.`)) {
    return;
}
```

Submit thật:

```javascript
form.dataset.deleteChecked = 'true';
if (typeof form.requestSubmit === 'function') {
    form.requestSubmit();
} else {
    form.submit();
}
```

Ý nghĩa:

- Đặt cờ đã kiểm tra.
- `requestSubmit()` kích hoạt submit đúng chuẩn.
- Nếu browser không hỗ trợ, fallback sang `form.submit()`.

Khôi phục nút nếu lỗi hoặc user hủy:

```javascript
finally {
    if (form.dataset.deleteChecked !== 'true') {
        submitButton?.removeAttribute('disabled');
    }
}
```

### 14.6. CheckPaymentMethodDelete

```javascript
async function checkPaymentMethodDelete(form) {
    const id = form.dataset.paymentId;
    if (!id) {
        throw new Error('Missing payment method id');
    }

    const response = await fetch(`/PaymentMethods/CheckDelete/${encodeURIComponent(id)}`, {
        method: 'POST',
        headers: {
            RequestVerificationToken: getAntiForgeryToken(form),
            'X-Requested-With': 'XMLHttpRequest',
        },
    });

    if (!response.ok) {
        throw new Error('Delete check failed');
    }

    return response.json();
}
```

Mục đích:

- Gọi endpoint kiểm tra xóa.
- Trả JSON cho `bindDeleteConfirmation`.
- Nếu thiếu id hoặc HTTP lỗi thì throw error.

### 14.7. Dismiss toast

```javascript
function bindToastDismiss() {
    document.querySelectorAll('[data-dismiss-target]').forEach(button => {
        button.addEventListener('click', () => {
            document.getElementById(button.dataset.dismissTarget)?.remove();
        });
    });

    setTimeout(() => {
        document.getElementById('toastSuccess')?.remove();
        document.getElementById('toastError')?.remove();
    }, 5000);
}
```

Mục đích:

- Cho user bấm `x` để đóng toast.
- Tự động ẩn toast sau 5 giây.
- Dùng optional chaining `?.remove()` để tránh lỗi nếu element không tồn tại.

## 15. CSS

File: `wwwroot/css/payment-methods.css`

CSS này chỉ áp dụng cho class có prefix `payment-`, hạn chế ảnh hưởng sang module khác.

### 15.1. Animation

```css
@keyframes payment-fade-up {
    from {
        opacity: 0;
        transform: translateY(16px);
    }

    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.payment-anim {
    animation: payment-fade-up 0.5s cubic-bezier(0.22, 1, 0.36, 1) backwards;
}
```

Mục đích:

- Tạo hiệu ứng fade up nhẹ khi load trang.
- Các element thêm class `payment-anim` sẽ dùng animation này.

### 15.2. Filter grid

```css
.payment-filter-grid {
    display: grid;
    grid-template-columns: minmax(260px, 1fr) 180px auto auto;
    gap: 0.75rem;
    align-items: center;
}

.payment-filter-search {
    min-width: 0;
}
```

Mục đích:

- Chia filter thành input search, select status, nút tìm, nút xóa lọc.
- `min-width: 0` giúp input không phá layout khi màn nhỏ.

### 15.3. Table scroll và grid cột

```css
.payment-table-scroll {
    overflow-x: auto;
}

.payment-index-grid {
    display: grid;
    grid-template-columns: minmax(320px, 1fr) 110px 130px 132px;
    gap: 0.75rem;
    align-items: center;
    min-width: 760px;
}
```

Mục đích:

- Cho bảng scroll ngang trên màn nhỏ.
- Đảm bảo header và row dùng cùng số cột.
- `min-width: 760px` giữ bảng không bị bóp quá mức.

### 15.4. Row hover và icon

```css
.payment-row {
    transition: background 0.12s ease;
}

.payment-row:hover {
    background: #f8fafc;
}

.payment-icon {
    background: #f0fdfa;
}
```

Mục đích:

- Row đổi nền nhẹ khi hover.
- Icon dùng nền teal nhạt.

### 15.5. Nút trạng thái

```css
.payment-status-btn {
    gap: 0.375rem;
    min-width: 68px;
    padding: 0.375rem 0.75rem;
    border-radius: 999px;
    font-size: 0.75rem;
    font-weight: 600;
}
```

Mục đích:

- Tạo pill button cho trạng thái.
- `min-width` giúp nút `Bật` và `Tắt` không làm lệch layout.

Trạng thái active:

```css
.payment-status-btn.is-active {
    background: #ccfbf1;
    color: #0f766e;
}

.payment-status-btn.is-active .dot {
    background: #14b8a6;
}
```

Trạng thái inactive:

```css
.payment-status-btn.is-inactive {
    background: #f1f5f9;
    color: #64748b;
}

.payment-status-btn.is-inactive .dot {
    background: #94a3b8;
}
```

### 15.6. Nút thao tác

```css
.payment-action-btn {
    gap: 0.375rem;
    padding: 0.375rem 0.625rem;
    border-radius: 0.5rem;
    font-size: 0.75rem;
    font-weight: 500;
    white-space: nowrap;
}
```

Mục đích:

- Style chung cho nút `Sửa` và `Xóa`.
- `white-space: nowrap` giữ text không xuống dòng trong nút.

Nút sửa:

```css
.payment-action-edit {
    background: #f0fdfa;
    color: #0d9488;
}

.payment-action-edit:hover {
    background: #ccfbf1;
    color: #0f766e;
}
```

Nút xóa:

```css
.payment-action-delete {
    background: #fff1f2;
    color: #ef4444;
}

.payment-action-delete:hover {
    background: #fee2e2;
    color: #dc2626;
}
```

### 15.7. Responsive

```css
@media (max-width: 1024px) {
    .payment-filter-grid {
        grid-template-columns: 1fr 1fr;
    }

    .payment-filter-search {
        grid-column: 1 / -1;
    }
}
```

Mục đích:

- Trên tablet, filter chia thành 2 cột.
- Input search chiếm full width.

Mobile:

```css
@media (max-width: 640px) {
    .payment-filter-grid {
        grid-template-columns: 1fr;
    }

    .payment-filter-search {
        grid-column: auto;
    }
}
```

Mục đích:

- Trên mobile, filter xếp dọc một cột.

## 16. Điều hướng

Admin layout đã có link tới module:

File: `Views/Shared/_AdminLayout.cshtml`

```cshtml
<a href="/PaymentMethods" class="nav-link ...">
    Phương thức thanh toán
</a>
```

Shared layout cũng có link:

File: `Views/Shared/_Layout.cshtml`

```cshtml
<a asp-controller="PaymentMethods" asp-action="Index">Phương thức thanh toán</a>
```

Ý nghĩa:

- User admin có thể vào module qua sidebar.
- Route controller mặc định map `/PaymentMethods` tới `PaymentMethodsController.Index`.

## 17. Luồng hoạt động tổng thể

### 17.1. Mở danh sách

```text
Browser -> GET /PaymentMethods
Controller -> GetIndexAsync(query)
Service -> Query payment_methods + count orders
Controller -> return View(viewModel)
Razor -> render Index.cshtml
```

### 17.2. Tạo mới

```text
Browser -> GET /PaymentMethods/Create
Controller -> GetCreateFormAsync()
Razor -> render Create.cshtml + _Form.cshtml

Browser -> POST /PaymentMethods/Create
Controller -> ModelState validation
Service -> NormalizeForm -> ValidateFormAsync -> SaveChangesAsync
Controller -> TempData Success -> Redirect Index
```

### 17.3. Chỉnh sửa

```text
Browser -> GET /PaymentMethods/Edit/{id}
Controller -> GetEditFormAsync(id)
Service -> Find payment method -> map form viewmodel
Razor -> render Edit.cshtml + _Form.cshtml

Browser -> POST /PaymentMethods/Edit/{id}
Controller -> check id == form.Id
Service -> NormalizeForm -> ValidateFormAsync(existingId) -> SaveChangesAsync
Controller -> Redirect Index
```

### 17.4. Toggle trạng thái

```text
User click Bật/Tắt
payment-methods.js -> POST /PaymentMethods/ToggleActive/{id}
Controller -> ToggleActiveAsync(id)
Service -> entity.IsActive = !entity.IsActive -> SaveChangesAsync
Controller -> JSON { isActive = true/false }
JS -> reload page
```

### 17.5. Xóa

```text
User click Xóa
payment-methods.js -> POST /PaymentMethods/CheckDelete/{id}
Controller -> CheckDeleteAsync(id)
Service -> count related orders

Nếu có order:
Controller -> JSON canDelete=false
JS -> alert message

Nếu không có order:
Controller -> JSON canDelete=true
JS -> confirm
JS -> submit form Delete
Controller -> DeleteAsync(id)
Service -> CheckDeleteAsync -> Remove -> SaveChangesAsync
Controller -> Redirect Index
```

## 18. Tách biệt frontend và backend

Module hiện đang tách như sau:

```text
Controller
    nhận request
    gọi service
    trả View/JSON

Service
    xử lý query
    validate nghiệp vụ
    tạo/sửa/xóa/toggle
    trả result object

ViewModel
    định nghĩa dữ liệu dành riêng cho UI

Razor
    render HTML từ ViewModel
    không query database

JavaScript
    xử lý tương tác browser
    gọi endpoint qua fetch

CSS
    style riêng theo prefix payment-
```

Điều module không làm:

- View không gọi `ApplicationDbContext`.
- Controller không viết query EF Core.
- JavaScript không chứa nghiệp vụ kiểm tra đơn hàng, chỉ gọi endpoint.
- CSS không sửa global layout.

## 19. Các điểm cần lưu ý khi bảo trì

### 19.1. Name chưa có unique index DB

Hiện validation trùng tên nằm ở service:

```csharp
method => method.Name == form.Name
```

Dùng admin bình thường thì ổn. Nếu muốn chắc tuyệt đối ở tầng database, thêm unique index bằng migration.

### 19.2. Xóa có race condition nhỏ

`DeleteAsync` kiểm tra trước rồi mới xóa:

```csharp
var deleteCheck = await CheckDeleteAsync(id, ct);
...
_db.PaymentMethods.Remove(entity);
await _db.SaveChangesAsync(ct);
```

Nếu có đơn hàng mới phát sinh đúng giữa hai bước này, database có thể ném lỗi FK. Với admin hiện tại rủi ro thấp, nhưng có thể bắt `DbUpdateException` nếu muốn message thân thiện hơn.

### 19.3. Page quá lớn có thể hiện empty state

Nếu URL là:

```text
/PaymentMethods?page=999
```

và không có trang 999, service sẽ trả danh sách rỗng. Đây là lỗi UX nhỏ, không ảnh hưởng dữ liệu. Nếu muốn mượt hơn, có thể clamp page về `TotalPages`.

## 20. Tóm tắt vai trò từng lớp

```text
PaymentMethodsController
    Điều phối HTTP request/response.

IPaymentMethodAdminService
    Hợp đồng nghiệp vụ cho controller.

PaymentMethodAdminService
    Xử lý database, validate, tạo, sửa, xóa, toggle, phân trang.

PaymentMethodServiceResults
    Chuẩn hóa dữ liệu trả về từ service.

PaymentMethodViewModels
    Chuẩn hóa dữ liệu đưa ra view.

Index.cshtml
    Trang danh sách, filter, thống kê, toggle, edit, delete, pagination.

Create.cshtml
    Trang tạo mới, dùng partial form.

Edit.cshtml
    Trang chỉnh sửa, dùng partial form.

_Form.cshtml
    Form dùng chung cho create/edit.

payment-methods.js
    Tương tác browser: toggle, check delete, confirm delete, toast.

payment-methods.css
    Style riêng cho module.
```
