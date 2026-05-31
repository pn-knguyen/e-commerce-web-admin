# Tài liệu backend quản lý Product

Tài liệu này giải thích toàn bộ phần code đang phục vụ màn hình quản lý sản phẩm trong admin. Module Product hiện tập trung vào **thông tin sản phẩm chính** như tên, slug, thương hiệu, danh mục, mô tả, trạng thái hiển thị và trạng thái nổi bật. Các phần chi tiết hơn như biến thể, thuộc tính biến thể, ảnh màu và thông số kỹ thuật là các bảng liên quan, được kiểm tra khi xóa sản phẩm nhưng chưa được quản lý trực tiếp trong form Product.

## 1. Các file chính

```text
Controllers/ProductsController.cs
Services/Products/IProductAdminService.cs
Services/Products/ProductAdminService.cs
Services/Products/ProductServiceResults.cs
ViewModels/Products/ProductViewModels.cs
Models/Entities/CatalogEntities.cs
Data/ApplicationDbContext.cs
Data/Seed/EcommerceSeedData.cs
Views/Products/Index.cshtml
Views/Products/Create.cshtml
Views/Products/Edit.cshtml
Views/Products/_Form.cshtml
wwwroot/js/products.js
wwwroot/css/products.css
```

Ý nghĩa từng nhóm:

- `Controllers/ProductsController.cs`: nhận request HTTP, gọi service, trả view, redirect hoặc JSON cho AJAX.
- `Services/Products/IProductAdminService.cs`: interface mô tả các nghiệp vụ Product mà controller được phép gọi.
- `Services/Products/ProductAdminService.cs`: nơi xử lý query, lọc, phân trang, tạo, sửa, xóa, toggle và validation.
- `Services/Products/ProductServiceResults.cs`: các object kết quả nghiệp vụ trả từ service về controller.
- `ViewModels/Products/ProductViewModels.cs`: dữ liệu riêng cho giao diện admin, không đưa trực tiếp entity EF Core ra view.
- `Models/Entities/CatalogEntities.cs`: định nghĩa entity `Product`, `ProductVariant`, `ProductColorImage`, `ProductSpecification` và các entity catalog liên quan.
- `Data/ApplicationDbContext.cs`: khai báo `DbSet`, mapping bảng, khóa, index, precision và quan hệ.
- `Data/Seed/EcommerceSeedData.cs`: dữ liệu mẫu ban đầu cho product, variant, image, specification, cart, wishlist, order item và promotion rule.
- `Views/Products/*.cshtml`: giao diện Razor cho danh sách, tạo, sửa và form dùng chung.
- `wwwroot/js/products.js`: xử lý slug tự động, bật/tắt trạng thái, bật/tắt nổi bật, kiểm tra xóa bằng AJAX, dismiss toast.
- `wwwroot/css/products.css`: layout grid, table scroll, trạng thái, action button và responsive cho module Product.

## 2. Đăng ký service trong Program

Trong `Program.cs`:

```csharp
builder.Services.AddScoped<IProductAdminService, ProductAdminService>();
```

Controller chỉ phụ thuộc vào `IProductAdminService`, không phụ thuộc trực tiếp `ApplicationDbContext`. Cách tách này giúp:

- Controller giữ vai trò điều phối request/response.
- Service chịu trách nhiệm nghiệp vụ và database.
- View chỉ nhận dữ liệu đã được chuẩn bị sẵn qua ViewModel.

## 3. Entity Product và các quan hệ chính

File: `Models/Entities/CatalogEntities.cs`

