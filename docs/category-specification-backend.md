# Tài liệu backend quản lý CategorySpecification

Tài liệu này giải thích module `CategorySpecification`, tức phần **gán thông số kỹ thuật vào danh mục**. Đây là module nối giữa `Category` và `Specification`.

Ví dụ:

```text
Danh mục: Điện thoại
Thông số được gán:
- screen_size  -> Kích thước màn hình
- ram          -> Bộ nhớ RAM
- storage      -> Dung lượng lưu trữ
- battery      -> Dung lượng pin
```

## 1. Vai trò nghiệp vụ

`Specification` là thông số gốc toàn hệ thống.

`Category` là danh mục sản phẩm.

`CategorySpecification` trả lời câu hỏi:

```text
Danh mục này cần những thông số kỹ thuật nào?
Thông số nào bắt buộc?
Thông số đó nằm trong nhóm nào?
Thông số đó hiển thị theo thứ tự nào?
```

Ví dụ:

```text
Điện thoại
- Màn hình: Kích thước màn hình, bắt buộc
- Hiệu năng: Bộ nhớ RAM, bắt buộc
- Pin: Dung lượng pin, không bắt buộc
```

## 2. Các file chính

```text
Controllers/CategorySpecificationsController.cs
Services/CategorySpecifications/ICategorySpecAdminService.cs
Services/CategorySpecifications/CategorySpecAdminService.cs
Services/CategorySpecifications/CategorySpecServiceResults.cs
ViewModels/CategorySpecifications/CategorySpecificationViewModels.cs
Views/CategorySpecifications/Index.cshtml
wwwroot/js/category-specifications.js
wwwroot/css/specifications.css
```

Ý nghĩa:

- `CategorySpecificationsController.cs`: nhận request cho màn hình gán thông số vào một danh mục.
- `ICategorySpecAdminService.cs`: interface nghiệp vụ.
- `CategorySpecAdminService.cs`: xử lý query, gán, cập nhật, bỏ gán.
- `CategorySpecServiceResults.cs`: object kết quả nghiệp vụ.
- `CategorySpecificationViewModels.cs`: ViewModel riêng cho màn hình này.
- `Views/CategorySpecifications/Index.cshtml`: giao diện gán thông số.
- `category-specifications.js`: frontend riêng cho màn hình gán thông số vào danh mục.
- `specifications.css`: style dùng chung cho giao diện thông số.

## 3. Entity và khóa chính

Trong database, bảng tương ứng là `category_specifications`.

```csharp
modelBuilder.Entity<CategorySpecification>(entity =>
{
    entity.ToTable("category_specifications");
    entity.HasKey(item => new { item.CategoryId, item.SpecificationId });
    entity.Property(item => item.GroupName).HasMaxLength(120);
    entity.HasOne(item => item.Category)
        .WithMany(category => category.CategorySpecifications)
        .HasForeignKey(item => item.CategoryId);
    entity.HasOne(item => item.Specification)
        .WithMany(specification => specification.CategorySpecifications)
        .HasForeignKey(item => item.SpecificationId);
});
```

Điểm quan trọng:

- Bảng này không dùng `Id` riêng.
- Khóa chính là cặp `{ CategoryId, SpecificationId }`.
- Một thông số chỉ được gán một lần cho một danh mục.
- Một danh mục có thể có nhiều thông số.
- Một thông số có thể được dùng bởi nhiều danh mục.

## 4. Đăng ký service

Trong `Program.cs`:

```csharp
builder.Services.AddScoped<ICategorySpecAdminService, CategorySpecAdminService>();
```

Controller chỉ phụ thuộc `ICategorySpecAdminService`, không tự gọi `ApplicationDbContext`.

## 5. Controller

File: `Controllers/CategorySpecificationsController.cs`

```csharp
public sealed class CategorySpecificationsController : Controller
{
    private readonly ICategorySpecAdminService _service;

    public CategorySpecificationsController(ICategorySpecAdminService service)
        => _service = service;
}
```

Controller này chỉ phục vụ màn hình gán thông số cho danh mục. Nó không quản lý CRUD thông số gốc, vì CRUD thông số gốc đã thuộc `SpecificationsController`.

## 6. Mở trang gán thông số cho danh mục

```csharp
public async Task<IActionResult> Index(
    long categoryId, string? search, int page = 1, CancellationToken ct = default)
{
    var vm = await _service.GetIndexAsync(
        categoryId, new CategorySpecIndexQuery { Search = search, Page = page }, ct);

    return vm is null ? NotFound() : View(vm);
}
```

Luồng:

1. Admin click nút `Thông số` từ trang Category.
2. URL đi tới `/CategorySpecifications?categoryId={id}`.
3. Controller gọi service lấy dữ liệu theo `categoryId`.
4. Nếu danh mục không tồn tại, trả `NotFound`.
5. Nếu tồn tại, render view.

## 7. Link từ Category sang CategorySpecification

Trong `Views/Categories/Index.cshtml`:

```cshtml
<a asp-controller="CategorySpecifications"
   asp-action="Index"
   asp-route-categoryId="@cat.Id"
   title="Thông số kỹ thuật"
   class="cat-action-btn cat-action-spec">
    <i data-lucide="sliders-horizontal" class="w-3.5 h-3.5"></i>
    <span>Thông số</span>
</a>
```

Đây là ranh giới rõ ràng:

- Trang Category chỉ quản lý danh mục.
- Khi cần cấu hình thông số của danh mục, chuyển sang controller riêng `CategorySpecificationsController`.

## 8. Service lấy dữ liệu Index

File: `Services/CategorySpecifications/CategorySpecAdminService.cs`

```csharp
var category = await _db.Categories.AsNoTracking()
    .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

if (category is null) return null;
```

Đầu tiên service kiểm tra danh mục có tồn tại không.

```csharp
var allCategoryAssignments = await _db.CategorySpecifications
    .Include(cs => cs.Specification)
    .Where(cs => cs.CategoryId == categoryId)
    .AsNoTracking()
    .ToListAsync(ct);
```

Service lấy toàn bộ thông số đã gán của danh mục. Dữ liệu này dùng cho 2 việc:

- Hiển thị danh sách thông số đã gán.
- Loại bỏ các thông số đã gán khỏi danh sách `AvailableSpecs`.

```csharp
var assignedQuery = allCategoryAssignments.AsEnumerable();
if (!string.IsNullOrWhiteSpace(query.Search))
{
    var term = query.Search.Trim();
    assignedQuery = assignedQuery.Where(cs =>
        cs.Specification!.Name.Contains(term) ||
        cs.Specification!.Key.Contains(term));
}
```

Search chỉ áp dụng cho danh sách thông số đã gán ở bên trái.

Điểm đã được chỉnh trong project hiện tại: danh sách “thông số chưa gán” không bị phụ thuộc search này. Nếu không tách như vậy, khi search có thể làm thông số đã gán nhưng không khớp search hiện nhầm ở danh sách chưa gán.

```csharp
var allAssigned = assignedQuery
    .OrderBy(cs => cs.SortOrder)
    .ThenBy(cs => cs.Specification!.Name)
    .ToList();
```

Danh sách đã gán được sắp xếp theo:

1. `SortOrder`
2. Tên thông số

## 9. Đếm usage theo sản phẩm

```csharp
var categoryProductIds = await _db.Products
    .Where(p => p.CategoryId == categoryId)
    .Select(p => p.Id)
    .ToListAsync(ct);

var usageMap = await _db.ProductSpecifications
    .Where(ps => categoryProductIds.Contains(ps.ProductId))
    .GroupBy(ps => ps.SpecificationId)
    .Select(g => new { SpecId = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.SpecId, x => x.Count, ct);
```

Mục đích:

- Biết mỗi thông số đang được bao nhiêu sản phẩm trong danh mục sử dụng.
- Khi bỏ gán thông số, nếu thông số đã được sản phẩm sử dụng thì không cho bỏ gán.

## 10. Mapping danh sách đã gán

```csharp
var rows = pageItems.Select(cs => new CategorySpecRowViewModel
{
    SpecificationId = cs.SpecificationId,
    Key = cs.Specification!.Key,
    Name = cs.Specification.Name,
    Unit = cs.Specification.Unit,
    GroupName = cs.GroupName,
    IsRequired = cs.IsRequired,
    SortOrder = cs.SortOrder,
    ProductUsageCount = usageMap.GetValueOrDefault(cs.SpecificationId),
}).ToList();
```

`CategorySpecRowViewModel` là dữ liệu mỗi dòng trong bảng đã gán:

- `SpecificationId`: id thông số.
- `Key`: mã kỹ thuật.
- `Name`: tên hiển thị.
- `Unit`: đơn vị.
- `GroupName`: nhóm hiển thị.
- `IsRequired`: có bắt buộc không.
- `SortOrder`: thứ tự.
- `ProductUsageCount`: số sản phẩm đang dùng thông số này.

## 11. Lấy danh sách thông số chưa gán

```csharp
var assignedIds = allCategoryAssignments.Select(cs => cs.SpecificationId).ToHashSet();
var available = await _db.Specifications.AsNoTracking()
    .Where(s => !assignedIds.Contains(s.Id))
    .OrderBy(s => s.Name)
    .Select(s => new AvailableSpecOption
    {
        Id = s.Id,
        Key = s.Key,
        Name = s.Name,
        Unit = s.Unit,
    })
    .ToListAsync(ct);
```

