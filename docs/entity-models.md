# Tài liệu entity model cho e-commerce web admin

Tài liệu này mô tả những phần đã tạo từ ERD trong ảnh
`e-commerce.jpg`.

Nội dung tập trung vào mục đích từng class, các quan hệ chính và các
quyết định thiết kế khi triển khai bằng ASP.NET Core MVC, Entity Framework
Core và SQL Server.

## Những gì đã làm

Mình đã tạo các nhóm file sau:

- `Models/Enums/CommerceEnums.cs`: chứa các enum dùng chung.
- `Models/Entities/UserEntities.cs`: chứa user, địa chỉ, giỏ hàng.
- `Models/Entities/CatalogEntities.cs`: chứa catalog và biến thể.
- `Models/Entities/OrderEntities.cs`: chứa thanh toán và đơn hàng.
- `Models/Entities/PromotionEntities.cs`: chứa voucher và promotion.
- `Data/ApplicationDbContext.cs`: chứa `DbContext` và cấu hình EF Core.
- `Program.cs`: đăng ký `ApplicationDbContext` với SQL Server.
- `appsettings*.json`: thêm connection string `DefaultConnection`.

## Nguyên tắc thiết kế chính

### 1. Khóa nội bộ và mã nghiệp vụ được tách riêng

Các entity vẫn dùng `Id` kiểu `long` để làm khóa chính nội bộ và khóa
ngoại. Đây là cách phổ biến để quan hệ bảng nhanh, gọn và dễ migration.

Tuy nhiên, các mã hiển thị với người dùng hoặc dùng trong nghiệp vụ không
dùng `int` tăng dần:

- `ProductVariant.Code`: mã SKU/mã biến thể thực tế, ví dụ `IP15PM-256-BLK`, `NIKE-AF1-42-WHT`.
- `Order.OrderCode`: mã đơn hàng thực tế, ví dụ `ORD-20260527-000001`.
- `Voucher.Code`: mã voucher thực tế, ví dụ `SUMMER2026`, `FREESHIP-05`.

Các mã này là `string` và đã được cấu hình unique index trong `ApplicationDbContext`.

### 2. Tiền tệ dùng `decimal`

Các trường tiền như `Price`, `UnitPrice`, `SubtotalAmount`,
`ShippingFee`, `VoucherDiscount`, `TotalAmount`, `DiscountValue` dùng
`decimal`.

Trong SQL Server, các trường này được cấu hình:

```csharp
HasPrecision(18, 2)
```

Lý do: không nên dùng `float` hoặc `double` cho tiền vì có sai số số học.

### 3. Trạng thái và loại dữ liệu cố định dùng enum

Các giá trị như trạng thái đơn hàng, trạng thái thanh toán, loại giảm giá
và role người dùng được khai báo bằng enum.

Cách này giúp tránh nhập sai chuỗi trong code.

Trong database, enum được lưu dạng chuỗi bằng:

```csharp
HasConversion<string>()
```

Cách này giúp dữ liệu trong SQL dễ đọc hơn so với lưu số `0`, `1`, `2`.

### 4. Polymorphic target giữ dạng `TargetType + TargetId`

Trong ERD có các bảng như:

- `voucher_targets`
- `promotion_targets`

Các bảng này có thể trỏ đến nhiều loại đối tượng khác nhau như sản phẩm,
biến thể, danh mục hoặc thương hiệu.

Vì vậy model dùng:

- `TargetType`: loại target.
- `TargetId`: id của target.

Kiểu này linh hoạt, nhưng không có FK cứng trực tiếp trong SQL đến nhiều
bảng cùng lúc.

Khi viết service, cần kiểm tra `TargetType` để query đúng bảng.

## Nhóm enum

### `Gender`

Đại diện giới tính của user.

Giá trị:

- `Unknown`
- `Male`
- `Female`
- `Other`

Được dùng trong class `User`.

### `UserRole`

Đại diện vai trò người dùng trong hệ thống.

Giá trị:

- `Customer`
- `Staff`
- `Manager`
- `Admin`

Được dùng trong class `User`.

Với hệ thống lớn hơn, có thể thay bằng ASP.NET Core Identity role.

### `AddressType`

Đại diện loại địa chỉ.

Giá trị:

- `Shipping`
- `Billing`
- `Other`

Được dùng trong class `UserAddress`.

### `DiscountType`

