using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using Microsoft.EntityFrameworkCore;
using AttributeEntity = e_commerce_web_admin.Models.Entities.Attribute;

namespace e_commerce_web_admin.Data.Seed;

public static class EcommerceSeedData
{
    private static readonly DateTime CreatedAt = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdatedAt = new(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CampaignStart = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CampaignEnd = new(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    public static void SeedEcommerceData(this ModelBuilder modelBuilder)
    {
        SeedUsers(modelBuilder);
        SeedCatalog(modelBuilder);
        SeedOrders(modelBuilder);
        SeedMarketing(modelBuilder);
    }

    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@ecommerce.local",
                PasswordHash = "sample_hash_admin_2026",
                FullName = "Nguyễn Minh Admin",
                Phone = "0901000001",
                Gender = Gender.Male,
                Role = UserRole.Admin,
                IsActive = true,
                AvatarImage = "/uploads/avatars/admin.png",
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new User
            {
                Id = 2,
                Username = "staff.an",
                Email = "an.staff@ecommerce.local",
                PasswordHash = "sample_hash_staff_2026",
                FullName = "Trần Hoàng An",
                Phone = "0901000002",
                Gender = Gender.Male,
                Role = UserRole.Staff,
                IsActive = true,
                AvatarImage = "/uploads/avatars/staff-an.png",
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new User
            {
                Id = 3,
                Username = "lan.nguyen",
                Email = "lan.nguyen@example.com",
                PasswordHash = "sample_hash_customer_3",
                FullName = "Nguyễn Thảo Lan",
                Phone = "0901000003",
                Gender = Gender.Female,
                Role = UserRole.Customer,
                IsActive = true,
                AvatarImage = "/uploads/avatars/lan.png",
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new User
            {
                Id = 4,
                Username = "minh.tran",
                Email = "minh.tran@example.com",
                PasswordHash = "sample_hash_customer_4",
                FullName = "Trần Quốc Minh",
                Phone = "0901000004",
                Gender = Gender.Male,
                Role = UserRole.Customer,
                IsActive = true,
                AvatarImage = "/uploads/avatars/minh.png",
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new User
            {
                Id = 5,
                Username = "quynh.pham",
                Email = "quynh.pham@example.com",
                PasswordHash = "sample_hash_customer_5",
                FullName = "Phạm Như Quỳnh",
                Phone = "0901000005",
                Gender = Gender.Female,
                Role = UserRole.Customer,
                IsActive = true,
                AvatarImage = "/uploads/avatars/quynh.png",
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            });

        modelBuilder.Entity<UserAddress>().HasData(
            new UserAddress
            {
                Id = 1,
                UserId = 1,
                ContactName = "Nguyễn Minh Admin",
                Phone = "0901000001",
                ProvinceCode = "79",
                ProvinceName = "Hồ Chí Minh",
                WardCode = "760",
                WardName = "Phường Bến Nghé",
                DetailAddress = "Tầng 8, 72 Lê Thánh Tôn, Quận 1",
                Type = AddressType.Billing,
                IsDefault = true,
                IsDeleted = false,
                DeletedAt = null,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new UserAddress
            {
                Id = 2,
                UserId = 2,
                ContactName = "Trần Hoàng An",
                Phone = "0901000002",
                ProvinceCode = "01",
                ProvinceName = "Hà Nội",
                WardCode = "001",
                WardName = "Phường Phúc Xá",
                DetailAddress = "24 Nguyễn Trung Trực, Ba Đình",
                Type = AddressType.Shipping,
                IsDefault = true,
                IsDeleted = false,
                DeletedAt = null,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new UserAddress
            {
                Id = 3,
                UserId = 3,
                ContactName = "Nguyễn Thảo Lan",
                Phone = "0901000003",
                ProvinceCode = "79",
                ProvinceName = "Hồ Chí Minh",
                WardCode = "771",
                WardName = "Phường Thảo Điền",
                DetailAddress = "145 Nguyễn Văn Hưởng, Thành phố Thủ Đức",
                Type = AddressType.Shipping,
                IsDefault = true,
                IsDeleted = false,
                DeletedAt = null,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new UserAddress
            {
                Id = 4,
                UserId = 4,
                ContactName = "Trần Quốc Minh",
                Phone = "0901000004",
                ProvinceCode = "48",
                ProvinceName = "Đà Nẵng",
                WardCode = "202",
                WardName = "Phường Hải Châu I",
                DetailAddress = "18 Bạch Đằng, Hải Châu",
                Type = AddressType.Shipping,
                IsDefault = true,
                IsDeleted = false,
                DeletedAt = null,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new UserAddress
            {
                Id = 5,
                UserId = 5,
                ContactName = "Phạm Như Quỳnh",
                Phone = "0901000005",
                ProvinceCode = "92",
                ProvinceName = "Cần Thơ",
                WardCode = "311",
                WardName = "Phường Ninh Kiều",
                DetailAddress = "62 Nguyễn Trãi, Ninh Kiều",
                Type = AddressType.Shipping,
                IsDefault = true,
                IsDeleted = false,
                DeletedAt = null,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            });
    }

    private static void SeedCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>().HasData(
            new Brand
            {
                Id = 1,
                Name = "Apple",
                Description = "Thiết bị di động và laptop cao cấp.",
                ImagePath = "/uploads/brands/apple.png",
                Slug = "apple",
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Brand
            {
                Id = 2,
                Name = "Samsung",
                Description = "Điện thoại và thiết bị thông minh.",
                ImagePath = "/uploads/brands/samsung.png",
                Slug = "samsung",
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Brand
            {
                Id = 3,
                Name = "Dell",
                Description = "Laptop văn phòng và doanh nghiệp.",
                ImagePath = "/uploads/brands/dell.png",
                Slug = "dell",
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Brand
            {
                Id = 4,
                Name = "Nike",
                Description = "Giày và thời trang thể thao.",
                ImagePath = "/uploads/brands/nike.png",
                Slug = "nike",
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Brand
            {
                Id = 5,
                Name = "Adidas",
                Description = "Giày chạy bộ và trang phục thể thao.",
                ImagePath = "/uploads/brands/adidas.png",
                Slug = "adidas",
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            });

        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = 1,
                Name = "Điện tử",
                ParentId = null,
                Description = "Thiết bị công nghệ và phụ kiện.",
                ImagePath = "/uploads/categories/electronics.jpg",
                Slug = "dien-tu",
                Position = 1,
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Category
            {
                Id = 2,
                Name = "Điện thoại",
                ParentId = 1,
                Description = "Smartphone chính hãng.",
                ImagePath = "/uploads/categories/smartphones.jpg",
                Slug = "dien-thoai",
                Position = 2,
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Category
            {
                Id = 3,
                Name = "Laptop",
                ParentId = 1,
                Description = "Laptop học tập, văn phòng và doanh nghiệp.",
                ImagePath = "/uploads/categories/laptops.jpg",
                Slug = "laptop",
                Position = 3,
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Category
            {
                Id = 4,
                Name = "Thời trang",
                ParentId = null,
                Description = "Thời trang nam nữ và phụ kiện.",
                ImagePath = "/uploads/categories/fashion.jpg",
                Slug = "thoi-trang",
                Position = 4,
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Category
            {
                Id = 5,
                Name = "Giày thể thao",
                ParentId = 4,
                Description = "Giày sneaker và giày chạy bộ.",
                ImagePath = "/uploads/categories/sneakers.jpg",
                Slug = "giay-the-thao",
                Position = 5,
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            });

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                BrandId = 1,
                CategoryId = 2,
                Name = "iPhone 15 Pro Max",
                Description = "iPhone 15 Pro Max chính hãng, chip A17 Pro.",
                Slug = "iphone-15-pro-max",
                ViewsCount = 15420,
                TotalSoldCount = 328,
                RatingAverage = 4.80m,
                RatingCount = 142,
                IsActive = true,
                IsFeatured = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Product
            {
                Id = 2,
                BrandId = 2,
                CategoryId = 2,
                Name = "Samsung Galaxy S24 Ultra",
                Description = "Galaxy S24 Ultra với S Pen và Galaxy AI.",
                Slug = "samsung-galaxy-s24-ultra",
                ViewsCount = 12110,
                TotalSoldCount = 276,
                RatingAverage = 4.70m,
                RatingCount = 118,
                IsActive = true,
                IsFeatured = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Product
            {
                Id = 3,
                BrandId = 3,
                CategoryId = 3,
                Name = "Dell XPS 13",
                Description = "Laptop mỏng nhẹ cho công việc và di chuyển.",
                Slug = "dell-xps-13",
                ViewsCount = 8420,
                TotalSoldCount = 94,
                RatingAverage = 4.60m,
                RatingCount = 61,
                IsActive = true,
                IsFeatured = false,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Product
            {
                Id = 4,
                BrandId = 4,
                CategoryId = 5,
                Name = "Nike Air Force 1 '07",
                Description = "Sneaker cổ thấp, thiết kế trắng cổ điển.",
                Slug = "nike-air-force-1-07",
                ViewsCount = 9730,
                TotalSoldCount = 451,
                RatingAverage = 4.75m,
                RatingCount = 203,
                IsActive = true,
                IsFeatured = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new Product
            {
                Id = 5,
                BrandId = 5,
                CategoryId = 5,
                Name = "Adidas Ultraboost Light",
                Description = "Giày chạy bộ nhẹ, đệm Boost đàn hồi tốt.",
                Slug = "adidas-ultraboost-light",
                ViewsCount = 6880,
                TotalSoldCount = 188,
                RatingAverage = 4.55m,
                RatingCount = 77,
                IsActive = true,
                IsFeatured = false,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            });

        modelBuilder.Entity<ProductVariant>().HasData(
            new ProductVariant
            {
                Id = 1,
                ProductId = 1,
                Code = "APP-IP15PM-256-BLK",
                Price = 29990000m,
                SoldCount = 185,
                Quantity = 42,
                ColorName = "Black Titanium",
                ColorHex = "#111827",
                IsDefault = true,
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new ProductVariant
            {
                Id = 2,
                ProductId = 2,
                Code = "SAM-S24U-512-GRY",
                Price = 31990000m,
                SoldCount = 132,
                Quantity = 35,
                ColorName = "Titanium Gray",
                ColorHex = "#71717A",
                IsDefault = true,
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new ProductVariant
            {
                Id = 3,
                ProductId = 3,
                Code = "DEL-XPS13-UL7-16-512",
                Price = 38990000m,
                SoldCount = 58,
                Quantity = 18,
                ColorName = "Platinum Silver",
                ColorHex = "#D6D3D1",
                IsDefault = true,
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new ProductVariant
            {
                Id = 4,
                ProductId = 4,
                Code = "NIK-AF1-42-WHT",
                Price = 2890000m,
                SoldCount = 224,
                Quantity = 76,
                ColorName = "White",
                ColorHex = "#FFFFFF",
                IsDefault = true,
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            },
            new ProductVariant
            {
                Id = 5,
                ProductId = 5,
                Code = "ADI-UBL-41-BLK",
                Price = 4200000m,
                SoldCount = 94,
                Quantity = 51,
                ColorName = "Core Black",
                ColorHex = "#111827",
                IsDefault = true,
                IsActive = true,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            });

        modelBuilder.Entity<ProductVariantImage>().HasData(
            new ProductVariantImage
            {
                Id = 1,
                ProductVariantId = 1,
                ImagePath = "/uploads/products/iphone-15-pro-max-black.jpg",
                AltText = "iPhone 15 Pro Max Black Titanium",
                Position = 1
            },
            new ProductVariantImage
            {
                Id = 2,
                ProductVariantId = 2,
                ImagePath = "/uploads/products/galaxy-s24-ultra-gray.jpg",
                AltText = "Samsung Galaxy S24 Ultra Titanium Gray",
                Position = 1
            },
            new ProductVariantImage
            {
                Id = 3,
                ProductVariantId = 3,
                ImagePath = "/uploads/products/dell-xps-13-silver.jpg",
                AltText = "Dell XPS 13 Platinum Silver",
                Position = 1
            },
            new ProductVariantImage
            {
                Id = 4,
                ProductVariantId = 4,
                ImagePath = "/uploads/products/nike-af1-white.jpg",
                AltText = "Nike Air Force 1 White",
                Position = 1
            },
            new ProductVariantImage
            {
                Id = 5,
                ProductVariantId = 5,
                ImagePath = "/uploads/products/adidas-ultraboost-black.jpg",
                AltText = "Adidas Ultraboost Light Core Black",
                Position = 1
            });

        SeedSpecificationsAndAttributes(modelBuilder);
        SeedUserShoppingData(modelBuilder);
    }

    private static void SeedSpecificationsAndAttributes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Specification>().HasData(
            new Specification { Id = 1, Key = "screen_size", Name = "Kích thước màn hình", Unit = "inch", Icon = "monitor" },
            new Specification { Id = 2, Key = "storage", Name = "Dung lượng lưu trữ", Unit = "GB", Icon = "hard-drive" },
            new Specification { Id = 3, Key = "ram", Name = "Bộ nhớ RAM", Unit = "GB", Icon = "memory-stick" },
            new Specification { Id = 4, Key = "material", Name = "Chất liệu", Unit = null, Icon = "layers" },
            new Specification { Id = 5, Key = "battery", Name = "Dung lượng pin", Unit = "mAh", Icon = "battery" });

        modelBuilder.Entity<CategorySpecification>().HasData(
            new CategorySpecification { CategoryId = 2, SpecificationId = 1, IsRequired = true, SortOrder = 1, GroupName = "Màn hình" },
            new CategorySpecification { CategoryId = 2, SpecificationId = 2, IsRequired = true, SortOrder = 2, GroupName = "Hiệu năng" },
            new CategorySpecification { CategoryId = 2, SpecificationId = 3, IsRequired = true, SortOrder = 3, GroupName = "Hiệu năng" },
            new CategorySpecification { CategoryId = 2, SpecificationId = 5, IsRequired = false, SortOrder = 4, GroupName = "Pin" },
            new CategorySpecification { CategoryId = 5, SpecificationId = 4, IsRequired = true, SortOrder = 1, GroupName = "Chất liệu" });

        modelBuilder.Entity<ProductSpecification>().HasData(
            new ProductSpecification { ProductId = 1, SpecificationId = 2, Value = "256GB", SortOrder = 1, IsHighlight = true },
            new ProductSpecification { ProductId = 2, SpecificationId = 3, Value = "12GB", SortOrder = 1, IsHighlight = true },
            new ProductSpecification { ProductId = 3, SpecificationId = 1, Value = "13.4", SortOrder = 1, IsHighlight = true },
            new ProductSpecification { ProductId = 4, SpecificationId = 4, Value = "Da tổng hợp", SortOrder = 1, IsHighlight = true },
            new ProductSpecification { ProductId = 5, SpecificationId = 4, Value = "Primeknit và cao su Continental", SortOrder = 1, IsHighlight = true });

        modelBuilder.Entity<AttributeEntity>().HasData(
            new AttributeEntity { Id = 1, Code = "color", Name = "Màu sắc" },
            new AttributeEntity { Id = 2, Code = "storage", Name = "Dung lượng" },
            new AttributeEntity { Id = 3, Code = "size", Name = "Kích thước" },
            new AttributeEntity { Id = 4, Code = "processor", Name = "Bộ xử lý" },
            new AttributeEntity { Id = 5, Code = "condition", Name = "Tình trạng" });

        modelBuilder.Entity<AttributeOption>().HasData(
            new AttributeOption { Id = 1, AttributeId = 1, Value = "black-titanium", Label = "Black Titanium" },
            new AttributeOption { Id = 2, AttributeId = 1, Value = "titanium-gray", Label = "Titanium Gray" },
            new AttributeOption { Id = 3, AttributeId = 2, Value = "256gb", Label = "256GB" },
            new AttributeOption { Id = 4, AttributeId = 3, Value = "42", Label = "Size 42" },
            new AttributeOption { Id = 5, AttributeId = 4, Value = "core-ultra-7", Label = "Intel Core Ultra 7" });

        modelBuilder.Entity<CategoryVariantAttribute>().HasData(
            new CategoryVariantAttribute { CategoryId = 2, AttributeId = 1, CreatedAt = CreatedAt },
            new CategoryVariantAttribute { CategoryId = 2, AttributeId = 2, CreatedAt = CreatedAt },
            new CategoryVariantAttribute { CategoryId = 3, AttributeId = 4, CreatedAt = CreatedAt },
            new CategoryVariantAttribute { CategoryId = 5, AttributeId = 1, CreatedAt = CreatedAt },
            new CategoryVariantAttribute { CategoryId = 5, AttributeId = 3, CreatedAt = CreatedAt });

        modelBuilder.Entity<VariantAttribute>().HasData(
            new VariantAttribute { ProductVariantId = 1, AttributeOptionId = 1, CreatedAt = CreatedAt },
            new VariantAttribute { ProductVariantId = 2, AttributeOptionId = 2, CreatedAt = CreatedAt },
            new VariantAttribute { ProductVariantId = 1, AttributeOptionId = 3, CreatedAt = CreatedAt },
            new VariantAttribute { ProductVariantId = 4, AttributeOptionId = 4, CreatedAt = CreatedAt },
            new VariantAttribute { ProductVariantId = 3, AttributeOptionId = 5, CreatedAt = CreatedAt });
    }

    private static void SeedUserShoppingData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CartItem>().HasData(
            new CartItem { Id = 1, UserId = 3, ProductVariantId = 1, Quantity = 1, UnitPrice = 29990000m, DiscountValue = 500000m, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new CartItem { Id = 2, UserId = 4, ProductVariantId = 2, Quantity = 1, UnitPrice = 31990000m, DiscountValue = 700000m, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new CartItem { Id = 3, UserId = 5, ProductVariantId = 3, Quantity = 1, UnitPrice = 38990000m, DiscountValue = 1000000m, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new CartItem { Id = 4, UserId = 2, ProductVariantId = 4, Quantity = 2, UnitPrice = 2890000m, DiscountValue = 300000m, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new CartItem { Id = 5, UserId = 1, ProductVariantId = 5, Quantity = 1, UnitPrice = 4200000m, DiscountValue = 250000m, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt });

        modelBuilder.Entity<Wishlist>().HasData(
            new Wishlist { Id = 1, UserId = 3, ProductVariantId = 2, CreatedAt = CreatedAt },
            new Wishlist { Id = 2, UserId = 3, ProductVariantId = 4, CreatedAt = CreatedAt },
            new Wishlist { Id = 3, UserId = 4, ProductVariantId = 1, CreatedAt = CreatedAt },
            new Wishlist { Id = 4, UserId = 5, ProductVariantId = 5, CreatedAt = CreatedAt },
            new Wishlist { Id = 5, UserId = 2, ProductVariantId = 3, CreatedAt = CreatedAt });
    }

    private static void SeedOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentMethod>().HasData(
            new PaymentMethod { Id = 1, Name = "Thanh toán khi nhận hàng", Description = "COD tại địa chỉ giao hàng.", IsActive = true },
            new PaymentMethod { Id = 2, Name = "Chuyển khoản ngân hàng", Description = "Thanh toán qua tài khoản ngân hàng.", IsActive = true },
            new PaymentMethod { Id = 3, Name = "Thẻ Visa/Mastercard", Description = "Thanh toán bằng thẻ quốc tế.", IsActive = true },
            new PaymentMethod { Id = 4, Name = "Ví MoMo", Description = "Thanh toán qua ví điện tử MoMo.", IsActive = true },
            new PaymentMethod { Id = 5, Name = "VNPAY", Description = "Thanh toán qua cổng VNPAY QR.", IsActive = true });

        modelBuilder.Entity<Order>().HasData(
            new Order
            {
                Id = 1,
                UserId = 3,
                PaymentMethodId = 1,
                VoucherId = 1,
                OrderCode = "ORD-20260520-000001",
                ShippingAddressId = 3,
                ShippingContactName = "Nguyễn Thảo Lan",
                ShippingPhone = "0901000003",
                ShippingProvince = "Hồ Chí Minh",
                ShippingWard = "Phường Thảo Điền",
                ShippingDetail = "145 Nguyễn Văn Hưởng, Thành phố Thủ Đức",
                SubtotalAmount = 29990000m,
                ShippingFee = 30000m,
                VoucherDiscount = 500000m,
                TotalAmount = 29520000m,
                OrderStatus = OrderStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = new DateTime(2026, 5, 20, 9, 15, 0, DateTimeKind.Utc),
                UpdatedAt = UpdatedAt
            },
            new Order
            {
                Id = 2,
                UserId = 4,
                PaymentMethodId = 4,
                VoucherId = 2,
                OrderCode = "ORD-20260521-000002",
                ShippingAddressId = 4,
                ShippingContactName = "Trần Quốc Minh",
                ShippingPhone = "0901000004",
                ShippingProvince = "Đà Nẵng",
                ShippingWard = "Phường Hải Châu I",
                ShippingDetail = "18 Bạch Đằng, Hải Châu",
                SubtotalAmount = 31990000m,
                ShippingFee = 0m,
                VoucherDiscount = 0m,
                TotalAmount = 31990000m,
                OrderStatus = OrderStatus.Shipping,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = new DateTime(2026, 5, 21, 10, 20, 0, DateTimeKind.Utc),
                UpdatedAt = UpdatedAt
            },
            new Order
            {
                Id = 3,
                UserId = 5,
                PaymentMethodId = 2,
                VoucherId = 3,
                OrderCode = "ORD-20260522-000003",
                ShippingAddressId = 5,
                ShippingContactName = "Phạm Như Quỳnh",
                ShippingPhone = "0901000005",
                ShippingProvince = "Cần Thơ",
                ShippingWard = "Phường Ninh Kiều",
                ShippingDetail = "62 Nguyễn Trãi, Ninh Kiều",
                SubtotalAmount = 38990000m,
                ShippingFee = 45000m,
                VoucherDiscount = 1000000m,
                TotalAmount = 38035000m,
                OrderStatus = OrderStatus.Confirmed,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = new DateTime(2026, 5, 22, 14, 30, 0, DateTimeKind.Utc),
                UpdatedAt = UpdatedAt
            },
            new Order
            {
                Id = 4,
                UserId = 3,
                PaymentMethodId = 3,
                VoucherId = 4,
                OrderCode = "ORD-20260523-000004",
                ShippingAddressId = 3,
                ShippingContactName = "Nguyễn Thảo Lan",
                ShippingPhone = "0901000003",
                ShippingProvince = "Hồ Chí Minh",
                ShippingWard = "Phường Thảo Điền",
                ShippingDetail = "145 Nguyễn Văn Hưởng, Thành phố Thủ Đức",
                SubtotalAmount = 5780000m,
                ShippingFee = 30000m,
                VoucherDiscount = 867000m,
                TotalAmount = 4943000m,
                OrderStatus = OrderStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = new DateTime(2026, 5, 23, 16, 5, 0, DateTimeKind.Utc),
                UpdatedAt = UpdatedAt
            },
            new Order
            {
                Id = 5,
                UserId = 4,
                PaymentMethodId = 5,
                VoucherId = 5,
                OrderCode = "ORD-20260524-000005",
                ShippingAddressId = 4,
                ShippingContactName = "Trần Quốc Minh",
                ShippingPhone = "0901000004",
                ShippingProvince = "Đà Nẵng",
                ShippingWard = "Phường Hải Châu I",
                ShippingDetail = "18 Bạch Đằng, Hải Châu",
                SubtotalAmount = 4200000m,
                ShippingFee = 35000m,
                VoucherDiscount = 100000m,
                TotalAmount = 4135000m,
                OrderStatus = OrderStatus.Processing,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = new DateTime(2026, 5, 24, 11, 45, 0, DateTimeKind.Utc),
                UpdatedAt = UpdatedAt
            });

        modelBuilder.Entity<OrderItem>().HasData(
            new OrderItem { Id = 1, OrderId = 1, ProductVariantId = 1, Quantity = 1, UnitPrice = 29990000m },
            new OrderItem { Id = 2, OrderId = 2, ProductVariantId = 2, Quantity = 1, UnitPrice = 31990000m },
            new OrderItem { Id = 3, OrderId = 3, ProductVariantId = 3, Quantity = 1, UnitPrice = 38990000m },
            new OrderItem { Id = 4, OrderId = 4, ProductVariantId = 4, Quantity = 2, UnitPrice = 2890000m },
            new OrderItem { Id = 5, OrderId = 5, ProductVariantId = 5, Quantity = 1, UnitPrice = 4200000m });

        modelBuilder.Entity<Rating>().HasData(
            new Rating { Id = 1, OrderItemId = 1, UserId = 3, Stars = 5, Comment = "Máy đẹp, giao nhanh, đóng gói kỹ.", IsApproved = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Rating { Id = 2, OrderItemId = 2, UserId = 4, Stars = 5, Comment = "Màn hình rất đẹp, dùng AI tiện.", IsApproved = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Rating { Id = 3, OrderItemId = 3, UserId = 5, Stars = 4, Comment = "Laptop nhẹ, pin ổn cho công việc.", IsApproved = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Rating { Id = 4, OrderItemId = 4, UserId = 3, Stars = 5, Comment = "Giày đúng size, form đẹp.", IsApproved = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Rating { Id = 5, OrderItemId = 5, UserId = 4, Stars = 4, Comment = "Đệm êm, phù hợp chạy bộ hằng ngày.", IsApproved = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt });
    }

    private static void SeedMarketing(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Voucher>().HasData(
            new Voucher { Id = 1, Code = "SUMMER2026", Description = "Giảm 500.000đ cho đơn công nghệ từ 10 triệu.", DiscountType = DiscountType.FixedAmount, DiscountValue = 500000m, MinOrderValue = 10000000m, MaxDiscountValue = 500000m, MaxUses = 500, MaxUsesPerUser = 1, UsedCount = 120, StartDate = CampaignStart, EndDate = CampaignEnd, Priority = 10, IsActive = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Voucher { Id = 2, Code = "FREESHIP-05", Description = "Miễn phí vận chuyển cho đơn từ 1 triệu.", DiscountType = DiscountType.FixedAmount, DiscountValue = 50000m, MinOrderValue = 1000000m, MaxDiscountValue = 50000m, MaxUses = 1000, MaxUsesPerUser = 3, UsedCount = 380, StartDate = CampaignStart, EndDate = CampaignEnd, Priority = 5, IsActive = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Voucher { Id = 3, Code = "TECH500K", Description = "Giảm 1.000.000đ cho laptop cao cấp.", DiscountType = DiscountType.FixedAmount, DiscountValue = 1000000m, MinOrderValue = 25000000m, MaxDiscountValue = 1000000m, MaxUses = 300, MaxUsesPerUser = 1, UsedCount = 72, StartDate = CampaignStart, EndDate = CampaignEnd, Priority = 20, IsActive = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Voucher { Id = 4, Code = "SHOES15", Description = "Giảm 15% cho giày thể thao.", DiscountType = DiscountType.Percentage, DiscountValue = 15m, MinOrderValue = 1500000m, MaxDiscountValue = 900000m, MaxUses = 700, MaxUsesPerUser = 2, UsedCount = 210, StartDate = CampaignStart, EndDate = CampaignEnd, Priority = 12, IsActive = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Voucher { Id = 5, Code = "NEWUSER100", Description = "Giảm 100.000đ cho khách hàng mới.", DiscountType = DiscountType.FixedAmount, DiscountValue = 100000m, MinOrderValue = 500000m, MaxDiscountValue = 100000m, MaxUses = 2000, MaxUsesPerUser = 1, UsedCount = 640, StartDate = CampaignStart, EndDate = CampaignEnd, Priority = 3, IsActive = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt });

        modelBuilder.Entity<VoucherUser>().HasData(
            new VoucherUser { Id = 1, VoucherId = 1, UserId = 3, MaxUses = 1, UsedCount = 1, AssignedAt = CreatedAt },
            new VoucherUser { Id = 2, VoucherId = 2, UserId = 4, MaxUses = 3, UsedCount = 1, AssignedAt = CreatedAt },
            new VoucherUser { Id = 3, VoucherId = 3, UserId = 5, MaxUses = 1, UsedCount = 1, AssignedAt = CreatedAt },
            new VoucherUser { Id = 4, VoucherId = 4, UserId = 3, MaxUses = 2, UsedCount = 1, AssignedAt = CreatedAt },
            new VoucherUser { Id = 5, VoucherId = 5, UserId = 4, MaxUses = 1, UsedCount = 1, AssignedAt = CreatedAt });

        modelBuilder.Entity<VoucherUsage>().HasData(
            new VoucherUsage { Id = 1, VoucherId = 1, UserId = 3, OrderId = 1, UsedAt = new DateTime(2026, 5, 20, 9, 16, 0, DateTimeKind.Utc) },
            new VoucherUsage { Id = 2, VoucherId = 2, UserId = 4, OrderId = 2, UsedAt = new DateTime(2026, 5, 21, 10, 21, 0, DateTimeKind.Utc) },
            new VoucherUsage { Id = 3, VoucherId = 3, UserId = 5, OrderId = 3, UsedAt = new DateTime(2026, 5, 22, 14, 31, 0, DateTimeKind.Utc) },
            new VoucherUsage { Id = 4, VoucherId = 4, UserId = 3, OrderId = 4, UsedAt = new DateTime(2026, 5, 23, 16, 6, 0, DateTimeKind.Utc) },
            new VoucherUsage { Id = 5, VoucherId = 5, UserId = 4, OrderId = 5, UsedAt = new DateTime(2026, 5, 24, 11, 46, 0, DateTimeKind.Utc) });

        modelBuilder.Entity<VoucherTarget>().HasData(
            new VoucherTarget { Id = 1, VoucherId = 1, TargetType = TargetType.Category, TargetId = 2 },
            new VoucherTarget { Id = 2, VoucherId = 2, TargetType = TargetType.Category, TargetId = 5 },
            new VoucherTarget { Id = 3, VoucherId = 3, TargetType = TargetType.Product, TargetId = 3 },
            new VoucherTarget { Id = 4, VoucherId = 4, TargetType = TargetType.Brand, TargetId = 4 },
            new VoucherTarget { Id = 5, VoucherId = 5, TargetType = TargetType.User, TargetId = 4 });

        SeedCampaignsAndPromotions(modelBuilder);
    }

    private static void SeedCampaignsAndPromotions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Campaign>().HasData(
            new Campaign { Id = 1, Name = "Summer Tech 2026", Slug = "summer-tech-2026", Type = CampaignType.Seasonal, Description = "Chiến dịch công nghệ mùa hè.", StartDate = CampaignStart, EndDate = CampaignEnd, IsActive = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Campaign { Id = 2, Name = "Back To School", Slug = "back-to-school-2026", Type = CampaignType.Category, Description = "Laptop và phụ kiện cho mùa tựu trường.", StartDate = CampaignStart, EndDate = CampaignEnd, IsActive = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Campaign { Id = 3, Name = "Sneaker Week", Slug = "sneaker-week-2026", Type = CampaignType.FlashSale, Description = "Tuần lễ sneaker chính hãng.", StartDate = CampaignStart, EndDate = CampaignEnd, IsActive = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Campaign { Id = 4, Name = "Member Day", Slug = "member-day-2026", Type = CampaignType.Banner, Description = "Ưu đãi cho khách hàng thành viên.", StartDate = CampaignStart, EndDate = CampaignEnd, IsActive = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Campaign { Id = 5, Name = "Laptop Deals", Slug = "laptop-deals-2026", Type = CampaignType.Category, Description = "Ưu đãi laptop doanh nghiệp.", StartDate = CampaignStart, EndDate = CampaignEnd, IsActive = true, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt });

        modelBuilder.Entity<CampaignCategory>().HasData(
            new CampaignCategory { Id = 1, CampaignId = 1, CategoryId = 2, Position = 1, ImagePath = "/uploads/campaigns/summer-phone.jpg", Title = "Điện thoại mùa hè", Description = "Ưu đãi smartphone bán chạy." },
            new CampaignCategory { Id = 2, CampaignId = 2, CategoryId = 3, Position = 1, ImagePath = "/uploads/campaigns/back-to-school-laptop.jpg", Title = "Laptop tựu trường", Description = "Laptop mỏng nhẹ cho học tập." },
            new CampaignCategory { Id = 3, CampaignId = 3, CategoryId = 5, Position = 1, ImagePath = "/uploads/campaigns/sneaker-week.jpg", Title = "Sneaker Week", Description = "Giày thể thao chính hãng." },
            new CampaignCategory { Id = 4, CampaignId = 4, CategoryId = 1, Position = 1, ImagePath = "/uploads/campaigns/member-day.jpg", Title = "Ngày hội thành viên", Description = "Ưu đãi toàn sàn cho thành viên." },
            new CampaignCategory { Id = 5, CampaignId = 5, CategoryId = 3, Position = 2, ImagePath = "/uploads/campaigns/laptop-deals.jpg", Title = "Laptop Deals", Description = "Deal tốt cho laptop doanh nghiệp." });

        modelBuilder.Entity<Promotion>().HasData(
            new Promotion { Id = 1, Name = "Flash Sale Smartphone", Description = "Giảm trực tiếp cho điện thoại nổi bật.", Priority = 30, IsActive = true, StartDate = CampaignStart, EndDate = CampaignEnd, MinOrderValue = 10000000m, MaxDiscountValue = 1500000m, UsageLimit = 500, UsedCount = 84, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Promotion { Id = 2, Name = "Laptop Bundle", Description = "Mua laptop nhận ưu đãi phụ kiện.", Priority = 25, IsActive = true, StartDate = CampaignStart, EndDate = CampaignEnd, MinOrderValue = 25000000m, MaxDiscountValue = 2000000m, UsageLimit = 200, UsedCount = 31, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Promotion { Id = 3, Name = "Sneaker Buy 2", Description = "Mua 2 đôi giày giảm thêm.", Priority = 20, IsActive = true, StartDate = CampaignStart, EndDate = CampaignEnd, MinOrderValue = 3000000m, MaxDiscountValue = 1000000m, UsageLimit = 300, UsedCount = 57, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Promotion { Id = 4, Name = "Apple Premium Day", Description = "Ưu đãi riêng cho sản phẩm Apple.", Priority = 35, IsActive = true, StartDate = CampaignStart, EndDate = CampaignEnd, MinOrderValue = 20000000m, MaxDiscountValue = 1800000m, UsageLimit = 250, UsedCount = 49, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt },
            new Promotion { Id = 5, Name = "Samsung Loyalty", Description = "Ưu đãi cho khách mua Samsung.", Priority = 28, IsActive = true, StartDate = CampaignStart, EndDate = CampaignEnd, MinOrderValue = 15000000m, MaxDiscountValue = 1200000m, UsageLimit = 350, UsedCount = 63, CreatedAt = CreatedAt, UpdatedAt = UpdatedAt });

        modelBuilder.Entity<PromotionTarget>().HasData(
            new PromotionTarget { Id = 1, PromotionId = 1, TargetType = TargetType.Category, TargetId = 2 },
            new PromotionTarget { Id = 2, PromotionId = 2, TargetType = TargetType.Category, TargetId = 3 },
            new PromotionTarget { Id = 3, PromotionId = 3, TargetType = TargetType.Category, TargetId = 5 },
            new PromotionTarget { Id = 4, PromotionId = 4, TargetType = TargetType.Brand, TargetId = 1 },
            new PromotionTarget { Id = 5, PromotionId = 5, TargetType = TargetType.Brand, TargetId = 2 });

        modelBuilder.Entity<PromotionRule>().HasData(
            new PromotionRule { Id = 1, PromotionId = 1, GiftProductVariantId = null, ActionType = PromotionActionType.DiscountProduct, DiscountValue = 800000m, BuyQuantity = 1, GetQuantity = 0 },
            new PromotionRule { Id = 2, PromotionId = 2, GiftProductVariantId = 5, ActionType = PromotionActionType.GiftProduct, DiscountValue = 0m, BuyQuantity = 1, GetQuantity = 1 },
            new PromotionRule { Id = 3, PromotionId = 3, GiftProductVariantId = null, ActionType = PromotionActionType.BuyXGetY, DiscountValue = 500000m, BuyQuantity = 2, GetQuantity = 0 },
            new PromotionRule { Id = 4, PromotionId = 4, GiftProductVariantId = null, ActionType = PromotionActionType.DiscountProduct, DiscountValue = 1000000m, BuyQuantity = 1, GetQuantity = 0 },
            new PromotionRule { Id = 5, PromotionId = 5, GiftProductVariantId = null, ActionType = PromotionActionType.DiscountOrder, DiscountValue = 700000m, BuyQuantity = 1, GetQuantity = 0 });
    }
}
