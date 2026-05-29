# Tài liệu backend quản lý Category

Tài liệu này giải thích phần backend của màn hình quản lý danh mục trong admin. Mục tiêu chính là giúp đọc code theo đúng luồng xử lý: route đi vào `Controller`, controller gọi `Service`, service làm nghiệp vụ với database và trả dữ liệu qua `ViewModel` hoặc `Result`.

## 1. Các file chính

```text
Controllers/CategoriesController.cs
Services/Categories/ICategoryAdminService.cs
Services/Categories/CategoryAdminService.cs
Services/Categories/CategoryServiceResults.cs
ViewModels/Categories/CategoryViewModels.cs
Views/Categories/*.cshtml
wwwroot/js/categories.js
wwwroot/css/categories.css
Controllers/CategorySpecificationsController.cs
Views/CategorySpecifications/Index.cshtml
wwwroot/js/category-specifications.js
```

Ý nghĩa từng nhóm:

- `Controllers/CategoriesController.cs`: nhận request HTTP từ trình duyệt, kiểm tra ModelState cơ bản, gọi service và quyết định trả `View`, `Redirect`, `NotFound`, `BadRequest`, `Ok`.
- `Services/Categories/ICategoryAdminService.cs`: hợp đồng nghiệp vụ của module Category. Controller chỉ phụ thuộc interface này, không phụ thuộc trực tiếp vào EF Core.
- `Services/Categories/CategoryAdminService.cs`: nơi xử lý nghiệp vụ chính: tìm kiếm, phân trang, tạo, sửa, xóa, bật/tắt trạng thái, upload ảnh, validate quan hệ cha con.
- `Services/Categories/CategoryServiceResults.cs`: các object kết quả trả về từ service để controller biết thao tác thành công hay thất bại.
- `ViewModels/Categories/CategoryViewModels.cs`: dữ liệu dành riêng cho giao diện admin, không đưa thẳng entity `Category` ra view.
- `Views/Categories/*.cshtml`, `wwwroot/js/categories.js`, `wwwroot/css/categories.css`: phần giao diện Category. Backend không đặt logic database trong các file này.
- `CategorySpecificationsController`, `Views/CategorySpecifications/Index.cshtml`, `wwwroot/js/category-specifications.js`: module riêng để cấu hình thông số kỹ thuật theo từng danh mục. Category chỉ dẫn sang module này, không tự xử lý logic gán thông số.

## 2. Đăng ký service trong Program

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICategoryAdminService, CategoryAdminService>();
builder.Services.AddScoped<ISpecificationAdminService, SpecificationAdminService>();
builder.Services.AddScoped<ICategorySpecAdminService, CategorySpecAdminService>();
builder.Services.AddScoped<IImageUploadService, CloudinaryImageUploadService>();
```

Đoạn này đăng ký các dependency cho ASP.NET Core DI container.

- `ApplicationDbContext` dùng SQL Server thông qua connection string `DefaultConnection`.
- `ICategoryAdminService` được map sang `CategoryAdminService`, nghĩa là controller chỉ cần yêu cầu interface.
- `ISpecificationAdminService` được map sang `SpecificationAdminService`, phục vụ CRUD thông số kỹ thuật toàn cục.
- `ICategorySpecAdminService` được map sang `CategorySpecAdminService`, phục vụ màn hình gán thông số vào danh mục.
- `IImageUploadService` được map sang `CloudinaryImageUploadService`, giúp module Category upload ảnh mà không cần biết chi tiết Cloudinary hoạt động như thế nào.

## 3. Controller nhận request và giữ logic mỏng

```csharp
public sealed class CategoriesController : Controller
{
    private readonly ICategoryAdminService _categoryService;

