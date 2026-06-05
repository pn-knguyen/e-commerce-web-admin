# Tài liệu backend quản lý Product

Tài liệu này mô tả phần backend đang phục vụ module quản lý sản phẩm trong admin. Trạng thái mới nhất của module Product:

- Form Product quản lý thông tin sản phẩm chính và cho phép nhập thông số kỹ thuật theo danh mục.
- Thông số kỹ thuật vẫn lưu ở cấp sản phẩm qua `product_specifications.ProductId`.
- Ảnh sản phẩm theo màu đã được chuyển sang cấp biến thể qua `product_variant_images.ProductVariantId`.
- Biến thể sản phẩm, thuộc tính biến thể, tồn kho, giỏ hàng, đơn hàng và khuyến mãi vẫn là các bảng liên quan riêng, không bị trộn vào service Product.

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
Migrations/20260605090000_MoveProductImagesToVariants.cs
Migrations/ApplicationDbContextModelSnapshot.cs
Views/Products/Index.cshtml
Views/Products/Create.cshtml
Views/Products/Edit.cshtml
Views/Products/_Form.cshtml
wwwroot/js/products.js
wwwroot/css/products.css
```

Ý nghĩa từng nhóm:

- `Controllers/ProductsController.cs`: nhận request HTTP, gọi service, trả view, redirect hoặc JSON cho AJAX.
- `Services/Products/IProductAdminService.cs`: interface nghiệp vụ để controller không phụ thuộc trực tiếp vào EF Core.
- `Services/Products/ProductAdminService.cs`: xử lý query, lọc, phân trang, tạo, sửa, xóa, toggle, validate và lưu thông số kỹ thuật.
- `Services/Products/ProductServiceResults.cs`: định nghĩa object kết quả trả từ service về controller.
- `ViewModels/Products/ProductViewModels.cs`: dữ liệu riêng cho giao diện admin, tránh đưa thẳng entity EF Core ra view.
- `Models/Entities/CatalogEntities.cs`: định nghĩa `Product`, `ProductVariant`, `ProductVariantImage`, `ProductSpecification` và các entity catalog liên quan.
- `Data/ApplicationDbContext.cs`: khai báo `DbSet`, mapping bảng, khóa, index, precision và quan hệ.
- `Data/Seed/EcommerceSeedData.cs`: seed dữ liệu mẫu cho product, variant, ảnh biến thể, thông số, giỏ hàng, đơn hàng và khuyến mãi.
- `Migrations/20260605090000_MoveProductImagesToVariants.cs`: migration đổi bảng ảnh từ `product_color_images` sang `product_variant_images`.
- `Views/Products/*.cshtml`: giao diện Razor cho danh sách, tạo, sửa và form dùng chung.
- `wwwroot/js/products.js`: validate form phía client, sinh slug, đồng bộ nhóm thông số theo danh mục, toggle trạng thái và kiểm tra xóa.
- `wwwroot/css/products.css`: style riêng cho module Product.

## 2. Đăng ký service

Trong `Program.cs`:

```csharp
builder.Services.AddScoped<IProductAdminService, ProductAdminService>();
```

Controller chỉ phụ thuộc vào `IProductAdminService`. Cách tách này giúp:

- Controller giữ vai trò điều phối request/response.
- Service chịu trách nhiệm nghiệp vụ và database.
- View chỉ nhận dữ liệu đã được chuẩn bị qua ViewModel.
- Frontend JS/CSS chỉ xử lý tương tác và hiển thị, không tự quyết định dữ liệu được lưu.

## 3. Entity Product

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

    public Brand? Brand { get; set; }
    public Category? Category { get; set; }
    public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductSpecification> ProductSpecifications { get; set; } = new List<ProductSpecification>();
}
```

Các cột chính:

- `BrandId`: khóa ngoại tới `brands`.
- `CategoryId`: khóa ngoại tới `categories`.
- `Name`: tên sản phẩm, bắt buộc.
- `Slug`: định danh URL, có unique index.
- `Description`: mô tả dài, tối đa 4000 ký tự theo mapping.
- `ViewsCount`: số lượt xem.
- `TotalSoldCount`: tổng số lượng đã bán.
- `RatingAverage`, `RatingCount`: điểm đánh giá trung bình và số đánh giá.
- `IsActive`: bật/tắt sản phẩm.
- `IsFeatured`: đánh dấu sản phẩm nổi bật.
- `CreatedAt`, `UpdatedAt`: thời gian tạo/cập nhật.

Điểm thay đổi quan trọng: `Product` không còn collection `ProductColorImages`. Ảnh đã chuyển sang đi theo `ProductVariant`.

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

    public Product? Product { get; set; }
    public ICollection<VariantAttribute> VariantAttributes { get; set; } = new List<VariantAttribute>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<GoodReceiptItem> GoodReceiptItems { get; set; } = new List<GoodReceiptItem>();
    public ICollection<PromotionRule> GiftPromotionRules { get; set; } = new List<PromotionRule>();
    public ICollection<ProductVariantImage> ProductVariantImages { get; set; } = new List<ProductVariantImage>();
}
```

`ProductVariant` là SKU/biến thể bán hàng thực tế. Ví dụ cùng một iPhone có nhiều biến thể theo màu và dung lượng.

Các navigation quan trọng:

- `Product`: biến thể thuộc sản phẩm nào.
- `VariantAttributes`: option thuộc tính của biến thể, ví dụ màu, dung lượng, size.
- `CartItems`, `Wishlists`, `OrderItems`: biến thể được dùng trong giỏ hàng, yêu thích và đơn hàng.
- `GoodReceiptItems`: biến thể được dùng trong phiếu nhập hàng.
- `GiftPromotionRules`: biến thể được dùng làm quà tặng trong khuyến mãi.
- `ProductVariantImages`: nhiều ảnh thuộc riêng biến thể đó.

## 5. Entity ProductVariantImage

```csharp
public class ProductVariantImage
{
    public long Id { get; set; }
    public long ProductVariantId { get; set; }
    public string Color { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int Position { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}
```

Đây là bảng ảnh mới thay cho `ProductColorImage`.

- Bảng vật lý là `product_variant_images`.
- Khóa ngoại là `ProductVariantId`.
- Một biến thể có thể có nhiều ảnh.
- `Color` vẫn được giữ để mô tả màu liên quan tới ảnh.
- `Position` dùng để sắp xếp thứ tự ảnh.

Ý nghĩa nghiệp vụ: khi người dùng chọn một biến thể, hệ thống có thể chỉ hiển thị ảnh của biến thể đó thay vì ảnh chung của toàn sản phẩm.

## 6. Entity ProductSpecification

```csharp
public class ProductSpecification
{
    public long ProductId { get; set; }
    public long SpecificationId { get; set; }
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsHighlight { get; set; }

    public Product? Product { get; set; }
    public Specification? Specification { get; set; }
}
```

Đây là bảng nối giữa `Product` và `Specification`.

- Khóa chính là cặp `{ ProductId, SpecificationId }`.
- `ProductId` xác nhận thông số thuộc cấp sản phẩm, không thuộc biến thể.
- `SpecificationId` trỏ tới định nghĩa thông số gốc.
- `Value` là giá trị cụ thể của thông số trên sản phẩm.
- `SortOrder` dùng để sắp xếp.
- `IsHighlight` dùng để đánh dấu thông số nổi bật.

Điểm quan trọng: vì thông số nằm ở cấp sản phẩm, admin không phải nhập lại cùng một bộ thông số cho từng biến thể.

## 7. Mapping trong ApplicationDbContext

File: `Data/ApplicationDbContext.cs`

DbSet liên quan:

```csharp
public DbSet<Product> Products => Set<Product>();
public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
public DbSet<ProductVariantImage> ProductVariantImages => Set<ProductVariantImage>();
public DbSet<Specification> Specifications => Set<Specification>();
public DbSet<CategorySpecification> CategorySpecifications => Set<CategorySpecification>();
public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
```

Mapping `ProductVariantImage`:

```csharp
modelBuilder.Entity<ProductVariantImage>(entity =>
{
    entity.ToTable("product_variant_images");
    entity.Property(image => image.Color).HasMaxLength(80).IsRequired();
    entity.Property(image => image.ImagePath).HasMaxLength(500).IsRequired();
    entity.Property(image => image.AltText).HasMaxLength(255);
    entity.HasOne(image => image.ProductVariant)
        .WithMany(variant => variant.ProductVariantImages)
        .HasForeignKey(image => image.ProductVariantId);
});
```

Mapping này đảm bảo ảnh thuộc về biến thể. Khi lấy ảnh cho một biến thể, chỉ cần query theo `ProductVariantId`.

Mapping `ProductSpecification`:

```csharp
modelBuilder.Entity<ProductSpecification>(entity =>
{
    entity.ToTable("product_specifications");
    entity.HasKey(item => new { item.ProductId, item.SpecificationId });
    entity.Property(item => item.Value).HasMaxLength(1000).IsRequired();
    entity.HasOne(item => item.Product)
        .WithMany(product => product.ProductSpecifications)
        .HasForeignKey(item => item.ProductId);
    entity.HasOne(item => item.Specification)
        .WithMany(specification => specification.ProductSpecifications)
        .HasForeignKey(item => item.SpecificationId);
});
```

Mapping này giữ thông số ở cấp sản phẩm. Khóa chính kép giúp một sản phẩm không bị trùng cùng một loại thông số.

Toàn bộ foreign key trong `OnModelCreating` đang được cấu hình `DeleteBehavior.Restrict`, nên SQL Server không tự cascade delete khi có dữ liệu liên quan.

## 8. Migration ảnh biến thể

File: `Migrations/20260605090000_MoveProductImagesToVariants.cs`

Migration này đổi mô hình ảnh:

```csharp
migrationBuilder.DropForeignKey(
    name: "FK_product_color_images_products_ProductId",
    table: "product_color_images");

migrationBuilder.RenameTable(
    name: "product_color_images",
    newName: "product_variant_images");

migrationBuilder.RenameColumn(
    name: "ProductId",
    table: "product_variant_images",
    newName: "ProductVariantId");
```

Sau khi đổi tên bảng và cột, migration chuyển dữ liệu cũ từ product sang biến thể mặc định:

```csharp
migrationBuilder.Sql("""
    UPDATE pvi
    SET ProductVariantId = mapped.Id
    FROM product_variant_images AS pvi
    CROSS APPLY (
        SELECT TOP (1) pv.Id
        FROM product_variants AS pv
        WHERE pv.ProductId = pvi.ProductVariantId
        ORDER BY CASE WHEN pv.IsDefault = CAST(1 AS bit) THEN 0 ELSE 1 END, pv.Id
    ) AS mapped;
    """);
```

Ý nghĩa:

- Trước migration, cột cũ là `ProductId`.
- Sau khi rename sang `ProductVariantId`, giá trị trong cột vẫn đang là id sản phẩm.
- Đoạn SQL tìm biến thể thuộc sản phẩm đó, ưu tiên biến thể `IsDefault = true`.
- Sau đó cập nhật `ProductVariantId` về id biến thể thật.

Cuối cùng migration tạo lại foreign key:

```csharp
migrationBuilder.AddForeignKey(
    name: "FK_product_variant_images_product_variants_ProductVariantId",
    table: "product_variant_images",
    column: "ProductVariantId",
    principalTable: "product_variants",
    principalColumn: "Id",
    onDelete: ReferentialAction.Restrict);
```

`Down` migration làm ngược lại: chuyển `ProductVariantId` về `ProductId`, rename bảng về `product_color_images`, rồi tạo lại foreign key tới `products`.

## 9. ViewModel ProductFormViewModel

File: `ViewModels/Products/ProductViewModels.cs`

```csharp
public sealed class ProductFormViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
    [StringLength(255, ErrorMessage = "Tên sản phẩm tối đa 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Slug tối đa 255 ký tự.")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug chỉ gồm chữ thường, số và dấu gạch ngang.")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu.")]
    [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn thương hiệu.")]
    public long? BrandId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn danh mục.")]
    public long? CategoryId { get; set; }

    [StringLength(4000, ErrorMessage = "Mô tả tối đa 4000 ký tự.")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }

    public List<ProductSelectItem> BrandOptions { get; set; } = [];
    public List<ProductCategorySelectItem> CategoryOptions { get; set; } = [];
    public List<ProductSpecificationInputViewModel> Specifications { get; set; } = [];
}
```

Điểm mới là `Specifications`. Danh sách này chứa toàn bộ input thông số kỹ thuật có thể hiển thị theo danh mục.

ViewModel cho từng input thông số:

```csharp
public sealed class ProductSpecificationInputViewModel
{
    public long CategoryId { get; set; }
    public long SpecificationId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? GroupName { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }

    [StringLength(1000, ErrorMessage = "Giá trị thông số tối đa 1000 ký tự.")]
    public string? Value { get; set; }

    public bool IsHighlight { get; set; }
}
```

Ý nghĩa từng field:

- `CategoryId`: thông số này thuộc cấu hình của danh mục nào.
- `SpecificationId`: id thông số gốc trong bảng `specifications`.
- `Key`: mã kỹ thuật của thông số.
- `Name`: tên hiển thị cho admin.
- `Unit`: đơn vị, ví dụ `GB`, `inch`, `Hz`.
- `GroupName`: nhóm thông số, ví dụ `Bộ xử lý & Đồ họa`, `Màn hình`.
- `IsRequired`: thông số bắt buộc theo cấu hình danh mục.
- `SortOrder`: thứ tự hiển thị và lưu.
- `Value`: giá trị admin nhập.
- `IsHighlight`: có hiển thị nổi bật hay không.

## 10. ProductAdminService - chuẩn bị form

Create form:

```csharp
public async Task<ProductFormViewModel> GetCreateFormAsync(CancellationToken ct = default)
{
    return await PrepareFormAsync(new ProductFormViewModel { IsActive = true }, ct);
}
```

Sản phẩm mới mặc định `IsActive = true`.

Edit form:

```csharp
var entity = await _db.Products
    .AsNoTracking()
    .Include(product => product.ProductSpecifications)
    .FirstOrDefaultAsync(product => product.Id == id, ct);
```

Khi sửa, service load thêm `ProductSpecifications` để fill giá trị đã lưu vào form.

Mapping thông số đang có:

```csharp
Specifications = entity.ProductSpecifications
    .Select(item => new ProductSpecificationInputViewModel
    {
        CategoryId = entity.CategoryId,
        SpecificationId = item.SpecificationId,
        Value = item.Value,
        SortOrder = item.SortOrder,
        IsHighlight = item.IsHighlight,
    })
    .ToList(),
```

`CategoryId` được lấy từ product hiện tại để service ghép đúng với cấu hình thông số của danh mục.

`PrepareFormAsync`:

```csharp
public async Task<ProductFormViewModel> PrepareFormAsync(
    ProductFormViewModel form,
    CancellationToken ct = default)
{
    form.BrandOptions = await BuildBrandOptionsAsync(ct);
    form.CategoryOptions = await BuildCategoryOptionsAsync(ct);
    form.Specifications = await BuildSpecificationInputsAsync(form.Specifications, ct);
    return form;
}
```

Method này luôn nạp lại:

- Danh sách thương hiệu.
- Danh sách danh mục.
- Danh sách thông số theo `CategorySpecifications`.

Nhờ vậy khi POST lỗi validation, form trả về vẫn đủ dữ liệu để render lại dropdown và thông số.

## 11. BuildSpecificationInputsAsync

```csharp
private async Task<List<ProductSpecificationInputViewModel>> BuildSpecificationInputsAsync(
    IEnumerable<ProductSpecificationInputViewModel> existingValues,
    CancellationToken ct)
{
    var valueMap = existingValues
        .GroupBy(item => new { item.CategoryId, item.SpecificationId })
        .ToDictionary(
            group => group.Key,
            group => group.Last());

    var categorySpecifications = await _db.CategorySpecifications
        .AsNoTracking()
        .Include(item => item.Specification)
        .OrderBy(item => item.CategoryId)
        .ThenBy(item => item.GroupName)
        .ThenBy(item => item.SortOrder)
        .ThenBy(item => item.Specification!.Name)
        .ToListAsync(ct);

    return categorySpecifications.Select(item =>
    {
        valueMap.TryGetValue(
            new { item.CategoryId, item.SpecificationId },
            out var existing);

        return new ProductSpecificationInputViewModel
        {
            CategoryId = item.CategoryId,
            SpecificationId = item.SpecificationId,
            Key = item.Specification!.Key,
            Name = item.Specification.Name,
            Unit = item.Specification.Unit,
            GroupName = item.GroupName,
            IsRequired = item.IsRequired,
            SortOrder = item.SortOrder,
            Value = string.IsNullOrWhiteSpace(existing?.Value) ? null : existing.Value.Trim(),
            IsHighlight = existing?.IsHighlight ?? false,
        };
    }).ToList();
}
```

Luồng xử lý:

1. Nhận `existingValues` từ form hoặc dữ liệu edit.
2. Tạo `valueMap` theo cặp `{ CategoryId, SpecificationId }`.
3. Query toàn bộ cấu hình thông số theo danh mục từ `CategorySpecifications`.
4. Ghép metadata từ `Specification` với giá trị đã nhập/lưu.
5. Trả về danh sách input hoàn chỉnh cho view.

Lý do backend tự rebuild metadata: không tin hoàn toàn vào hidden input từ frontend. Frontend chỉ gửi id và value, còn tên thông số, unit, required, group, sort order được lấy lại từ DB.

## 12. CreateAsync - lưu thông số kỹ thuật

```csharp
NormalizeForm(form);
form = await PrepareFormAsync(form, ct);

var errors = await ValidateFormAsync(form, existingId: null, ct);
if (errors.Count > 0)
{
    return ProductSaveResult.Failed(form, errors);
}
```

Trước khi validate, service gọi `PrepareFormAsync` để đảm bảo danh sách thông số có đủ metadata từ DB. Đây là điểm quan trọng vì required/spec group không được quyết định bởi client.

Sau khi tạo entity product:

```csharp
foreach (var specification in GetSelectedSpecificationInputs(form))
{
    entity.ProductSpecifications.Add(new ProductSpecification
    {
        SpecificationId = specification.SpecificationId,
        Value = specification.Value!,
        SortOrder = specification.SortOrder,
        IsHighlight = specification.IsHighlight,
    });
}
```

Chỉ những thông số thuộc danh mục đang chọn và có `Value` mới được lưu. Thông số trống không được insert, trừ trường bắt buộc sẽ bị validate chặn trước đó.

## 13. UpdateAsync - cập nhật thông số kỹ thuật

Khi cập nhật, service load product kèm thông số hiện có:

```csharp
var entity = await _db.Products
    .Include(product => product.ProductSpecifications)
    .FirstOrDefaultAsync(product => product.Id == id, ct);
```

Sau khi validate product chính:

```csharp
entity.BrandId = form.BrandId!.Value;
entity.CategoryId = form.CategoryId!.Value;
entity.Name = form.Name;
entity.Slug = form.Slug!;
entity.Description = form.Description;
entity.IsActive = form.IsActive;
entity.IsFeatured = form.IsFeatured;
entity.UpdatedAt = DateTime.UtcNow;
ApplyProductSpecifications(entity, GetSelectedSpecificationInputs(form));
```

`ApplyProductSpecifications` chịu trách nhiệm đồng bộ collection `ProductSpecifications`.

## 14. GetSelectedSpecificationInputs

```csharp
private static List<ProductSpecificationInputViewModel> GetSelectedSpecificationInputs(ProductFormViewModel form)
{
    if (!form.CategoryId.HasValue)
    {
        return [];
    }

    return form.Specifications
        .Where(item => item.CategoryId == form.CategoryId.Value)
        .Where(item => !string.IsNullOrWhiteSpace(item.Value))
        .Select(item =>
        {
            item.Value = item.Value!.Trim();
            return item;
        })
        .ToList();
}
```

Method này lọc thông số theo đúng danh mục đang chọn. Nếu admin đổi danh mục, các thông số của danh mục cũ không được giữ lại trong danh sách lưu.

## 15. ApplyProductSpecifications

```csharp
private void ApplyProductSpecifications(
    Product product,
    IReadOnlyCollection<ProductSpecificationInputViewModel> selectedSpecifications)
{
    var selectedMap = selectedSpecifications.ToDictionary(item => item.SpecificationId);
    var existingItems = product.ProductSpecifications.ToList();

    foreach (var existing in existingItems)
    {
        if (!selectedMap.TryGetValue(existing.SpecificationId, out var selected))
        {
            _db.ProductSpecifications.Remove(existing);
            product.ProductSpecifications.Remove(existing);
            continue;
        }

        existing.Value = selected.Value!;
        existing.SortOrder = selected.SortOrder;
        existing.IsHighlight = selected.IsHighlight;
    }

    var existingIds = existingItems.Select(item => item.SpecificationId).ToHashSet();
    foreach (var selected in selectedSpecifications.Where(item => !existingIds.Contains(item.SpecificationId)))
    {
        product.ProductSpecifications.Add(new ProductSpecification
        {
            ProductId = product.Id,
            SpecificationId = selected.SpecificationId,
            Value = selected.Value!,
            SortOrder = selected.SortOrder,
            IsHighlight = selected.IsHighlight,
        });
    }
}
```

Luồng đồng bộ:

- Nếu thông số cũ không còn trong form hiện tại, remove khỏi DbContext và collection.
- Nếu thông số đã tồn tại, cập nhật `Value`, `SortOrder`, `IsHighlight`.
- Nếu thông số mới có value, thêm mới vào collection.

Việc gọi `_db.ProductSpecifications.Remove(existing)` là cần thiết vì delete behavior đang là `Restrict`; service chủ động xóa row nối thay vì trông chờ cascade.

## 16. ValidateFormAsync

Validation nghiệp vụ trong service gồm:

1. Kiểm tra slug trùng.
2. Kiểm tra brand đã chọn chưa.
3. Kiểm tra brand có tồn tại trong DB không.
4. Kiểm tra category đã chọn chưa.
5. Kiểm tra category có tồn tại trong DB không.
6. Kiểm tra category có phải danh mục lá không.
7. Kiểm tra các thông số bắt buộc của danh mục lá.

Rule danh mục lá:

```csharp
else if (category.ChildCount > 0)
{
    errors.Add(new ProductValidationError(
        nameof(form.CategoryId),
        "Chỉ chọn danh mục con cuối cùng, không chọn danh mục cha còn chứa danh mục con."));
}
```

Rule thông số bắt buộc:

```csharp
if (category is not null && category.ChildCount == 0)
{
    foreach (var item in form.Specifications.Select((specification, index) => new { specification, index }))
    {
        if (item.specification.CategoryId != form.CategoryId.Value || !item.specification.IsRequired)
        {
            continue;
        }

        if (string.IsNullOrWhiteSpace(item.specification.Value))
        {
            errors.Add(new ProductValidationError(
                $"{nameof(form.Specifications)}[{item.index}].{nameof(ProductSpecificationInputViewModel.Value)}",
                $"Vui lòng nhập {item.specification.Name}."));
        }
    }
}
```

Field name dạng `Specifications[index].Value` giúp controller add lỗi vào đúng input trong form Razor.

## 17. NormalizeForm

```csharp
private static void NormalizeForm(ProductFormViewModel form)
{
    form.Name = form.Name.Trim();
    form.Slug = string.IsNullOrWhiteSpace(form.Slug)
        ? GenerateSlug(form.Name)
        : GenerateSlug(form.Slug);
    form.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();

    foreach (var specification in form.Specifications)
    {
        specification.Value = string.IsNullOrWhiteSpace(specification.Value)
            ? null
            : specification.Value.Trim();
    }
}
```

Service chuẩn hóa cả dữ liệu sản phẩm và giá trị thông số:

- Tên được trim.
- Slug được sinh hoặc chuẩn hóa lại.
- Description rỗng chuyển về `null`.
- Value thông số rỗng chuyển về `null`, value có nội dung được trim.

## 18. CheckDeleteAsync

```csharp
var product = await _db.Products
    .AsNoTracking()
    .Where(item => item.Id == id)
    .Select(item => new
    {
        item.Name,
        VariantCount = item.ProductVariants.Count,
        SpecificationCount = item.ProductSpecifications.Count,
        ImageCount = item.ProductVariants.Sum(variant => variant.ProductVariantImages.Count),
    })
    .FirstOrDefaultAsync(ct);
```

Điểm thay đổi: `ImageCount` không còn đếm `ProductColorImages` trực tiếp trên product. Ảnh hiện nằm dưới variant nên service đếm tổng ảnh qua:

```csharp
item.ProductVariants.Sum(variant => variant.ProductVariantImages.Count)
```

`BuildDeleteBlockers` vẫn chặn xóa nếu còn:

- Biến thể.
- Thông số kỹ thuật.
- Ảnh biến thể.

## 19. Controller ProductsController

Controller không thay đổi lớn ở lần cập nhật này. Luồng vẫn như cũ:

- GET `Create`: gọi `GetCreateFormAsync`.
- POST `Create`: bind `ProductFormViewModel`, gọi `CreateAsync`.
- GET `Edit`: gọi `GetEditFormAsync`.
- POST `Edit`: gọi `UpdateAsync`.
- POST `CheckDelete`: gọi `CheckDeleteAsync`.
- POST toggle: gọi `ToggleActiveAsync` hoặc `ToggleFeaturedAsync`.

Khi service trả lỗi theo field, controller add lỗi vào `ModelState`, ví dụ lỗi của thông số sẽ map vào `Specifications[index].Value`.

## 20. View _Form - nhập thông số kỹ thuật

File: `Views/Products/_Form.cshtml`

Form có thêm card thông số kỹ thuật:

```cshtml
<div class="surface-form-card product-spec-card bg-white rounded-2xl border border-slate-200 p-5"
     data-product-spec-section>
    <h3 class="text-sm font-semibold text-slate-700 mb-4">Thông số kỹ thuật</h3>

    <div class="product-spec-empty" data-product-spec-empty>
        Chọn danh mục sản phẩm để nhập thông số kỹ thuật.
    </div>
```

View group thông số theo `CategoryId` và `GroupName`:

```cshtml
var specItems = Model.Specifications
    .Select((spec, index) => new { spec, index })
    .GroupBy(item => new
    {
        item.spec.CategoryId,
        GroupName = string.IsNullOrWhiteSpace(item.spec.GroupName)
            ? "Thông số khác"
            : item.spec.GroupName
    })
```

Mỗi input thông số gửi đủ dữ liệu cần thiết:

```cshtml
<input type="hidden" name="Specifications[@item.index].CategoryId" value="@item.spec.CategoryId" />
<input type="hidden" name="Specifications[@item.index].SpecificationId" value="@item.spec.SpecificationId" />
```

Input value:

```cshtml
<input id="Specifications_@(item.index)__Value"
       name="@fieldName"
       value="@item.spec.Value"
       maxlength="1000"
       data-product-spec-value
       data-product-spec-required="@isRequired"
       data-required-message="Vui lòng nhập @item.spec.Name."
       class="w-full border border-slate-200 rounded-xl px-3.5 py-2.5 text-sm text-slate-800 ..."
       placeholder="Nhập @item.spec.Name.ToLowerInvariant()" />
```

Validation message:

```cshtml
@Html.ValidationMessage(
    fieldName,
    null,
    new { @class = "text-xs text-red-500 mt-1 block" })
```

Checkbox nổi bật:

```cshtml
<input type="checkbox"
       name="Specifications[@item.index].IsHighlight"
       value="true"
       checked="@item.spec.IsHighlight" />
```

Lưu ý: không đặt hidden `false` trước checkbox này để tránh binder nhận giá trị sai khi checkbox được tick.

## 21. products.js - validate và đồng bộ thông số

File: `wwwroot/js/products.js`

Khi DOM ready:

```javascript
document.addEventListener('DOMContentLoaded', () => {
    bindProductFormValidation();
    bindSlugGenerator();
    bindProductSpecifications();
    bindStatusToggles();
    bindFeaturedToggles();
    bindDeleteConfirmation();
    bindToastDismiss();
});
```

`bindProductSpecifications()` là phần mới để đồng bộ thông số theo danh mục:

```javascript
function bindProductSpecifications() {
    const categorySelect = document.getElementById('productCategoryId');
    const section = document.querySelector('[data-product-spec-section]');

    if (!categorySelect || !section) {
        return;
    }
```

Khi category thay đổi, JS chỉ hiển thị group có `data-category-id` khớp category đang chọn:

```javascript
const isVisible = Boolean(selectedCategoryId) && group.dataset.categoryId === selectedCategoryId;
group.hidden = !isVisible;
```

Thông số bắt buộc chỉ bật required khi group đang hiển thị:

```javascript
setRequiredState(field, isVisible && field.dataset.productSpecRequired === 'true');
```

Khi group bị ẩn, JS clear lỗi cũ:

```javascript
if (!isVisible) {
    clearFieldError(field);
}
```

Điểm này giúp admin đổi danh mục không còn thấy lỗi của thông số thuộc danh mục cũ.

`bindSurfaceFormClientValidation` cũng bỏ qua field nằm trong group ẩn:

```javascript
.filter(field => !field.closest('[data-product-spec-group][hidden], [data-product-spec-row][hidden]'))
```

Nhờ vậy client validation không bắt lỗi các thông số đang không áp dụng.

## 22. products.css - style thông số kỹ thuật

File: `wwwroot/css/products.css`

Các class mới:

```css
.product-spec-empty { ... }
.product-spec-group { ... }
.product-spec-grid { ... }
.product-spec-field { ... }
.product-spec-input-wrap { ... }
.product-spec-input-has-unit { ... }
.product-highlight-check { ... }
```

Grid thông số:

```css
.product-spec-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.95rem;
}
```

Responsive:

```css
@media (max-width: 640px) {
  .product-spec-grid {
    grid-template-columns: 1fr;
  }
}
```

Nếu thông số có unit, input được chừa khoảng phải:

```css
.product-spec-input-has-unit {
  padding-right: 4rem;
}
```

## 23. Luồng tạo sản phẩm mới sau cập nhật

```text
GET Create
-> ProductAdminService.GetCreateFormAsync
-> PrepareFormAsync
   -> BuildBrandOptionsAsync
   -> BuildCategoryOptionsAsync
   -> BuildSpecificationInputsAsync
-> View _Form
```

Khi admin chọn danh mục:

```text
products.js
-> bindProductSpecifications
-> hiện nhóm thông số có CategoryId khớp
-> bật required cho thông số bắt buộc
```

Khi submit:

```text
POST Create
-> NormalizeForm
-> PrepareFormAsync để rebuild metadata thông số
-> ValidateFormAsync
-> GetSelectedSpecificationInputs
-> entity.ProductSpecifications.Add(...)
-> SaveChanges
```

## 24. Luồng sửa sản phẩm sau cập nhật

```text
GET Edit
-> Products.Include(ProductSpecifications)
-> map value cũ vào ProductSpecificationInputViewModel
-> PrepareFormAsync ghép lại metadata từ CategorySpecifications
-> View _Form
```

Khi submit:

```text
POST Edit
-> load Product kèm ProductSpecifications
-> NormalizeForm
-> PrepareFormAsync
-> ValidateFormAsync
-> ApplyProductSpecifications
   -> update thông số cũ
   -> remove thông số không còn áp dụng
   -> add thông số mới
-> SaveChanges
```

## 25. Luồng ảnh biến thể sau migration

```text
Product
-> ProductVariants
   -> ProductVariantImages
```

Quan hệ DB:

```text
products.Id
  1 - n product_variants.ProductId
product_variants.Id
  1 - n product_variant_images.ProductVariantId
```

Khi cần hiển thị ảnh theo biến thể:

```csharp
var images = await _db.ProductVariantImages
    .Where(image => image.ProductVariantId == variantId)
    .OrderBy(image => image.Position)
    .ToListAsync(ct);
```

Module Product admin hiện mới cập nhật backend/entity/migration và delete check cho ảnh biến thể. Form upload/quản lý ảnh biến thể có thể làm ở module variant riêng để giữ frontend/backend gọn.

## 26. Seed data liên quan

File: `Data/Seed/EcommerceSeedData.cs`

Ảnh mẫu hiện dùng entity mới:

```csharp
modelBuilder.Entity<ProductVariantImage>().HasData(
    new ProductVariantImage
    {
        Id = 1,
        ProductVariantId = 1,
        Color = "...",
        ImagePath = "...",
        AltText = "...",
        Position = 1
    });
```

Thông số kỹ thuật mẫu vẫn dùng `ProductSpecification` theo product:

```csharp
modelBuilder.Entity<ProductSpecification>().HasData(
    new ProductSpecification
    {
        ProductId = 1,
        SpecificationId = 2,
        Value = "256GB",
        SortOrder = 1,
        IsHighlight = true
    });
```

## 27. Những điểm đang ổn

- Controller mỏng, không query database trực tiếp.
- Service giữ nghiệp vụ tạo/sửa/xóa/toggle/validation.
- ViewModel tách khỏi entity EF Core.
- Product form hiện đã nhập được thông số kỹ thuật theo cấu hình danh mục.
- Backend rebuild metadata thông số từ DB, không tin hidden input từ frontend.
- Required specification được validate ở cả client và backend.
- `product_specifications` vẫn nối với `products`, đúng hướng thông số cấp sản phẩm.
- `product_variant_images` nối với `product_variants`, đúng hướng ảnh theo biến thể.
- Delete check đã cập nhật để đếm ảnh thông qua variant.
- Build hiện tại đã kiểm tra qua `dotnet build --no-restore` và không có warning/error.

## 28. Điểm cần lưu ý nếu mở rộng

- Migration `20260605090000_MoveProductImagesToVariants.cs` đang là migration viết tay, chạy được nhưng không có file `.Designer.cs` sinh tự động.
- Module Product hiện chưa có form quản lý biến thể và ảnh biến thể; nên làm ở module riêng để không làm form Product quá nặng.
- Khi thêm module quản lý ảnh biến thể, nên query theo `ProductVariantId` và giữ thứ tự bằng `Position`.
- Nếu sau này thông số kỹ thuật cần khác nhau theo từng biến thể, phải thay đổi thiết kế DB. Hiện tại thiết kế đã chốt là thông số ở cấp sản phẩm.
- Delete check hiện chưa kiểm tra target polymorphic như `VoucherTarget` hoặc `PromotionTarget` nếu target trỏ thẳng tới product.
