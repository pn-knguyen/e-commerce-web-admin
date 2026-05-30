# Tài liệu quản lý Voucher

Tài liệu này giải thích toàn bộ module quản lý voucher trong admin. Module Voucher đang dùng pattern:

```text
MVC + Service Layer + DTO boundary + Controller mapping
```

Điểm khác với Brand/Category là service của Voucher không nhận/trả trực tiếp ViewModel của Razor. Service làm việc với DTO riêng như `VoucherFormData`, `VoucherIndexRequest`, `VoucherIndexResult`. Controller chịu trách nhiệm map giữa DTO của service và ViewModel của giao diện.

Mục tiêu của cách tách này:

- Backend service không phụ thuộc frontend ViewModel.
- Controller giữ vai trò chuyển đổi dữ liệu giữa HTTP/Razor và nghiệp vụ.
- Validation rule dùng chung được gom vào một nơi.
- Thời gian voucher được xử lý rõ giữa giờ admin Việt Nam và UTC.
- JavaScript chỉ hỗ trợ trải nghiệm nhập liệu realtime, backend vẫn là nơi quyết định dữ liệu có hợp lệ hay không.

## 1. Các file chính

```text
Controllers/VouchersController.cs
Services/Vouchers/IVoucherAdminService.cs
Services/Vouchers/VoucherAdminService.cs
Services/Vouchers/VoucherAdminModels.cs
Services/Vouchers/VoucherServiceResults.cs
Services/Vouchers/VoucherDateTime.cs
Models/Validation/VoucherValidationRules.cs
ViewModels/Vouchers/VoucherViewModels.cs
Views/Vouchers/Index.cshtml
Views/Vouchers/Create.cshtml
Views/Vouchers/Edit.cshtml
Views/Vouchers/_Form.cshtml
wwwroot/js/vouchers.js
wwwroot/css/vouchers.css
```

Ý nghĩa từng nhóm:

- `Controllers/VouchersController.cs`: nhận request HTTP, gọi service, map DTO sang ViewModel và ngược lại.
- `Services/Vouchers/IVoucherAdminService.cs`: interface mô tả các nghiệp vụ Voucher mà controller được phép gọi.
- `Services/Vouchers/VoucherAdminService.cs`: xử lý query database, validate nghiệp vụ, tạo/sửa/xóa/bật tắt voucher.
- `Services/Vouchers/VoucherAdminModels.cs`: DTO riêng của service, không phụ thuộc Razor.
- `Services/Vouchers/VoucherServiceResults.cs`: object kết quả trả về từ service.
- `Services/Vouchers/VoucherDateTime.cs`: chuyển đổi giờ admin Việt Nam và UTC.
- `Models/Validation/VoucherValidationRules.cs`: rule và message validation dùng chung.
- `ViewModels/Vouchers/VoucherViewModels.cs`: dữ liệu dành riêng cho Razor View.
- `Views/Vouchers/*.cshtml`: giao diện danh sách, tạo, sửa và partial form.
- `wwwroot/js/vouchers.js`: formatter mã voucher, validation realtime, toggle trạng thái, confirm xóa.
- `wwwroot/css/vouchers.css`: style riêng cho module voucher.

## 2. Đăng ký service trong Program

Trong `Program.cs`, Voucher service được đăng ký vào DI container:

```csharp
builder.Services.AddScoped<IVoucherAdminService, VoucherAdminService>();
```

Nhờ vậy `VouchersController` chỉ phụ thuộc interface `IVoucherAdminService`, không tự tạo service và không query `ApplicationDbContext` trực tiếp.

Lợi ích:

- Dễ thay implementation nếu sau này cần mock hoặc test.
- Controller không chứa logic database.
- Tách rõ HTTP layer và business layer.

## 3. Kiến trúc tách lớp của Voucher

Luồng tổng quát:

```text
Browser
  -> VouchersController
      -> IVoucherAdminService / VoucherAdminService
          -> ApplicationDbContext
      <- VoucherIndexResult / VoucherFormData / VoucherSaveResult
  -> Controller map sang VoucherViewModel
  -> Razor View
  -> vouchers.js hỗ trợ tương tác realtime
```

Ranh giới quan trọng:

- View dùng `VoucherIndexViewModel`, `VoucherFormViewModel`.
- Service dùng `VoucherIndexRequest`, `VoucherIndexResult`, `VoucherFormData`.
- Controller là nơi map giữa hai thế giới.

Vì vậy service không bị dính các field phục vụ UI như option list, display label, hoặc data attribute cho frontend.

## 4. Service contract

File:

```text
Services/Vouchers/IVoucherAdminService.cs
```

Interface hiện tại:

```csharp
public interface IVoucherAdminService
{
    Task<VoucherIndexResult> GetIndexAsync(VoucherIndexRequest query, CancellationToken cancellationToken = default);
    VoucherFormData GetCreateForm();
    Task<VoucherFormData?> GetEditFormAsync(long id, CancellationToken cancellationToken = default);
    Task<VoucherSaveResult> CreateAsync(VoucherFormData form, CancellationToken cancellationToken = default);
    Task<VoucherSaveResult> UpdateAsync(long id, VoucherFormData form, CancellationToken cancellationToken = default);
    Task<VoucherDeleteResult> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<VoucherToggleResult?> ToggleActiveAsync(long id, CancellationToken cancellationToken = default);
}
```

Điểm cần chú ý:

- Không có `VoucherFormViewModel` trong service contract.
- Không có `VoucherIndexViewModel` trong service contract.
- Service chỉ biết các object phục vụ nghiệp vụ admin voucher.

Đây là điểm tách biệt lớn nhất so với Brand/Category hiện tại.

## 5. Service DTO

File:

```text
Services/Vouchers/VoucherAdminModels.cs
```

### VoucherIndexRequest

```csharp
public sealed class VoucherIndexRequest
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
}
```

`VoucherIndexRequest` là dữ liệu lọc danh sách lấy từ query string:

- `Search`: tìm theo mã voucher hoặc mô tả.
- `Status`: lọc trạng thái.
- `Page`: trang hiện tại.

### VoucherIndexResult