    public CategoriesController(ICategoryAdminService categoryService)
    {
        _categoryService = categoryService;
    }
}
```

Controller inject `ICategoryAdminService`. Đây là cách tách backend theo hướng sạch hơn:

- Controller không tự query database.
- Controller không tự upload ảnh.
- Controller không tự tính cây danh mục.
- Controller chỉ điều phối request và response.

## 4. Luồng danh sách Index

```csharp
public async Task<IActionResult> Index(
    string? search,
    string? status,
    int page = 1,
    CancellationToken cancellationToken = default)
{
    var viewModel = await _categoryService.GetIndexAsync(
        new CategoryIndexQuery
        {
            Search = search,
            Status = status,
            Page = page,
        },
        cancellationToken);

    return View(viewModel);
}
```

Khi admin mở trang danh sách danh mục, action `Index` nhận các tham số từ query string:

- `search`: từ khóa tìm theo tên hoặc slug.
- `status`: lọc danh mục đang bật hoặc đang tắt.
- `page`: trang hiện tại.

Controller đóng gói các tham số này vào `CategoryIndexQuery` rồi gọi `GetIndexAsync`. Sau đó controller trả `CategoryIndexViewModel` cho view.

## 5. Service query danh sách Category

```csharp
var allQuery = _db.Categories
    .Include(category => category.Parent)
    .Include(category => category.Children)
    .Include(category => category.Products)
    .AsSplitQuery()
    .AsNoTracking();
```

Ý nghĩa:

- `Include(category => category.Parent)`: lấy danh mục cha để hiển thị tên cha.
- `Include(category => category.Children)`: lấy danh mục con để biết danh mục hiện tại có bao nhiêu con.
- `Include(category => category.Products)`: lấy sản phẩm thuộc danh mục để tính số lượng sản phẩm.
- `AsSplitQuery()`: tránh query quá nặng khi include nhiều collection.
- `AsNoTracking()`: chỉ đọc dữ liệu, không cần EF Core theo dõi thay đổi nên nhẹ hơn.

```csharp
if (query.Status == "active")
{
    allQuery = allQuery.Where(category => category.IsActive);
}
else if (query.Status == "inactive")
{
    allQuery = allQuery.Where(category => !category.IsActive);
}