```csharp
public class Product
{
    public long Id { get; set; }
    public long BrandId { get; set; }
    public long CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public int ViewsCount { get; set; }
    public int TotalSoldCount { get; set; }
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

Các cột chính:

- `BrandId`: FK tới `brands`.
- `CategoryId`: FK tới `categories`.
- `Name`: tên sản phẩm.
- `Slug`: định danh URL, có unique index.
- `Description`: mô tả dài, tối đa 4000 ký tự theo mapping.
- `ViewsCount`: số lượt xem.
- `TotalSoldCount`: tổng số lượng đã bán.
- `RatingAverage`, `RatingCount`: điểm đánh giá trung bình và số đánh giá.
- `IsActive`: bật/tắt hiển thị.
- `IsFeatured`: đánh dấu sản phẩm nổi bật.
- `CreatedAt`, `UpdatedAt`: thời gian tạo/cập nhật.

Navigation trong `Product`:

```csharp
public Brand? Brand { get; set; }
public Category? Category { get; set; }
public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
public ICollection<ProductColorImage> ProductColorImages { get; set; } = new List<ProductColorImage>();
public ICollection<ProductSpecification> ProductSpecifications { get; set; } = new List<ProductSpecification>();
```

Ý nghĩa:

- Mỗi sản phẩm thuộc một thương hiệu.
- Mỗi sản phẩm thuộc một danh mục.
- Một sản phẩm có nhiều biến thể.
- Một sản phẩm có nhiều ảnh theo màu.
- Một sản phẩm có nhiều thông số kỹ thuật.

## 4. Entity ProductVariant

```csharp
public class ProductVariant
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int SoldCount { get; set; }
    public int Quantity { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

`ProductVariant` là SKU/biến thể bán hàng thực tế. Ví dụ: cùng một sản phẩm iPhone có thể có nhiều biến thể theo màu hoặc dung lượng.

Navigation đáng chú ý:

- `Product`: biến thể thuộc về sản phẩm nào.
- `VariantAttributes`: các option thuộc tính của biến thể, ví dụ màu, dung lượng, size.
- `CartItems`, `Wishlists`, `OrderItems`: biến thể được dùng trong giỏ hàng, yêu thích và đơn hàng.
- `GoodReceiptItems`: biến thể được dùng trong phiếu nhập hàng.
- `GiftPromotionRules`: biến thể được dùng làm quà tặng trong khuyến mãi.

## 5. Entity ProductColorImage

```csharp
public class ProductColorImage
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Color { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int Position { get; set; }
}
```

Bảng này lưu ảnh sản phẩm theo màu. Product admin hiện chỉ kiểm tra số ảnh khi xóa, chưa có form upload ảnh trong module Product.

## 6. Entity ProductSpecification

```csharp
public class ProductSpecification
{
    public long ProductId { get; set; }
    public long SpecificationId { get; set; }
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsHighlight { get; set; }
}
```

Đây là bảng nối giữa `Product` và `Specification`.

- Khóa chính là cặp `{ ProductId, SpecificationId }`.
- `Value` là giá trị cụ thể của thông số trên sản phẩm.
- `SortOrder` dùng để sắp xếp.
- `IsHighlight` dùng để đánh dấu thông số nổi bật.

## 7. Mapping trong ApplicationDbContext

File: `Data/ApplicationDbContext.cs`

DbSet liên quan:

```csharp
public DbSet<Product> Products => Set<Product>();
public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
public DbSet<ProductColorImage> ProductColorImages => Set<ProductColorImage>();
public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
```

Mapping `Product`:

```csharp
modelBuilder.Entity<Product>(entity =>
{
    entity.ToTable("products");
    entity.HasIndex(product => product.Slug).IsUnique();
    entity.Property(product => product.Name).HasMaxLength(255).IsRequired();
    entity.Property(product => product.Description).HasMaxLength(4000);
    entity.Property(product => product.Slug).HasMaxLength(255).IsRequired();
    entity.Property(product => product.RatingAverage).HasPrecision(3, 2);
});
```

Các ràng buộc quan trọng:

- Bảng là `products`.
- `Slug` là duy nhất.
- `Name` bắt buộc, tối đa 255 ký tự.
- `Description` tối đa 4000 ký tự.
- `RatingAverage` dùng precision `(3,2)`.

Quan hệ:

```csharp
entity.HasOne(product => product.Brand)
    .WithMany(brand => brand.Products)
    .HasForeignKey(product => product.BrandId);

entity.HasOne(product => product.Category)
    .WithMany(category => category.Products)
    .HasForeignKey(product => product.CategoryId);
```

Toàn bộ foreign key trong `OnModelCreating` được set `DeleteBehavior.Restrict`, nên khi có dữ liệu liên quan, SQL Server sẽ không tự cascade delete.

## 8. Controller ProductsController

File: `Controllers/ProductsController.cs`

Controller inject service:

```csharp
private readonly IProductAdminService _productService;

public ProductsController(IProductAdminService productService)
    => _productService = productService;
```

Controller không tự query database. Nó nhận request, đóng gói input, gọi service và quyết định response.

## 9. Action Index

```csharp
public async Task<IActionResult> Index(
    string? search,
    string? status,
    string? featured,
    long? brandId,
    long? categoryId,
    int page = 1,
    CancellationToken ct = default)
```

Tham số query string:

- `search`: tìm theo tên, slug, brand name, category name.
- `status`: `active`, `inactive` hoặc rỗng.
- `featured`: `featured`, `normal` hoặc rỗng.
- `brandId`: lọc theo thương hiệu.
- `categoryId`: lọc theo danh mục, bao gồm cả danh mục con.
- `page`: trang hiện tại.

Action tạo `ProductIndexQuery`, gọi:

```csharp
_productService.GetIndexAsync(query, ct)
```

Sau đó trả `ProductIndexViewModel` cho `Views/Products/Index.cshtml`.

## 10. Action Create

GET:

```csharp
public async Task<IActionResult> Create(CancellationToken ct)
    => View(await _productService.GetCreateFormAsync(ct));
```

GET Create chuẩn bị form rỗng, trong đó service đã nạp danh sách brand/category để render select.

POST:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Create(ProductFormViewModel viewModel, CancellationToken ct)
```

Luồng xử lý:

1. Kiểm tra `ModelState`.
2. Nếu invalid, gọi `PrepareFormAsync` để nạp lại dropdown.
3. Nếu valid, gọi `CreateAsync`.
4. Nếu service trả lỗi nghiệp vụ, add lỗi vào `ModelState`.
5. Nếu thành công, set `TempData["Success"]` và redirect về `Index`.

## 11. Action Edit

GET:

```csharp
public async Task<IActionResult> Edit(long id, CancellationToken ct)
```

Service gọi `GetEditFormAsync(id)`. Nếu không tìm thấy product thì trả `NotFound()`.

POST:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(long id, ProductFormViewModel viewModel, CancellationToken ct)
```

Điểm quan trọng:

- Nếu `id != viewModel.Id` thì trả `BadRequest()`.
- Nếu `ModelState` lỗi thì nạp lại dropdown và trả view.
- Nếu update thành công thì redirect về index.

## 12. Action Delete và CheckDelete

`Delete` là POST thường:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Delete(long id, CancellationToken ct)
```

Controller gọi `DeleteAsync`. Service sẽ tự gọi `CheckDeleteAsync` trước khi xóa thật.

`CheckDelete` là POST dùng cho AJAX:

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> CheckDelete(long id, CancellationToken ct)
```

Response JSON:

```json
{
  "canDelete": true,
  "message": "...",
  "blockers": []
}
```

Frontend gọi action này trước khi hiện `confirm`. Nếu sản phẩm còn dữ liệu liên quan thì frontend hiển thị alert và không submit form xóa.

## 13. Action ToggleActive và ToggleFeatured

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleActive(long id, CancellationToken ct)
```

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleFeatured(long id, CancellationToken ct)
```

Hai action này phục vụ AJAX trong trang index.

- `ToggleActive`: đảo `IsActive`.
- `ToggleFeatured`: đảo `IsFeatured`.
- Nếu không tìm thấy product thì trả `NotFound()`.
- Nếu thành công thì trả JSON chứa trạng thái mới.

## 14. Interface IProductAdminService

File: `Services/Products/IProductAdminService.cs`

```csharp
public interface IProductAdminService
{
    Task<ProductIndexViewModel> GetIndexAsync(ProductIndexQuery query, CancellationToken ct = default);
    Task<ProductFormViewModel> GetCreateFormAsync(CancellationToken ct = default);
    Task<ProductFormViewModel?> GetEditFormAsync(long id, CancellationToken ct = default);
    Task<ProductFormViewModel> PrepareFormAsync(ProductFormViewModel form, CancellationToken ct = default);
    Task<ProductSaveResult> CreateAsync(ProductFormViewModel form, CancellationToken ct = default);
    Task<ProductSaveResult> UpdateAsync(long id, ProductFormViewModel form, CancellationToken ct = default);
    Task<ProductDeleteCheckResult> CheckDeleteAsync(long id, CancellationToken ct = default);
    Task<ProductDeleteResult> DeleteAsync(long id, CancellationToken ct = default);
    Task<ProductToggleResult?> ToggleActiveAsync(long id, CancellationToken ct = default);
    Task<ProductToggleResult?> ToggleFeaturedAsync(long id, CancellationToken ct = default);
}
```

Interface này chính là hợp đồng nghiệp vụ của module Product. Controller không cần biết EF Core query ra sao, chỉ cần gọi đúng method.

## 15. ProductAdminService - cấu hình chung

File: `Services/Products/ProductAdminService.cs`

```csharp
private const int DefaultPageSize = 30;
private readonly ApplicationDbContext _db;
```

- `DefaultPageSize = 30`: mỗi trang index hiển thị 30 sản phẩm.
- `_db`: EF Core DbContext để query và lưu dữ liệu.

Service có private class `ProductIndexItem` dùng làm DTO nội bộ khi projection từ database:

```csharp
private sealed class ProductIndexItem
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public int VariantCount { get; init; }
    public int ViewsCount { get; init; }
    public int TotalSoldCount { get; init; }
    public decimal RatingAverage { get; init; }
    public int RatingCount { get; init; }
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

DTO này giúp query chỉ lấy đúng các trường cần hiển thị, không load toàn bộ entity và collection.

## 16. GetIndexAsync - lọc, thống kê, phân trang

Khởi đầu:

```csharp
var page = Math.Max(1, query.Page);
var dbQuery = _db.Products.AsNoTracking();
```

- Trang nhỏ hơn 1 được đưa về 1.
- `AsNoTracking()` dùng cho màn hình đọc dữ liệu, nhẹ hơn tracking entity.

Lọc trạng thái:

```csharp
if (query.Status == "active")
    dbQuery = dbQuery.Where(product => product.IsActive);
else if (query.Status == "inactive")
    dbQuery = dbQuery.Where(product => !product.IsActive);
```

Lọc nổi bật:

```csharp
if (query.Featured == "featured")
    dbQuery = dbQuery.Where(product => product.IsFeatured);
else if (query.Featured == "normal")
    dbQuery = dbQuery.Where(product => !product.IsFeatured);
```

Lọc brand:

```csharp
if (query.BrandId.HasValue)
    dbQuery = dbQuery.Where(product => product.BrandId == query.BrandId.Value);
```

Lọc category:

```csharp
var categoryIds = await GetCategoryAndDescendantIdsAsync(query.CategoryId.Value, ct);
dbQuery = categoryIds.Count == 0
    ? dbQuery.Where(product => false)
    : dbQuery.Where(product => categoryIds.Contains(product.CategoryId));
```

Nếu lọc theo danh mục cha, service lấy cả danh mục cha và toàn bộ danh mục con. Nhờ vậy admin chọn "Điện tử" vẫn thấy sản phẩm thuộc "Điện thoại", "Laptop" nếu các danh mục đó là con.

Tìm kiếm:

```csharp
dbQuery = dbQuery.Where(product =>
    product.Name.Contains(term) ||
    product.Slug.Contains(term) ||
    product.Brand!.Name.Contains(term) ||
    product.Category!.Name.Contains(term));
```

Search hiện tìm theo:

- Tên sản phẩm.
- Slug sản phẩm.
- Tên thương hiệu.
- Tên danh mục.

Thống kê:

```csharp
var totalCount = await dbQuery.CountAsync(ct);
var activeCount = await dbQuery.CountAsync(product => product.IsActive, ct);
var inactiveCount = await dbQuery.CountAsync(product => !product.IsActive, ct);
var featuredCount = await dbQuery.CountAsync(product => product.IsFeatured, ct);
```

Các con số này được tính trên `dbQuery` sau khi đã áp filter. Nghĩa là khi đang lọc theo brand/category/search, các thẻ thống kê phản ánh tập dữ liệu đang lọc, không phải toàn bộ hệ thống.

Query danh sách:

```csharp
var items = await dbQuery
    .OrderByDescending(product => product.CreatedAt)
    .ThenBy(product => product.Name)
    .Skip((page - 1) * DefaultPageSize)
    .Take(DefaultPageSize)
    .Select(product => new ProductIndexItem { ... })
    .ToListAsync(ct);
```

Điểm tốt:

- Có phân trang bằng `Skip`/`Take`.
- Có projection sang DTO, tránh load dư entity.
- Lấy `VariantCount` bằng count collection ngay trong query.

## 17. GetCreateFormAsync và GetEditFormAsync

Create:

```csharp
return await PrepareFormAsync(new ProductFormViewModel { IsActive = true }, ct);
```

Sản phẩm mới mặc định `IsActive = true`.

Edit:

```csharp
var entity = await _db.Products
    .AsNoTracking()
    .FirstOrDefaultAsync(product => product.Id == id, ct);
```

Edit chỉ load các trường cần cho form:

- `Id`
- `Name`
- `Slug`
- `BrandId`
- `CategoryId`
- `Description`
- `IsActive`
- `IsFeatured`

Sau đó gọi `PrepareFormAsync` để nạp dropdown brand/category.

## 18. PrepareFormAsync

```csharp
form.BrandOptions = await BuildBrandOptionsAsync(ct);
form.CategoryOptions = await BuildCategoryOptionsAsync(ct);
return form;
```

Method này được gọi trong cả GET Create, GET Edit và khi POST bị lỗi. Đây là điểm quan trọng vì nếu validation lỗi mà không nạp lại options thì view select sẽ thiếu dữ liệu.

## 19. CreateAsync

Luồng:

```csharp
NormalizeForm(form);
var errors = await ValidateFormAsync(form, existingId: null, ct);
```

Trước khi lưu, service chuẩn hóa:

- Trim tên.
- Sinh slug nếu để trống.
- Chuẩn hóa slug nếu admin nhập thủ công.
- Trim description hoặc đưa về null.

Nếu không có lỗi, tạo entity:

```csharp
var entity = new Product
{
    BrandId = form.BrandId!.Value,
    CategoryId = form.CategoryId!.Value,
    Name = form.Name,
    Slug = form.Slug!,
    Description = form.Description,
    IsActive = form.IsActive,
    IsFeatured = form.IsFeatured,
    CreatedAt = DateTime.UtcNow,
};
```

Các trường chưa nhập trên form như `ViewsCount`, `TotalSoldCount`, `RatingAverage`, `RatingCount` sẽ dùng giá trị mặc định của kiểu dữ liệu.

## 20. UpdateAsync

Update cũng gọi:

```csharp
NormalizeForm(form);
ValidateFormAsync(form, existingId: id, ct);
```

Khác với create, validate slug khi update bỏ qua chính sản phẩm hiện tại:

```csharp
product => product.Slug == form.Slug && product.Id != existingId.Value
```

Sau đó cập nhật entity:

```csharp
entity.BrandId = form.BrandId!.Value;
entity.CategoryId = form.CategoryId!.Value;
entity.Name = form.Name;
entity.Slug = form.Slug!;
entity.Description = form.Description;
entity.IsActive = form.IsActive;
entity.IsFeatured = form.IsFeatured;
entity.UpdatedAt = DateTime.UtcNow;
```

## 21. ValidateFormAsync

Validation nghiệp vụ trong service gồm:

1. Kiểm tra slug trùng.
2. Kiểm tra brand có được chọn không.
3. Kiểm tra brand có tồn tại trong DB không.
4. Kiểm tra category có được chọn không.
5. Kiểm tra category có tồn tại trong DB không.
6. Kiểm tra category có phải danh mục lá không.

Rule danh mục lá:

```csharp
else if (category.ChildCount > 0)
{
    errors.Add(new ProductValidationError(
        nameof(form.CategoryId),
        "Chỉ chọn danh mục con cuối cùng, không chọn danh mục cha còn chứa danh mục con."));
}
```

Nghĩa là sản phẩm không được gán vào danh mục cha nếu danh mục đó còn danh mục con. Đây là rule hợp lý để dữ liệu sản phẩm nằm ở cấp phân loại cụ thể nhất.

## 22. CheckDeleteAsync và DeleteAsync

`CheckDeleteAsync` lấy số lượng dữ liệu phụ thuộc:

```csharp
VariantCount = item.ProductVariants.Count,
SpecificationCount = item.ProductSpecifications.Count,
ImageCount = item.ProductColorImages.Count,
```

`BuildDeleteBlockers` tạo danh sách lý do không được xóa:

- Còn biến thể.
- Còn thông số kỹ thuật.
- Còn ảnh sản phẩm.

`DeleteAsync` luôn gọi `CheckDeleteAsync` trước:

```csharp
var deleteCheck = await CheckDeleteAsync(id, ct);
if (!deleteCheck.CanDelete)
    return ProductDeleteResult.Failed(deleteCheck.Message);
```

Chỉ khi không còn blocker thì service mới gọi:

```csharp
_db.Products.Remove(entity);
await _db.SaveChangesAsync(ct);
```

Lưu ý khi kiểm tra code: `VoucherTarget` và `PromotionTarget` là polymorphic target, không có FK trực tiếp tới `Product`. Nếu sau này có sản phẩm không còn variant/spec/image nhưng vẫn đang được tham chiếu bởi target kiểu `Product`, code hiện tại chưa chặn trường hợp đó.

## 23. ToggleActiveAsync và ToggleFeaturedAsync

Toggle active:

```csharp
entity.IsActive = !entity.IsActive;
entity.UpdatedAt = DateTime.UtcNow;
```

Toggle featured:

```csharp
entity.IsFeatured = !entity.IsFeatured;
entity.UpdatedAt = DateTime.UtcNow;
```

Hai method này trả `ProductToggleResult` để controller trả JSON cho frontend.

## 24. BuildBrandOptionsAsync

```csharp
return await _db.Brands
    .AsNoTracking()
    .OrderBy(brand => brand.Name)
    .Select(brand => new ProductSelectItem
    {
        Id = brand.Id,
        Label = brand.Name,
        IsActive = brand.IsActive,
    })
    .ToListAsync(ct);
```

Brand options gồm cả brand đang tắt. View sẽ hiển thị hậu tố `(đang tắt)` để admin biết trạng thái.

## 25. BuildCategoryOptionsAsync

Service lấy toàn bộ category rồi dựng cây:

```csharp
AppendCategoryOptions(categories, parentId: null, depth: 0, result);
```

`AppendCategoryOptions` thêm prefix theo độ sâu:

```csharp
var prefix = depth == 0 ? string.Empty : $"{new string('-', depth * 2)} ";
```

View dùng `HasChildren` để disable danh mục cha:

```cshtml
disabled="@category.HasChildren"
```

Như vậy rule "chỉ chọn danh mục lá" được hỗ trợ ở cả UI và backend.

## 26. GenerateSlug và NormalizeForm

Server-side slug generator:

```csharp
private static string GenerateSlug(string value)
```

Các bước:

1. Trim và lowercase.
2. Normalize Unicode dạng FormD.
3. Bỏ dấu tiếng Việt bằng cách bỏ `NonSpacingMark`.
4. Chuyển `đ` thành `d`.
5. Giữ chữ `a-z`, số `0-9`.
6. Chuyển khoảng trắng, `-`, `_` thành `-`.
7. Gộp nhiều dấu `-` liên tiếp.
8. Trim dấu `-` ở đầu/cuối.

Frontend cũng có hàm `toSlug` tương tự trong `wwwroot/js/products.js`, nhưng server vẫn là nguồn chuẩn cuối cùng vì POST luôn đi qua `NormalizeForm`.

## 27. ProductServiceResults

File: `Services/Products/ProductServiceResults.cs`

`ProductValidationError`:

```csharp
public sealed record ProductValidationError(string FieldName, string Message);
```

Dùng để service trả lỗi theo từng field. Controller đưa lỗi này vào `ModelState`.

`ProductSaveResult`:

- `Succeeded`: create/update thành công hay thất bại.
- `Message`: thông báo thành công.
- `Form`: form đã chuẩn bị lại, thường đã có dropdown.
- `Errors`: lỗi nghiệp vụ.

`ProductDeleteResult`:

- `Found`: có tìm thấy sản phẩm không.
- `Succeeded`: xóa thành công không.
- `Message`: thông báo cho `TempData`.

`ProductDeleteCheckResult`:

- `Found`: có tìm thấy sản phẩm không.
- `CanDelete`: có được xóa không.
- `ProductName`: tên sản phẩm.
- `Message`: message hiển thị.
- `Blockers`: danh sách dữ liệu liên quan đang chặn xóa.

`ProductToggleResult`:

```csharp
public sealed record ProductToggleResult(bool Value);
```

Dùng chung cho active và featured toggle.

## 28. ViewModel Product

File: `ViewModels/Products/ProductViewModels.cs`

`ProductIndexQuery`:

```csharp
public sealed class ProductIndexQuery
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Featured { get; set; }
    public long? BrandId { get; set; }
    public long? CategoryId { get; set; }
    public int Page { get; set; } = 1;
}
```

Đây là object nhận filter từ controller trước khi chuyển xuống service.

`ProductIndexViewModel` chứa:

- `Products`: rows hiển thị trong bảng.
- `BrandOptions`: dropdown brand filter.
- `CategoryOptions`: dropdown category filter.
- Các filter hiện tại để giữ trạng thái UI.
- `Page`, `PageSize`, `TotalCount`.
- Các số thống kê: active, inactive, featured, variant.
- Computed properties `TotalPages`, `HasPrev`, `HasNext`.

`ProductRowViewModel` là một dòng trong table:

- Tên, slug.
- Brand/category.
- Số biến thể.
- Lượt xem.
- Đã bán.
- Rating.
- Active/featured.
- CreatedAt.

`ProductFormViewModel` dùng cho create/edit:

- `Name`: required, tối đa 255.
- `Slug`: tối đa 255, regex chỉ cho lowercase, số và dấu gạch ngang.
- `BrandId`: required và phải >= 1.
- `CategoryId`: required và phải >= 1.
- `Description`: tối đa 4000.
- `IsActive`, `IsFeatured`.
- `BrandOptions`, `CategoryOptions`.

## 29. View Index

File: `Views/Products/Index.cshtml`

View này gồm các phần:

- Header với nút "Thêm sản phẩm".
- Toast success/error từ `TempData`.
- 4 ô thống kê: tổng sản phẩm, đang bật, nổi bật, biến thể.
- Form filter GET.
- Bảng sản phẩm.
- Empty state nếu không có dữ liệu.
- Pagination.
- Script `products.js`.

Form filter gửi về `Products/Index` bằng method GET:

```cshtml
<form method="get" asp-action="Index" class="product-filter-grid">
```

Các filter trong UI khớp với `ProductIndexQuery`:

- `search`
- `brandId`
- `categoryId`
- `status`
- `featured`

Mỗi row có:

- Nút sửa.
- Form xóa POST có anti-forgery token.
- Nút toggle active.
- Nút toggle featured.

## 30. View _Form

File: `Views/Products/_Form.cshtml`

Partial form dùng chung cho Create và Edit.

Các input chính:

- `Name`: input text.
- `Slug`: input text có prefix `/products/`.
- `BrandId`: select brand.
- `CategoryId`: select category.
- `Description`: textarea.
- `IsActive`: toggle checkbox.
- `IsFeatured`: toggle checkbox.

Category select có logic disable danh mục cha:

```cshtml
disabled="@category.HasChildren"
data-has-children="@category.HasChildren.ToString().ToLower()"
```

Nếu form invalid, partial hiển thị validation summary.

## 31. View Create và Edit

`Create.cshtml`:

- Set title "Thêm sản phẩm".
- Render breadcrumb.
- Form POST về `Create`.
- Render `_Form`.
- Include `products.js`.

`Edit.cshtml`:

- Set title theo tên sản phẩm.
- Form POST về `Edit` với route id.
- Có hidden `Id`.
- Render `_Form`.
- Include `products.js`.

## 32. Frontend products.js

File: `wwwroot/js/products.js`

Khi DOM ready:

```javascript
bindSlugGenerator();
bindStatusToggles();
bindFeaturedToggles();
bindDeleteConfirmation();
bindToastDismiss();
```

### Slug generator

`toSlug(text)` chuẩn hóa tiếng Việt và loại ký tự không hợp lệ. `bindSlugGenerator` tự cập nhật slug theo tên sản phẩm cho tới khi admin tự sửa slug.

Logic:

- Nếu slug đang rỗng, khi nhập tên thì tự sinh slug.
- Nếu admin nhập slug thủ công, không auto overwrite nữa.
- Khi blur slug input, slug được chuẩn hóa lại.

### Toggle active/featured

```javascript
toggleProductState(button, 'ToggleActive')
toggleProductState(button, 'ToggleFeatured')
```

Hàm này gửi POST tới:

```text
/Products/ToggleActive/{id}
/Products/ToggleFeatured/{id}
```

Header có anti-forgery token:

```javascript
RequestVerificationToken: token
```

Sau khi update thành công, trang reload để đồng bộ lại row, badge và thống kê.

### Delete confirmation

Trước khi submit form xóa, JS gọi:

```text
/Products/CheckDelete/{id}
```

Nếu `canDelete = false`, hiển thị message từ backend. Nếu được xóa, JS mới hiện `confirm`, sau đó submit form thật.

### Toast dismiss

Toast success/error có thể đóng bằng nút `x`, hoặc tự biến mất sau 5 giây.

## 33. CSS products.css

File: `wwwroot/css/products.css`

Các nhóm style:

- `product-fade-up`, `product-anim`: animation vào trang.
- `product-filter-grid`: grid filter responsive.
- `product-table-scroll`: cho table cuộn ngang.
- `product-index-grid`: grid layout của header và row.
- `product-status-btn`: pill bật/tắt active.
- `product-feature-btn`: nút star nổi bật.
- `product-action-btn`: nút sửa/xóa.
- Media query `1280px` và `640px`: chuyển filter grid về 2 cột hoặc 1 cột.

## 34. Seed data liên quan Product

File: `Data/Seed/EcommerceSeedData.cs`

Các seed chính:

- `Product`: 5 sản phẩm mẫu.
- `ProductVariant`: 5 biến thể mẫu.
- `ProductColorImage`: ảnh màu cho từng sản phẩm.
- `Specification`: thông số gốc.
- `ProductSpecification`: giá trị thông số theo sản phẩm.
- `VariantAttribute`: thuộc tính theo biến thể.
- `CartItem`, `Wishlist`, `OrderItem`: dữ liệu user/cart/order gắn với variant.
- `PromotionRule`: có rule dùng `GiftProductVariantId`.

Seed này làm cho dữ liệu product được nối với nhiều module khác, vì vậy logic xóa Product phải cẩn thận.

## 35. Luồng nghiệp vụ tổng quát

Luồng mở danh sách:

```text
Browser -> ProductsController.Index
        -> ProductAdminService.GetIndexAsync
        -> ApplicationDbContext.Products
        -> ProductIndexViewModel
        -> Views/Products/Index.cshtml