Đại diện loại giảm giá.

Giá trị:

- `FixedAmount`: giảm số tiền cố định.
- `Percentage`: giảm theo phần trăm.

Được dùng trong `Voucher`.

### `OrderStatus`

Đại diện trạng thái xử lý đơn hàng.

Giá trị:

- `Pending`
- `Confirmed`
- `Processing`
- `Shipping`
- `Completed`
- `Cancelled`
- `Returned`

Được dùng trong `Order`.

### `PaymentStatus`

Đại diện trạng thái thanh toán.

Giá trị:

- `Unpaid`
- `Paid`
- `Failed`
- `Refunded`

Được dùng trong `Order`.

### `CampaignType`

Đại diện loại campaign.

Giá trị:

- `Banner`
- `Category`
- `FlashSale`
- `Seasonal`

Được dùng trong `Campaign`.

### `TargetType`

Đại diện loại đối tượng được áp dụng voucher hoặc promotion.

Giá trị:

- `Product`
- `ProductVariant`
- `Category`
- `Brand`
- `User`

Được dùng trong `VoucherTarget` và `PromotionTarget`.

### `PromotionActionType`

Đại diện loại hành động của promotion.

Giá trị:

- `DiscountOrder`
- `DiscountProduct`
- `BuyXGetY`
- `GiftProduct`

Được dùng trong `PromotionRule`.

## Nhóm người dùng

### `User`

Đại diện tài khoản người dùng trong hệ thống.

Field quan trọng:

- `Id`: khóa chính nội bộ.
- `Username`: tên đăng nhập, được unique.
- `Email`: email, được unique.
- `PasswordHash`: mật khẩu đã hash, không lưu mật khẩu gốc.
- `FullName`: họ tên.
- `Phone`: số điện thoại.
- `Gender`: giới tính, dùng enum `Gender`.
- `Role`: vai trò, dùng enum `UserRole`.
- `IsActive`: trạng thái tài khoản.
- `AvatarImage`: đường dẫn ảnh đại diện.
- `CreatedAt`, `UpdatedAt`: thời điểm tạo/cập nhật.

Quan hệ:

- Một `User` có nhiều `UserAddress`.
- Một `User` có nhiều `CartItem`.
- Một `User` có nhiều `Wishlist`.
- Một `User` có nhiều `Order`.
- Một `User` có nhiều `Rating`.
- Một `User` có nhiều `VoucherUser`.
- Một `User` có nhiều `VoucherUsage`.

### `UserAddress`

Đại diện địa chỉ nhận hàng hoặc thanh toán của user.

Field quan trọng:

- `UserId`: FK đến `User`.
- `ContactName`: tên người nhận.
- `Phone`: số điện thoại nhận hàng.
- `ProvinceCode`, `ProvinceName`: mã và tên tỉnh/thành.
- `WardCode`, `WardName`: mã và tên phường/xã.
- `DetailAddress`: địa chỉ chi tiết.
- `Type`: loại địa chỉ, dùng enum `AddressType`.
- `IsDefault`: có phải địa chỉ mặc định hay không.
- `IsDeleted`: đánh dấu địa chỉ đã bị user xóa mềm.
- `DeletedAt`: thời điểm xóa mềm địa chỉ, có thể `null` nếu địa chỉ vẫn đang dùng.

Quan hệ:

- Nhiều `UserAddress` thuộc về một `User`.
- Một `UserAddress` có thể được tham chiếu bởi nhiều `Order` thông qua `ShippingAddressId`.
- Khi user xóa địa chỉ, hệ thống nên set `IsDeleted = true` thay vì xóa cứng để các đơn hàng cũ vẫn giữ được liên kết lịch sử.

### `CartItem`

Đại diện một dòng sản phẩm trong giỏ hàng.

Field quan trọng:

- `UserId`: FK đến `User`.
- `ProductVariantId`: FK đến `ProductVariant`.
- `Quantity`: số lượng.
- `UnitPrice`: giá tại thời điểm thêm vào giỏ.
- `DiscountValue`: giá trị giảm giá đang áp dụng nếu có.

Quan hệ:

- Một `CartItem` thuộc về một `User`.
- Một `CartItem` trỏ đến một `ProductVariant`.

Ràng buộc:

- Unique index theo `UserId + ProductVariantId`.
  Ràng buộc này tránh một user có hai dòng trùng cùng một biến thể.