```csharp
public sealed class VoucherIndexResult
{
    public List<VoucherListItem> Vouchers { get; init; } = new();
    public string? Search { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 30;
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }
    public int InactiveCount { get; init; }
    public int RunningCount { get; init; }
    public int UpcomingCount { get; init; }
    public int ExpiredCount { get; init; }
    public int ExhaustedCount { get; init; }
    public int TotalUsedCount { get; init; }
}
```

`VoucherIndexResult` là kết quả service trả cho danh sách:

- `Vouchers`: các dòng voucher của trang hiện tại.
- `TotalCount`: tổng voucher sau filter.
- `RunningCount`, `UpcomingCount`, `ExpiredCount`, `ExhaustedCount`: thống kê trạng thái.
- `TotalUsedCount`: tổng lượt dùng.

### VoucherListItem

`VoucherListItem` là một dòng voucher ở tầng service. Nó dùng `StartDateUtc` và `EndDateUtc`, chưa convert sang giờ hiển thị.

```csharp
public sealed class VoucherListItem
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public DiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public DateTime StartDateUtc { get; init; }
    public DateTime EndDateUtc { get; init; }
    public string StatusKey { get; init; } = "inactive";
}
```

Trong code thật class này có nhiều field hơn, nhưng ý chính là:

- Service trả dữ liệu raw cho nghiệp vụ.
- Controller quyết định convert sang ViewModel.
- Label tiếng Việt cho UI không nằm trong service result.

### VoucherFormData

```csharp
public sealed class VoucherFormData
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;
    public decimal DiscountValue { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public bool IsActive { get; set; } = true;
}
```

`VoucherFormData` là dữ liệu create/update ở tầng service.

Điểm quan trọng:

- Ngày giờ trong service luôn là UTC.
- Không có `DiscountTypeOptions`.
- Không có field chỉ phục vụ Razor.
- Không có annotation UI.

## 6. Controller là boundary mapper

File:

```text
Controllers/VouchersController.cs
```

Controller có hai nhiệm vụ chính:

1. Nhận request, gọi service.
2. Map dữ liệu giữa service DTO và ViewModel.

### Index

```csharp
public async Task<IActionResult> Index(
    string? search,
    string? status,
    int page = 1,
    CancellationToken cancellationToken = default)
{
    var result = await _voucherService.GetIndexAsync(
        new VoucherIndexRequest
        {
            Search = search,
            Status = status,
            Page = page,
        },
        cancellationToken);

    return View(ToIndexViewModel(result));
}
```

Controller không nhận `VoucherIndexViewModel` từ service. Nó nhận `VoucherIndexResult`, sau đó gọi `ToIndexViewModel`.

### Mapping danh sách

```csharp
private static VoucherIndexViewModel ToIndexViewModel(VoucherIndexResult result)
{
    return new VoucherIndexViewModel
    {
        Vouchers = result.Vouchers.Select(ToRowViewModel).ToList(),
        Search = result.Search,
        Status = result.Status,
        Page = result.Page,
        PageSize = result.PageSize,
        TotalCount = result.TotalCount,
    };
}
```

`ToRowViewModel` convert giờ UTC sang giờ admin:

```csharp
StartDate = VoucherDateTime.ToAdminLocal(item.StartDateUtc),
EndDate = VoucherDateTime.ToAdminLocal(item.EndDateUtc),
```

Nó cũng map `StatusKey` sang `StatusLabel` tiếng Việt:

```csharp
StatusLabel = ResolveStatusLabel(item.StatusKey),
```

Như vậy service chỉ trả `running`, `upcoming`, `expired`, còn text hiển thị là chuyện của UI layer.

### Create GET

```csharp
public IActionResult Create()
{
    return View(ToFormViewModel(_voucherService.GetCreateForm()));
}
```

Service trả `VoucherFormData`, controller convert sang `VoucherFormViewModel`.

### Create POST

```csharp
public async Task<IActionResult> Create(
    VoucherFormViewModel viewModel,
    CancellationToken cancellationToken)
{
    if (!ModelState.IsValid)
    {
        return View(PrepareForm(viewModel));
    }

    var result = await _voucherService.CreateAsync(ToFormData(viewModel), cancellationToken);
    if (!result.Succeeded)
    {
        AddValidationErrors(result.Errors);
        return View(ToFormViewModel(result.Form));
    }

    TempData["Success"] = result.Message;
    return RedirectToAction(nameof(Index));
}
```

Luồng xử lý:

1. ASP.NET Core kiểm tra DataAnnotations của ViewModel.
2. Nếu lỗi cơ bản, trả lại view.
3. Nếu hợp lệ, controller map ViewModel sang `VoucherFormData`.
4. Service validate nghiệp vụ và lưu database.
5. Nếu lỗi nghiệp vụ, controller add vào `ModelState`.
6. Nếu thành công, redirect về index.

### ToFormData

```csharp
private static VoucherFormData ToFormData(VoucherFormViewModel viewModel)
{
    return new VoucherFormData
    {
        Code = viewModel.Code,
        DiscountType = viewModel.DiscountType,
        DiscountValue = viewModel.DiscountValue,
        StartDateUtc = VoucherDateTime.FromAdminLocal(viewModel.StartDate),
        EndDateUtc = VoucherDateTime.FromAdminLocal(viewModel.EndDate),
        IsActive = viewModel.IsActive,
    };
}
```

Đây là nơi chuyển giờ admin nhập từ form thành UTC trước khi đưa xuống service.

## 7. Timezone

File:

```text
Services/Vouchers/VoucherDateTime.cs
```

Voucher có thời gian bắt đầu và kết thúc nên phải xử lý timezone rõ ràng.

```csharp
private const string WindowsTimeZoneId = "SE Asia Standard Time";
private const string IanaTimeZoneId = "Asia/Ho_Chi_Minh";
```

Service hỗ trợ cả Windows và Linux/macOS:

- Windows dùng `SE Asia Standard Time`.
- Linux/macOS thường dùng `Asia/Ho_Chi_Minh`.

### ToAdminLocal

```csharp
public static DateTime ToAdminLocal(DateTime utcDateTime)
{
    var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
    return TimeZoneInfo.ConvertTimeFromUtc(utc, AdminTimeZone);
}
```

Dùng khi hiển thị dữ liệu từ database ra form/view.

### FromAdminLocal

