# Backend quản lý Product

Tài liệu này mô tả kiến trúc hiện tại của module Product sau khi tách màu,
ảnh biến thể và logic cây danh mục về đúng lớp chịu trách nhiệm.

## 1. Phạm vi module

Module Product quản lý:

- Thông tin sản phẩm gốc: tên, slug, thương hiệu, danh mục và mô tả.
- Trạng thái hoạt động và trạng thái nổi bật.
- Thông số kỹ thuật ở cấp sản phẩm.
- Lọc sản phẩm theo danh mục, bao gồm toàn bộ danh mục con.

Module Product không trực tiếp quản lý:

- SKU, giá, tồn kho và màu của biến thể.
- Thuộc tính tạo biến thể.
- Ảnh của từng biến thể.

Các phần trên thuộc module ProductVariant. Ranh giới này giữ form Product
nhẹ và tránh trộn nghiệp vụ sản phẩm gốc với SKU bán hàng.

## 2. Các file chính

```text
Controllers/ProductsController.cs
Services/Products/IProductAdminService.cs
Services/Products/ProductAdminService.cs
Services/Products/ProductServiceResults.cs
Services/Categories/ICategoryHierarchyService.cs
Services/Categories/CategoryHierarchyService.cs
ViewModels/Products/ProductViewModels.cs
Models/Entities/CatalogEntities.cs
Data/ApplicationDbContext.cs
Data/Seed/EcommerceSeedData.cs
Migrations/20260605090000_MoveProductImagesToVariants.cs
Migrations/20260608151939_MoveVariantColorToProductVariants.cs
Views/Products/_Form.cshtml
wwwroot/js/products.js
wwwroot/css/products.css
```

## 3. Ranh giới frontend và backend

Backend chịu trách nhiệm:

- Query, lọc, phân trang và kiểm tra tồn tại.
- Chuẩn hóa slug và dữ liệu nhập.
- Xác định thông số hiệu lực theo cây danh mục.
- Validate danh mục lá và thông số bắt buộc.
- Đồng bộ `ProductSpecifications`.
- Kiểm tra ràng buộc trước khi xóa.

Frontend chịu trách nhiệm:

- Render ViewModel do backend chuẩn bị.
- Hiện đúng nhóm thông số khi đổi danh mục.
- Validate sớm để cải thiện trải nghiệm.
- Gửi form hoặc request toggle tới controller.

Frontend không tự quyết định thông số nào áp dụng, dữ liệu nào được lưu hoặc
quy tắc kế thừa. Backend luôn dựng lại metadata từ database trước khi validate.

## 4. Đăng ký dependency

```csharp
builder.Services.AddScoped<ICategoryHierarchyService, CategoryHierarchyService>();
builder.Services.AddScoped<IProductAdminService, ProductAdminService>();
```

`ProductsController` chỉ phụ thuộc `IProductAdminService`.
`ProductAdminService` dùng `ICategoryHierarchyService` thay vì tự cài đặt lại
thuật toán cây danh mục.

## 5. Mô hình dữ liệu

### Product

`Product` là sản phẩm gốc và giữ:

- `BrandId`, `CategoryId`.
- `Name`, `Slug`, `Description`.
- `ViewsCount`, `TotalSoldCount`, dữ liệu đánh giá.
- `IsActive`, `IsFeatured`.
- Collection `ProductVariants`.
- Collection `ProductSpecifications`.

`Product` không giữ collection ảnh. Ảnh thuộc từng `ProductVariant`.

### ProductVariant

`ProductVariant` là SKU bán hàng thực tế:

```csharp
public class ProductVariant
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int SoldCount { get; set; }
    public int Quantity { get; set; }
    public string? ColorName { get; set; }
    public string? ColorHex { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<VariantAttribute> VariantAttributes { get; set; } = [];
    public ICollection<ProductVariantImage> ProductVariantImages { get; set; } = [];
}
```

