# Tài liệu backend quản lý Brand

Tài liệu này giải thích phần backend của màn hình quản lý thương hiệu trong admin. Module Brand đơn giản hơn Category vì Brand là dữ liệu phẳng, không có quan hệ cha con. Tuy vậy nó vẫn cần xử lý đủ các nghiệp vụ như tìm kiếm, lọc trạng thái, tạo, sửa, xóa, bật/tắt trạng thái và upload ảnh logo lên Cloudinary.

## 1. Các file chính

```text
Controllers/BrandsController.cs
Services/Brands/IBrandAdminService.cs
Services/Brands/BrandAdminService.cs
Services/Brands/BrandServiceResults.cs
ViewModels/Brands/BrandViewModels.cs
Views/Brands/*.cshtml
wwwroot/js/brands.js
wwwroot/css/brands.css
```

Ý nghĩa từng nhóm:

- `Controllers/BrandsController.cs`: nhận request HTTP, gọi service và trả response phù hợp.
- `Services/Brands/IBrandAdminService.cs`: interface mô tả các nghiệp vụ mà controller được phép gọi.
- `Services/Brands/BrandAdminService.cs`: xử lý nghiệp vụ chính với database và upload ảnh.
- `Services/Brands/BrandServiceResults.cs`: object kết quả trả về từ service.
- `ViewModels/Brands/BrandViewModels.cs`: dữ liệu dành riêng cho view admin.
- `Views/Brands/*.cshtml`, `wwwroot/js/brands.js`, `wwwroot/css/brands.css`: phần giao diện và tương tác phía trình duyệt.

## 2. Đăng ký service trong Program

```csharp
builder.Services.AddScoped<IBrandAdminService, BrandAdminService>();
builder.Services.AddScoped<IImageUploadService, CloudinaryImageUploadService>();
```

Đoạn này đưa service vào DI container:

- `IBrandAdminService` được implement bởi `BrandAdminService`.
- `IImageUploadService` được implement bởi `CloudinaryImageUploadService`.

Nhờ vậy `BrandsController` không cần tự tạo object service. ASP.NET Core sẽ tự inject dependency qua constructor.

## 3. Controller giữ vai trò điều phối request

```csharp
public sealed class BrandsController : Controller
{
    private readonly IBrandAdminService _brandService;

    public BrandsController(IBrandAdminService brandService)
        => _brandService = brandService;
}
```

Controller chỉ nhận `IBrandAdminService`, không nhận trực tiếp `ApplicationDbContext`. Đây là ranh giới quan trọng:

- Controller không chứa logic query database.
- Controller không chứa logic upload ảnh.
- Controller không chứa logic kiểm tra thương hiệu có sản phẩm hay không.
- Những việc đó thuộc về service.

## 4. Luồng danh sách Index

```csharp
public async Task<IActionResult> Index(
    string? search,
    string? status,
    int page = 1,
    CancellationToken ct = default)
{
    var vm = await _brandService.GetIndexAsync(
        new BrandIndexQuery { Search = search, Status = status, Page = page },
        ct);

    return View(vm);
}
```

Action `Index` nhận dữ liệu từ query string:

- `search`: tìm theo tên thương hiệu hoặc slug.
- `status`: lọc thương hiệu đang bật hoặc đang tắt.
- `page`: trang hiện tại.

Controller đóng gói dữ liệu vào `BrandIndexQuery`, gọi service và trả `BrandIndexViewModel` cho view.

## 5. Service query danh sách Brand

```csharp
var dbQuery = _db.Brands.AsNoTracking();
```

`AsNoTracking()` được dùng vì trang index chỉ đọc dữ liệu. EF Core không cần tracking entity, giúp query nhẹ hơn.

```csharp
if (query.Status == "active")
    dbQuery = dbQuery.Where(b => b.IsActive);
else if (query.Status == "inactive")
    dbQuery = dbQuery.Where(b => !b.IsActive);
```

Đoạn này lọc theo trạng thái:

- `active`: chỉ lấy thương hiệu đang bật.
- `inactive`: chỉ lấy thương hiệu đang tắt.
- Không truyền status thì lấy tất cả.

```csharp
if (!string.IsNullOrWhiteSpace(query.Search))
{
    var term = query.Search.Trim();
    dbQuery = dbQuery.Where(b => b.Name.Contains(term) || b.Slug.Contains(term));
}
```

Đoạn này xử lý tìm kiếm. Admin có thể nhập tên hoặc slug thương hiệu.

## 6. Projection thay vì Include toàn bộ Products