```csharp
public static DateTime FromAdminLocal(DateTime localDateTime)
{
    var local = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
    return TimeZoneInfo.ConvertTimeToUtc(local, AdminTimeZone);
}
```

Dùng khi admin nhập `datetime-local` trong form. Input này không có timezone, nên controller coi nó là giờ admin Việt Nam rồi convert sang UTC.

Quy ước hiện tại:

- Database lưu UTC.
- Service tính trạng thái bằng UTC.
- Form/view hiển thị giờ Việt Nam.

## 8. Validation rules và messages

File:

```text
Models/Validation/VoucherValidationRules.cs
```

File này gom rule và message dùng chung:

```csharp
public static class VoucherValidationRules
{
    public const int CodeMaxLength = 80;
    public const int CodeInputMaxLength = 120;
    public const string CodePattern = @"^[A-Za-z0-9][A-Za-z0-9_-]*$";
    public const double PositiveAmountMin = 0.01;
    public const int PriorityMin = 0;
    public const int PriorityMax = 9999;
    public const int PercentageDiscountMax = 100;
}
```

Ý nghĩa:

- `CodeMaxLength = 80`: giới hạn thật của mã voucher.
- `CodeInputMaxLength = 120`: input cho phép nhập dài hơn 80 để admin thấy lỗi realtime.
- `CodePattern`: mã phải bắt đầu bằng chữ/số; các ký tự sau có thể là chữ, số, `-`, `_`.
- `PositiveAmountMin = 0.01`: các giá trị tiền/giảm giá phải lớn hơn 0 khi field có nhập.
- `PercentageDiscountMax = 100`: giảm phần trăm không vượt quá 100.

Message tiếng Việt nằm trong `VoucherValidationMessages`, ví dụ:

```csharp
public const string CodeRequired = "Mã voucher là bắt buộc.";
public const string CodeMaxLength = "Mã voucher tối đa 80 ký tự.";
public const string DiscountValuePositive = "Giá trị giảm phải lớn hơn 0.";
public const string EndDateAfterStart = "Ngày kết thúc phải sau ngày bắt đầu.";
```

Lợi ích:

- Không rải message ở nhiều file.
- ViewModel và service dùng chung cùng message.
- JavaScript đọc message từ HTML render ra, hạn chế hardcode.

## 9. ViewModel của Voucher

File:

```text
ViewModels/Vouchers/VoucherViewModels.cs
```

ViewModel chỉ phục vụ Razor.

### VoucherIndexViewModel

`VoucherIndexViewModel` chứa dữ liệu trang danh sách:

- `Vouchers`: danh sách voucher cần hiển thị.
- `Search`, `Status`, `Page`, `PageSize`: filter và pagination.
- `TotalCount`, `RunningCount`, `ExpiredCount`, `TotalUsedCount`: thống kê.
- `TotalPages`, `HasPrev`, `HasNext`: hỗ trợ phân trang.

### VoucherRowViewModel

`VoucherRowViewModel` có các property display:

```csharp
public string DiscountTypeLabel => DiscountType == DiscountType.Percentage
    ? "Theo phần trăm"
    : "Số tiền cố định";
```

```csharp
public string DiscountDisplay => DiscountType == DiscountType.Percentage
    ? $"{DiscountValue.ToString("N0", ViCulture)}%"
    : $"{DiscountValue.ToString("N0", ViCulture)} đ";
```

Các property này thuộc UI, nên để ở ViewModel là hợp lý.

### VoucherFormViewModel

`VoucherFormViewModel` dùng cho create/edit:

- `Code`
- `Description`
- `DiscountType`
- `DiscountValue`
- `MinOrderValue`
- `MaxDiscountValue`
- `MaxUses`
- `MaxUsesPerUser`
- `StartDate`
- `EndDate`
- `Priority`
- `IsActive`

Các DataAnnotations dùng rule/message từ `VoucherValidationRules`:

```csharp
[StringLength(VoucherValidationRules.CodeMaxLength, ErrorMessage = VoucherValidationMessages.CodeMaxLength)]
[RegularExpression(VoucherValidationRules.CodePattern, ErrorMessage = VoucherValidationMessages.CodePattern)]
public string Code { get; set; } = string.Empty;
```

ViewModel cũng expose một số metadata cho Razor:

```csharp
public int CodeInputMaxLength => VoucherValidationRules.CodeInputMaxLength;
public int CodeMaxLength => VoucherValidationRules.CodeMaxLength;
public int PercentageDiscountMax => VoucherValidationRules.PercentageDiscountMax;
```

Các property này không phục vụ database. Chúng giúp Razor render đúng validation metadata cho frontend.

## 10. Query danh sách voucher

File:

```text
Services/Vouchers/VoucherAdminService.cs
```

Action index gọi:

```csharp
public async Task<VoucherIndexResult> GetIndexAsync(
    VoucherIndexRequest query,
    CancellationToken cancellationToken = default)
```

### Tạo query base

```csharp
var page = Math.Max(1, query.Page);
var now = VoucherDateTime.UtcNow();
var searchQuery = _db.Vouchers.AsNoTracking();
```

- `Math.Max(1, query.Page)`: không cho page nhỏ hơn 1.
- `VoucherDateTime.UtcNow()`: tính trạng thái voucher bằng UTC.
- `AsNoTracking()`: index chỉ đọc dữ liệu nên không cần EF tracking.

### Search

```csharp
if (!string.IsNullOrWhiteSpace(query.Search))
{
    var search = query.Search.Trim();
    searchQuery = searchQuery.Where(voucher =>
        voucher.Code.Contains(search) ||
        (voucher.Description != null && voucher.Description.Contains(search)));
}
```

Tìm theo:

- `Code`
- `Description`

### Status filter

```csharp
var filteredQuery = ApplyStatusFilter(searchQuery, query.Status, now);
```

Các status hiện hỗ trợ:

- `active`: voucher đang bật.
- `inactive`: voucher đang tắt.
- `running`: đang bật, nằm trong thời gian, chưa hết lượt.
- `upcoming`: đang bật nhưng chưa tới thời gian bắt đầu.
- `expired`: đã hết hạn.
- `exhausted`: đã hết lượt dùng.

### Thống kê bằng query DB

