# Tài liệu backend quản lý Specification

Tài liệu này giải thích module `Specification`, tức phần **Thông số kỹ thuật** dùng chung toàn hệ thống. Đây là nơi admin khai báo các thông số gốc như `ram`, `storage`, `screen_size`, `battery`, `material`. Sau đó từng danh mục sẽ chọn những thông số nào được áp dụng thông qua module `CategorySpecification`.

## 1. Vai trò của Specification

`Specification` là danh mục thông số kỹ thuật toàn cục, chưa gắn với sản phẩm cụ thể và cũng chưa bắt buộc thuộc danh mục cụ thể.

Ví dụ:

```text
ram          -> Bộ nhớ RAM
storage      -> Dung lượng lưu trữ
screen_size  -> Kích thước màn hình
battery      -> Dung lượng pin
material     -> Chất liệu
```

Khi tạo sản phẩm sau này, hệ thống sẽ dựa vào danh mục sản phẩm để biết sản phẩm đó cần nhập những thông số nào.

## 2. Các file chính

```text
Controllers/SpecificationsController.cs
Services/Specifications/ISpecificationAdminService.cs
Services/Specifications/SpecificationAdminService.cs
Services/Specifications/SpecificationServiceResults.cs
ViewModels/Specifications/SpecificationViewModels.cs
Views/Specifications/Index.cshtml
Views/Specifications/Create.cshtml
Views/Specifications/Edit.cshtml
Views/Specifications/_Form.cshtml
wwwroot/js/specifications.js
wwwroot/css/specifications.css
```

Ý nghĩa:

- `SpecificationsController.cs`: nhận request HTTP cho trang quản lý thông số.
- `ISpecificationAdminService.cs`: interface định nghĩa các nghiệp vụ của thông số.
- `SpecificationAdminService.cs`: xử lý query, tạo, sửa, xóa, validate key.
- `SpecificationServiceResults.cs`: định nghĩa kết quả nghiệp vụ trả về cho controller.
- `SpecificationViewModels.cs`: dữ liệu riêng cho view, không dùng trực tiếp entity.
- `Views/Specifications/*.cshtml`: giao diện Razor cho CRUD thông số.
- `wwwroot/js/specifications.js`: xử lý frontend riêng cho module Specification.
- `wwwroot/css/specifications.css`: style riêng cho màn hình thông số.

## 3. Đăng ký service

Trong `Program.cs`:

```csharp
builder.Services.AddScoped<ISpecificationAdminService, SpecificationAdminService>();
```

Controller không gọi trực tiếp `ApplicationDbContext`. Controller chỉ gọi `ISpecificationAdminService`. Điều này giúp backend tách rõ:

- Controller điều phối request.
- Service xử lý nghiệp vụ.
- DbContext chỉ nằm trong service.

## 4. Controller

File: `Controllers/SpecificationsController.cs`

```csharp
public sealed class SpecificationsController : Controller
{
    private readonly ISpecificationAdminService _specService;

    public SpecificationsController(ISpecificationAdminService specService)
        => _specService = specService;
}
```

Controller inject interface `ISpecificationAdminService`. Đây là cách giữ controller mỏng và dễ bảo trì.

## 5. Danh sách thông số

```csharp
public async Task<IActionResult> Index(
    string? search, int page = 1, CancellationToken ct = default)
{
    var vm = await _specService.GetIndexAsync(
        new SpecificationIndexQuery { Search = search, Page = page }, ct);
    return View(vm);
}
```

Luồng xử lý:

1. Admin vào `/Specifications`.
2. Controller nhận `search` và `page`.
3. Controller tạo `SpecificationIndexQuery`.
4. Service trả về `SpecificationIndexViewModel`.
5. View render danh sách.

Controller không tự query database và không tự tính thống kê.

## 6. Query danh sách trong Service

File: `Services/Specifications/SpecificationAdminService.cs`

```csharp
var dbQuery = _db.Specifications
    .Include(s => s.CategorySpecifications)
    .Include(s => s.ProductSpecifications)
    .AsNoTracking();
```

Ý nghĩa:

- `Specifications`: bảng thông số gốc.
- `CategorySpecifications`: các danh mục đang sử dụng thông số này.
- `ProductSpecifications`: các sản phẩm đã nhập giá trị cho thông số này.
- `AsNoTracking()`: chỉ đọc dữ liệu nên không cần EF Core tracking.

```csharp
if (!string.IsNullOrWhiteSpace(query.Search))
{
    var term = query.Search.Trim();
    dbQuery = dbQuery.Where(s => s.Key.Contains(term) || s.Name.Contains(term));
}
```

Admin có thể tìm theo:

- `Key`, ví dụ `ram`, `battery`.
- `Name`, ví dụ `Bộ nhớ RAM`, `Dung lượng pin`.

```csharp
var all = await dbQuery.OrderBy(s => s.Name).ToListAsync(ct);
var pageItems = all.Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToList();
```

Service sắp xếp theo tên và phân trang. Hiện tại `DefaultPageSize = 30`.

## 7. Mapping sang ViewModel

```csharp
var rows = pageItems.Select(s => new SpecificationRowViewModel
{
    Id = s.Id,
    Key = s.Key,
    Name = s.Name,
    Unit = s.Unit,
    Icon = s.Icon,
    CategoryCount = s.CategorySpecifications.Count,
    ProductCount = s.ProductSpecifications.Count,
}).ToList();
```

Service không trả entity `Specification` trực tiếp ra view. Nó map sang `SpecificationRowViewModel`.

Các field thống kê:

- `CategoryCount`: thông số này đang được gán vào bao nhiêu danh mục.
- `ProductCount`: thông số này đã được nhập ở bao nhiêu sản phẩm.

```csharp
return new SpecificationIndexViewModel
{
    Specifications = rows,
    Search = query.Search,
    Page = page,
    PageSize = DefaultPageSize,
    TotalCount = all.Count,
    TotalCategoryUsageCount = all.Sum(s => s.CategorySpecifications.Count),
    TotalProductUsageCount = all.Sum(s => s.ProductSpecifications.Count),
};
```

ViewModel trả về đủ dữ liệu cho trang index:

- Danh sách thông số.
- Từ khóa tìm kiếm.
- Thông tin phân trang.
- Tổng số thông số.
- Tổng lượt gán vào danh mục.
- Tổng lượt dùng ở sản phẩm.

## 8. ViewModel

File: `ViewModels/Specifications/SpecificationViewModels.cs`

```csharp
public sealed class SpecificationIndexQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
}
```

`SpecificationIndexQuery` là dữ liệu filter từ request.

```csharp
public sealed class SpecificationIndexViewModel
{
    public List<SpecificationRowViewModel> Specifications { get; set; } = new();
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalCount { get; set; }
    public int TotalCategoryUsageCount { get; set; }
    public int TotalProductUsageCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}
```

`SpecificationIndexViewModel` là dữ liệu cho trang danh sách.

```csharp
public sealed class SpecificationRowViewModel
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? Icon { get; set; }
    public int CategoryCount { get; set; }
    public int ProductCount { get; set; }
}
```

Mỗi `SpecificationRowViewModel` tương ứng một dòng trong bảng thông số.

## 9. Form ViewModel

```csharp
public sealed class SpecificationFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Key là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Key tối đa 100 ký tự.")]
    [RegularExpression(@"^[a-z0-9_]+$",
        ErrorMessage = "Key chỉ gồm chữ thường, số và dấu gạch dưới (_).")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên thông số là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Đơn vị tối đa 50 ký tự.")]
    public string? Unit { get; set; }

    [StringLength(100, ErrorMessage = "Icon tối đa 100 ký tự.")]
    public string? Icon { get; set; }
}
```

Ý nghĩa:

- `Key`: mã định danh kỹ thuật, ví dụ `ram`, `storage`, `screen_size`. Key nên ổn định vì sau này sản phẩm có thể dùng nó để render thông số.
- `Name`: tên hiển thị cho admin hoặc khách hàng.
- `Unit`: đơn vị như `GB`, `inch`, `mAh`.
- `Icon`: tên icon Lucide nếu muốn hiển thị icon.

## 10. Tạo thông số