```csharp
private sealed class BrandIndexItem
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ImagePath { get; init; }
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

`BrandIndexItem` là class nội bộ trong service, chỉ dùng để hứng dữ liệu query từ database. Nó không đi ra view và không phải entity.

```csharp
var all = await dbQuery
    .OrderBy(b => b.Name)
    .Select(b => new BrandIndexItem
    {
        Id = b.Id,
        Name = b.Name,
        Slug = b.Slug,
        ImagePath = b.ImagePath,
        IsActive = b.IsActive,
        ProductCount = b.Products.Count,
        CreatedAt = b.CreatedAt,
    })
    .ToListAsync(ct);
```

Đoạn này dùng projection để lấy đúng dữ liệu cần cho index.

Điểm quan trọng là `ProductCount = b.Products.Count` chỉ lấy số lượng sản phẩm, không load toàn bộ danh sách sản phẩm vào memory. Cách này sạch hơn và hiệu quả hơn so với `Include(b => b.Products)` nếu trang chỉ cần số lượng.

## 7. Mapping sang ViewModel

```csharp
var rows = pageItems.Select(b => new BrandRowViewModel
{
    Id = b.Id,
    Name = b.Name,
    Slug = b.Slug,
    ImagePath = b.ImagePath,
    IsActive = b.IsActive,
    ProductCount = b.ProductCount,
    CreatedAt = b.CreatedAt,
}).ToList();
```

Service chuyển dữ liệu trung gian `BrandIndexItem` sang `BrandRowViewModel`. View không dùng entity `Brand` trực tiếp.

Lợi ích:

- View chỉ nhận field cần hiển thị.
- Dễ thêm field thống kê mà không sửa entity.
- Giữ ranh giới giữa database model và UI model.

```csharp
return new BrandIndexViewModel
{
    Brands = rows,
    Search = query.Search,
    Status = query.Status,
    Page = page,
    PageSize = DefaultPageSize,
    TotalCount = totalCount,
    ActiveCount = all.Count(b => b.IsActive),
    InactiveCount = all.Count(b => !b.IsActive),
    TotalProductCount = all.Sum(b => b.ProductCount),
};
```

`BrandIndexViewModel` chứa cả danh sách và thống kê:

- Tổng thương hiệu.
- Số thương hiệu đang bật.
- Số thương hiệu đang tắt.
- Tổng sản phẩm thuộc các thương hiệu đang hiển thị theo filter hiện tại.

## 8. ViewModel của Brand

```csharp
public sealed class BrandIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}
```

`BrandIndexQuery` đại diện cho điều kiện lọc từ URL/query string.

```csharp
public sealed class BrandIndexViewModel
{
    public List<BrandRowViewModel> Brands { get; set; } = new();

    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int TotalProductCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}
```

`BrandIndexViewModel` là dữ liệu cho trang danh sách. Nó chứa cả dữ liệu hiển thị, filter hiện tại và phân trang.

```csharp
public sealed class BrandRowViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

Mỗi `BrandRowViewModel` tương ứng một dòng trong bảng Brand ở màn hình admin.

## 9. Form ViewModel

```csharp
public sealed class BrandFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tên thương hiệu là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Slug tối đa 255 ký tự.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
    public string Slug { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự.")]
    public string? ImagePath { get; set; }

    public IFormFile? ImageFile { get; set; }

    public bool IsActive { get; set; } = true;
}
```

`BrandFormViewModel` dùng chung cho tạo và sửa:

- `Name`: tên thương hiệu, bắt buộc.
- `Slug`: slug URL, có thể để trống để service tự sinh.
- `Description`: mô tả thương hiệu.
- `ImagePath`: link ảnh đang lưu trong database.
- `ImageFile`: file ảnh mới admin upload.
- `IsActive`: trạng thái bật/tắt.

## 10. Luồng tạo Brand

```csharp
public async Task<IActionResult> Create(CancellationToken ct)
    => View(await _brandService.GetCreateFormAsync(ct));
```