if (!string.IsNullOrWhiteSpace(query.Search))
{
    var search = query.Search.Trim();
    allQuery = allQuery.Where(category =>
        category.Name.Contains(search) || category.Slug.Contains(search));
}
```

Đoạn này xử lý filter:

- Nếu status là `active`, chỉ lấy danh mục đang bật.
- Nếu status là `inactive`, chỉ lấy danh mục đang tắt.
- Nếu có search, tìm theo `Name` hoặc `Slug`.

## 6. Sắp xếp theo dạng cây cha con

```csharp
var allCategories = await allQuery.ToListAsync(cancellationToken);
var ordered = BuildTreeOrder(allCategories);
var productCountByCategoryId = BuildDescendantProductCounts(allCategories);
```

Sau khi lấy danh sách từ database, service không hiển thị theo thứ tự phẳng thông thường. Nó đưa danh sách về dạng cây:

- Danh mục cha đứng trước.
- Danh mục con nằm ngay bên dưới danh mục cha.
- Mỗi dòng có `Depth` để view biết cần thụt vào bao nhiêu.

```csharp
private static IReadOnlyList<(Category Category, int Depth)> BuildTreeOrder(List<Category> categories)
{
    var result = new List<(Category Category, int Depth)>();
    var categoryIds = categories.Select(category => category.Id).ToHashSet();
    var rootCategories = categories
        .Where(category => category.ParentId is null || !categoryIds.Contains(category.ParentId.Value))
        .OrderBy(category => category.Position)
        .ThenBy(category => category.Name);

    foreach (var category in rootCategories)
    {
        AppendTree(category, depth: 0, categories, result);
    }

    return result;
}
```

Hàm này tìm các danh mục gốc, tức là danh mục không có `ParentId` hoặc `ParentId` không còn tồn tại trong tập dữ liệu hiện tại. Sau đó nó gọi `AppendTree` để đưa từng node vào kết quả.

```csharp
private static void AppendTree(
    Category category,
    int depth,
    List<Category> all,
    List<(Category Category, int Depth)> result)
{
    result.Add((category, depth));

    var children = all
        .Where(child => child.ParentId == category.Id)
        .OrderBy(child => child.Position)
        .ThenBy(child => child.Name);

    foreach (var child in children)
    {
        AppendTree(child, depth + 1, all, result);
    }
}
```

Đây là xử lý đệ quy. Mỗi khi gặp danh mục con, `depth` tăng thêm 1. Nhờ vậy view có thể phân biệt danh mục cha và danh mục con.

## 7. Tính tổng sản phẩm cho danh mục cha

```csharp
private static Dictionary<long, int> BuildDescendantProductCounts(List<Category> categories)
{
    var childrenByParentId = categories
        .Where(category => category.ParentId.HasValue)
        .GroupBy(category => category.ParentId!.Value)
        .ToDictionary(group => group.Key, group => group.ToList());

    var result = new Dictionary<long, int>();

    foreach (var category in categories)
    {
        CountProducts(category, childrenByParentId, result);
    }

    return result;
}
```

Hàm này tạo map danh mục cha sang danh sách danh mục con. Mục đích là tính sản phẩm theo cả cây danh mục, không chỉ sản phẩm trực tiếp của danh mục đó.

```csharp
private static int CountProducts(
    Category category,
    IReadOnlyDictionary<long, List<Category>> childrenByParentId,
    Dictionary<long, int> result)
{
    if (result.TryGetValue(category.Id, out var cachedCount))
    {
        return cachedCount;
    }

    var count = category.Products.Count;
    if (childrenByParentId.TryGetValue(category.Id, out var children))
    {
        foreach (var child in children)
        {
            count += CountProducts(child, childrenByParentId, result);
        }
    }

    result[category.Id] = count;
    return count;
}
```

Ví dụ:

- Danh mục cha `Điện tử` không có sản phẩm trực tiếp.
- Danh mục con `Điện thoại` có 2 sản phẩm.
- Danh mục con `Laptop` có 1 sản phẩm.

Kết quả hiển thị cho `Điện tử` sẽ là 3 sản phẩm, vì nó cộng cả sản phẩm của danh mục con.

## 8. Mapping dữ liệu sang ViewModel

```csharp
var rows = pageItems.Select(entry => new CategoryRowViewModel
{
    Id = entry.Category.Id,
    Name = entry.Category.Name,
    Slug = entry.Category.Slug,
    ImagePath = entry.Category.ImagePath,
    ParentId = entry.Category.ParentId,
    ParentName = entry.Category.Parent?.Name ?? string.Empty,
    Position = entry.Category.Position,
    IsActive = entry.Category.IsActive,
    ProductCount = productCountByCategoryId.GetValueOrDefault(entry.Category.Id),
    ChildCount = entry.Category.Children.Count,
    Depth = entry.Depth,
    CreatedAt = entry.Category.CreatedAt,
}).ToList();
```

Service không trả entity `Category` trực tiếp ra view. Nó chuyển dữ liệu sang `CategoryRowViewModel`.

Lợi ích:

- View chỉ nhận đúng dữ liệu cần hiển thị.
- Không để view phụ thuộc vào entity database.
- Dễ thay đổi giao diện mà không làm vỡ model database.
- Dễ thêm các field tính toán như `ProductCount`, `ChildCount`, `Depth`.

## 9. ViewModel của trang danh sách

```csharp
public sealed class CategoryIndexViewModel
{
    public List<CategoryRowViewModel> Categories { get; set; } = new();

    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }

    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int TotalProductCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}
```

`CategoryIndexViewModel` chứa toàn bộ dữ liệu cho trang index:

- Danh sách dòng cần hiển thị.
- Điều kiện lọc hiện tại.
- Thông tin phân trang.
- Thống kê: tổng danh mục, đang bật, đang tắt, tổng sản phẩm.

Các property như `TotalPages`, `HasPrev`, `HasNext` là dữ liệu phục vụ giao diện phân trang.

## 10. Luồng tạo Category

```csharp
public async Task<IActionResult> Create(CancellationToken cancellationToken)
{
    return View(await _categoryService.GetCreateFormAsync(cancellationToken));
}
```

Action GET trả form tạo mới. Service chuẩn bị dữ liệu mặc định như `IsActive = true`, `Position`, và danh sách danh mục cha.

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(
    CategoryFormViewModel viewModel,
    CancellationToken cancellationToken)
{
    if (!ModelState.IsValid)
    {
        return View(await _categoryService.PrepareFormAsync(viewModel, excludeId: null, cancellationToken));
    }

    var result = await _categoryService.CreateAsync(viewModel, cancellationToken);
    if (!result.Succeeded)
    {
        AddValidationErrors(result.Errors);
        return View(result.Form);
    }

    TempData["Success"] = result.Message;
    return RedirectToAction(nameof(Index));
}
```