```csharp
var totalCount = await filteredQuery.CountAsync(cancellationToken);
var activeCount = await filteredQuery.CountAsync(voucher => voucher.IsActive, cancellationToken);
var runningCount = await filteredQuery.CountAsync(voucher =>
    voucher.IsActive &&
    voucher.StartDate <= now &&
    voucher.EndDate >= now &&
    (!voucher.MaxUses.HasValue || voucher.UsedCount < voucher.MaxUses.Value),
    cancellationToken);
```

Điểm tốt:

- Không load toàn bộ voucher vào memory để đếm.
- Database xử lý count/aggregate.
- Dễ scale hơn khi voucher nhiều.

### Phân trang DB-side

```csharp
var pageItems = await filteredQuery
    .OrderByDescending(voucher => voucher.IsActive)
    .ThenByDescending(voucher => voucher.Priority)
    .ThenByDescending(voucher => voucher.StartDate)
    .ThenBy(voucher => voucher.Code)
    .Skip((page - 1) * DefaultPageSize)
    .Take(DefaultPageSize)
    .Select(voucher => new VoucherIndexItem { ... })
    .ToListAsync(cancellationToken);
```

Điểm quan trọng:

- `Skip` và `Take` nằm trước `ToListAsync`.
- EF Core sinh SQL có `OFFSET/FETCH`.
- Không còn tình trạng load toàn bộ voucher rồi phân trang trong memory.

### Resolve status

Sau khi lấy page items, service tính `StatusKey`:

```csharp
StatusKey = ResolveStatusKey(item, now),
```

`StatusKey` chỉ là key kỹ thuật: `running`, `expired`, `inactive`...

Text tiếng Việt không nằm trong service mà nằm ở controller/ViewModel layer.

## 11. Tạo form voucher

Service tạo dữ liệu mặc định:

```csharp
public VoucherFormData GetCreateForm()
{
    var localNow = VoucherDateTime.ToAdminLocal(VoucherDateTime.UtcNow());
    var startLocal = new DateTime(
        localNow.Year,
        localNow.Month,
        localNow.Day,
        localNow.Hour,
        0,
        0,
        DateTimeKind.Unspecified);
    var endLocal = startLocal.Date.AddDays(30).AddHours(23).AddMinutes(59);

    return new VoucherFormData
    {
        DiscountType = DiscountType.FixedAmount,
        DiscountValue = 100000m,
        MinOrderValue = 0m,
        MaxDiscountValue = 100000m,
        StartDateUtc = VoucherDateTime.FromAdminLocal(startLocal),
        EndDateUtc = VoucherDateTime.FromAdminLocal(endLocal),
        Priority = 0,
        IsActive = true,
    };
}
```

Ý nghĩa:

- Form mặc định là giảm số tiền cố định.
- Giá trị giảm mặc định 100.000đ.
- Voucher bắt đầu từ giờ hiện tại, làm tròn phút về `00`.
- Kết thúc sau 30 ngày lúc 23:59 giờ Việt Nam.
- Service vẫn trả về UTC cho controller.

## 12. Lấy form edit

```csharp
public async Task<VoucherFormData?> GetEditFormAsync(
    long id,
    CancellationToken cancellationToken = default)
{
    var entity = await _db.Vouchers
        .AsNoTracking()
        .FirstOrDefaultAsync(voucher => voucher.Id == id, cancellationToken);

    if (entity is null)
    {
        return null;
    }

    return new VoucherFormData
    {
        Id = entity.Id,
        Code = entity.Code,
        DiscountType = entity.DiscountType,
        StartDateUtc = entity.StartDate,
        EndDateUtc = entity.EndDate,
        IsActive = entity.IsActive,
    };
}
```

Controller nhận `VoucherFormData`, convert sang `VoucherFormViewModel` và convert ngày giờ sang giờ admin để hiển thị.

## 13. Luồng create/update

Create:

```csharp
public async Task<VoucherSaveResult> CreateAsync(
    VoucherFormData form,
    CancellationToken cancellationToken = default)
{
    NormalizeForm(form);

    var errors = await ValidateFormAsync(form, existingId: null, usedCount: 0, cancellationToken);
    if (errors.Count > 0)
    {
        return VoucherSaveResult.Failed(form, errors);
    }

    var entity = new Voucher
    {
        Code = form.Code,
        Description = form.Description,
        DiscountType = form.DiscountType,
        DiscountValue = form.DiscountValue,
        StartDate = form.StartDateUtc,
        EndDate = form.EndDateUtc,
        CreatedAt = VoucherDateTime.UtcNow(),
    };

    _db.Vouchers.Add(entity);
    await _db.SaveChangesAsync(cancellationToken);

    form.Id = entity.Id;
    return VoucherSaveResult.Success(form, $"Đã tạo voucher \"{entity.Code}\" thành công.");
}
```

Update:

```csharp
public async Task<VoucherSaveResult> UpdateAsync(
    long id,
    VoucherFormData form,
    CancellationToken cancellationToken = default)
{
    NormalizeForm(form);

    var entity = await _db.Vouchers.FirstOrDefaultAsync(voucher => voucher.Id == id, cancellationToken);
    if (entity is null)
    {
        return VoucherSaveResult.Failed(
            form,
            new[] { new VoucherValidationError(string.Empty, "Không tìm thấy voucher.") });
    }

    var errors = await ValidateFormAsync(form, id, entity.UsedCount, cancellationToken);
    if (errors.Count > 0)
    {
        form.UsedCount = entity.UsedCount;
        return VoucherSaveResult.Failed(form, errors);
    }

    entity.Code = form.Code;
    entity.Description = form.Description;
    entity.DiscountType = form.DiscountType;
    entity.DiscountValue = form.DiscountValue;
    entity.UpdatedAt = VoucherDateTime.UtcNow();

    await _db.SaveChangesAsync(cancellationToken);
    return VoucherSaveResult.Success(form, $"Đã cập nhật voucher \"{entity.Code}\" thành công.");
}
```

Điểm khác biệt khi update:

- Phải tìm entity cũ.
- Không cho `MaxUses` nhỏ hơn số lượt đã dùng.
- Set `UpdatedAt`.

## 14. Normalize dữ liệu form