GET `Create` trả form rỗng. Service set mặc định `IsActive = true`.

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Create(BrandFormViewModel vm, CancellationToken ct)
{
    if (!ModelState.IsValid) return View(vm);

    var result = await _brandService.CreateAsync(vm, ct);
    if (!result.Succeeded)
    {
        AddErrors(result.Errors);
        return View(result.Form);
    }

    TempData["Success"] = result.Message;
    return RedirectToAction(nameof(Index));
}
```

POST `Create` xử lý theo luồng:

1. Validate form bằng DataAnnotations.
2. Gọi service để validate nghiệp vụ và lưu database.
3. Nếu lỗi, đưa lỗi vào `ModelState`.
4. Nếu thành công, redirect về trang danh sách.

## 11. Service tạo Brand

```csharp
public async Task<BrandSaveResult> CreateAsync(BrandFormViewModel form, CancellationToken ct = default)
{
    NormalizeForm(form);

    var errors = await ValidateAsync(form, existingId: null, ct);
    if (errors.Count > 0)
        return BrandSaveResult.Failed(form, errors);

    var uploadError = await UploadImageIfNeededAsync(form, ct);
    if (uploadError is not null)
        return BrandSaveResult.Failed(form, new[] { uploadError });

    var entity = new Brand
    {
        Name = form.Name,
        Slug = form.Slug,
        Description = form.Description,
        ImagePath = form.ImagePath,
        IsActive = form.IsActive,
        CreatedAt = DateTime.UtcNow,
    };

    _db.Brands.Add(entity);
    await _db.SaveChangesAsync(ct);

    form.Id = entity.Id;
    return BrandSaveResult.Success(form, $"Đã tạo thương hiệu \"{entity.Name}\" thành công.");
}
```

Thứ tự xử lý:

1. Chuẩn hóa dữ liệu nhập.
2. Kiểm tra trùng slug.
3. Upload ảnh nếu admin chọn file.
4. Tạo entity `Brand`.
5. Lưu vào database.
6. Trả kết quả thành công.

## 12. Normalize và sinh slug

```csharp
private static void NormalizeForm(BrandFormViewModel form)
{
    form.Name = form.Name.Trim();
    form.Slug = string.IsNullOrWhiteSpace(form.Slug)
        ? GenerateSlug(form.Name)
        : GenerateSlug(form.Slug);
    form.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
    form.ImagePath = string.IsNullOrWhiteSpace(form.ImagePath) ? null : form.ImagePath.Trim();
}
```

`NormalizeForm` đảm bảo dữ liệu lưu vào database sạch:

- Xóa khoảng trắng thừa.
- Tự sinh slug nếu slug rỗng.
- Chuẩn hóa slug nếu admin nhập slug thủ công.
- Chuyển mô tả rỗng thành `null`.
- Chuyển link ảnh rỗng thành `null`.

```csharp
private static string GenerateSlug(string value)
{
    if (string.IsNullOrWhiteSpace(value)) return string.Empty;

    var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder(normalized.Length);

    foreach (var ch in normalized)
    {
        var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
        if (uc == UnicodeCategory.NonSpacingMark) continue;
        if (ch == '\u0111') { sb.Append('d'); continue; }
        if (ch is >= 'a' and <= 'z' or >= '0' and <= '9') { sb.Append(ch); continue; }
        if (char.IsWhiteSpace(ch) || ch is '-' or '_') sb.Append('-');
    }

    return Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
}
```

Hàm này chuyển tên thương hiệu thành slug an toàn. Ví dụ `Thời Trang Việt` thành `thoi-trang-viet`.

## 13. Validate nghiệp vụ Brand

```csharp
private async Task<List<BrandValidationError>> ValidateAsync(
    BrandFormViewModel form,
    long? existingId,
    CancellationToken ct)
{
    var errors = new List<BrandValidationError>();

    if (await _db.Brands.AnyAsync(
            b => b.Slug == form.Slug && (!existingId.HasValue || b.Id != existingId.Value), ct))
    {
        errors.Add(new BrandValidationError(nameof(form.Slug), "Slug đã tồn tại, hãy dùng slug khác."));
    }

    return errors;
}
```

Hiện tại nghiệp vụ chính của Brand là slug không được trùng.

- Khi tạo mới, kiểm tra toàn bộ bảng Brand.
- Khi cập nhật, bỏ qua chính Brand đang sửa.

Đây là validate ở tầng service, không phụ thuộc vào view.

## 14. Upload ảnh logo lên Cloudinary

```csharp
private async Task<BrandValidationError?> UploadImageIfNeededAsync(
    BrandFormViewModel form,
    CancellationToken ct)
{
    if (form.ImageFile is null || form.ImageFile.Length <= 0)
        return null;

    var result = await _imageUploadService.UploadAsync(form.ImageFile, BrandImageFolder, ct);
    if (!result.Succeeded)
        return new BrandValidationError(nameof(form.ImageFile),
            result.ErrorMessage ?? "Không thể tải ảnh lên Cloudinary.");

    form.ImagePath = result.SecureUrl;
    return null;
}
```

Luồng upload:

1. Nếu không có file ảnh mới, bỏ qua upload.
2. Nếu có file, gọi `IImageUploadService`.
3. Service upload lên folder `brands` trên Cloudinary.
4. Nếu upload lỗi, trả lỗi vào field `ImageFile`.
5. Nếu upload thành công, gán link `SecureUrl` vào `form.ImagePath`.

SQL Server chỉ lưu link ảnh, không lưu binary ảnh.

## 15. Luồng cập nhật Brand

```csharp
public async Task<BrandSaveResult> UpdateAsync(long id, BrandFormViewModel form, CancellationToken ct = default)
{
    NormalizeForm(form);

    var entity = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, ct);
    if (entity is null)
        return BrandSaveResult.Failed(form,
            new[] { new BrandValidationError(string.Empty, "Không tìm thấy thương hiệu.") });

    var errors = await ValidateAsync(form, existingId: id, ct);
    if (errors.Count > 0)
        return BrandSaveResult.Failed(form, errors);

    var uploadError = await UploadImageIfNeededAsync(form, ct);
    if (uploadError is not null)
        return BrandSaveResult.Failed(form, new[] { uploadError });

    entity.Name = form.Name;
    entity.Slug = form.Slug;
    entity.Description = form.Description;
    entity.ImagePath = form.ImagePath;
    entity.IsActive = form.IsActive;
    entity.UpdatedAt = DateTime.UtcNow;

    await _db.SaveChangesAsync(ct);
    return BrandSaveResult.Success(form, $"Đã cập nhật thương hiệu \"{entity.Name}\" thành công.");
}
```

Update xử lý theo cùng nguyên tắc với create:

- Chuẩn hóa dữ liệu.
- Tìm entity đang sửa.
- Validate slug.
- Upload ảnh nếu có ảnh mới.
- Gán field được phép sửa.
- Set `UpdatedAt`.
- Lưu database.

## 16. Luồng xóa Brand

```csharp
var entity = await _db.Brands
    .Include(b => b.Products)
    .FirstOrDefaultAsync(b => b.Id == id, ct);