Action POST xử lý theo thứ tự:

1. ASP.NET Core validate DataAnnotations trong `CategoryFormViewModel`.
2. Nếu form không hợp lệ, service chuẩn bị lại danh sách danh mục cha rồi trả view.
3. Nếu form hợp lệ, gọi `CreateAsync`.
4. Nếu service báo lỗi nghiệp vụ, đưa lỗi vào `ModelState`.
5. Nếu thành công, redirect về Index.

## 11. Form ViewModel và validate dữ liệu nhập

```csharp
public sealed class CategoryFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Slug tối đa 255 ký tự.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
    public string Slug { get; set; } = string.Empty;

    public long? ParentId { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public IFormFile? ImageFile { get; set; }
    public int Position { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CategorySelectItem> ParentOptions { get; set; } = new();
}
```

Đây là model dành cho form tạo/sửa:

- `Name`: bắt buộc.
- `Slug`: có thể để trống, service sẽ tự sinh từ tên.
- `ParentId`: danh mục cha, cho phép null nếu là danh mục gốc.
- `ImagePath`: link ảnh đang lưu trong database.
- `ImageFile`: file upload từ form.
- `Position`: thứ tự sắp xếp.
- `IsActive`: trạng thái bật/tắt.
- `ParentOptions`: danh sách lựa chọn danh mục cha cho dropdown.

## 12. Service tạo mới Category

```csharp
public async Task<CategorySaveResult> CreateAsync(
    CategoryFormViewModel form,
    CancellationToken cancellationToken = default)
{
    NormalizeForm(form);

    var errors = await ValidateFormAsync(form, existingId: null, cancellationToken);
    if (errors.Count > 0)
    {
        return CategorySaveResult.Failed(
            await PrepareFormAsync(form, excludeId: null, cancellationToken),
            errors);
    }

    var uploadError = await UploadImageIfNeededAsync(form, cancellationToken);
    if (uploadError is not null)
    {
        return CategorySaveResult.Failed(
            await PrepareFormAsync(form, excludeId: null, cancellationToken),
            new[] { uploadError });
    }

    var entity = new Category
    {
        Name = form.Name,
        Slug = form.Slug,
        ParentId = form.ParentId,
        Description = form.Description,
        ImagePath = form.ImagePath,
        Position = form.Position,
        IsActive = form.IsActive,
        CreatedAt = DateTime.UtcNow,
    };

    _db.Categories.Add(entity);
    await _db.SaveChangesAsync(cancellationToken);

    form.Id = entity.Id;
    return CategorySaveResult.Success(form, $"Đã tạo danh mục \"{entity.Name}\" thành công.");
}
```

Thứ tự xử lý rất quan trọng:

1. `NormalizeForm`: chuẩn hóa tên, slug, mô tả, link ảnh.
2. `ValidateFormAsync`: kiểm tra nghiệp vụ như trùng slug, danh mục cha không hợp lệ.
3. `UploadImageIfNeededAsync`: nếu admin chọn ảnh mới thì upload lên Cloudinary.
4. Tạo entity `Category`.
5. Lưu vào database.
6. Trả `CategorySaveResult.Success`.

## 13. Normalize và sinh slug

```csharp
private static void NormalizeForm(CategoryFormViewModel form)
{
    form.Name = form.Name.Trim();
    form.Slug = string.IsNullOrWhiteSpace(form.Slug)
        ? GenerateSlug(form.Name)
        : GenerateSlug(form.Slug);
    form.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
    form.ImagePath = string.IsNullOrWhiteSpace(form.ImagePath) ? null : form.ImagePath.Trim();
}
```