Điểm quan trọng là `assignedIds` lấy từ `allCategoryAssignments`, tức toàn bộ thông số đã gán của danh mục. Nó không lấy từ danh sách đã bị filter search.

Nhờ vậy danh sách bên phải “Thông số chưa gán” luôn đúng.

## 12. ViewModel Index

File: `ViewModels/CategorySpecifications/CategorySpecificationViewModels.cs`

```csharp
public sealed class CategorySpecIndexViewModel
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;

    public List<CategorySpecRowViewModel> AssignedSpecs { get; set; } = new();
    public List<AvailableSpecOption> AvailableSpecs { get; set; } = new();
    public CategorySpecAssignViewModel AssignForm { get; set; } = new();

    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalAssigned { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalAssigned / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}
```

ViewModel này chứa:

- Thông tin danh mục đang cấu hình.
- Danh sách thông số đã gán.
- Danh sách thông số chưa gán.
- Form gán mới.
- Filter và phân trang.

## 13. Form gán thông số

```csharp
public sealed class CategorySpecAssignViewModel
{
    [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "Không xác định được danh mục.")]
    public long CategoryId { get; set; }

    [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "Vui lòng chọn thông số.")]
    public long SpecificationId { get; set; }

    [StringLength(120, ErrorMessage = "Tên nhóm tối đa 120 ký tự.")]
    public string? GroupName { get; set; }

    public bool IsRequired { get; set; }

    [Range(0, 9999)]
    public int SortOrder { get; set; }
}
```

Lưu ý: dùng `Range` thay vì chỉ dùng `Required` cho `long`.

Lý do: `long` là value type, nếu không bind được thì mặc định là `0`. `[Required]` không bắt được lỗi `0`, còn `Range(1, long.MaxValue)` thì bắt được.

## 14. Gán thông số vào danh mục

Controller:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Assign(CategorySpecAssignViewModel form, CancellationToken ct)
{
    if (!ModelState.IsValid)
    {
        TempData["Error"] = "Dữ liệu không hợp lệ.";
        return RedirectToAction(nameof(Index), new { categoryId = form.CategoryId });
    }

    var result = await _service.AssignAsync(form, ct);
    TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
    return RedirectToAction(nameof(Index), new { categoryId = form.CategoryId });
}
```

Service:

```csharp
var categoryExists = await _db.Categories.AsNoTracking()
    .AnyAsync(c => c.Id == form.CategoryId, ct);

if (!categoryExists)
    return new CategorySpecSaveResult(false, "Không tìm thấy danh mục.");
```

Trước khi gán, service kiểm tra danh mục tồn tại.

```csharp
var spec = await _db.Specifications.AsNoTracking()
    .FirstOrDefaultAsync(s => s.Id == form.SpecificationId, ct);

if (spec is null)
    return new CategorySpecSaveResult(false, "Không tìm thấy thông số.");
```

Sau đó kiểm tra thông số tồn tại.

```csharp
var existing = await _db.CategorySpecifications
    .FirstOrDefaultAsync(
        cs => cs.CategoryId == form.CategoryId && cs.SpecificationId == form.SpecificationId, ct);

if (existing is not null)
    return new CategorySpecSaveResult(false, $"Thông số \"{spec.Name}\" đã được gán cho danh mục này.");
```

Không cho gán trùng cùng một thông số vào cùng một danh mục.

```csharp
_db.CategorySpecifications.Add(new CategorySpecification
{
    CategoryId = form.CategoryId,
    SpecificationId = form.SpecificationId,
    GroupName = string.IsNullOrWhiteSpace(form.GroupName) ? null : form.GroupName.Trim(),
    IsRequired = form.IsRequired,
    SortOrder = form.SortOrder,
});

await _db.SaveChangesAsync(ct);
```

Nếu hợp lệ thì tạo bản ghi ở bảng nối `category_specifications`.

## 15. Cập nhật thông số đã gán

Controller:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Update(CategorySpecUpdateViewModel form, CancellationToken ct)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(new { succeeded = false, message = "Dữ liệu cập nhật không hợp lệ." });
    }

    var result = await _service.UpdateAsync(form, ct);
    return Ok(new { result.Succeeded, result.Message });
}
```

Action này phục vụ AJAX inline update từ frontend.

Service:

```csharp
var entity = await _db.CategorySpecifications
    .FirstOrDefaultAsync(
        cs => cs.CategoryId == form.CategoryId && cs.SpecificationId == form.SpecificationId, ct);

if (entity is null)
    return new CategorySpecSaveResult(false, "Không tìm thấy liên kết thông số - danh mục.");
```