```csharp
private static void NormalizeForm(VoucherFormData form)
{
    form.Code = form.Code.Trim().ToUpperInvariant();
    form.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
    form.StartDateUtc = DateTime.SpecifyKind(form.StartDateUtc, DateTimeKind.Utc);
    form.EndDateUtc = DateTime.SpecifyKind(form.EndDateUtc, DateTimeKind.Utc);
}
```

Normalize giúp dữ liệu lưu vào DB nhất quán:

- Mã voucher luôn uppercase.
- Mô tả rỗng chuyển thành `null`.
- DateTime được đánh dấu là UTC.

## 15. Validate nghiệp vụ

Hàm chính:

```csharp
private async Task<List<VoucherValidationError>> ValidateFormAsync(
    VoucherFormData form,
    long? existingId,
    int usedCount,
    CancellationToken cancellationToken)
```

Các rule hiện có:

### Mã voucher

```csharp
if (string.IsNullOrWhiteSpace(form.Code))
{
    errors.Add(new VoucherValidationError(nameof(form.Code), VoucherValidationMessages.CodeRequired));
}

if (form.Code.Length > VoucherValidationRules.CodeMaxLength)
{
    errors.Add(new VoucherValidationError(nameof(form.Code), VoucherValidationMessages.CodeMaxLength));
}

if (!CodeRegex.IsMatch(form.Code))
{
    errors.Add(new VoucherValidationError(nameof(form.Code), VoucherValidationMessages.CodePattern));
}
```

Mã voucher:

- Bắt buộc.
- Tối đa 80 ký tự.
- Bắt đầu bằng chữ hoặc số.
- Các ký tự tiếp theo chỉ gồm chữ, số, `-`, `_`.

### Trùng mã

```csharp
if (isCodeValid &&
    await _db.Vouchers.AnyAsync(voucher =>
        voucher.Code == form.Code &&
        (!existingId.HasValue || voucher.Id != existingId.Value),
        cancellationToken))
{
    errors.Add(new VoucherValidationError(nameof(form.Code), VoucherValidationMessages.DuplicateCode));
}
```

Chỉ check duplicate nếu code đã qua validate cơ bản. Khi update, bỏ qua chính voucher đang sửa.

### Giá trị giảm

```csharp
if (form.DiscountValue <= 0)
{
    errors.Add(new VoucherValidationError(nameof(form.DiscountValue), VoucherValidationMessages.DiscountValuePositive));
}
```

Voucher giảm `0` hoặc số âm là không hợp lý, nên không cho lưu.

### Giảm phần trăm

```csharp
if (form.DiscountType == DiscountType.Percentage &&
    form.DiscountValue > VoucherValidationRules.PercentageDiscountMax)
{
    errors.Add(new VoucherValidationError(nameof(form.DiscountValue), VoucherValidationMessages.PercentageDiscountMax));
}
```

Giảm phần trăm không được vượt quá 100%.

### Mức giảm tối đa

```csharp
if (form.MaxDiscountValue.HasValue && form.MaxDiscountValue.Value <= 0)
{
    errors.Add(new VoucherValidationError(nameof(form.MaxDiscountValue), VoucherValidationMessages.MaxDiscountPositive));
}
```

Nếu nhập mức giảm tối đa thì phải lớn hơn 0.

Với giảm cố định:

```csharp
if (form.DiscountType == DiscountType.FixedAmount &&
    form.MaxDiscountValue.HasValue &&
    form.MaxDiscountValue.Value < form.DiscountValue)
{
    errors.Add(new VoucherValidationError(nameof(form.MaxDiscountValue), VoucherValidationMessages.FixedMaxDiscount));
}
```

Mức giảm tối đa không được nhỏ hơn giá trị giảm cố định.

### Đơn tối thiểu

```csharp
if (form.MinOrderValue < 0)
{
    errors.Add(new VoucherValidationError(nameof(form.MinOrderValue), VoucherValidationMessages.MinOrderNonNegative));
}
```

Đơn tối thiểu được phép bằng 0, nhưng không được âm.

### Lượt dùng

```csharp
if (form.MaxUses.HasValue && form.MaxUses.Value <= 0)
{
    errors.Add(new VoucherValidationError(nameof(form.MaxUses), VoucherValidationMessages.MaxUsesPositive));
}
```

Nếu có giới hạn lượt dùng thì phải lớn hơn 0.

Khi update:

```csharp
if (form.MaxUses.HasValue && form.MaxUses.Value < usedCount)
{
    errors.Add(new VoucherValidationError(
        nameof(form.MaxUses),
        string.Format(VoucherValidationMessages.MaxUsesLessThanUsed, usedCount)));
}
```

Không cho set tổng lượt dùng nhỏ hơn số lượt đã dùng.

### Lượt dùng mỗi khách

```csharp
if (form.MaxUsesPerUser.HasValue && form.MaxUsesPerUser.Value <= 0)
{
    errors.Add(new VoucherValidationError(nameof(form.MaxUsesPerUser), VoucherValidationMessages.MaxUsesPerUserPositive));
}
```

Nếu có giới hạn theo khách thì phải lớn hơn 0.

```csharp
if (form.MaxUses.HasValue &&
    form.MaxUsesPerUser.HasValue &&
    form.MaxUsesPerUser.Value > form.MaxUses.Value)
{
    errors.Add(new VoucherValidationError(
        nameof(form.MaxUsesPerUser),
        VoucherValidationMessages.MaxUsesPerUserExceedsMaxUses));
}
```

Lượt dùng mỗi khách không được lớn hơn tổng lượt dùng.

### Thời gian

```csharp
if (form.EndDateUtc <= form.StartDateUtc)
{
    errors.Add(new VoucherValidationError(nameof(form.EndDateUtc), VoucherValidationMessages.EndDateAfterStart));
}
```

Ngày kết thúc phải sau ngày bắt đầu.

### Độ ưu tiên

```csharp
if (form.Priority < VoucherValidationRules.PriorityMin ||
    form.Priority > VoucherValidationRules.PriorityMax)
{
    errors.Add(new VoucherValidationError(nameof(form.Priority), VoucherValidationMessages.PriorityRange));
}
```

Độ ưu tiên nằm từ 0 đến 9999.

## 16. Result objects

File:

```text
Services/Vouchers/VoucherServiceResults.cs
```

### VoucherValidationError

```csharp
public sealed record VoucherValidationError(string FieldName, string Message);
```