### `Wishlist`

Đại diện sản phẩm yêu thích của user.

Field quan trọng:

- `UserId`: FK đến `User`.
- `ProductVariantId`: FK đến `ProductVariant`.
- `CreatedAt`: thời điểm thêm vào wishlist.

Ràng buộc:

- Unique index theo `UserId + ProductVariantId`, tránh thêm trùng.

## Nhóm catalog sản phẩm

### `Brand`

Đại diện thương hiệu sản phẩm.

Field quan trọng:

- `Name`: tên thương hiệu.
- `Description`: mô tả.
- `ImagePath`: ảnh/logo thương hiệu.
- `Slug`: slug dùng trên URL, được unique.
- `IsActive`: trạng thái hiển thị.

Quan hệ:

- Một `Brand` có nhiều `Product`.

### `Category`

Đại diện danh mục sản phẩm.

Field quan trọng:

- `Name`: tên danh mục.
- `ParentId`: FK tự tham chiếu, hỗ trợ danh mục cha/con.
- `Description`: mô tả.
- `ImagePath`: ảnh danh mục.
- `Slug`: slug dùng trên URL, được unique.
- `Position`: thứ tự hiển thị.
- `IsActive`: trạng thái hiển thị.

Quan hệ:

- Một `Category` có thể có một `Parent`.
- Một `Category` có nhiều `Children`.
- Một `Category` có nhiều `Product`.
- Một `Category` có nhiều `CategorySpecification`.
- Một `Category` có nhiều `CategoryVariantAttribute`.
- Một `Category` có nhiều `CampaignCategory`.

### `Product`

Đại diện sản phẩm gốc, chưa phân biệt biến thể.

Ví dụ: `iPhone 15 Pro Max` là product.

`iPhone 15 Pro Max 256GB Black` là product variant.

Field quan trọng:

- `BrandId`: FK đến `Brand`.
- `CategoryId`: FK đến `Category`.
- `Name`: tên sản phẩm.
- `Description`: mô tả sản phẩm.
- `Slug`: slug dùng trên URL, được unique.
- `ViewsCount`: số lượt xem.
- `TotalSoldCount`: tổng số đã bán.
- `RatingAverage`: điểm đánh giá trung bình.
- `RatingCount`: số lượt đánh giá.
- `IsActive`: trạng thái bán/hiển thị.
- `IsFeatured`: sản phẩm nổi bật.

Quan hệ:

- Một `Product` thuộc về một `Brand`.
- Một `Product` thuộc về một `Category`.
- Một `Product` có nhiều `ProductVariant`.
- Một `Product` có nhiều ảnh gián tiếp qua `ProductVariant.ProductVariantImages`.
- Một `Product` có nhiều `ProductSpecification`.

### `ProductVariant`

Đại diện biến thể cụ thể của sản phẩm.

Ví dụ:

- `IP15PM-256-BLK`
- `IP15PM-512-NATURAL`
- `NIKE-AF1-42-WHT`

Field quan trọng:

- `ProductId`: FK đến `Product`.
- `Code`: mã SKU/mã biến thể thực tế, kiểu `string`, unique.
- `Price`: giá bán của biến thể.
- `SoldCount`: số lượng đã bán.
- `Quantity`: tồn kho hiện tại.
- `ColorName`: tên màu của biến thể, tối đa 120 ký tự.
- `ColorHex`: mã màu dạng `#RRGGBB`, tối đa 7 ký tự.
- `IsDefault`: biến thể mặc định khi mở sản phẩm.
- `IsActive`: biến thể còn bán hay không.

Quan hệ:

- Một `ProductVariant` thuộc về một `Product`.
- Một `ProductVariant` có nhiều `VariantAttribute`.
- Một `ProductVariant` có nhiều `ProductVariantImage`.
- Một `ProductVariant` có thể xuất hiện trong `CartItem`.
- Một `ProductVariant` có thể xuất hiện trong `Wishlist`.
- Một `ProductVariant` có thể xuất hiện trong `OrderItem`.
- Một `ProductVariant` có thể là quà tặng trong `PromotionRule`.

Lưu ý quan trọng:

- `Code` không dùng số `1`, `2`, `3`.
- `Code` nên là SKU thực tế có ý nghĩa nghiệp vụ.
- Màu được quản lý trực tiếp bằng `ColorName` và `ColorHex`.
- Attribute code `color` chỉ là dữ liệu legacy và không còn là chiều tạo biến thể mới.