```

Khi xóa Brand, service lấy thêm danh sách `Products` để kiểm tra có sản phẩm nào đang dùng thương hiệu này không.

```csharp
if (entity.Products.Count > 0)
    return BrandDeleteResult.Failed(
        $"Không thể xoá \"{entity.Name}\" vì có {entity.Products.Count} sản phẩm đang dùng thương hiệu này.");

_db.Brands.Remove(entity);
await _db.SaveChangesAsync(ct);
```

Không cho xóa Brand nếu còn sản phẩm đang tham chiếu đến Brand đó. Đây là cách bảo vệ dữ liệu thực tế:

- Không làm sản phẩm mất thương hiệu.
- Không gây lỗi khóa ngoại.
- Buộc admin xử lý sản phẩm trước khi xóa thương hiệu.

## 17. Bật/tắt trạng thái Brand

```csharp
public async Task<BrandToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default)
{
    var entity = await _db.Brands.FirstOrDefaultAsync(b => b.Id == id, ct);
    if (entity is null) return null;

    entity.IsActive = !entity.IsActive;
    entity.UpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync(ct);
    return new BrandToggleResult(entity.IsActive);
}
```

Toggle chỉ đảo trạng thái `IsActive`.

Controller trả JSON:

```csharp
return result is null ? NotFound() : Ok(new { isActive = result.IsActive });
```

JavaScript ở trang index gọi endpoint này. Sau khi request thành công, frontend reload lại trang để thống kê như tổng bật/tắt được cập nhật chính xác.

## 18. Các object Result

```csharp
public sealed record BrandValidationError(string FieldName, string Message);
```

`BrandValidationError` mô tả lỗi nghiệp vụ. Controller dùng `FieldName` để đưa lỗi vào đúng input trong form.

```csharp
public sealed class BrandSaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public BrandFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<BrandValidationError> Errors { get; init; } = Array.Empty<BrandValidationError>();
}
```

`BrandSaveResult` dùng cho create và update:

- `Succeeded`: thao tác thành công hay không.
- `Message`: thông báo thành công.
- `Form`: form hiện tại để trả lại view nếu lỗi.
- `Errors`: danh sách lỗi nghiệp vụ.

```csharp
public sealed class BrandDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
}
```

`BrandDeleteResult` dùng cho delete:

- `Found = false`: không tìm thấy Brand.
- `Found = true`, `Succeeded = false`: tìm thấy nhưng không cho xóa.
- `Found = true`, `Succeeded = true`: xóa thành công.

## 19. Ranh giới frontend và backend

Backend của Brand chịu trách nhiệm:

- Query database.
- Tìm kiếm, lọc trạng thái, phân trang.
- Validate slug không trùng.
- Upload logo lên Cloudinary.
- Lưu link ảnh vào SQL Server.
- Ngăn xóa thương hiệu đang có sản phẩm.
- Bật/tắt trạng thái.

Frontend của Brand chịu trách nhiệm:

- Hiển thị bảng thương hiệu.
- Hiển thị logo.
- Preview ảnh trước khi gửi form.
- Gửi request bật/tắt trạng thái.
- Reload trang sau khi toggle để thống kê đúng.

Điểm quan trọng là các ràng buộc nghiệp vụ luôn nằm trong service backend. Frontend có thể hỗ trợ trải nghiệm người dùng, nhưng không phải nơi quyết định dữ liệu có hợp lệ để lưu hay không.