`FieldName` dùng để controller đưa lỗi vào đúng field trong `ModelState`.

### VoucherSaveResult

```csharp
public sealed class VoucherSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public VoucherFormData Form { get; init; } = new();
    public IReadOnlyCollection<VoucherValidationError> Errors { get; init; } = Array.Empty<VoucherValidationError>();
}
```

`VoucherSaveResult` dùng cho create/update:

- `Succeeded`: lưu thành công hay không.
- `Message`: thông báo thành công.
- `Form`: dữ liệu form hiện tại.
- `Errors`: lỗi nghiệp vụ.

Điểm quan trọng là `Form` là `VoucherFormData`, không phải ViewModel.

### VoucherDeleteResult

`VoucherDeleteResult` tách rõ ba trạng thái:

- Không tìm thấy voucher.
- Tìm thấy nhưng không cho xóa.
- Xóa thành công.

### VoucherToggleResult

```csharp
public sealed record VoucherToggleResult(bool IsActive);
```

Dùng cho API toggle trạng thái.

## 17. Xóa voucher

```csharp
var entity = await _db.Vouchers
    .Include(voucher => voucher.Orders)
    .Include(voucher => voucher.VoucherUsages)
    .Include(voucher => voucher.VoucherUsers)
    .Include(voucher => voucher.VoucherTargets)
    .FirstOrDefaultAsync(voucher => voucher.Id == id, cancellationToken);
```

Khi xóa, service load các quan hệ cần kiểm tra:

- `Orders`
- `VoucherUsages`
- `VoucherUsers`
- `VoucherTargets`

Không cho xóa nếu voucher đã phát sinh đơn hàng hoặc lượt sử dụng:

```csharp
if (entity.Orders.Count > 0 || entity.VoucherUsages.Count > 0 || entity.UsedCount > 0)
{
    return VoucherDeleteResult.Failed(
        $"Không thể xoá \"{entity.Code}\" vì voucher đã phát sinh đơn hàng hoặc lượt sử dụng.");
}
```

Nếu voucher chưa dùng nhưng có assignment/target, service xóa các bản ghi phụ trước:

```csharp
if (entity.VoucherUsers.Count > 0)
{
    _db.VoucherUsers.RemoveRange(entity.VoucherUsers);
}

if (entity.VoucherTargets.Count > 0)
{
    _db.VoucherTargets.RemoveRange(entity.VoucherTargets);
}

_db.Vouchers.Remove(entity);
```

Lý do:

- Không làm mất lịch sử giao dịch.
- Không để dữ liệu phụ mồ côi.
- Chỉ cho xóa voucher thật sự chưa phát sinh sử dụng.

## 18. Bật/tắt voucher

Service:

```csharp
public async Task<VoucherToggleResult?> ToggleActiveAsync(
    long id,
    CancellationToken cancellationToken = default)
{
    var entity = await _db.Vouchers.FirstOrDefaultAsync(voucher => voucher.Id == id, cancellationToken);
    if (entity is null)
    {
        return null;
    }

    entity.IsActive = !entity.IsActive;
    entity.UpdatedAt = VoucherDateTime.UtcNow();
    await _db.SaveChangesAsync(cancellationToken);

    return new VoucherToggleResult(entity.IsActive);
}
```

Controller:

```csharp
return result is null ? NotFound() : Ok(new { isActive = result.IsActive });
```

Frontend gọi endpoint bằng URL được Razor render ra, không hardcode trong JS.

## 19. Views

### Index.cshtml

File:

```text
Views/Vouchers/Index.cshtml
```

Trang index hiển thị:

- Header module.
- Toast success/error.
- Thống kê tổng voucher, đang chạy, lượt đã dùng, hết hạn.
- Form search/filter.
- Bảng voucher.
- Pagination.

Nút toggle trạng thái có URL do Razor render:

```cshtml
data-voucher-toggle-url="@Url.Action("ToggleActive", "Vouchers", new { id = voucher.Id })"
```

Nhờ vậy `vouchers.js` không cần biết route backend cụ thể.

Nút xóa có các data attribute:

```cshtml
data-voucher-delete
data-voucher-code="@voucher.Code"
data-used-count="@voucher.UsedCount"
data-usage-count="@voucher.UsageCount"
data-order-count="@voucher.OrderCount"
```

Frontend dùng các giá trị này để cảnh báo trước khi submit.

Backend vẫn kiểm tra lại ở `DeleteAsync`, nên frontend chỉ là lớp hỗ trợ trải nghiệm.

### Create.cshtml và Edit.cshtml

Hai file này render form:

```cshtml
<form asp-action="Create"
      method="post"
      data-voucher-form
      data-percentage-discount-max="@Model.PercentageDiscountMax"
      data-percentage-discount-max-message="@Model.PercentageDiscountMaxMessage"
      data-fixed-max-discount-message="@Model.FixedMaxDiscountMessage"
      data-max-uses-per-user-message="@Model.MaxUsesPerUserExceedsMaxUsesMessage"
      data-end-after-start-message="@Model.EndDateAfterStartMessage"
      novalidate>
```

Ý nghĩa:

- `data-voucher-form`: marker để JavaScript bind validation realtime.
- `data-*message`: message cross-field lấy từ server.
- `novalidate`: tắt popup HTML mặc định của browser để dùng thông báo tiếng Việt tự render.

### _Form.cshtml

Partial này chứa các field dùng chung cho create/edit:

- Mã voucher.
- Loại giảm giá.
- Giá trị giảm.
- Mức giảm tối đa.
- Mô tả.
- Đơn tối thiểu.
- Tổng lượt dùng.
- Lượt dùng mỗi khách.
- Thời gian bắt đầu/kết thúc.
- Độ ưu tiên.
- Trạng thái.
- Nút lưu/hủy.

Mã voucher:

```cshtml
<input asp-for="Code"
       id="voucherCode"
       autocomplete="off"
       maxlength="@Model.CodeInputMaxLength"
       ... />
```

`maxlength` là 120 để admin có thể nhập hơn 80 ký tự và thấy lỗi realtime. Giới hạn thật vẫn là 80 thông qua DataAnnotations và service validation.

Ngày giờ:

```cshtml
<input asp-for="StartDate"
       type="datetime-local"
       asp-format="{0:yyyy-MM-ddTHH:mm}" />
```