### `ProductVariantImage`

Đại diện ảnh thuộc một biến thể sản phẩm.

Field quan trọng:

- `ProductVariantId`: FK đến `ProductVariant`.
- `ImagePath`: đường dẫn ảnh.
- `AltText`: mô tả ảnh.
- `Position`: thứ tự hiển thị.

Quan hệ:

- Nhiều `ProductVariantImage` thuộc về một `ProductVariant`.

Ảnh không lưu màu riêng. Tên màu và mã màu được lấy từ biến thể cha để tránh
nhiều nguồn dữ liệu mô tả cùng một màu.

### `Specification`

Đại diện định nghĩa thông số kỹ thuật.

Ví dụ:

- `screen_size`
- `ram`
- `storage`
- `material`

Field quan trọng:

- `Key`: mã thông số, unique.
- `Name`: tên hiển thị.
- `Unit`: đơn vị, ví dụ `GB`, `inch`, `kg`.
- `Icon`: icon đại diện nếu cần.

Quan hệ:

- Một `Specification` có thể dùng cho nhiều `Category` thông qua `CategorySpecification`.
- Một `Specification` có thể có nhiều giá trị theo từng sản phẩm qua `ProductSpecification`.

### `CategorySpecification`

Bảng nối giữa `Category` và `Specification`.

Mục đích:

- Xác định danh mục nào cần những thông số nào.
- Ví dụ danh mục điện thoại cần `RAM`, `ROM`, `Pin`.
- Danh mục giày có thể cần `Chất liệu`, `Đế giày`.

Field quan trọng:

- `CategoryId`: FK đến `Category`.
- `SpecificationId`: FK đến `Specification`.
- `IsRequired`: thông số có bắt buộc nhập không.
- `SortOrder`: thứ tự hiển thị.
- `GroupName`: nhóm thông số, ví dụ `Màn hình`, `Hiệu năng`.

Khóa chính:

- Composite key: `CategoryId + SpecificationId`.

Quy tắc hiệu lực:

- Danh mục con kế thừa specification được gán ở danh mục cha.
- Nếu cha và con cùng gán một specification, assignment gần danh mục con nhất
  được ưu tiên.
- Assignment vẫn được lưu trực tiếp theo từng danh mục; service cây danh mục
  chịu trách nhiệm phân giải bộ cấu hình hiệu lực.

### `ProductSpecification`

Đại diện giá trị thông số cụ thể của một sản phẩm.

Ví dụ:

- Product `iPhone 15 Pro Max`, specification `storage`, value `256GB`.
- Product `Áo thun`, specification `material`, value `Cotton`.

Field quan trọng:

- `ProductId`: FK đến `Product`.
- `SpecificationId`: FK đến `Specification`.
- `Value`: giá trị thông số.
- `SortOrder`: thứ tự hiển thị.
- `IsHighlight`: có hiển thị như thông số nổi bật hay không.

Khóa chính:

- Composite key: `ProductId + SpecificationId`.

### `Attribute`

Đại diện loại thuộc tính tạo biến thể.

Ví dụ:

- `size`
- `storage`
- `processor`

Field quan trọng:

- `Code`: mã thuộc tính, unique.
- `Name`: tên hiển thị.

Quan hệ:

- Một `Attribute` có nhiều `AttributeOption`.
- Một `Attribute` có thể được gắn với nhiều `Category` qua `CategoryVariantAttribute`.

Mã `color` được dành riêng cho dữ liệu cũ. Module quản trị không hiển thị,
tạo, sửa hoặc xóa option màu qua hệ thống attribute.

### `AttributeOption`

Đại diện lựa chọn cụ thể của một thuộc tính.

Ví dụ:

- Attribute `size`: option `S`, `M`, `L`, `XL`.
- Attribute `storage`: option `128GB`, `256GB`, `512GB`.
- Attribute `processor`: option `i5`, `i7`, `ultra-7`.

Field quan trọng:

- `AttributeId`: FK đến `Attribute`.
- `Value`: giá trị dùng trong hệ thống.
- `Label`: nhãn hiển thị cho người dùng.

Ràng buộc:

- Unique index theo `AttributeId + Value`.

### `CategoryVariantAttribute`

Bảng nối giữa `Category` và `Attribute`.