`NormalizeForm` giúp dữ liệu trước khi lưu luôn sạch:

- Bỏ khoảng trắng thừa ở tên.
- Nếu không nhập slug thì tự sinh slug từ tên.
- Nếu nhập slug thì vẫn chuẩn hóa slug.
- Mô tả rỗng được chuyển thành `null`.
- Link ảnh rỗng được chuyển thành `null`.

```csharp
private static string GenerateSlug(string value)
{
    var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
    var builder = new StringBuilder(capacity: normalized.Length);

    foreach (var character in normalized)
    {
        var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
        if (unicodeCategory == UnicodeCategory.NonSpacingMark)
        {
            continue;
        }

        if (character == '\u0111')
        {
            builder.Append('d');
        }
        else if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
        {
            builder.Append(character);
        }
        else if (char.IsWhiteSpace(character) || character is '-' or '_')
        {
            builder.Append('-');
        }
    }

    return Regex.Replace(builder.ToString(), "-+", "-").Trim('-');
}
```

Hàm này chuyển chuỗi tiếng Việt thành slug an toàn:

- Chuyển chữ hoa thành chữ thường.
- Bỏ dấu tiếng Việt.
- Chuyển `đ` thành `d`.
- Chỉ giữ chữ cái, số và dấu gạch ngang.
- Gộp nhiều dấu `-` liên tiếp thành một dấu.

Ví dụ `Điện thoại cao cấp` thành `dien-thoai-cao-cap`.

## 14. Validate nghiệp vụ Category

```csharp
if (await _db.Categories.AnyAsync(category =>
        category.Slug == form.Slug && (!existingId.HasValue || category.Id != existingId.Value),
        cancellationToken))
{
    errors.Add(new CategoryValidationError(nameof(form.Slug), "Slug đã tồn tại, hãy dùng slug khác."));
}
```

Slug phải là duy nhất. Khi tạo mới, chỉ cần kiểm tra có category nào dùng slug đó chưa. Khi cập nhật, bỏ qua chính bản ghi đang sửa bằng điều kiện `category.Id != existingId.Value`.

```csharp
if (form.ParentId.HasValue)
{
    if (existingId.HasValue && form.ParentId.Value == existingId.Value)
    {
        errors.Add(new CategoryValidationError(nameof(form.ParentId),
            "Không thể chọn chính mình làm danh mục cha."));
    }
    else if (!await _db.Categories.AnyAsync(category => category.Id == form.ParentId.Value, cancellationToken))
    {
        errors.Add(new CategoryValidationError(nameof(form.ParentId),
            "Danh mục cha không tồn tại."));
    }
    else if (existingId.HasValue && await IsDescendantAsync(existingId.Value, form.ParentId.Value, cancellationToken))
    {
        errors.Add(new CategoryValidationError(nameof(form.ParentId),
            "Không thể chọn danh mục con làm danh mục cha."));
    }
}
```

Đây là phần quan trọng của category vì category có quan hệ cha con:

- Không được chọn chính nó làm cha.
- Không được chọn một id không tồn tại làm cha.
- Không được chọn danh mục con làm cha, vì sẽ tạo vòng lặp trong cây.

## 15. Upload ảnh lên Cloudinary

```csharp
private async Task<CategoryValidationError?> UploadImageIfNeededAsync(
    CategoryFormViewModel form,
    CancellationToken cancellationToken)
{
    if (form.ImageFile is null || form.ImageFile.Length <= 0)
    {
        return null;
    }

    var uploadResult = await _imageUploadService.UploadAsync(
        form.ImageFile,
        CategoryImageFolder,
        cancellationToken);

    if (!uploadResult.Succeeded)
    {
        return new CategoryValidationError(
            nameof(form.ImageFile),
            uploadResult.ErrorMessage ?? "Không thể tải ảnh lên Cloudinary.");
    }

    form.ImagePath = uploadResult.SecureUrl;
    return null;
}
```

Backend không lưu file ảnh trực tiếp vào SQL Server. Luồng đúng hiện tại là:

1. Admin chọn file ảnh.
2. Service gọi `IImageUploadService`.
3. `CloudinaryImageUploadService` upload ảnh lên Cloudinary.
4. Cloudinary trả về `SecureUrl`.
5. SQL Server chỉ lưu link ảnh trong `Category.ImagePath`.

Cách này giúp database nhẹ hơn và phù hợp khi sau này giao diện cần hiển thị ảnh từ CDN.

## 16. Luồng cập nhật Category

```csharp
public async Task<CategorySaveResult> UpdateAsync(
    long id,
    CategoryFormViewModel form,
    CancellationToken cancellationToken = default)
{
    NormalizeForm(form);

    var entity = await _db.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
    if (entity is null)
    {
        return CategorySaveResult.Failed(
            await PrepareFormAsync(form, excludeId: id, cancellationToken),
            new[] { new CategoryValidationError(string.Empty, "Không tìm thấy danh mục.") });
    }

    var errors = await ValidateFormAsync(form, existingId: id, cancellationToken);
    if (errors.Count > 0)
    {
        return CategorySaveResult.Failed(
            await PrepareFormAsync(form, excludeId: id, cancellationToken),
            errors);
    }

    var uploadError = await UploadImageIfNeededAsync(form, cancellationToken);
    if (uploadError is not null)
    {
        return CategorySaveResult.Failed(
            await PrepareFormAsync(form, excludeId: id, cancellationToken),
            new[] { uploadError });
    }

    entity.Name = form.Name;
    entity.Slug = form.Slug;
    entity.ParentId = form.ParentId;
    entity.Description = form.Description;
    entity.ImagePath = form.ImagePath;
    entity.Position = form.Position;
    entity.IsActive = form.IsActive;
    entity.UpdatedAt = DateTime.UtcNow;

    await _db.SaveChangesAsync(cancellationToken);

    return CategorySaveResult.Success(form, $"Đã cập nhật danh mục \"{entity.Name}\" thành công.");
}
```

Update giống create ở các bước chuẩn hóa, validate và upload. Khác biệt là update cần tìm entity cũ trước, sau đó gán lại từng field được phép chỉnh sửa. `UpdatedAt` được set để biết lần cuối bản ghi thay đổi.

## 17. Luồng xóa Category

```csharp
var entity = await _db.Categories
    .Include(category => category.Children)
    .Include(category => category.Products)
    .FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
```

Khi xóa, service lấy thêm `Children` và `Products` để kiểm tra ràng buộc nghiệp vụ.

```csharp
if (entity.Children.Count > 0)
{
    return CategoryDeleteResult.Failed(
        $"Không thể xoá \"{entity.Name}\" vì có {entity.Children.Count} danh mục con. Hãy xoá hoặc chuyển danh mục con trước.");
}

if (entity.Products.Count > 0)
{
    return CategoryDeleteResult.Failed(
        $"Không thể xoá \"{entity.Name}\" vì có {entity.Products.Count} sản phẩm đang thuộc danh mục này.");
}
```

Không cho xóa category nếu:

- Nó đang có danh mục con.
- Nó đang có sản phẩm.

Lý do là để tránh dữ liệu mồ côi. Ví dụ nếu xóa `Điện tử` khi còn `Điện thoại` bên dưới, cây danh mục sẽ bị hỏng.

## 18. Bật/tắt trạng thái Category

```csharp
public async Task<CategoryToggleResult?> ToggleActiveAsync(long id, CancellationToken cancellationToken = default)
{
    var entity = await _db.Categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
    if (entity is null)
    {
        return null;
    }

    entity.IsActive = !entity.IsActive;
    entity.UpdatedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync(cancellationToken);

    return new CategoryToggleResult(entity.IsActive);
}
```

Toggle chỉ đảo giá trị `IsActive`:

- Đang bật thì chuyển sang tắt.
- Đang tắt thì chuyển sang bật.

Controller trả JSON:

```csharp
return result is null ? NotFound() : Ok(new { isActive = result.IsActive });
```

Phần JavaScript ở frontend gọi endpoint này. Sau khi toggle thành công, trang index reload lại để thống kê bật/tắt hiển thị đúng.

