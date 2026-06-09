# Tai lieu module quan ly bien the san pham

Cap nhat lan cuoi: 2026-06-09.

## 1. Pham vi module

Module `ProductVariants` quan ly SKU ban hang cua tung san pham:

- Chon san pham cha.
- Quan ly SKU, gia ban, mau, trang thai va bien the mac dinh.
- Gan cac thuoc tinh bien the nhu dung luong, kich thuoc, bo xu ly.
- Upload va sap xep nhieu anh rieng cho bien the.
- Hien thi ton kho readonly; ton kho khong duoc nhap thu cong tai form nay.

Ranh gioi trach nhiem:

- Controller chi dieu phoi HTTP va ModelState.
- Service xu ly nghiep vu, EF Core, validation va upload.
- ViewModel la contract cua form/admin UI.
- Razor chi render giao dien.
- JavaScript chi xu ly validation nhanh, preview, reindex row va AJAX.
- Backend van validate lai toan bo du lieu truoc khi luu.

Day la ASP.NET Core MVC server-rendered, khong phai frontend va backend deploy doc lap.

## 2. File lien quan

### Backend

```text
Program.cs
Controllers/ProductVariantsController.cs
Services/ProductVariants/IProductVariantAdminService.cs
Services/ProductVariants/ProductVariantAdminService.cs
Services/ProductVariants/ProductVariantServiceResults.cs
Services/Categories/ICategoryHierarchyService.cs
Services/Categories/CategoryHierarchyService.cs
Services/Uploads/IImageUploadService.cs
ViewModels/ProductVariants/ProductVariantViewModels.cs
Models/Constants/CatalogAttributeCodes.cs
Models/Entities/CatalogEntities.cs
Data/ApplicationDbContext.cs
Data/Seed/EcommerceSeedData.cs
Migrations/20260608151939_MoveVariantColorToProductVariants.cs
```

### Frontend

```text
Views/ProductVariants/Index.cshtml
Views/ProductVariants/Create.cshtml
Views/ProductVariants/Edit.cshtml
Views/ProductVariants/_Form.cshtml
wwwroot/js/product-variants.js
wwwroot/css/product-variants.css
```

## 3. Mo hinh du lieu hien tai

### ProductVariant

`ProductVariant` la noi luu thong tin cua mot SKU:

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
}
```

Quy tac mau:

- `ColorName` toi da 120 ky tu.
- `ColorHex` co dinh dang `#RRGGBB`, toi da 7 ky tu.
- Ten mau va ma mau phai cung co hoac cung rong.
- Mau la du lieu cap `ProductVariant`, khong con nam tren tung anh.

### ProductVariantImage

```csharp
public class ProductVariantImage
{
    public long Id { get; set; }
    public long ProductVariantId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int Position { get; set; }
}
```

Mot bien the co the co nhieu anh. Moi anh chi luu URL, alt text va thu tu.

## 4. Nguon du lieu mau duy nhat

Truoc day he thong co them attribute code `color` va cot
`ProductVariantImage.Color`. Dieu nay tao nhieu nguon du lieu co the mau
thuan nhau.

Trang thai hien tai:

- Form variant chi dung `ColorName` va `ColorHex`.
- Attribute code `color` duoc xem la du lieu legacy.
- Module Attributes khong hien, tao, sua hoac xoa option cua `color`.
- Module CategoryVariantAttributes khong cho gan `color` vao category.
- Build attribute form va attribute summary bo qua attribute `color`.
- Khi cap nhat variant, cac lien ket color attribute cu khong con duoc chon
  va se duoc dong bo ra khoi variant do.

Hang so dung chung:

```csharp
CatalogAttributeCodes.Color
```

## 5. Migration mau

Migration:

```text
Migrations/20260608151939_MoveVariantColorToProductVariants.cs
```

Luong `Up`:

1. Them `ColorName` va `ColorHex` vao `product_variants`.
2. Backfill ten mau tu option attribute `color` legacy neu co.
3. Backfill tu `product_variant_images.Color` neu co.
4. Neu gia tri anh la ma hex hop le, dua vao `ColorHex`.
5. Drop cot `Color` khoi `product_variant_images`.
6. Cap nhat seed data mau cho cac variant mau.

Luong `Down` tao lai cot `Color`, copy du lieu tu variant ve cac anh, sau
do drop `ColorName` va `ColorHex`.

## 6. Thuoc tinh ke thua tu danh muc cha

`CategoryHierarchyService` la noi xu ly cay danh muc dung chung:

```csharp
Task<IReadOnlyList<CategoryHierarchyNode>> GetNodesAsync(...);
Task<IReadOnlyList<long>> GetSelfAndDescendantIdsAsync(...);
IReadOnlyList<EffectiveCategoryAssignment<T>> ResolveEffectiveAssignments(...);
```

Khi tao form variant:

- Thuoc tinh gan truc tiep cho category con duoc uu tien.
- Thuoc tinh chua co o category con duoc ke thua tu category cha.
- Cung mot attribute chi xuat hien mot lan.
- Attribute `color` bi loai bo vi mau da duoc quan ly truc tiep tren variant.

Service nay cung duoc dung boi Product, CategorySpecification va
CategoryVariantAttribute, tranh lap lai thuat toan cay danh muc.

## 7. ViewModel form

`ProductVariantFormViewModel` gom:

```csharp
public long? ProductId { get; set; }
public string Code { get; set; } = string.Empty;
public decimal? Price { get; set; }
public int Quantity { get; set; }
public string? ColorName { get; set; }
public string? ColorHex { get; set; }
public bool IsDefault { get; set; }
public bool IsActive { get; set; }
public List<ProductVariantAttributeInputViewModel> Attributes { get; set; } = [];
public List<ProductVariantImageInputViewModel> Images { get; set; } = [];
public List<IFormFile> BulkImageFiles { get; set; } = [];
```

`Quantity` chi dung de hien thi. Service tao variant voi `Quantity = 0`
va khong cap nhat quantity tu form edit.

Moi `ProductVariantImageInputViewModel` chi co mot `IFormFile`:

```csharp
public long? Id { get; set; }
public string? ImagePath { get; set; }
public IFormFile? ImageFile { get; set; }
public string? AltText { get; set; }
public int? Position { get; set; }
public bool Remove { get; set; }
```

## 8. Upload anh

Form co hai cach chon anh:

### Chon tung anh

- Moi row co mot file input.
- File input khong dung `multiple`.
- Row co position, preview, alt text va nut xoa.

### Chon nhieu anh

- Toolbar co input `BulkImageFiles` voi `multiple`.
- JavaScript tao mot row rieng cho tung file de preview.
- Neu trinh duyet khong ho tro `DataTransfer`, input bulk van duoc submit.
- Backend `MergeBulkImageFiles` chuyen moi file bulk thanh mot image input
  truoc khi normalize, validate va upload.

Nhu vay backend khong phu thuoc hoan toan vao JavaScript de nhan nhieu file.

## 9. Validation backend

Service kiem tra:

- Product ton tai.
- SKU bat buoc, dung format va khong trung.
- Gia khong am.
- Mau co du ca ten va ma hex.
- Ma mau dung `#RRGGBB`.
- Cac attribute bat buoc cua category da duoc chon.
- Option duoc chon thuoc dung attribute.
- Anh co file moi hoac URL cu.
- Alt text va image path khong vuot maxlength.
- Position khong am.
- To hop mau va attribute khong trung voi variant khac cua cung product.

So sanh duplicate bo qua attribute `color` legacy va dung
`ColorName`/`ColorHex` lam nguon mau chinh.

## 10. Dong bo entity

Khi create:

- Merge bulk files.
- Normalize form.
- Rebuild options va effective attributes tu database.
- Validate.
- Upload anh qua `IImageUploadService`.
- Tao `ProductVariant`, `VariantAttribute` va `ProductVariantImage`.

Khi update:

- Product cua variant bi khoa, khong doi qua form.
- Attribute khong con duoc chon se bi remove.
- Anh cu bi danh dau `Remove=true` se bi remove.
- Anh moi duoc upload va add.
- Neu variant la default, cac sibling default khac bi bo.

## 11. Frontend

`product-variants.js` chi xu ly:

- Validate nhanh SKU, gia, ma mau va row anh.
- Dong bo color picker voi input ma mau.
- An/hien attribute theo category cua product.
- Preview va reindex image rows.
- Bulk image UI va fallback submit.
- Toggle active, set default va check delete qua AJAX.

Frontend khong query database va khong quyet dinh nghiep vu luu.

## 12. Kiem tra da chay

```powershell
dotnet build --no-restore -o .codex-build-check
node --check wwwroot\js\product-variants.js
git diff --check
```

Ket qua:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

`git diff --check` khong co loi whitespace. Git chi canh bao chuyen line
ending LF sang CRLF tren Windows.