Mục đích:

- Xác định danh mục nào được phép dùng thuộc tính nào để tạo biến thể.
- Ví dụ danh mục điện thoại dùng `storage`.
- Danh mục quần áo dùng `size`.
- Màu không đi qua bảng này vì được lưu trực tiếp trên `ProductVariant`.

Field quan trọng:

- `CategoryId`: FK đến `Category`.
- `AttributeId`: FK đến `Attribute`.
- `CreatedAt`: thời điểm gắn thuộc tính vào danh mục.

Khóa chính:

- Composite key: `CategoryId + AttributeId`.

Danh mục con kế thừa attribute từ danh mục cha. Nếu cùng `AttributeId` xuất
hiện ở nhiều cấp, assignment gần danh mục con nhất được dùng.

### `VariantAttribute`

Bảng nối giữa `ProductVariant` và `AttributeOption`.

Mục đích:

- Mô tả một biến thể được tạo từ những option nào.
- Ví dụ SKU `IP15PM-256-BLK` gồm option `256GB` và `Black`.

Field quan trọng:

- `ProductVariantId`: FK đến `ProductVariant`.
- `AttributeOptionId`: FK đến `AttributeOption`.
- `CreatedAt`: thời điểm gắn option.

Khóa chính:

- Composite key: `ProductVariantId + AttributeOptionId`.

## Nhóm đơn hàng

### `PaymentMethod`

Đại diện phương thức thanh toán.

Ví dụ:

- COD.
- Chuyển khoản.
- Thẻ.
- Ví điện tử.

Field quan trọng:

- `Name`: tên phương thức thanh toán.
- `Description`: mô tả.
- `IsActive`: còn sử dụng hay không.

Quan hệ:

- Một `PaymentMethod` có nhiều `Order`.

### `Order`

Đại diện đơn hàng.

Field quan trọng:

- `Id`: khóa chính nội bộ.
- `OrderCode`: mã đơn hàng thực tế, kiểu `string`, unique.
- `UserId`: FK đến `User`.
- `PaymentMethodId`: FK đến `PaymentMethod`.
- `VoucherId`: FK tùy chọn đến `Voucher`.
- `ShippingAddressId`: FK tùy chọn đến `UserAddress`.
- `ShippingContactName`: tên người nhận tại thời điểm đặt hàng.
- `ShippingPhone`: số điện thoại nhận hàng tại thời điểm đặt hàng.
- `ShippingProvince`, `ShippingWard`, `ShippingDetail`: địa chỉ giao hàng snapshot.
- `SubtotalAmount`: tổng tiền hàng.
- `ShippingFee`: phí giao hàng.
- `VoucherDiscount`: tiền giảm từ voucher.
- `TotalAmount`: tổng tiền cuối cùng.
- `OrderStatus`: trạng thái đơn hàng.
- `PaymentStatus`: trạng thái thanh toán.

Quan hệ:

- Một `Order` thuộc về một `User`.
- Một `Order` có một `PaymentMethod`.
- Một `Order` có thể dùng một `Voucher`.
- Một `Order` có thể tham chiếu một `UserAddress` thông qua `ShippingAddressId`.
- Một `Order` có nhiều `OrderItem`.
- Một `Order` có thể có `VoucherUsage`.

Lưu ý quan trọng:

- Địa chỉ giao hàng được lưu snapshot trong order.
  Nếu user sửa địa chỉ sau này, đơn hàng cũ vẫn giữ đúng thông tin lúc đặt.
- `OrderCode` không dùng số tăng dần như `1`, `2`, `3`.

### `OrderItem`

Đại diện một dòng sản phẩm trong đơn hàng.

Field quan trọng:

- `OrderId`: FK đến `Order`.
- `ProductVariantId`: FK đến `ProductVariant`.
- `Quantity`: số lượng mua.
- `UnitPrice`: giá tại thời điểm đặt hàng.

Quan hệ:

- Một `OrderItem` thuộc về một `Order`.
- Một `OrderItem` trỏ đến một `ProductVariant`.
- Một `OrderItem` có thể có một `Rating`.

### `Rating`

Đại diện đánh giá sản phẩm sau khi mua.

Field quan trọng:

- `OrderItemId`: FK đến `OrderItem`, unique.
- `UserId`: FK đến `User`.
- `Stars`: số sao.
- `Comment`: nội dung đánh giá.
- `IsApproved`: đã được duyệt hay chưa.

