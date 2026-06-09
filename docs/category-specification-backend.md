# Backend quản lý CategorySpecification

`CategorySpecification` là cấu hình nối một danh mục với các thông số kỹ thuật
được phép hoặc bắt buộc nhập cho sản phẩm.

## 1. Trách nhiệm

Module trả lời các câu hỏi:

- Danh mục được dùng những specification nào?
- Specification nào bắt buộc?
- Specification thuộc nhóm hiển thị nào?
- Thứ tự hiển thị là bao nhiêu?
- Có sản phẩm nào trong danh mục hoặc cây danh mục con đang sử dụng không?

## 2. Các file chính

```text
Controllers/CategorySpecificationsController.cs
Services/CategorySpecifications/ICategorySpecAdminService.cs
Services/CategorySpecifications/CategorySpecAdminService.cs
Services/CategorySpecifications/CategorySpecServiceResults.cs
Services/Categories/ICategoryHierarchyService.cs
Services/Categories/CategoryHierarchyService.cs
ViewModels/CategorySpecifications/CategorySpecificationViewModels.cs
Views/CategorySpecifications/Index.cshtml
wwwroot/js/category-specifications.js
wwwroot/css/specifications.css
```

## 3. Ranh giới frontend và backend

Backend chịu trách nhiệm:

- Kiểm tra category và specification tồn tại.
- Chặn assignment trùng.
- Lấy usage trong cả danh mục hiện tại và các danh mục con.
- Chặn bỏ gán nếu sản phẩm trong cây đang sử dụng specification.
- Lưu `GroupName`, `IsRequired` và `SortOrder`.

Frontend chịu trách nhiệm:

- Render danh sách đã gán và chưa gán.
- Lọc nhanh option.
- Gửi request assign, update và remove.
- Hiển thị phản hồi.

Frontend không quyết định một assignment có được xóa hay không.

## 4. Entity và khóa

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

Một specification chỉ được gán trực tiếp một lần cho mỗi danh mục.

## 5. Dependency injection

```csharp
builder.Services.AddScoped<ICategoryHierarchyService, CategoryHierarchyService>();
builder.Services.AddScoped<ICategorySpecAdminService, CategorySpecAdminService>();
```

`CategorySpecAdminService` dùng service cây danh mục chung, không tự duyệt cây.

## 6. Danh sách assignment

Trang Index lấy:

- Danh mục đang cấu hình.
- Assignment được gán trực tiếp cho danh mục đó.
- Specification chưa được gán trực tiếp.
- Usage count trong toàn bộ cây con.

Search chỉ lọc danh sách đã gán. Danh sách chưa gán luôn được tính từ toàn bộ
assignment trực tiếp để một specification đang có không xuất hiện nhầm ở cả
hai phía.

## 7. Usage trong cây danh mục

```csharp
var categoryIds = await _categoryHierarchy
    .GetSelfAndDescendantIdsAsync(categoryId, ct);

var categoryProductIds = await _db.Products
    .AsNoTracking()
    .Where(product => categoryIds.Contains(product.CategoryId))
    .Select(product => product.Id)
    .ToListAsync(ct);

var usageMap = await _db.ProductSpecifications
    .AsNoTracking()
    .Where(item => categoryProductIds.Contains(item.ProductId))
    .GroupBy(item => item.SpecificationId)
    .Select(group => new
    {
        SpecId = group.Key,
        Count = group.Count()
    })
    .ToDictionaryAsync(item => item.SpecId, item => item.Count, ct);
```

Usage phải tính cả descendant vì sản phẩm ở danh mục con có thể kế thừa
specification được gán từ danh mục cha.

## 8. Quy tắc kế thừa

Module Product áp dụng assignment theo quy tắc:

- Danh mục con kế thừa specification của cha.
- Assignment gần danh mục con nhất ghi đè assignment cùng `SpecificationId`
  từ cấp cha.
- `GroupName`, `IsRequired` và `SortOrder` lấy từ assignment hiệu lực.

`CategorySpecification` vẫn chỉ lưu assignment trực tiếp. Việc phân giải bộ
assignment hiệu lực do `CategoryHierarchyService.ResolveEffectiveAssignments`
thực hiện khi chuẩn bị form Product.

## 9. Assign

Trước khi thêm, service kiểm tra:

1. Category tồn tại.
2. Form có ít nhất một item được chọn.
3. Các item không trùng `SpecificationId`.
4. Toàn bộ specification được chọn đều tồn tại.
5. Không có cặp `{ CategoryId, SpecificationId }` nào đã tồn tại.

Form gửi một collection `Items`. Service chuẩn hóa rồi thêm từng assignment:

```csharp
foreach (var item in selectedItems)
{
    _db.CategorySpecifications.Add(new CategorySpecification
    {
        CategoryId = form.CategoryId,
        SpecificationId = item.SpecificationId,
        GroupName = item.GroupName,
        IsRequired = item.IsRequired,
        SortOrder = item.SortOrder
    });
}
```

Nhờ đó admin có thể gán nhiều thông số trong một lần submit nhưng backend vẫn
kiểm tra đầy đủ toàn bộ collection trước khi ghi database.

## 10. Update

Update tìm entity bằng khóa kép và chỉ sửa:

- `GroupName`.
- `IsRequired`.
- `SortOrder`.

`CategoryId` và `SpecificationId` không được đổi vì chúng tạo thành khóa.

Frontend phải gửi đúng tên field:

```text
CategoryId
SpecificationId
GroupName
SortOrder
IsRequired
```

## 11. Remove

Service tìm assignment trực tiếp bằng khóa kép, sau đó kiểm tra usage trong
cả cây:

```csharp
var categoryIds = await _categoryHierarchy
    .GetSelfAndDescendantIdsAsync(categoryId, ct);

var inUse = await _db.ProductSpecifications.AnyAsync(
    productSpec =>
        productSpec.SpecificationId == specId &&
        _db.Products.Any(product =>
            product.Id == productSpec.ProductId &&
            categoryIds.Contains(product.CategoryId)),
    ct);
```

Nếu `inUse` là `true`, service chặn remove. Điều này ngăn xóa cấu hình ở danh
mục cha trong khi sản phẩm thuộc danh mục con vẫn đang lưu specification đó.

## 12. ViewModel và validation

Các id kiểu `long` dùng `Range(1, long.MaxValue)` thay vì chỉ dùng `Required`,
vì model binder có thể trả `0` khi dữ liệu không hợp lệ.

Backend trim `GroupName`, giới hạn độ dài và kiểm tra lại toàn bộ dữ liệu trước
khi lưu. Validation phía JavaScript chỉ là lớp hỗ trợ trải nghiệm.

## 13. Trạng thái kiến trúc

- Controller chỉ điều phối HTTP.
- Service giữ query, validation và nghiệp vụ.
- ViewModel tách khỏi entity.
- Logic cây danh mục nằm trong service dùng chung.
- Usage và remove đã bao phủ toàn bộ descendant.
- Quy tắc kế thừa thống nhất với Product và ProductVariant.
