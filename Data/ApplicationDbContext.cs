using e_commerce_web_admin.Data.Seed;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AttributeEntity = e_commerce_web_admin.Models.Entities.Attribute;

namespace e_commerce_web_admin.Data;

public class ApplicationDbContext : IdentityDbContext<Staff, IdentityRole<long>, long>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public new DbSet<User> Users => Set<User>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<CustomerConversation> CustomerConversations => Set<CustomerConversation>();
    public DbSet<CustomerMessage> CustomerMessages => Set<CustomerMessage>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantImage> ProductVariantImages => Set<ProductVariantImage>();
    public DbSet<Specification> Specifications => Set<Specification>();
    public DbSet<CategorySpecification> CategorySpecifications => Set<CategorySpecification>();
    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
    public DbSet<AttributeEntity> Attributes => Set<AttributeEntity>();
    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();
    public DbSet<CategoryVariantAttribute> CategoryVariantAttributes => Set<CategoryVariantAttribute>();
    public DbSet<VariantAttribute> VariantAttributes => Set<VariantAttribute>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<FulfillmentLocation> FulfillmentLocations => Set<FulfillmentLocation>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentPackage> ShipmentPackages => Set<ShipmentPackage>();
    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherUser> VoucherUsers => Set<VoucherUser>();
    public DbSet<VoucherUsage> VoucherUsages => Set<VoucherUsage>();
    public DbSet<VoucherTarget> VoucherTargets => Set<VoucherTarget>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignCategory> CampaignCategories => Set<CampaignCategory>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionTarget> PromotionTargets => Set<PromotionTarget>();
    public DbSet<PromotionRule> PromotionRules => Set<PromotionRule>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodReceiptItem> GoodReceiptItems => Set<GoodReceiptItem>();
    public DbSet<InventoryStockLot> InventoryStockLots => Set<InventoryStockLot>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<OrderItemCostAllocation> OrderItemCostAllocations => Set<OrderItemCostAllocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIdentity(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureCustomerMessages(modelBuilder);
        ConfigureCatalog(modelBuilder);
        ConfigureOrders(modelBuilder);
        ConfigureShipping(modelBuilder);
        ConfigureMarketing(modelBuilder);
        ConfigureInventory(modelBuilder);
        modelBuilder.SeedEcommerceData();

        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("staff");
            entity.Property(staff => staff.UserName).HasMaxLength(100).IsRequired();
            entity.Property(staff => staff.NormalizedUserName).HasMaxLength(100);
            entity.Property(staff => staff.Email).HasMaxLength(255).IsRequired();
            entity.Property(staff => staff.NormalizedEmail).HasMaxLength(255);
            entity.Property(staff => staff.PhoneNumber).HasMaxLength(30);
            entity.Property(staff => staff.FullName).HasMaxLength(255).IsRequired();
            entity.Property(staff => staff.AvatarImage).HasMaxLength(500);
        });

        modelBuilder.Entity<IdentityRole<long>>(entity =>
        {
            entity.ToTable("staff_roles");
            entity.Property(role => role.Name).HasMaxLength(100);
            entity.Property(role => role.NormalizedName).HasMaxLength(100);
        });

        modelBuilder.Entity<IdentityUserRole<long>>(entity => entity.ToTable("staff_user_roles"));
        modelBuilder.Entity<IdentityUserClaim<long>>(entity => entity.ToTable("staff_claims"));
        modelBuilder.Entity<IdentityUserLogin<long>>(entity => entity.ToTable("staff_logins"));
        modelBuilder.Entity<IdentityRoleClaim<long>>(entity => entity.ToTable("staff_role_claims"));
        modelBuilder.Entity<IdentityUserToken<long>>(entity => entity.ToTable("staff_tokens"));
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Username).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(255).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.FullName).HasMaxLength(255).IsRequired();
            entity.Property(user => user.Phone).HasMaxLength(30);
            entity.Property(user => user.Gender).HasConversion<string>().HasMaxLength(30);
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(30);
            entity.Property(user => user.AvatarImage).HasMaxLength(500);
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.ToTable("user_addresses");
            entity.HasIndex(address => new { address.UserId, address.IsDefault });
            entity.HasIndex(address => new { address.UserId, address.IsDeleted });
            entity.Property(address => address.ContactName).HasMaxLength(255).IsRequired();
            entity.Property(address => address.Phone).HasMaxLength(30).IsRequired();
            entity.Property(address => address.ProvinceCode).HasMaxLength(30).IsRequired();
            entity.Property(address => address.ProvinceName).HasMaxLength(120).IsRequired();
            entity.Property(address => address.DistrictCode).HasMaxLength(30);
            entity.Property(address => address.DistrictName).HasMaxLength(120);
            entity.Property(address => address.WardCode).HasMaxLength(30).IsRequired();
            entity.Property(address => address.WardName).HasMaxLength(120).IsRequired();
            entity.Property(address => address.DetailAddress).HasMaxLength(500).IsRequired();
            entity.Property(address => address.Latitude).HasPrecision(10, 7);
            entity.Property(address => address.Longitude).HasPrecision(10, 7);
            entity.Property(address => address.PlaceId).HasMaxLength(160);
            entity.Property(address => address.FormattedAddress).HasMaxLength(700);
            entity.Property(address => address.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(address => address.IsDeleted).HasDefaultValue(false);
            entity.HasOne(address => address.User)
                .WithMany(user => user.Addresses)
                .HasForeignKey(address => address.UserId);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("cart_items");
            entity.HasIndex(item => new { item.UserId, item.ProductVariantId }).IsUnique();
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.Property(item => item.DiscountValue).HasPrecision(18, 2);
            entity.HasOne(item => item.User)
                .WithMany(user => user.CartItems)
                .HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.ProductVariant)
                .WithMany(variant => variant.CartItems)
                .HasForeignKey(item => item.ProductVariantId);
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.ToTable("wishlist");
            entity.HasIndex(item => new { item.UserId, item.ProductVariantId }).IsUnique();
            entity.HasOne(item => item.User)
                .WithMany(user => user.Wishlists)
                .HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.ProductVariant)
                .WithMany(variant => variant.Wishlists)
                .HasForeignKey(item => item.ProductVariantId);
        });
    }

    private static void ConfigureCustomerMessages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerConversation>(entity =>
        {
            entity.ToTable("customer_conversations");
            entity.HasIndex(conversation => new { conversation.UserId, conversation.Status });
            entity.HasIndex(conversation => new { conversation.UserId, conversation.Channel, conversation.LastMessageAt });
            entity.HasIndex(conversation => conversation.AssignedStaffId);
            entity.HasIndex(conversation => conversation.LastMessageAt);
            entity.Property(conversation => conversation.Subject).HasMaxLength(255);
            entity.Property(conversation => conversation.Channel)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(CustomerConversationChannel.Support);
            entity.Property(conversation => conversation.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(conversation => conversation.User)
                .WithMany(user => user.CustomerConversations)
                .HasForeignKey(conversation => conversation.UserId);
            entity.HasOne(conversation => conversation.AssignedStaff)
                .WithMany(staff => staff.AssignedCustomerConversations)
                .HasForeignKey(conversation => conversation.AssignedStaffId);
        });

        modelBuilder.Entity<CustomerMessage>(entity =>
        {
            entity.ToTable("customer_messages");
            entity.HasIndex(message => new { message.ConversationId, message.CreatedAt });
            entity.HasIndex(message => new { message.ConversationId, message.Sender, message.IsReadByAdmin });
            entity.HasIndex(message => new { message.UserId, message.Sender, message.ClientMessageId })
                .IsUnique()
                .HasFilter("[ClientMessageId] IS NOT NULL AND [UserId] IS NOT NULL");
            entity.HasIndex(message => new { message.StaffId, message.Sender, message.ClientMessageId })
                .IsUnique()
                .HasFilter("[ClientMessageId] IS NOT NULL AND [StaffId] IS NOT NULL");
            entity.HasIndex(message => message.Sender);
            entity.HasIndex(message => message.AiResponseId)
                .IsUnique()
                .HasFilter("[AiResponseId] IS NOT NULL");
            entity.HasIndex(message => message.UserId);
            entity.HasIndex(message => message.StaffId);
            entity.Property(message => message.Sender).HasConversion<string>().HasMaxLength(30);
            entity.Property(message => message.ClientMessageId).HasMaxLength(64).IsUnicode(false);
            entity.Property(message => message.Body).IsRequired();
            entity.Property(message => message.AiProvider).HasMaxLength(80);
            entity.Property(message => message.AiModel).HasMaxLength(120);
            entity.Property(message => message.AiResponseId).HasMaxLength(160);
            entity.HasOne(message => message.Conversation)
                .WithMany(conversation => conversation.Messages)
                .HasForeignKey(message => message.ConversationId);
            entity.HasOne(message => message.User)
                .WithMany(user => user.CustomerMessages)
                .HasForeignKey(message => message.UserId);
            entity.HasOne(message => message.Staff)
                .WithMany(staff => staff.CustomerMessages)
                .HasForeignKey(message => message.StaffId);
        });
    }

    private static void ConfigureCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("brands");
            entity.HasIndex(brand => brand.Slug).IsUnique();
            entity.Property(brand => brand.Name).HasMaxLength(255).IsRequired();
            entity.Property(brand => brand.Description).HasMaxLength(1000);
            entity.Property(brand => brand.ImagePath).HasMaxLength(500);
            entity.Property(brand => brand.Slug).HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasIndex(category => category.Slug).IsUnique();
            entity.Property(category => category.Name).HasMaxLength(255).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(1000);
            entity.Property(category => category.ImagePath).HasMaxLength(500);
            entity.Property(category => category.Slug).HasMaxLength(255).IsRequired();
            entity.HasOne(category => category.Parent)
                .WithMany(category => category.Children)
                .HasForeignKey(category => category.ParentId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasIndex(product => product.Slug).IsUnique();
            entity.Property(product => product.Name).HasMaxLength(255).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(4000);
            entity.Property(product => product.Slug).HasMaxLength(255).IsRequired();
            entity.Property(product => product.RatingAverage).HasPrecision(3, 2);
            entity.HasOne(product => product.Brand)
                .WithMany(brand => brand.Products)
                .HasForeignKey(product => product.BrandId);
            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId);
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("product_variants");
            entity.HasIndex(variant => variant.Code).IsUnique();
            entity.Property(variant => variant.Code).HasMaxLength(80).IsRequired();
            entity.Property(variant => variant.Price).HasPrecision(18, 2);
            entity.Property(variant => variant.AverageCost).HasPrecision(18, 2);
            entity.Property(variant => variant.ColorName).HasMaxLength(120);
            entity.Property(variant => variant.ColorHex).HasMaxLength(7).IsUnicode(false);
            entity.HasOne(variant => variant.Product)
                .WithMany(product => product.ProductVariants)
                .HasForeignKey(variant => variant.ProductId);
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

        modelBuilder.Entity<Specification>(entity =>
        {
            entity.ToTable("specifications");
            entity.HasIndex(specification => specification.Key).IsUnique();
            entity.Property(specification => specification.Key).HasMaxLength(100).IsRequired();
            entity.Property(specification => specification.Name).HasMaxLength(255).IsRequired();
            entity.Property(specification => specification.Unit).HasMaxLength(50);
            entity.Property(specification => specification.Icon).HasMaxLength(100);
        });

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

        modelBuilder.Entity<AttributeEntity>(entity =>
        {
            entity.ToTable("attributes");
            entity.HasIndex(attribute => attribute.Code).IsUnique();
            entity.Property(attribute => attribute.Code).HasMaxLength(80).IsRequired();
            entity.Property(attribute => attribute.Name).HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<AttributeOption>(entity =>
        {
            entity.ToTable("attribute_options");
            entity.HasIndex(option => new { option.AttributeId, option.Value }).IsUnique();
            entity.Property(option => option.Value).HasMaxLength(120).IsRequired();
            entity.Property(option => option.Label).HasMaxLength(255).IsRequired();
            entity.HasOne(option => option.Attribute)
                .WithMany(attribute => attribute.AttributeOptions)
                .HasForeignKey(option => option.AttributeId);
        });

        modelBuilder.Entity<CategoryVariantAttribute>(entity =>
        {
            entity.ToTable("category_variant_attributes");
            entity.HasKey(item => new { item.CategoryId, item.AttributeId });
            entity.HasOne(item => item.Category)
                .WithMany(category => category.CategoryVariantAttributes)
                .HasForeignKey(item => item.CategoryId);
            entity.HasOne(item => item.Attribute)
                .WithMany(attribute => attribute.CategoryVariantAttributes)
                .HasForeignKey(item => item.AttributeId);
        });

        modelBuilder.Entity<VariantAttribute>(entity =>
        {
            entity.ToTable("variant_attributes");
            entity.HasKey(item => new { item.ProductVariantId, item.AttributeOptionId });
            entity.HasOne(item => item.ProductVariant)
                .WithMany(variant => variant.VariantAttributes)
                .HasForeignKey(item => item.ProductVariantId);
            entity.HasOne(item => item.AttributeOption)
                .WithMany(option => option.VariantAttributes)
                .HasForeignKey(item => item.AttributeOptionId);
        });
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.ToTable("payment_methods");
            entity.Property(method => method.Name).HasMaxLength(120).IsRequired();
            entity.Property(method => method.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasIndex(order => order.OrderCode).IsUnique();
            entity.Property(order => order.OrderCode).HasMaxLength(50).IsRequired();
            entity.Property(order => order.ShippingContactName).HasMaxLength(255).IsRequired();
            entity.Property(order => order.ShippingPhone).HasMaxLength(30).IsRequired();
            entity.Property(order => order.ShippingProvince).HasMaxLength(120).IsRequired();
            entity.Property(order => order.ShippingWard).HasMaxLength(120).IsRequired();
            entity.Property(order => order.ShippingDetail).HasMaxLength(500).IsRequired();
            entity.Property(order => order.SubtotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.ShippingFee).HasPrecision(18, 2);
            entity.Property(order => order.VoucherDiscount).HasPrecision(18, 2);
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.OrderStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(order => order.PaymentStatus).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(order => order.User)
                .WithMany(user => user.Orders)
                .HasForeignKey(order => order.UserId);
            entity.HasOne(order => order.PaymentMethod)
                .WithMany(method => method.Orders)
                .HasForeignKey(order => order.PaymentMethodId);
            entity.HasOne(order => order.Voucher)
                .WithMany(voucher => voucher.Orders)
                .HasForeignKey(order => order.VoucherId);
            entity.HasOne(order => order.ShippingAddress)
                .WithMany(address => address.Orders)
                .HasForeignKey(order => order.ShippingAddressId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.Property(item => item.UnitCost).HasPrecision(18, 2);
            entity.HasOne(item => item.Order)
                .WithMany(order => order.OrderItems)
                .HasForeignKey(item => item.OrderId);
            entity.HasOne(item => item.ProductVariant)
                .WithMany(variant => variant.OrderItems)
                .HasForeignKey(item => item.ProductVariantId);
        });

        modelBuilder.Entity<OrderItemCostAllocation>(entity =>
        {
            entity.ToTable("order_item_cost_allocations");
            entity.HasIndex(allocation => allocation.OrderItemId);
            entity.HasIndex(allocation => allocation.StockLotId);
            entity.Property(allocation => allocation.UnitCost).HasPrecision(18, 2);
            entity.HasOne(allocation => allocation.OrderItem)
                .WithMany(item => item.CostAllocations)
                .HasForeignKey(allocation => allocation.OrderItemId);
            entity.HasOne(allocation => allocation.StockLot)
                .WithMany(lot => lot.OrderItemCostAllocations)
                .HasForeignKey(allocation => allocation.StockLotId);
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.ToTable("ratings");
            entity.HasIndex(rating => rating.OrderItemId).IsUnique();
            entity.Property(rating => rating.Comment).HasMaxLength(1000);
            entity.HasOne(rating => rating.OrderItem)
                .WithOne(orderItem => orderItem.Rating)
                .HasForeignKey<Rating>(rating => rating.OrderItemId);
            entity.HasOne(rating => rating.User)
                .WithMany(user => user.Ratings)
                .HasForeignKey(rating => rating.UserId);
        });
    }

    private static void ConfigureShipping(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FulfillmentLocation>(entity =>
        {
            entity.ToTable("fulfillment_locations");
            entity.HasIndex(location => location.IsDefault)
                .IsUnique()
                .HasFilter("[IsDefault] = 1");
            entity.HasIndex(location => location.IsActive);
            entity.Property(location => location.Name).HasMaxLength(255).IsRequired();
            entity.Property(location => location.ContactName).HasMaxLength(255).IsRequired();
            entity.Property(location => location.Phone).HasMaxLength(30).IsRequired();
            entity.Property(location => location.ProvinceCode).HasMaxLength(30);
            entity.Property(location => location.ProvinceName).HasMaxLength(120).IsRequired();
            entity.Property(location => location.DistrictCode).HasMaxLength(30);
            entity.Property(location => location.DistrictName).HasMaxLength(120);
            entity.Property(location => location.WardCode).HasMaxLength(30);
            entity.Property(location => location.WardName).HasMaxLength(120).IsRequired();
            entity.Property(location => location.DetailAddress).HasMaxLength(500).IsRequired();
            entity.Property(location => location.FormattedAddress).HasMaxLength(700);
            entity.Property(location => location.Latitude).HasPrecision(10, 7);
            entity.Property(location => location.Longitude).HasPrecision(10, 7);
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("shipments");
            entity.HasIndex(shipment => shipment.OrderId);
            entity.HasIndex(shipment => shipment.Status);
            entity.HasIndex(shipment => new { shipment.OrderId, shipment.Provider })
                .IsUnique()
                .HasDatabaseName("IX_shipments_OrderId_Provider_Open")
                .HasFilter("[ProviderDeliveryId] IS NULL");
            entity.HasIndex(shipment => new { shipment.Provider, shipment.ProviderDeliveryId })
                .IsUnique()
                .HasFilter("[ProviderDeliveryId] IS NOT NULL");
            entity.Property(shipment => shipment.Provider).HasConversion<string>().HasMaxLength(30);
            entity.Property(shipment => shipment.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(shipment => shipment.ProviderDeliveryId).HasMaxLength(160);
            entity.Property(shipment => shipment.ProviderQuoteId).HasMaxLength(160);
            entity.Property(shipment => shipment.ProviderStatus).HasMaxLength(80);
            entity.Property(shipment => shipment.TrackingUrl).HasMaxLength(1000);
            entity.Property(shipment => shipment.PickupContactName).HasMaxLength(255).IsRequired();
            entity.Property(shipment => shipment.PickupPhone).HasMaxLength(30).IsRequired();
            entity.Property(shipment => shipment.PickupDetailAddress).HasMaxLength(500);
            entity.Property(shipment => shipment.PickupAddress).HasMaxLength(700).IsRequired();
            entity.Property(shipment => shipment.PickupLatitude).HasPrecision(10, 7);
            entity.Property(shipment => shipment.PickupLongitude).HasPrecision(10, 7);
            entity.Property(shipment => shipment.ProviderPickupProvinceCode).HasMaxLength(50);
            entity.Property(shipment => shipment.ProviderPickupProvinceName).HasMaxLength(120);
            entity.Property(shipment => shipment.ProviderPickupDistrictCode).HasMaxLength(50);
            entity.Property(shipment => shipment.ProviderPickupDistrictName).HasMaxLength(120);
            entity.Property(shipment => shipment.ProviderPickupWardCode).HasMaxLength(50);
            entity.Property(shipment => shipment.ProviderPickupWardName).HasMaxLength(120);
            entity.Property(shipment => shipment.DropoffContactName).HasMaxLength(255).IsRequired();
            entity.Property(shipment => shipment.DropoffPhone).HasMaxLength(30).IsRequired();
            entity.Property(shipment => shipment.DropoffDetailAddress).HasMaxLength(500);
            entity.Property(shipment => shipment.DropoffAddress).HasMaxLength(700).IsRequired();
            entity.Property(shipment => shipment.DropoffLatitude).HasPrecision(10, 7);
            entity.Property(shipment => shipment.DropoffLongitude).HasPrecision(10, 7);
            entity.Property(shipment => shipment.ProviderDropoffProvinceCode).HasMaxLength(50);
            entity.Property(shipment => shipment.ProviderDropoffProvinceName).HasMaxLength(120);
            entity.Property(shipment => shipment.ProviderDropoffDistrictCode).HasMaxLength(50);
            entity.Property(shipment => shipment.ProviderDropoffDistrictName).HasMaxLength(120);
            entity.Property(shipment => shipment.ProviderDropoffWardCode).HasMaxLength(50);
            entity.Property(shipment => shipment.ProviderDropoffWardName).HasMaxLength(120);
            entity.Property(shipment => shipment.QuotedFee).HasPrecision(18, 2);
            entity.Property(shipment => shipment.ActualFee).HasPrecision(18, 2);
            entity.Property(shipment => shipment.Currency).HasMaxLength(3).IsRequired();
            entity.Property(shipment => shipment.FailureReason).HasMaxLength(1000);
            entity.HasOne(shipment => shipment.Order)
                .WithMany(order => order.Shipments)
                .HasForeignKey(shipment => shipment.OrderId);
            entity.HasOne(shipment => shipment.FulfillmentLocation)
                .WithMany(location => location.Shipments)
                .HasForeignKey(shipment => shipment.FulfillmentLocationId);
            entity.HasOne(shipment => shipment.RequestedByStaff)
                .WithMany(staff => staff.RequestedShipments)
                .HasForeignKey(shipment => shipment.RequestedByStaffId);
        });

        modelBuilder.Entity<ShipmentPackage>(entity =>
        {
            entity.ToTable("shipment_packages");
            entity.HasIndex(package => new { package.ShipmentId, package.Sequence }).IsUnique();
            entity.Property(package => package.Description).HasMaxLength(500).IsRequired();
            entity.Property(package => package.LengthCm).HasPrecision(8, 2);
            entity.Property(package => package.WidthCm).HasPrecision(8, 2);
            entity.Property(package => package.HeightCm).HasPrecision(8, 2);
            entity.Property(package => package.DeclaredValue).HasPrecision(18, 2);
            entity.Property(package => package.Notes).HasMaxLength(1000);
            entity.HasOne(package => package.Shipment)
                .WithMany(shipment => shipment.Packages)
                .HasForeignKey(package => package.ShipmentId);
        });

        modelBuilder.Entity<ShipmentEvent>(entity =>
        {
            entity.ToTable("shipment_events");
            entity.HasIndex(shipmentEvent => shipmentEvent.ShipmentId);
            entity.HasIndex(shipmentEvent => shipmentEvent.ProviderEventId)
                .IsUnique()
                .HasFilter("[ProviderEventId] IS NOT NULL");
            entity.Property(shipmentEvent => shipmentEvent.ProviderEventId).HasMaxLength(160);
            entity.Property(shipmentEvent => shipmentEvent.ProviderStatus).HasMaxLength(80);
            entity.Property(shipmentEvent => shipmentEvent.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(shipmentEvent => shipmentEvent.Message).HasMaxLength(1000);
            entity.Property(shipmentEvent => shipmentEvent.DriverName).HasMaxLength(255);
            entity.Property(shipmentEvent => shipmentEvent.DriverPhone).HasMaxLength(30);
            entity.Property(shipmentEvent => shipmentEvent.VehiclePlate).HasMaxLength(50);
            entity.HasOne(shipmentEvent => shipmentEvent.Shipment)
                .WithMany(shipment => shipment.Events)
                .HasForeignKey(shipmentEvent => shipmentEvent.ShipmentId);
        });
    }

    private static void ConfigureMarketing(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.ToTable("vouchers");
            entity.HasIndex(voucher => voucher.Code).IsUnique();
            entity.Property(voucher => voucher.Code).HasMaxLength(80).IsRequired();
            entity.Property(voucher => voucher.Description).HasMaxLength(1000);
            entity.Property(voucher => voucher.DiscountType).HasConversion<string>().HasMaxLength(30);
            entity.Property(voucher => voucher.DiscountValue).HasPrecision(18, 2);
            entity.Property(voucher => voucher.MinOrderValue).HasPrecision(18, 2);
            entity.Property(voucher => voucher.MaxDiscountValue).HasPrecision(18, 2);
        });

        modelBuilder.Entity<VoucherUser>(entity =>
        {
            entity.ToTable("voucher_users");
            entity.HasIndex(item => new { item.VoucherId, item.UserId }).IsUnique();
            entity.HasOne(item => item.Voucher)
                .WithMany(voucher => voucher.VoucherUsers)
                .HasForeignKey(item => item.VoucherId);
            entity.HasOne(item => item.User)
                .WithMany(user => user.VoucherUsers)
                .HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<VoucherUsage>(entity =>
        {
            entity.ToTable("voucher_usages");
            entity.HasIndex(item => item.OrderId).IsUnique();
            entity.HasOne(item => item.Voucher)
                .WithMany(voucher => voucher.VoucherUsages)
                .HasForeignKey(item => item.VoucherId);
            entity.HasOne(item => item.User)
                .WithMany(user => user.VoucherUsages)
                .HasForeignKey(item => item.UserId);
            entity.HasOne(item => item.Order)
                .WithMany(order => order.VoucherUsages)
                .HasForeignKey(item => item.OrderId);
        });

        modelBuilder.Entity<VoucherTarget>(entity =>
        {
            entity.ToTable("voucher_targets");
            entity.HasIndex(item => new { item.VoucherId, item.TargetType, item.TargetId }).IsUnique();
            entity.Property(item => item.TargetType).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(item => item.Voucher)
                .WithMany(voucher => voucher.VoucherTargets)
                .HasForeignKey(item => item.VoucherId);
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.ToTable("campaigns");
            entity.HasIndex(campaign => campaign.Slug).IsUnique();
            entity.Property(campaign => campaign.Name).HasMaxLength(255).IsRequired();
            entity.Property(campaign => campaign.Slug).HasMaxLength(255).IsRequired();
            entity.Property(campaign => campaign.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(campaign => campaign.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<CampaignCategory>(entity =>
        {
            entity.ToTable("campaign_categories");
            entity.Property(item => item.ImagePath).HasMaxLength(500);
            entity.Property(item => item.Title).HasMaxLength(255);
            entity.Property(item => item.Description).HasMaxLength(1000);
            entity.HasOne(item => item.Campaign)
                .WithMany(campaign => campaign.CampaignCategories)
                .HasForeignKey(item => item.CampaignId);
            entity.HasOne(item => item.Category)
                .WithMany(category => category.CampaignCategories)
                .HasForeignKey(item => item.CategoryId);
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("promotions");
            entity.Property(promotion => promotion.Name).HasMaxLength(255).IsRequired();
            entity.Property(promotion => promotion.Description).HasMaxLength(1000);
            entity.Property(promotion => promotion.MinOrderValue).HasPrecision(18, 2);
            entity.Property(promotion => promotion.MaxDiscountValue).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PromotionTarget>(entity =>
        {
            entity.ToTable("promotion_targets");
            entity.HasIndex(item => new { item.PromotionId, item.TargetType, item.TargetId }).IsUnique();
            entity.Property(item => item.TargetType).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(item => item.Promotion)
                .WithMany(promotion => promotion.PromotionTargets)
                .HasForeignKey(item => item.PromotionId);
        });

        modelBuilder.Entity<PromotionRule>(entity =>
        {
            entity.ToTable("promotion_rules");
            entity.Property(rule => rule.ActionType).HasConversion<string>().HasMaxLength(50);
            entity.Property(rule => rule.DiscountValue).HasPrecision(18, 2);
            entity.HasOne(rule => rule.Promotion)
                .WithMany(promotion => promotion.PromotionRules)
                .HasForeignKey(rule => rule.PromotionId);
            entity.HasOne(rule => rule.GiftProductVariant)
                .WithMany(variant => variant.GiftPromotionRules)
                .HasForeignKey(rule => rule.GiftProductVariantId);
        });
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("suppliers");
            entity.Property(supplier => supplier.Name).HasMaxLength(255).IsRequired();
            entity.Property(supplier => supplier.Phone).HasMaxLength(30);
            entity.Property(supplier => supplier.Email).HasMaxLength(100);
            entity.Property(supplier => supplier.Address).HasMaxLength(500);
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable("goods_receipts");
            entity.HasIndex(receipt => receipt.ReceiptCode).IsUnique();
            entity.Property(receipt => receipt.ReceiptCode).HasMaxLength(50).IsRequired();
            entity.Property(receipt => receipt.TotalAmount).HasPrecision(18, 2);
            entity.Property(receipt => receipt.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(receipt => receipt.Supplier)
                .WithMany(supplier => supplier.GoodsReceipts)
                .HasForeignKey(receipt => receipt.SupplierId);
            entity.HasOne(receipt => receipt.FulfillmentLocation)
                .WithMany()
                .HasForeignKey(receipt => receipt.FulfillmentLocationId);
            entity.HasOne(receipt => receipt.CreatedByStaff)
                .WithMany(staff => staff.CreatedGoodsReceipts)
                .HasForeignKey(receipt => receipt.CreatedBy);
            entity.HasOne(receipt => receipt.ApprovedByStaff)
                .WithMany(staff => staff.ApprovedGoodsReceipts)
                .HasForeignKey(receipt => receipt.ApprovedBy);
        });

        modelBuilder.Entity<GoodReceiptItem>(entity =>
        {
            entity.ToTable("good_receipt_items");
            entity.Property(item => item.ImportPrice).HasPrecision(18, 2);
            entity.HasOne(item => item.GoodsReceipt)
                .WithMany(receipt => receipt.GoodReceiptItems)
                .HasForeignKey(item => item.GoodsReceiptId);
            entity.HasOne(item => item.ProductVariant)
                .WithMany(variant => variant.GoodReceiptItems)
                .HasForeignKey(item => item.ProductVariantId);
        });

        modelBuilder.Entity<InventoryStockLot>(entity =>
        {
            entity.ToTable("inventory_stock_lots");
            entity.HasIndex(lot => lot.ProductVariantId);
            entity.HasIndex(lot => lot.FulfillmentLocationId);
            entity.HasIndex(lot => lot.GoodReceiptItemId)
                .IsUnique()
                .HasFilter("[GoodReceiptItemId] IS NOT NULL");
            entity.HasIndex(lot => lot.LotCode).IsUnique();
            entity.Property(lot => lot.LotCode).HasMaxLength(80).IsRequired();
            entity.Property(lot => lot.UnitCost).HasPrecision(18, 2);
            entity.HasOne(lot => lot.ProductVariant)
                .WithMany(variant => variant.InventoryStockLots)
                .HasForeignKey(lot => lot.ProductVariantId);
            entity.HasOne(lot => lot.FulfillmentLocation)
                .WithMany()
                .HasForeignKey(lot => lot.FulfillmentLocationId);
            entity.HasOne(lot => lot.GoodReceiptItem)
                .WithMany(item => item.InventoryStockLots)
                .HasForeignKey(lot => lot.GoodReceiptItemId);
        });

        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.ToTable("inventory_balances");
            entity.HasIndex(balance => new { balance.ProductVariantId, balance.FulfillmentLocationId })
                .IsUnique()
                .HasFilter(null);
            entity.Property(balance => balance.AverageCost).HasPrecision(18, 2);
            entity.Property(balance => balance.RowVersion).IsRowVersion();
            entity.Ignore(balance => balance.AvailableQuantity);
            entity.HasOne(balance => balance.ProductVariant)
                .WithMany(variant => variant.InventoryBalances)
                .HasForeignKey(balance => balance.ProductVariantId);
            entity.HasOne(balance => balance.FulfillmentLocation)
                .WithMany()
                .HasForeignKey(balance => balance.FulfillmentLocationId);
        });

        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.ToTable("inventory_movements");
            entity.HasIndex(movement => new { movement.ProductVariantId, movement.OccurredAt });
            entity.HasIndex(movement => movement.FulfillmentLocationId);
            entity.HasIndex(movement => movement.StockLotId);
            entity.HasIndex(movement => new { movement.ReferenceType, movement.ReferenceId });
            entity.Property(movement => movement.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(movement => movement.UnitCost).HasPrecision(18, 2);
            entity.Property(movement => movement.TotalCost).HasPrecision(18, 2);
            entity.Property(movement => movement.ReferenceType).HasMaxLength(80).IsRequired();
            entity.Property(movement => movement.Note).HasMaxLength(500);
            entity.HasOne(movement => movement.ProductVariant)
                .WithMany(variant => variant.InventoryMovements)
                .HasForeignKey(movement => movement.ProductVariantId);
            entity.HasOne(movement => movement.FulfillmentLocation)
                .WithMany()
                .HasForeignKey(movement => movement.FulfillmentLocationId);
            entity.HasOne(movement => movement.StockLot)
                .WithMany(lot => lot.InventoryMovements)
                .HasForeignKey(movement => movement.StockLotId);
        });
    }

}