Form hiển thị giờ admin local. Controller convert sang UTC trước khi đưa xuống service.

## 20. JavaScript

File:

```text
wwwroot/js/vouchers.js
```

JS được bind khi DOM load:

```javascript
document.addEventListener('DOMContentLoaded', () => {
    bindCodeFormatter();
    bindDiscountType();
    bindVoucherFormValidation();
    bindStatusToggles();
    bindDeleteConfirmation();
    bindToastDismiss();
});
```

### Formatter mã voucher

```javascript
function toVoucherCode(value) {
    return value
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/[đĐ]/g, 'd')
        .replace(/\s+/g, '-')
        .replace(/[^A-Za-z0-9_-]/g, '')
        .toUpperCase();
}
```

Hành vi:

- Bỏ dấu tiếng Việt.
- Chuyển `đ` thành `d`.
- Khoảng trắng thành `-`.
- Chỉ giữ chữ, số, `-`, `_`.
- Chuyển uppercase.

Lưu ý:

- Không tự xóa dấu `-` hoặc `_`.
- Nếu admin gõ `SALE-2026`, giữ nguyên dấu `-`.
- Nếu admin gõ `SALE_2026`, giữ nguyên dấu `_`.

### Validation realtime

```javascript
function bindVoucherFormValidation() {
    const form = document.querySelector('[data-voucher-form]');
    if (!form) {
        return;
    }

    const touchedFields = new Set();
    const watchedFields = [
        'Code',
        'DiscountType',
        'DiscountValue',
        'MaxDiscountValue',
        'MinOrderValue',
        'MaxUses',
        'MaxUsesPerUser',
        'StartDate',
        'EndDate',
        'Priority',
    ];
}
```

JS nghe các event:

- `input`
- `change`
- `blur`

Khi field thay đổi, JS validate ngay và hiện lỗi nếu field đã được chạm.

### Đọc rule từ data-val

Ví dụ validate code:

```javascript
const maxLength = Number(field.dataset.valLengthMax || field.getAttribute('maxlength') || 0);
const pattern = field.dataset.valRegexPattern;
```

Các giá trị này đến từ Razor/DataAnnotations:

- `data-val-length-max="80"`
- `data-val-regex-pattern="^[A-Za-z0-9][A-Za-z0-9_-]*$"`
- `data-val-required="Mã voucher là bắt buộc."`

Nhờ vậy JS không cần tự định nghĩa lại tất cả rule.

### Cross-field validation

Một số rule cần so sánh nhiều field nên JS xử lý thêm:

- Giảm phần trăm không vượt 100.
- Mức giảm tối đa không nhỏ hơn giá trị giảm cố định.
- Lượt dùng mỗi khách không lớn hơn tổng lượt dùng.
- Ngày kết thúc phải sau ngày bắt đầu.

Message lấy từ data attribute trên form:

```javascript
form.dataset.fixedMaxDiscountMessage
form.dataset.maxUsesPerUserMessage
form.dataset.endAfterStartMessage
```

Các message này được Razor render từ ViewModel, còn nguồn gốc cuối cùng là `VoucherValidationMessages`.

### setCustomValidity

```javascript
field.setCustomValidity(message || '');
field.setAttribute('aria-invalid', message ? 'true' : 'false');
field.classList.toggle('voucher-field-invalid', Boolean(visibleMessage));
```

Mục đích:

- Browser biết field đang invalid.
- UI có class để highlight lỗi.
- Hỗ trợ accessibility qua `aria-invalid`.

### Toggle trạng thái

```javascript
const url = button.dataset.voucherToggleUrl;
const response = await fetch(url, {
    method: 'POST',
    headers: {
        RequestVerificationToken: token,
        'X-Requested-With': 'XMLHttpRequest',
    },
});
```

URL lấy từ `data-voucher-toggle-url`, không hardcode trong JS.

Sau khi toggle thành công:

```javascript
window.location.reload();
```

Reload để thống kê trên index luôn đúng.

### Confirm xóa

```javascript
if (usedCount > 0 || usageCount > 0 || orderCount > 0) {
    event.preventDefault();
    alert(`Không thể xoá "${code}" vì voucher đã phát sinh đơn hàng hoặc lượt sử dụng.`);
    return;
}
```

Frontend chặn sớm để admin thấy cảnh báo ngay.

Backend vẫn kiểm tra lại trong `DeleteAsync`, nên không phụ thuộc vào JS để bảo vệ dữ liệu.

## 21. CSS

File:

```text
wwwroot/css/vouchers.css
```

CSS chứa style riêng cho module voucher:

- Animation row/card.
- Style mã voucher.
- Style field invalid.
- Status pill theo trạng thái.
- Nút action edit/delete.
- Usage progress bar.

Class quan trọng:

```css
.voucher-field-invalid
```

Class này được JS bật/tắt khi validate realtime. Nó giúp input lỗi có border/ring đỏ thay vì dùng popup mặc định của browser.

## 22. Luồng dữ liệu create voucher từ đầu đến cuối

```text
Admin mở /Vouchers/Create
  -> VouchersController.Create()
  -> VoucherAdminService.GetCreateForm()
  -> Controller ToFormViewModel()
  -> Create.cshtml + _Form.cshtml render form
  -> vouchers.js bind validation realtime

Admin nhập form và submit
  -> VouchersController.Create(POST)
  -> ModelState kiểm tra DataAnnotations
  -> Controller ToFormData()
  -> VoucherAdminService.CreateAsync()
      -> NormalizeForm()
      -> ValidateFormAsync()
      -> SaveChangesAsync()
  -> Redirect về Index nếu thành công
  -> Trả view kèm ModelState nếu lỗi
```

## 23. Luồng dữ liệu edit voucher

```text
Admin mở /Vouchers/Edit/{id}
  -> VouchersController.Edit(GET)
  -> VoucherAdminService.GetEditFormAsync(id)
  -> Controller ToFormViewModel()
      -> convert UTC sang giờ admin
  -> Edit.cshtml + _Form.cshtml

Admin submit
  -> VouchersController.Edit(POST)
  -> ToFormData()
      -> convert giờ admin sang UTC
  -> VoucherAdminService.UpdateAsync()
      -> tìm entity cũ
      -> validate rule
      -> update field
      -> SaveChangesAsync()
```