```

Luồng tạo:

```text
GET Create -> GetCreateFormAsync -> PrepareFormAsync -> View
POST Create -> ModelState -> CreateAsync -> NormalizeForm -> ValidateFormAsync -> SaveChanges -> Redirect Index
```

Luồng sửa:

```text
GET Edit -> GetEditFormAsync -> PrepareFormAsync -> View
POST Edit -> ModelState -> UpdateAsync -> NormalizeForm -> ValidateFormAsync -> SaveChanges -> Redirect Index
```

Luồng xóa:

```text
Click Xóa -> products.js gọi CheckDelete
          -> ProductAdminService.CheckDeleteAsync
          -> nếu không có blocker, confirm
          -> form POST Delete
          -> ProductAdminService.DeleteAsync
          -> SaveChanges
```

Luồng toggle:

```text
Click Active/Featured -> products.js POST AJAX
                       -> ProductsController.Toggle...
                       -> ProductAdminService.Toggle...
                       -> SaveChanges
                       -> JSON
                       -> reload page
```

## 36. Kết quả kiểm tra nhanh

Những điểm đang ổn:

- Controller mỏng, logic nghiệp vụ nằm trong service.
- Service dùng `AsNoTracking()` cho query đọc.
- Index dùng projection thay vì load toàn bộ entity.
- Slug được chuẩn hóa ở cả frontend và backend.
- Có validation backend cho brand/category tồn tại.
- Có rule không cho chọn danh mục cha còn con.
- Có check trước khi xóa để tránh xóa product còn variant/spec/image.
- Form lỗi sẽ được nạp lại dropdown bằng `PrepareFormAsync`.
- Toggle active/featured dùng POST và anti-forgery token.

Những điểm cần lưu ý nếu mở rộng sau này:

- Product form hiện chỉ quản lý thông tin chính, chưa quản lý variant, ảnh, thông số ngay trong cùng màn hình.
- Delete check chưa kiểm tra `VoucherTarget`/`PromotionTarget` dạng polymorphic trỏ tới product.
- Các số thống kê trên index là thống kê theo tập dữ liệu đã lọc, không phải thống kê toàn hệ thống.
- JS toggle lấy anti-forgery token từ input đầu tiên trên trang; hiện trang index có token trong từng form xóa, nên hoạt động khi có row sản phẩm.
- Khi thêm màn hình ProductVariant riêng, cần giữ nhất quán với ràng buộc xóa của Product vì variant đang là blocker quan trọng nhất.