Màu có một nguồn dữ liệu chính:

- `ColorName`: tên màu, tối đa 120 ký tự.
- `ColorHex`: mã màu `#RRGGBB`, tối đa 7 ký tự.

Các thuộc tính như dung lượng, kích thước hoặc bộ xử lý vẫn đi qua
`VariantAttributes`. Attribute code `color` là dữ liệu legacy và không còn
được dùng để tạo biến thể mới.

### ProductVariantImage

```csharp
public class ProductVariantImage
{
    public long Id { get; set; }
    public long ProductVariantId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int Position { get; set; }

    public ProductVariant? ProductVariant { get; set; }
}
```

Ảnh chỉ mô tả file, alt text và thứ tự. Ảnh không còn trường `Color`; màu được
lấy từ biến thể cha.

### ProductSpecification

`ProductSpecification` nối `Product` với `Specification` bằng khóa kép:

```text
ProductId + SpecificationId
```

Giá trị thông số được lưu ở cấp sản phẩm, không lặp lại trên từng biến thể.

## 6. Mapping EF Core

```csharp
modelBuilder.Entity<ProductVariant>(entity =>
{
    entity.ToTable("product_variants");
    entity.HasIndex(variant => variant.Code).IsUnique();
    entity.Property(variant => variant.Code).HasMaxLength(80).IsRequired();
    entity.Property(variant => variant.Price).HasPrecision(18, 2);
    entity.Property(variant => variant.ColorName).HasMaxLength(120);
    entity.Property(variant => variant.ColorHex).HasMaxLength(7).IsUnicode(false);
});

modelBuilder.Entity<ProductVariantImage>(entity =>
{
    entity.ToTable("product_variant_images");
    entity.Property(image => image.ImagePath).HasMaxLength(500).IsRequired();
    entity.Property(image => image.AltText).HasMaxLength(255);
    entity.HasOne(image => image.ProductVariant)
        .WithMany(variant => variant.ProductVariantImages)
        .HasForeignKey(image => image.ProductVariantId);
});
```

Không còn mapping `ProductVariantImage.Color`.

## 7. Migration dữ liệu ảnh và màu

### MoveProductImagesToVariants

`20260605090000_MoveProductImagesToVariants`:

- Đổi `product_color_images` thành `product_variant_images`.
- Đổi `ProductId` thành `ProductVariantId`.
- Chuyển ảnh cũ sang biến thể mặc định của sản phẩm.
- Tạo foreign key tới `product_variants`.

### MoveVariantColorToProductVariants

`20260608151939_MoveVariantColorToProductVariants`:

1. Thêm `ColorName` và `ColorHex` vào `product_variants`.
2. Backfill tên màu từ option thuộc attribute `color` legacy.
3. Fallback sang giá trị `product_variant_images.Color` cũ.
4. Chuyển mã hex hợp lệ sang `ColorHex`.
5. Xóa cột `Color` khỏi `product_variant_images`.
6. Cập nhật seed data cho các biến thể mẫu.

`Down` migration tạo lại cột màu trên ảnh và chép dữ liệu từ biến thể về ảnh.

## 8. Kế thừa cấu hình danh mục

`CategoryHierarchyService` là nơi dùng chung cho logic cây:

```csharp
Task<IReadOnlyList<CategoryHierarchyNode>> GetNodesAsync(...);
Task<IReadOnlyList<long>> GetSelfAndDescendantIdsAsync(long categoryId, ...);
IReadOnlyList<EffectiveCategoryAssignment<TAssignment>>
    ResolveEffectiveAssignments<TAssignment, TKey>(...);
```

Quy tắc hiệu lực:

- Danh mục con kế thừa thông số từ tất cả danh mục cha.
- Nếu cha và con cùng gán một `SpecificationId`, cấu hình gần danh mục con nhất
  được ưu tiên.