Controller:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Create(SpecificationFormViewModel vm, CancellationToken ct)
{
    if (!ModelState.IsValid) return View(vm);

    var result = await _specService.CreateAsync(vm, ct);
    if (!result.Succeeded)
    {
        AddErrors(result.Errors);
        return View(result.Form);
    }

    TempData["Success"] = result.Message;
    return RedirectToAction(nameof(Index));
}
```

Service:

```csharp
public async Task<SpecSaveResult> CreateAsync(SpecificationFormViewModel form, CancellationToken ct = default)
{
    NormalizeForm(form);
    var errors = await ValidateAsync(form, existingId: null, ct);
    if (errors.Count > 0) return SpecSaveResult.Failed(form, errors);

    var entity = new Specification
    {
        Key = form.Key,
        Name = form.Name,
        Unit = form.Unit,
        Icon = form.Icon,
    };

    _db.Specifications.Add(entity);
    await _db.SaveChangesAsync(ct);
    form.Id = entity.Id;
    return SpecSaveResult.Success(form, $"Đã tạo thông số \"{entity.Name}\" thành công.");
}
```

Thứ tự xử lý:

1. Chuẩn hóa dữ liệu.
2. Validate key không trùng.
3. Tạo entity.
4. Lưu database.
5. Trả kết quả thành công.

## 11. Cập nhật thông số

```csharp
public async Task<SpecSaveResult> UpdateAsync(long id, SpecificationFormViewModel form, CancellationToken ct = default)
{
    NormalizeForm(form);

    var entity = await _db.Specifications.FirstOrDefaultAsync(s => s.Id == id, ct);
    if (entity is null)
        return SpecSaveResult.Failed(form,
            new[] { new SpecValidationError(string.Empty, "Không tìm thấy thông số.") });

    var errors = await ValidateAsync(form, existingId: id, ct);
    if (errors.Count > 0) return SpecSaveResult.Failed(form, errors);

    entity.Key = form.Key;
    entity.Name = form.Name;
    entity.Unit = form.Unit;
    entity.Icon = form.Icon;

    await _db.SaveChangesAsync(ct);
    return SpecSaveResult.Success(form, $"Đã cập nhật thông số \"{entity.Name}\" thành công.");
}
```

Update khác create ở điểm:

- Phải tìm entity hiện có.
- Khi kiểm tra trùng key, bỏ qua chính entity đang sửa.
- Chỉ cập nhật các field cho phép sửa.

## 12. Normalize dữ liệu

```csharp
private static void NormalizeForm(SpecificationFormViewModel form)
{
    form.Key = form.Key.Trim().ToLowerInvariant().Replace(' ', '_');
    form.Name = form.Name.Trim();
    form.Unit = string.IsNullOrWhiteSpace(form.Unit) ? null : form.Unit.Trim();
    form.Icon = string.IsNullOrWhiteSpace(form.Icon) ? null : form.Icon.Trim();
}
```

Mục đích:

- Key luôn viết thường.
- Khoảng trắng trong key chuyển thành `_`.
- Chuỗi rỗng ở `Unit` và `Icon` chuyển thành `null`.
- Tránh lưu dữ liệu bẩn vào database.

## 13. Validate key duy nhất

```csharp
private async Task<List<SpecValidationError>> ValidateAsync(
    SpecificationFormViewModel form, long? existingId, CancellationToken ct)
{
    var errors = new List<SpecValidationError>();

    if (await _db.Specifications.AnyAsync(
            s => s.Key == form.Key && (!existingId.HasValue || s.Id != existingId.Value), ct))
    {
        errors.Add(new SpecValidationError(nameof(form.Key), $"Key \"{form.Key}\" đã tồn tại."));
    }

    return errors;
}
```

`Key` phải duy nhất vì nó là mã định danh của thông số.

Ví dụ không nên có 2 thông số cùng key `ram`.

## 14. Xóa thông số

```csharp
public async Task<SpecDeleteResult> DeleteAsync(long id, CancellationToken ct = default)
{
    var entity = await _db.Specifications
        .Include(s => s.CategorySpecifications)
        .Include(s => s.ProductSpecifications)
        .FirstOrDefaultAsync(s => s.Id == id, ct);

    if (entity is null) return SpecDeleteResult.NotFound();

    if (entity.CategorySpecifications.Count > 0)
        return SpecDeleteResult.Failed(
            $"Không thể xoá \"{entity.Name}\" vì đang được gán cho {entity.CategorySpecifications.Count} danh mục.");

    if (entity.ProductSpecifications.Count > 0)
        return SpecDeleteResult.Failed(
            $"Không thể xoá \"{entity.Name}\" vì đang được dùng bởi {entity.ProductSpecifications.Count} sản phẩm.");

    _db.Specifications.Remove(entity);
    await _db.SaveChangesAsync(ct);
    return SpecDeleteResult.Success($"Đã xoá thông số \"{entity.Name}\" thành công.");
}
```

Không cho xóa thông số nếu:

- Đang được gán vào danh mục.
- Đang được sản phẩm sử dụng.

Đây là xử lý đúng trong admin vì tránh làm mất dữ liệu liên quan.

## 15. Result objects

File: `Services/Specifications/SpecificationServiceResults.cs`

```csharp
public sealed record SpecValidationError(string FieldName, string Message);
```

`SpecValidationError` mô tả lỗi nghiệp vụ để controller đưa vào `ModelState`.

```csharp
public sealed class SpecSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public SpecificationFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<SpecValidationError> Errors { get; init; } = Array.Empty<SpecValidationError>();
}
```

`SpecSaveResult` dùng cho create/update.

```csharp
public sealed class SpecDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
}
```

`SpecDeleteResult` dùng cho delete, tách rõ:

- Không tìm thấy.
- Tìm thấy nhưng không cho xóa.
- Xóa thành công.

## 16. Frontend của Specification

File: `wwwroot/js/specifications.js`

```javascript
function bindKeyFormatter() {
    const keyInput = document.getElementById('specKey');
    if (!keyInput) {
        return;
    }

    keyInput.addEventListener('input', () => {
        const caret = keyInput.selectionStart;
        keyInput.value = keyInput.value.toLowerCase().replace(/[^a-z0-9_]/g, '_');
        keyInput.setSelectionRange(caret, caret);
    });

    keyInput.addEventListener('blur', () => {
        keyInput.value = keyInput.value.replace(/_+/g, '_').replace(/^_|_$/g, '');
    });
}
```

JS chỉ hỗ trợ trải nghiệm nhập liệu:

- Tự chuyển key thành chữ thường.
- Ký tự không hợp lệ chuyển thành `_`.
- Khi blur thì gộp `_` dư.

Backend vẫn là nơi validate cuối cùng.

```javascript
function bindDeleteConfirmation() {
    document.querySelectorAll('[data-spec-delete]').forEach(form => {
        form.addEventListener('submit', event => {
            const name = form.dataset.specName || 'thông số này';
            const categoryCount = Number.parseInt(form.dataset.categoryCount || '0', 10);
            const productCount = Number.parseInt(form.dataset.productCount || '0', 10);

            if (categoryCount > 0 || productCount > 0) {
                event.preventDefault();
                alert(`Không thể xoá "${name}" vì đang được dùng (${categoryCount} danh mục, ${productCount} sản phẩm).`);
                return;
            }

            if (!confirm(`Bạn có chắc muốn xoá thông số "${name}"?`)) {
                event.preventDefault();
            }
        });
    });
}
```

JS chỉ cảnh báo trước cho admin. Service backend vẫn kiểm tra lại khi delete, nên nếu người dùng bypass frontend thì dữ liệu vẫn được bảo vệ.

## 17. Ranh giới với CategorySpecification

`Specification` chỉ quản lý thông số gốc.

`CategorySpecification` mới là nơi quyết định:

- Danh mục nào dùng thông số nào.
- Thông số đó có bắt buộc trong danh mục không.
- Thứ tự hiển thị của thông số trong danh mục.
- Nhóm hiển thị của thông số trong danh mục.

Vì vậy không nên đưa logic gán thông số vào `SpecificationsController`. Logic đó nằm riêng ở `CategorySpecificationsController`.