Vì bảng dùng khóa chính kép, service phải tìm bằng cả:

- `CategoryId`
- `SpecificationId`

```csharp
entity.GroupName = string.IsNullOrWhiteSpace(form.GroupName) ? null : form.GroupName.Trim();
entity.IsRequired = form.IsRequired;
entity.SortOrder = form.SortOrder;

await _db.SaveChangesAsync(ct);
```

Các field được phép update:

- Tên nhóm.
- Có bắt buộc không.
- Thứ tự hiển thị.

Không cho update `CategoryId` hoặc `SpecificationId` vì đó là khóa liên kết.

## 16. Frontend update inline

File: `wwwroot/js/category-specifications.js`

```javascript
const categoryId = row.dataset.categoryId;
const specificationId = row.dataset.specId;
const groupName = row.querySelector('[data-field="groupName"]')?.value ?? '';
const sortOrder = Number.parseInt(row.querySelector('[data-field="sortOrder"]')?.value ?? '0', 10);
const isRequired = row.querySelector('[data-field="isRequired"]')?.classList.contains('active') ?? false;
```

JS lấy dữ liệu từ row hiện tại.

```javascript
body: new URLSearchParams({
    CategoryId: categoryId,
    SpecificationId: specificationId,
    GroupName: groupName,
    SortOrder: Number.isNaN(sortOrder) ? 0 : sortOrder,
    IsRequired: isRequired,
}),
```

Điểm quan trọng: frontend gửi đúng tên field `CategoryId` và `SpecificationId` để model binding vào `CategorySpecUpdateViewModel`.

Trước đây nếu gửi `specId`, backend không bind được vào `SpecificationId`, giá trị thành `0`, dẫn tới lỗi “Không tìm thấy liên kết thông số - danh mục”.

## 17. Bỏ gán thông số khỏi danh mục

Controller:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Remove(long categoryId, long specId, CancellationToken ct)
{
    var result = await _service.RemoveAsync(categoryId, specId, ct);

    if (!result.Found) return NotFound();

    TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
    return RedirectToAction(nameof(Index), new { categoryId });
}
```

Service:

```csharp
var entity = await _db.CategorySpecifications
    .Include(cs => cs.Specification)
    .FirstOrDefaultAsync(
        cs => cs.CategoryId == categoryId && cs.SpecificationId == specId, ct);

if (entity is null)
    return new CategorySpecRemoveResult(false, false, "Không tìm thấy liên kết.");
```

Tìm liên kết bằng khóa kép.

```csharp
var inUse = await _db.ProductSpecifications.AnyAsync(
    ps => ps.SpecificationId == specId &&
          _db.Products.Any(p => p.Id == ps.ProductId && p.CategoryId == categoryId), ct);

if (inUse)
    return new CategorySpecRemoveResult(true, false,
        $"Không thể bỏ gán \"{entity.Specification!.Name}\" vì đang được dùng bởi sản phẩm trong danh mục.");
```

Không cho bỏ gán nếu đã có sản phẩm trong danh mục đang dùng thông số này.

```csharp
_db.CategorySpecifications.Remove(entity);
await _db.SaveChangesAsync(ct);
```

Nếu chưa có sản phẩm dùng, cho phép xóa liên kết.

## 18. Result objects

File: `Services/CategorySpecifications/CategorySpecServiceResults.cs`

```csharp
public sealed record CategorySpecSaveResult(bool Succeeded, string Message);
public sealed record CategorySpecRemoveResult(bool Found, bool Succeeded, string Message);
```

`CategorySpecSaveResult` dùng cho:

- Gán thông số.
- Cập nhật inline.

`CategorySpecRemoveResult` dùng cho bỏ gán, cần thêm `Found` để phân biệt:

- Không tìm thấy liên kết.
- Tìm thấy nhưng không cho bỏ gán.
- Bỏ gán thành công.

## 19. Ranh giới frontend/backend

Backend chịu trách nhiệm:

- Kiểm tra category tồn tại.
- Kiểm tra specification tồn tại.
- Kiểm tra gán trùng.
- Kiểm tra liên kết bằng khóa kép.
- Không cho bỏ gán nếu sản phẩm đang dùng.
- Lưu `GroupName`, `IsRequired`, `SortOrder`.

Frontend chịu trách nhiệm:

- Render danh sách.
- Lọc nhanh danh sách thông số chưa gán.
- Gửi AJAX khi sửa `GroupName`, `SortOrder`, `IsRequired`.
- Hiển thị toast phản hồi.

Frontend không quyết định dữ liệu có hợp lệ hay không. Backend vẫn validate lại toàn bộ.