Quan hệ:

- Một `Rating` thuộc về một `OrderItem`.
- Một `Rating` thuộc về một `User`.

Ràng buộc:

- Một `OrderItem` chỉ có một `Rating`.

## Nhóm voucher

### `Voucher`

Đại diện mã giảm giá.

Field quan trọng:

- `Code`: mã voucher thực tế, kiểu `string`, unique.
- `Description`: mô tả.
- `DiscountType`: loại giảm giá.
- `DiscountValue`: giá trị giảm.
- `MinOrderValue`: giá trị đơn hàng tối thiểu.
- `MaxDiscountValue`: mức giảm tối đa.
- `MaxUses`: tổng số lượt dùng tối đa.
- `MaxUsesPerUser`: số lượt dùng tối đa mỗi user.
- `UsedCount`: số lượt đã dùng.
- `StartDate`, `EndDate`: thời gian hiệu lực.
- `Priority`: độ ưu tiên.
- `IsActive`: còn hoạt động hay không.

Quan hệ:

- Một `Voucher` có nhiều `Order`.
- Một `Voucher` có nhiều `VoucherUser`.
- Một `Voucher` có nhiều `VoucherUsage`.
- Một `Voucher` có nhiều `VoucherTarget`.

### `VoucherUser`

Đại diện voucher được gán riêng cho user.

Field quan trọng:

- `VoucherId`: FK đến `Voucher`.
- `UserId`: FK đến `User`.
- `MaxUses`: số lượt user này được dùng.
- `UsedCount`: số lượt user này đã dùng.
- `AssignedAt`: thời điểm gán voucher.

Ràng buộc:

- Unique index theo `VoucherId + UserId`.

### `VoucherUsage`

Đại diện lịch sử sử dụng voucher.

Field quan trọng:

- `VoucherId`: FK đến `Voucher`.
- `UserId`: FK đến `User`.
- `OrderId`: FK đến `Order`.
- `UsedAt`: thời điểm sử dụng.

Ràng buộc:

- Unique index theo `OrderId`.
  Nghĩa là một order chỉ có một lần ghi nhận usage voucher.

### `VoucherTarget`

Đại diện phạm vi áp dụng voucher.

Ví dụ:

- Voucher áp dụng cho một category.
- Voucher áp dụng cho một brand.
- Voucher áp dụng cho một product.
- Voucher áp dụng cho một product variant.

Field quan trọng:

- `VoucherId`: FK đến `Voucher`.
- `TargetType`: loại target.
- `TargetId`: id của target.

Ràng buộc:

- Unique index theo `VoucherId + TargetType + TargetId`.

## Nhóm campaign

### `Campaign`

Đại diện chiến dịch hiển thị hoặc bán hàng.

Ví dụ:

- Banner mùa hè.
- Flash sale.
- Bộ sưu tập theo mùa.

Field quan trọng:

- `Name`: tên campaign.
- `Slug`: slug unique.
- `Type`: loại campaign.
- `Description`: mô tả.
- `StartDate`, `EndDate`: thời gian chạy.
- `IsActive`: trạng thái hoạt động.

Quan hệ:

- Một `Campaign` có nhiều `CampaignCategory`.

### `CampaignCategory`

Đại diện danh mục nằm trong campaign.

Field quan trọng:

- `CampaignId`: FK đến `Campaign`.
- `CategoryId`: FK đến `Category`.
- `Position`: thứ tự hiển thị.
- `ImagePath`: ảnh đại diện trong campaign.
- `Title`: tiêu đề hiển thị.
- `Description`: mô tả hiển thị.

Quan hệ:

- Một `CampaignCategory` thuộc về một `Campaign`.
- Một `CampaignCategory` trỏ đến một `Category`.

## Nhóm promotion

### `Promotion`

Đại diện chương trình khuyến mãi.

Field quan trọng:

- `Name`: tên promotion.
- `Description`: mô tả.
- `Priority`: độ ưu tiên.
- `IsActive`: trạng thái hoạt động.
- `StartDate`, `EndDate`: thời gian hiệu lực.
- `MinOrderValue`: giá trị đơn tối thiểu.
- `MaxDiscountValue`: mức giảm tối đa.
- `UsageLimit`: số lượt dùng tối đa.
- `UsedCount`: số lượt đã dùng.