- Thuật toán có tập `visited` để không lặp vô hạn nếu dữ liệu cây bị lỗi.

`BuildSpecificationInputsAsync` lấy toàn bộ node và assignment, sau đó gọi:

```csharp
_categoryHierarchy.ResolveEffectiveAssignments(
    categories,
    categorySpecifications,
    assignment => assignment.CategoryId,
    assignment => assignment.SpecificationId);
```

Nhờ vậy ViewModel chứa bộ thông số hiệu lực cho từng danh mục, thay vì chỉ các
assignment được gán trực tiếp.

## 9. Lọc theo danh mục

Khi admin lọc danh sách Product theo một danh mục, service lấy cả cây con:

```csharp
var categoryIds = await _categoryHierarchy.GetSelfAndDescendantIdsAsync(
    query.CategoryId.Value,
    ct);

dbQuery = categoryIds.Count == 0
    ? dbQuery.Where(product => false)
    : dbQuery.Where(product => categoryIds.Contains(product.CategoryId));
```

Chọn danh mục cha vì vậy sẽ trả cả sản phẩm thuộc các danh mục con.

## 10. Chuẩn bị và lưu form

`PrepareFormAsync` luôn nạp lại:

- Brand options.
- Category options.
- Bộ thông số hiệu lực theo cây danh mục.

Luồng create/update:

```text
NormalizeForm
-> PrepareFormAsync
-> ValidateFormAsync
-> GetSelectedSpecificationInputs
-> tạo hoặc đồng bộ ProductSpecifications
-> SaveChangesAsync
```

Backend chỉ lưu thông số:

- Thuộc danh mục đang chọn sau khi áp dụng kế thừa.
- Có giá trị không rỗng.
- Vượt qua validation required.

Hidden input từ frontend không được xem là nguồn metadata đáng tin cậy.

## 11. Quy tắc validation

Service kiểm tra:

- Slug không trùng.
- Brand và category tồn tại.
- Product chỉ được gán vào danh mục lá.
- Các thông số hiệu lực có `IsRequired = true` phải có giá trị.
- Giá trị thông số được trim và giới hạn theo ViewModel.

Lỗi thông số dùng key dạng:

```text
Specifications[index].Value
```

để Razor hiển thị đúng tại input tương ứng.

## 12. Xóa Product

Delete check đếm ảnh qua biến thể:

```csharp
ImageCount = item.ProductVariants
    .Sum(variant => variant.ProductVariantImages.Count)
```

Backend chặn xóa khi sản phẩm còn:

- ProductVariant.
- ProductSpecification.
- ProductVariantImage thông qua variant.

Delete behavior toàn hệ thống là `Restrict`, vì vậy service phải xử lý quan hệ
rõ ràng thay vì dựa vào cascade delete.

## 13. Seed data

Màu nằm trên biến thể:

```csharp
new ProductVariant
{
    Id = 1,
    ProductId = 1,
    Code = "APP-IP15PM-256-BLK",
    ColorName = "Black Titanium",
    ColorHex = "#111827"
}
```

Ảnh chỉ tham chiếu biến thể:

```csharp
new ProductVariantImage
{
    Id = 1,
    ProductVariantId = 1,
    ImagePath = "/uploads/products/iphone-15-pro-max-black.jpg",
    AltText = "iPhone 15 Pro Max Black Titanium",
    Position = 1
}
```

## 14. Trạng thái kiến trúc

- Controller mỏng và không query EF Core trực tiếp.
- Service giữ toàn bộ nghiệp vụ và validation phía server.
- ViewModel tách khỏi entity.
- Logic cây danh mục được dùng chung, không lặp giữa Product, ProductVariant,
  CategorySpecification và CategoryVariantAttribute.
- Màu có một nguồn dữ liệu chính trên `ProductVariant`.
- Ảnh biến thể không mang metadata màu trùng lặp.
- Frontend chỉ phụ trách tương tác và trình bày.