## 24. Luồng dữ liệu index voucher

```text
Admin mở /Vouchers?search=&status=&page=
  -> VouchersController.Index()
  -> VoucherAdminService.GetIndexAsync()
      -> apply search
      -> apply status
      -> count/stat bằng DB query
      -> Skip/Take DB-side
      -> projection sang VoucherIndexItem
      -> resolve StatusKey
  -> Controller ToIndexViewModel()
      -> convert UTC sang giờ admin
      -> map StatusKey sang StatusLabel tiếng Việt
  -> Index.cshtml render bảng
```

## 25. Ranh giới frontend/backend hiện tại

Backend chịu trách nhiệm:

- Query database.
- Validate nghiệp vụ thật.
- Check trùng mã.
- Check thời gian.
- Check giới hạn lượt dùng.
- Check xóa an toàn.
- Lưu UTC.
- Tạo/sửa/xóa/toggle dữ liệu.

Frontend chịu trách nhiệm:

- Format mã voucher khi nhập.
- Hiển thị cảnh báo realtime.
- Đổi ký hiệu `đ`/`%` theo loại giảm giá.
- Confirm xóa.
- Gọi endpoint toggle.
- Hiển thị UI.

Quan trọng: frontend không phải nguồn sự thật. Nếu người dùng tắt JavaScript hoặc gọi request thủ công, backend service vẫn validate đầy đủ.

## 26. Cách thêm rule mới đúng pattern

Ví dụ muốn thêm rule: voucher phần trăm bắt buộc có `MaxDiscountValue`.

Các bước nên làm:

1. Thêm message vào `VoucherValidationMessages`.
2. Thêm rule backend trong `ValidateFormAsync`.
3. Nếu rule cần realtime, render message qua data attribute trong `Create.cshtml`/`Edit.cshtml`.
4. Thêm logic realtime trong `vouchers.js`.
5. Build và test create/edit.

Không nên:

- Chỉ thêm rule ở JavaScript.
- Hardcode message một bên JS, một bên service.
- Để view tự quyết định dữ liệu có được lưu hay không.

## 27. Cách thêm field mới đúng pattern

Ví dụ muốn thêm field `CampaignId` cho voucher.

Nên đi theo thứ tự:

1. Thêm field vào entity/migration nếu cần lưu DB.
2. Thêm field vào `VoucherFormData`.
3. Thêm field vào `VoucherFormViewModel`.
4. Update mapper `ToFormData` và `ToFormViewModel` trong controller.
5. Update `CreateAsync` và `UpdateAsync` trong service.
6. Thêm field vào `_Form.cshtml`.
7. Thêm validation rule nếu cần.
8. Thêm JS realtime nếu field có rule UX.

Lý do phải update cả DTO và ViewModel:

- DTO phục vụ service/backend.
- ViewModel phục vụ Razor/frontend.
- Controller là boundary nối hai phía.

## 28. Điểm có thể cải tiến sau này

Hiện tại module Voucher đã đủ sạch cho MVC admin. Nếu module lớn hơn, có thể tách tiếp:

- `VoucherAdminMapper`: chuyển mapper ra khỏi controller.
- `VoucherValidator`: tách `ValidateFormAsync` ra khỏi service.
- `VoucherIndexQueryService`: tách query index/thống kê nếu dashboard phức tạp.
- Unit test cho validation rule.
- Integration test cho create/update/delete.
- Đưa timezone admin vào `appsettings.json` nếu hệ thống có nhiều thị trường.

Không nên tách thêm quá sớm nếu module chưa lớn, vì sẽ tăng số file mà chưa đem lại nhiều lợi ích thực tế.

## 29. Các lệnh kiểm tra nên chạy

Kiểm tra build:

```powershell
dotnet build
```

Kiểm tra syntax JavaScript:

```powershell
node --check wwwroot\js\vouchers.js
```

Kiểm tra whitespace diff:

```powershell
git diff --check
```

Kiểm tra render local:

```powershell
dotnet run --no-build --urls http://127.0.0.1:5299
```

Sau đó mở:

```text
http://127.0.0.1:5299/Vouchers
http://127.0.0.1:5299/Vouchers/Create
```

## 30. Checklist test thủ công

Nên test các case sau khi sửa Voucher:

- Tạo voucher mã hợp lệ: `SALE-2026`, `SALE_2026`.
- Nhập mã bắt đầu bằng `-` hoặc `_` và thấy lỗi.
- Nhập mã hơn 80 ký tự và thấy lỗi.
- Nhập giá trị giảm `0` hoặc số âm và thấy lỗi.
- Chọn giảm phần trăm, nhập trên 100 và thấy lỗi.
- Nhập mức giảm tối đa `0` hoặc số âm và thấy lỗi.
- Với giảm cố định, nhập mức giảm tối đa nhỏ hơn giá trị giảm và thấy lỗi.
- Nhập tổng lượt dùng `0` hoặc số âm và thấy lỗi.
- Nhập lượt dùng mỗi khách lớn hơn tổng lượt dùng và thấy lỗi.
- Chọn ngày kết thúc trước ngày bắt đầu và thấy lỗi.
- Tạo voucher thành công và kiểm tra danh sách.
- Sửa voucher đã dùng, không cho đặt tổng lượt dùng nhỏ hơn số lượt đã dùng.
- Xóa voucher chưa dùng thành công.
- Xóa voucher đã có đơn/lượt dùng bị chặn.
- Toggle trạng thái và kiểm tra thống kê cập nhật.

## 31. Tóm tắt pattern hiện tại

Voucher hiện tại dùng pattern:

```text
Controller
  - HTTP request/response
  - ModelState
  - Mapping DTO <-> ViewModel
  - TempData/Redirect/View

Service
  - Business rules
  - Database query/write
  - UTC date handling
  - Return service DTO/result

ViewModel
  - Data for Razor
  - Display formatting
  - DataAnnotations for form
  - Metadata for frontend

JavaScript
  - Realtime UI validation
  - Formatter
  - Toggle/delete interactions
```

Đây là mức tách phù hợp cho dự án MVC hiện tại: đủ sạch để bảo trì, nhưng chưa tách quá nhỏ gây phức tạp không cần thiết.