Quan hệ:

- Một `Promotion` có nhiều `PromotionTarget`.
- Một `Promotion` có nhiều `PromotionRule`.

### `PromotionTarget`

Đại diện phạm vi áp dụng promotion.

Field quan trọng:

- `PromotionId`: FK đến `Promotion`.
- `TargetType`: loại target.
- `TargetId`: id của target.

Ràng buộc:

- Unique index theo `PromotionId + TargetType + TargetId`.

### `PromotionRule`

Đại diện rule cụ thể của promotion.

Ví dụ:

- Giảm giá đơn hàng.
- Giảm giá sản phẩm.
- Mua X tặng Y.
- Tặng một biến thể sản phẩm.

Field quan trọng:

- `PromotionId`: FK đến `Promotion`.
- `GiftProductVariantId`: FK tùy chọn đến `ProductVariant` nếu có quà tặng.
- `ActionType`: loại hành động promotion.
- `DiscountValue`: giá trị giảm.
- `BuyQuantity`: số lượng cần mua.
- `GetQuantity`: số lượng được nhận.

Quan hệ:

- Một `PromotionRule` thuộc về một `Promotion`.
- Một `PromotionRule` có thể trỏ đến một `ProductVariant` làm quà tặng.

## `ApplicationDbContext`

`ApplicationDbContext` là class trung tâm để EF Core biết project có
những entity nào và chúng map xuống SQL Server như thế nào.

### DbSet đã tạo

Các `DbSet` tương ứng với bảng:

- `Users`
- `UserAddresses`
- `CartItems`
- `Wishlists`
- `Brands`
- `Categories`
- `Products`
- `ProductVariants`
- `ProductVariantImages`
- `Specifications`
- `CategorySpecifications`
- `ProductSpecifications`
- `Attributes`
- `AttributeOptions`
- `CategoryVariantAttributes`
- `VariantAttributes`
- `PaymentMethods`
- `Orders`
- `OrderItems`
- `Ratings`
- `Vouchers`
- `VoucherUsers`
- `VoucherUsages`
- `VoucherTargets`
- `Campaigns`
- `CampaignCategories`
- `Promotions`
- `PromotionTargets`
- `PromotionRules`

### Cấu hình quan trọng

`ApplicationDbContext` đang cấu hình:

- Tên bảng theo snake_case, ví dụ `product_variants`, `order_items`, `voucher_usages`.
- Unique index cho các mã nghiệp vụ:
  - `ProductVariant.Code`
  - `Order.OrderCode`
  - `Voucher.Code`
- Unique index cho các bảng tránh dữ liệu trùng:
  - `CartItem`: `UserId + ProductVariantId`
  - `Wishlist`: `UserId + ProductVariantId`
  - `VoucherUser`: `VoucherId + UserId`
  - `VoucherTarget`: `VoucherId + TargetType + TargetId`
  - `PromotionTarget`: `PromotionId + TargetType + TargetId`
- Composite key cho các bảng nối:
  - `CategorySpecification`: `CategoryId + SpecificationId`
  - `ProductSpecification`: `ProductId + SpecificationId`
  - `CategoryVariantAttribute`: `CategoryId + AttributeId`
  - `VariantAttribute`: `ProductVariantId + AttributeOptionId`
- Precision cho tiền:
  - `HasPrecision(18, 2)`
- Enum lưu dạng string:
  - `Gender`
  - `UserRole`
  - `AddressType`
  - `DiscountType`
  - `OrderStatus`
  - `PaymentStatus`
  - `CampaignType`
  - `TargetType`
  - `PromotionActionType`
- Delete behavior mặc định là `Restrict` để tránh xóa dây chuyền ngoài ý muốn.

## Những điểm nên làm tiếp theo

Model và seed data mẫu đã được tạo. Các bước tiếp theo hợp lý là:

1. Tạo migration đầu tiên:

```powershell
dotnet ef migrations add InitialCreate
```

1. Cập nhật database:

```powershell
dotnet ef database update
```

1. Tạo ViewModel và Validator:

- `ProductCreateViewModel`
- `ProductVariantCreateViewModel`
- `VoucherCreateViewModel`
- `OrderFilterViewModel`

1. Scaffold hoặc viết controller quản trị:

- Products.
- Product variants.
- Categories.
- Brands.
- Orders.
- Vouchers.
- Promotions.