## 19. Các object Result

```csharp
public sealed record CategoryValidationError(string FieldName, string Message);
```

`CategoryValidationError` biểu diễn lỗi validate nghiệp vụ. `FieldName` cho biết lỗi thuộc field nào, `Message` là nội dung hiển thị.

```csharp
public sealed class CategorySaveResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public CategoryFormViewModel Form { get; init; } = new();
    public IReadOnlyCollection<CategoryValidationError> Errors { get; init; } = Array.Empty<CategoryValidationError>();
}
```

`CategorySaveResult` dùng cho create và update. Nó giúp service nói rõ:

- Lưu thành công hay thất bại.
- Nếu thành công thì message là gì.
- Nếu thất bại thì form hiện tại và danh sách lỗi là gì.

```csharp
public sealed class CategoryDeleteResult
{
    public bool Found { get; init; }
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
}
```

`CategoryDeleteResult` dùng cho delete. Nó tách rõ 2 tình huống:

- Không tìm thấy bản ghi.
- Tìm thấy nhưng không cho xóa vì vi phạm nghiệp vụ.

## 20. Ranh giới frontend và backend

Backend của Category chịu trách nhiệm:

- Query database.
- Validate nghiệp vụ.
- Upload ảnh thông qua service.
- Tạo/sửa/xóa dữ liệu.
- Bật/tắt trạng thái.
- Trả ViewModel hoặc Result.

Frontend của Category chịu trách nhiệm:

- Hiển thị bảng danh mục.
- Hiển thị form.
- Preview ảnh trước khi upload.
- Gọi endpoint bật/tắt trạng thái.
- Reload trang sau khi toggle để thống kê cập nhật.

Điểm quan trọng là frontend không tự xử lý nghiệp vụ database. Các ràng buộc như trùng slug, không chọn danh mục con làm cha, không xóa danh mục đang có sản phẩm đều nằm ở service backend.

## 21. Cập nhật theo project hiện tại

Ở project hiện tại, Category không chỉ có CRUD danh mục mà còn là điểm vào để cấu hình thông số kỹ thuật theo từng danh mục.

Trong `Views/Categories/Index.cshtml`, mỗi dòng danh mục có nút đi sang màn hình cấu hình thông số:

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

Ý nghĩa:

- Category vẫn chỉ quản lý danh mục.
- Việc gán thông số vào danh mục được chuyển sang module riêng `CategorySpecification`.
- URL nhận `categoryId` để biết đang cấu hình thông số cho danh mục nào.

Frontend của Category hiện nằm ở:

```text
wwwroot/js/categories.js
wwwroot/css/categories.css
```

Trong `categories.js`, thao tác bật/tắt trạng thái gọi endpoint backend rồi reload trang:

```javascript
const response = await fetch(`/Categories/ToggleActive/${id}`, {
    method: 'POST',
    headers: {
        RequestVerificationToken: token,
        'X-Requested-With': 'XMLHttpRequest',
    },
});

if (!response.ok) {
    throw new Error('Server error');
}

await response.json();
window.location.reload();
```

Lý do reload sau khi bật/tắt:

- Cập nhật lại thống kê tổng danh mục đang bật.
- Cập nhật lại thống kê danh mục đã tắt.
- Tránh UI hiển thị lệch với dữ liệu thực tế trong database.

Hiện tại phần thông số theo danh mục đã được tách sang file riêng:

```text
Views/CategorySpecifications/Index.cshtml
wwwroot/js/category-specifications.js
Services/CategorySpecifications/CategorySpecAdminService.cs
```

Như vậy ranh giới hiện tại là:

- `CategoriesController` và `CategoryAdminService`: CRUD danh mục.
- `SpecificationsController` và `SpecificationAdminService`: CRUD thông số kỹ thuật toàn cục.
- `CategorySpecificationsController` và `CategorySpecAdminService`: gán thông số kỹ thuật vào danh mục.

Đây là cách tách hợp lý hơn so với đưa tất cả logic vào một controller lớn, vì mỗi module có nghiệp vụ riêng và dễ bảo trì hơn.
