using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attributes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categories_categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_methods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "promotions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MinOrderValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxDiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "specifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AvatarImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vouchers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiscountType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinOrderValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxDiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxUses = table.Column<int>(type: "int", nullable: true),
                    MaxUsesPerUser = table.Column<int>(type: "int", nullable: true),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vouchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "attribute_options",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attribute_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attribute_options_attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "campaign_categories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<long>(type: "bigint", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campaign_categories_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_campaign_categories_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "category_variant_attributes",
                columns: table => new
                {
                    AttributeId = table.Column<long>(type: "bigint", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_variant_attributes", x => new { x.CategoryId, x.AttributeId });
                    table.ForeignKey(
                        name: "FK_category_variant_attributes_attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_category_variant_attributes_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandId = table.Column<long>(type: "bigint", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ViewsCount = table.Column<int>(type: "int", nullable: false),
                    TotalSoldCount = table.Column<int>(type: "int", nullable: false),
                    RatingAverage = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    RatingCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_products_brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_targets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromotionId = table.Column<long>(type: "bigint", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_targets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_targets_promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "category_specifications",
                columns: table => new
                {
                    SpecificationId = table.Column<long>(type: "bigint", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_specifications", x => new { x.CategoryId, x.SpecificationId });
                    table.ForeignKey(
                        name: "FK_category_specifications_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_category_specifications_specifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "specifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_addresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProvinceCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProvinceName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WardCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WardName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DetailAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_addresses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "voucher_targets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher_targets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_voucher_targets_vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "voucher_users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    MaxUses = table.Column<int>(type: "int", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_voucher_users_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_users_vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_color_images",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_color_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_color_images_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_specifications",
                columns: table => new
                {
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    SpecificationId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsHighlight = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_specifications", x => new { x.ProductId, x.SpecificationId });
                    table.ForeignKey(
                        name: "FK_product_specifications_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_specifications_specifications_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "specifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SoldCount = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_variants_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentMethodId = table.Column<long>(type: "bigint", nullable: false),
                    VoucherId = table.Column<long>(type: "bigint", nullable: true),
                    OrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShippingAddressId = table.Column<long>(type: "bigint", nullable: true),
                    ShippingContactName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ShippingPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ShippingProvince = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ShippingWard = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ShippingDetail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VoucherDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OrderStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_orders_payment_methods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "payment_methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_user_addresses_ShippingAddressId",
                        column: x => x.ShippingAddressId,
                        principalTable: "user_addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cart_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ProductVariantId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cart_items_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cart_items_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_rules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromotionId = table.Column<long>(type: "bigint", nullable: false),
                    GiftProductVariantId = table.Column<long>(type: "bigint", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BuyQuantity = table.Column<int>(type: "int", nullable: false),
                    GetQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_rules_product_variants_GiftProductVariantId",
                        column: x => x.GiftProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotion_rules_promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "variant_attributes",
                columns: table => new
                {
                    ProductVariantId = table.Column<long>(type: "bigint", nullable: false),
                    AttributeOptionId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variant_attributes", x => new { x.ProductVariantId, x.AttributeOptionId });
                    table.ForeignKey(
                        name: "FK_variant_attributes_attribute_options_AttributeOptionId",
                        column: x => x.AttributeOptionId,
                        principalTable: "attribute_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_variant_attributes_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wishlist",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ProductVariantId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wishlist_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wishlist_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    ProductVariantId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_items_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "voucher_usages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher_usages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_voucher_usages_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_usages_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_usages_vouchers_VoucherId",
                        column: x => x.VoucherId,
                        principalTable: "vouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ratings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Stars = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ratings_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ratings_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "attributes",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { 1L, "color", "Màu sắc" },
                    { 2L, "storage", "Dung lượng" },
                    { 3L, "size", "Kích thước" },
                    { 4L, "processor", "Bộ xử lý" },
                    { 5L, "condition", "Tình trạng" }
                });

            migrationBuilder.InsertData(
                table: "brands",
                columns: new[] { "Id", "CreatedAt", "Description", "ImagePath", "IsActive", "Name", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thiết bị di động và laptop cao cấp.", "/uploads/brands/apple.png", true, "Apple", "apple", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Điện thoại và thiết bị thông minh.", "/uploads/brands/samsung.png", true, "Samsung", "samsung", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Laptop văn phòng và doanh nghiệp.", "/uploads/brands/dell.png", true, "Dell", "dell", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Giày và thời trang thể thao.", "/uploads/brands/nike.png", true, "Nike", "nike", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Giày chạy bộ và trang phục thể thao.", "/uploads/brands/adidas.png", true, "Adidas", "adidas", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "campaigns",
                columns: new[] { "Id", "CreatedAt", "Description", "EndDate", "IsActive", "Name", "Slug", "StartDate", "Type", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Chiến dịch công nghệ mùa hè.", new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, "Summer Tech 2026", "summer-tech-2026", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seasonal", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Laptop và phụ kiện cho mùa tựu trường.", new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, "Back To School", "back-to-school-2026", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Category", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tuần lễ sneaker chính hãng.", new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, "Sneaker Week", "sneaker-week-2026", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "FlashSale", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ưu đãi cho khách hàng thành viên.", new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, "Member Day", "member-day-2026", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Banner", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ưu đãi laptop doanh nghiệp.", new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, "Laptop Deals", "laptop-deals-2026", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Category", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "CreatedAt", "Description", "ImagePath", "IsActive", "Name", "ParentId", "Position", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thiết bị công nghệ và phụ kiện.", "/uploads/categories/electronics.jpg", true, "Điện tử", null, 1, "dien-tu", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thời trang nam nữ và phụ kiện.", "/uploads/categories/fashion.jpg", true, "Thời trang", null, 4, "thoi-trang", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "payment_methods",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1L, "COD tại địa chỉ giao hàng.", true, "Thanh toán khi nhận hàng" },
                    { 2L, "Thanh toán qua tài khoản ngân hàng.", true, "Chuyển khoản ngân hàng" },
                    { 3L, "Thanh toán bằng thẻ quốc tế.", true, "Thẻ Visa/Mastercard" },
                    { 4L, "Thanh toán qua ví điện tử MoMo.", true, "Ví MoMo" },
                    { 5L, "Thanh toán qua cổng VNPAY QR.", true, "VNPAY" }
                });

            migrationBuilder.InsertData(
                table: "promotions",
                columns: new[] { "Id", "CreatedAt", "Description", "EndDate", "IsActive", "MaxDiscountValue", "MinOrderValue", "Name", "Priority", "StartDate", "UpdatedAt", "UsageLimit", "UsedCount" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Giảm trực tiếp cho điện thoại nổi bật.", new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 1500000m, 10000000m, "Flash Sale Smartphone", 30, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 500, 84 },
                    { 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mua laptop nhận ưu đãi phụ kiện.", new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 2000000m, 25000000m, "Laptop Bundle", 25, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 200, 31 },
                    { 3L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mua 2 đôi giày giảm thêm.", new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 1000000m, 3000000m, "Sneaker Buy 2", 20, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 300, 57 },
                    { 4L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ưu đãi riêng cho sản phẩm Apple.", new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 1800000m, 20000000m, "Apple Premium Day", 35, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 250, 49 },
                    { 5L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ưu đãi cho khách mua Samsung.", new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 1200000m, 15000000m, "Samsung Loyalty", 28, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 350, 63 }
                });

            migrationBuilder.InsertData(
                table: "specifications",
                columns: new[] { "Id", "Icon", "Key", "Name", "Unit" },
                values: new object[,]
                {
                    { 1L, "monitor", "screen_size", "Kích thước màn hình", "inch" },
                    { 2L, "hard-drive", "storage", "Dung lượng lưu trữ", "GB" },
                    { 3L, "memory-stick", "ram", "Bộ nhớ RAM", "GB" },
                    { 4L, "layers", "material", "Chất liệu", null },
                    { 5L, "battery", "battery", "Dung lượng pin", "mAh" }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "AvatarImage", "CreatedAt", "Email", "FullName", "Gender", "IsActive", "PasswordHash", "Phone", "Role", "UpdatedAt", "Username" },
                values: new object[,]
                {
                    { 1L, "/uploads/avatars/admin.png", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@ecommerce.local", "Nguyễn Minh Admin", "Male", true, "sample_hash_admin_2026", "0901000001", "Admin", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), "admin" },
                    { 2L, "/uploads/avatars/staff-an.png", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "an.staff@ecommerce.local", "Trần Hoàng An", "Male", true, "sample_hash_staff_2026", "0901000002", "Staff", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), "staff.an" },
                    { 3L, "/uploads/avatars/lan.png", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "lan.nguyen@example.com", "Nguyễn Thảo Lan", "Female", true, "sample_hash_customer_3", "0901000003", "Customer", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), "lan.nguyen" },
                    { 4L, "/uploads/avatars/minh.png", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "minh.tran@example.com", "Trần Quốc Minh", "Male", true, "sample_hash_customer_4", "0901000004", "Customer", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), "minh.tran" },
                    { 5L, "/uploads/avatars/quynh.png", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "quynh.pham@example.com", "Phạm Như Quỳnh", "Female", true, "sample_hash_customer_5", "0901000005", "Customer", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), "quynh.pham" }
                });

            migrationBuilder.InsertData(
                table: "vouchers",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "DiscountType", "DiscountValue", "EndDate", "IsActive", "MaxDiscountValue", "MaxUses", "MaxUsesPerUser", "MinOrderValue", "Priority", "StartDate", "UpdatedAt", "UsedCount" },
                values: new object[,]
                {
                    { 1L, "SUMMER2026", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Giảm 500.000đ cho đơn công nghệ từ 10 triệu.", "FixedAmount", 500000m, new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 500000m, 500, 1, 10000000m, 10, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 120 },
                    { 2L, "FREESHIP-05", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Miễn phí vận chuyển cho đơn từ 1 triệu.", "FixedAmount", 50000m, new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 50000m, 1000, 3, 1000000m, 5, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 380 },
                    { 3L, "TECH500K", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Giảm 1.000.000đ cho laptop cao cấp.", "FixedAmount", 1000000m, new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 1000000m, 300, 1, 25000000m, 20, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 72 },
                    { 4L, "SHOES15", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Giảm 15% cho giày thể thao.", "Percentage", 15m, new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 900000m, 700, 2, 1500000m, 12, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 210 },
                    { 5L, "NEWUSER100", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Giảm 100.000đ cho khách hàng mới.", "FixedAmount", 100000m, new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, 100000m, 2000, 1, 500000m, 3, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 640 }
                });

            migrationBuilder.InsertData(
                table: "attribute_options",
                columns: new[] { "Id", "AttributeId", "Label", "Value" },
                values: new object[,]
                {
                    { 1L, 1L, "Black Titanium", "black-titanium" },
                    { 2L, 1L, "Titanium Gray", "titanium-gray" },
                    { 3L, 2L, "256GB", "256gb" },
                    { 4L, 3L, "Size 42", "42" },
                    { 5L, 4L, "Intel Core Ultra 7", "core-ultra-7" }
                });

            migrationBuilder.InsertData(
                table: "campaign_categories",
                columns: new[] { "Id", "CampaignId", "CategoryId", "Description", "ImagePath", "Position", "Title" },
                values: new object[] { 4L, 4L, 1L, "Ưu đãi toàn sàn cho thành viên.", "/uploads/campaigns/member-day.jpg", 1, "Ngày hội thành viên" });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "CreatedAt", "Description", "ImagePath", "IsActive", "Name", "ParentId", "Position", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Smartphone chính hãng.", "/uploads/categories/smartphones.jpg", true, "Điện thoại", 1L, 2, "dien-thoai", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Laptop học tập, văn phòng và doanh nghiệp.", "/uploads/categories/laptops.jpg", true, "Laptop", 1L, 3, "laptop", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Giày sneaker và giày chạy bộ.", "/uploads/categories/sneakers.jpg", true, "Giày thể thao", 4L, 5, "giay-the-thao", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "promotion_rules",
                columns: new[] { "Id", "ActionType", "BuyQuantity", "DiscountValue", "GetQuantity", "GiftProductVariantId", "PromotionId" },
                values: new object[,]
                {
                    { 1L, "DiscountProduct", 1, 800000m, 0, null, 1L },
                    { 3L, "BuyXGetY", 2, 500000m, 0, null, 3L },
                    { 4L, "DiscountProduct", 1, 1000000m, 0, null, 4L },
                    { 5L, "DiscountOrder", 1, 700000m, 0, null, 5L }
                });

            migrationBuilder.InsertData(
                table: "promotion_targets",
                columns: new[] { "Id", "PromotionId", "TargetId", "TargetType" },
                values: new object[,]
                {
                    { 1L, 1L, 2L, "Category" },
                    { 2L, 2L, 3L, "Category" },
                    { 3L, 3L, 5L, "Category" },
                    { 4L, 4L, 1L, "Brand" },
                    { 5L, 5L, 2L, "Brand" }
                });

            migrationBuilder.InsertData(
                table: "user_addresses",
                columns: new[] { "Id", "ContactName", "CreatedAt", "DetailAddress", "IsDefault", "Phone", "ProvinceCode", "ProvinceName", "Type", "UpdatedAt", "UserId", "WardCode", "WardName" },
                values: new object[,]
                {
                    { 1L, "Nguyễn Minh Admin", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tầng 8, 72 Lê Thánh Tôn, Quận 1", true, "0901000001", "79", "Hồ Chí Minh", "Billing", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 1L, "760", "Phường Bến Nghé" },
                    { 2L, "Trần Hoàng An", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "24 Nguyễn Trung Trực, Ba Đình", true, "0901000002", "01", "Hà Nội", "Shipping", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 2L, "001", "Phường Phúc Xá" },
                    { 3L, "Nguyễn Thảo Lan", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "145 Nguyễn Văn Hưởng, Thành phố Thủ Đức", true, "0901000003", "79", "Hồ Chí Minh", "Shipping", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 3L, "771", "Phường Thảo Điền" },
                    { 4L, "Trần Quốc Minh", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "18 Bạch Đằng, Hải Châu", true, "0901000004", "48", "Đà Nẵng", "Shipping", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 4L, "202", "Phường Hải Châu I" },
                    { 5L, "Phạm Như Quỳnh", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "62 Nguyễn Trãi, Ninh Kiều", true, "0901000005", "92", "Cần Thơ", "Shipping", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 5L, "311", "Phường Ninh Kiều" }
                });

            migrationBuilder.InsertData(
                table: "voucher_targets",
                columns: new[] { "Id", "TargetId", "TargetType", "VoucherId" },
                values: new object[,]
                {
                    { 1L, 2L, "Category", 1L },
                    { 2L, 5L, "Category", 2L },
                    { 3L, 3L, "Product", 3L },
                    { 4L, 4L, "Brand", 4L },
                    { 5L, 4L, "User", 5L }
                });

            migrationBuilder.InsertData(
                table: "voucher_users",
                columns: new[] { "Id", "AssignedAt", "MaxUses", "UsedCount", "UserId", "VoucherId" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, 3L, 1L },
                    { 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, 1, 4L, 2L },
                    { 3L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, 5L, 3L },
                    { 4L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, 1, 3L, 4L },
                    { 5L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, 4L, 5L }
                });

            migrationBuilder.InsertData(
                table: "campaign_categories",
                columns: new[] { "Id", "CampaignId", "CategoryId", "Description", "ImagePath", "Position", "Title" },
                values: new object[,]
                {
                    { 1L, 1L, 2L, "Ưu đãi smartphone bán chạy.", "/uploads/campaigns/summer-phone.jpg", 1, "Điện thoại mùa hè" },
                    { 2L, 2L, 3L, "Laptop mỏng nhẹ cho học tập.", "/uploads/campaigns/back-to-school-laptop.jpg", 1, "Laptop tựu trường" },
                    { 3L, 3L, 5L, "Giày thể thao chính hãng.", "/uploads/campaigns/sneaker-week.jpg", 1, "Sneaker Week" },
                    { 5L, 5L, 3L, "Deal tốt cho laptop doanh nghiệp.", "/uploads/campaigns/laptop-deals.jpg", 2, "Laptop Deals" }
                });

            migrationBuilder.InsertData(
                table: "category_specifications",
                columns: new[] { "CategoryId", "SpecificationId", "GroupName", "IsRequired", "SortOrder" },
                values: new object[,]
                {
                    { 2L, 1L, "Màn hình", true, 1 },
                    { 2L, 2L, "Hiệu năng", true, 2 },
                    { 2L, 3L, "Hiệu năng", true, 3 },
                    { 2L, 5L, "Pin", false, 4 },
                    { 5L, 4L, "Chất liệu", true, 1 }
                });

            migrationBuilder.InsertData(
                table: "category_variant_attributes",
                columns: new[] { "AttributeId", "CategoryId", "CreatedAt" },
                values: new object[,]
                {
                    { 1L, 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, 3L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 1L, 5L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, 5L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "orders",
                columns: new[] { "Id", "CreatedAt", "OrderCode", "OrderStatus", "PaymentMethodId", "PaymentStatus", "ShippingAddressId", "ShippingContactName", "ShippingDetail", "ShippingFee", "ShippingPhone", "ShippingProvince", "ShippingWard", "SubtotalAmount", "TotalAmount", "UpdatedAt", "UserId", "VoucherDiscount", "VoucherId" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 5, 20, 9, 15, 0, 0, DateTimeKind.Utc), "ORD-20260520-000001", "Completed", 1L, "Paid", 3L, "Nguyễn Thảo Lan", "145 Nguyễn Văn Hưởng, Thành phố Thủ Đức", 30000m, "0901000003", "Hồ Chí Minh", "Phường Thảo Điền", 29990000m, 29520000m, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 3L, 500000m, 1L },
                    { 2L, new DateTime(2026, 5, 21, 10, 20, 0, 0, DateTimeKind.Utc), "ORD-20260521-000002", "Shipping", 4L, "Paid", 4L, "Trần Quốc Minh", "18 Bạch Đằng, Hải Châu", 0m, "0901000004", "Đà Nẵng", "Phường Hải Châu I", 31990000m, 31990000m, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 4L, 0m, 2L },
                    { 3L, new DateTime(2026, 5, 22, 14, 30, 0, 0, DateTimeKind.Utc), "ORD-20260522-000003", "Confirmed", 2L, "Paid", 5L, "Phạm Như Quỳnh", "62 Nguyễn Trãi, Ninh Kiều", 45000m, "0901000005", "Cần Thơ", "Phường Ninh Kiều", 38990000m, 38035000m, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 5L, 1000000m, 3L },
                    { 4L, new DateTime(2026, 5, 23, 16, 5, 0, 0, DateTimeKind.Utc), "ORD-20260523-000004", "Completed", 3L, "Paid", 3L, "Nguyễn Thảo Lan", "145 Nguyễn Văn Hưởng, Thành phố Thủ Đức", 30000m, "0901000003", "Hồ Chí Minh", "Phường Thảo Điền", 5780000m, 4943000m, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 3L, 867000m, 4L },
                    { 5L, new DateTime(2026, 5, 24, 11, 45, 0, 0, DateTimeKind.Utc), "ORD-20260524-000005", "Processing", 5L, "Paid", 4L, "Trần Quốc Minh", "18 Bạch Đằng, Hải Châu", 35000m, "0901000004", "Đà Nẵng", "Phường Hải Châu I", 4200000m, 4135000m, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 4L, 100000m, 5L }
                });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "Id", "BrandId", "CategoryId", "CreatedAt", "Description", "IsActive", "IsFeatured", "Name", "RatingAverage", "RatingCount", "Slug", "TotalSoldCount", "UpdatedAt", "ViewsCount" },
                values: new object[,]
                {
                    { 1L, 1L, 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "iPhone 15 Pro Max chính hãng, chip A17 Pro.", true, true, "iPhone 15 Pro Max", 4.80m, 142, "iphone-15-pro-max", 328, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 15420 },
                    { 2L, 2L, 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Galaxy S24 Ultra với S Pen và Galaxy AI.", true, true, "Samsung Galaxy S24 Ultra", 4.70m, 118, "samsung-galaxy-s24-ultra", 276, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 12110 },
                    { 3L, 3L, 3L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Laptop mỏng nhẹ cho công việc và di chuyển.", true, false, "Dell XPS 13", 4.60m, 61, "dell-xps-13", 94, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 8420 },
                    { 4L, 4L, 5L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sneaker cổ thấp, thiết kế trắng cổ điển.", true, true, "Nike Air Force 1 '07", 4.75m, 203, "nike-air-force-1-07", 451, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 9730 },
                    { 5L, 5L, 5L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Giày chạy bộ nhẹ, đệm Boost đàn hồi tốt.", true, false, "Adidas Ultraboost Light", 4.55m, 77, "adidas-ultraboost-light", 188, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 6880 }
                });

            migrationBuilder.InsertData(
                table: "product_color_images",
                columns: new[] { "Id", "AltText", "Color", "ImagePath", "Position", "ProductId" },
                values: new object[,]
                {
                    { 1L, "iPhone 15 Pro Max Black Titanium", "Black Titanium", "/uploads/products/iphone-15-pro-max-black.jpg", 1, 1L },
                    { 2L, "Samsung Galaxy S24 Ultra Titanium Gray", "Titanium Gray", "/uploads/products/galaxy-s24-ultra-gray.jpg", 1, 2L },
                    { 3L, "Dell XPS 13 Platinum Silver", "Platinum Silver", "/uploads/products/dell-xps-13-silver.jpg", 1, 3L },
                    { 4L, "Nike Air Force 1 White", "White", "/uploads/products/nike-af1-white.jpg", 1, 4L },
                    { 5L, "Adidas Ultraboost Light Core Black", "Core Black", "/uploads/products/adidas-ultraboost-black.jpg", 1, 5L }
                });

            migrationBuilder.InsertData(
                table: "product_specifications",
                columns: new[] { "ProductId", "SpecificationId", "IsHighlight", "SortOrder", "Value" },
                values: new object[,]
                {
                    { 1L, 2L, true, 1, "256GB" },
                    { 2L, 3L, true, 1, "12GB" },
                    { 3L, 1L, true, 1, "13.4" },
                    { 4L, 4L, true, 1, "Da tổng hợp" },
                    { 5L, 4L, true, 1, "Primeknit và cao su Continental" }
                });

            migrationBuilder.InsertData(
                table: "product_variants",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "IsDefault", "Price", "ProductId", "Quantity", "SoldCount", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, "APP-IP15PM-256-BLK", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, 29990000m, 1L, 42, 185, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, "SAM-S24U-512-GRY", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, 31990000m, 2L, 35, 132, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, "DEL-XPS13-UL7-16-512", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, 38990000m, 3L, 18, 58, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, "NIK-AF1-42-WHT", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, 2890000m, 4L, 76, 224, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5L, "ADI-UBL-41-BLK", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, 4200000m, 5L, 51, 94, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "voucher_usages",
                columns: new[] { "Id", "OrderId", "UsedAt", "UserId", "VoucherId" },
                values: new object[,]
                {
                    { 1L, 1L, new DateTime(2026, 5, 20, 9, 16, 0, 0, DateTimeKind.Utc), 3L, 1L },
                    { 2L, 2L, new DateTime(2026, 5, 21, 10, 21, 0, 0, DateTimeKind.Utc), 4L, 2L },
                    { 3L, 3L, new DateTime(2026, 5, 22, 14, 31, 0, 0, DateTimeKind.Utc), 5L, 3L },
                    { 4L, 4L, new DateTime(2026, 5, 23, 16, 6, 0, 0, DateTimeKind.Utc), 3L, 4L },
                    { 5L, 5L, new DateTime(2026, 5, 24, 11, 46, 0, 0, DateTimeKind.Utc), 4L, 5L }
                });

            migrationBuilder.InsertData(
                table: "cart_items",
                columns: new[] { "Id", "CreatedAt", "DiscountValue", "ProductVariantId", "Quantity", "UnitPrice", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 500000m, 1L, 1, 29990000m, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 3L },
                    { 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 700000m, 2L, 1, 31990000m, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 4L },
                    { 3L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000000m, 3L, 1, 38990000m, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 5L },
                    { 4L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 300000m, 4L, 2, 2890000m, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 2L },
                    { 5L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 250000m, 5L, 1, 4200000m, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 1L }
                });

            migrationBuilder.InsertData(
                table: "order_items",
                columns: new[] { "Id", "OrderId", "ProductVariantId", "Quantity", "UnitPrice" },
                values: new object[,]
                {
                    { 1L, 1L, 1L, 1, 29990000m },
                    { 2L, 2L, 2L, 1, 31990000m },
                    { 3L, 3L, 3L, 1, 38990000m },
                    { 4L, 4L, 4L, 2, 2890000m },
                    { 5L, 5L, 5L, 1, 4200000m }
                });

            migrationBuilder.InsertData(
                table: "promotion_rules",
                columns: new[] { "Id", "ActionType", "BuyQuantity", "DiscountValue", "GetQuantity", "GiftProductVariantId", "PromotionId" },
                values: new object[] { 2L, "GiftProduct", 1, 0m, 1, 5L, 2L });

            migrationBuilder.InsertData(
                table: "variant_attributes",
                columns: new[] { "AttributeOptionId", "ProductVariantId", "CreatedAt" },
                values: new object[,]
                {
                    { 1L, 1L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, 1L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5L, 3L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, 4L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "wishlist",
                columns: new[] { "Id", "CreatedAt", "ProductVariantId", "UserId" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2L, 3L },
                    { 2L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4L, 3L },
                    { 3L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1L, 4L },
                    { 4L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5L, 5L },
                    { 5L, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3L, 2L }
                });

            migrationBuilder.InsertData(
                table: "ratings",
                columns: new[] { "Id", "Comment", "CreatedAt", "IsApproved", "OrderItemId", "Stars", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { 1L, "Máy đẹp, giao nhanh, đóng gói kỹ.", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1L, 5, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 3L },
                    { 2L, "Màn hình rất đẹp, dùng AI tiện.", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 2L, 5, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 4L },
                    { 3L, "Laptop nhẹ, pin ổn cho công việc.", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 3L, 4, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 5L },
                    { 4L, "Giày đúng size, form đẹp.", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 4L, 5, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 3L },
                    { 5L, "Đệm êm, phù hợp chạy bộ hằng ngày.", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 5L, 4, new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), 4L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_attribute_options_AttributeId_Value",
                table: "attribute_options",
                columns: new[] { "AttributeId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attributes_Code",
                table: "attributes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_brands_Slug",
                table: "brands",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_campaign_categories_CampaignId",
                table: "campaign_categories",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_campaign_categories_CategoryId",
                table: "campaign_categories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_campaigns_Slug",
                table: "campaigns",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_ProductVariantId",
                table: "cart_items",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_UserId_ProductVariantId",
                table: "cart_items",
                columns: new[] { "UserId", "ProductVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_ParentId",
                table: "categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_categories_Slug",
                table: "categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_specifications_SpecificationId",
                table: "category_specifications",
                column: "SpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_category_variant_attributes_AttributeId",
                table: "category_variant_attributes",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_OrderId",
                table: "order_items",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_ProductVariantId",
                table: "order_items",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_OrderCode",
                table: "orders",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_PaymentMethodId",
                table: "orders",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_ShippingAddressId",
                table: "orders",
                column: "ShippingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_UserId",
                table: "orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_VoucherId",
                table: "orders",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_product_color_images_ProductId",
                table: "product_color_images",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_specifications_SpecificationId",
                table: "product_specifications",
                column: "SpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_Code",
                table: "product_variants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_ProductId",
                table: "product_variants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_products_BrandId",
                table: "products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_products_CategoryId",
                table: "products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_products_Slug",
                table: "products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_rules_GiftProductVariantId",
                table: "promotion_rules",
                column: "GiftProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_rules_PromotionId",
                table: "promotion_rules",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_PromotionId_TargetType_TargetId",
                table: "promotion_targets",
                columns: new[] { "PromotionId", "TargetType", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ratings_OrderItemId",
                table: "ratings",
                column: "OrderItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ratings_UserId",
                table: "ratings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_specifications_Key",
                table: "specifications",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_addresses_UserId_IsDefault",
                table: "user_addresses",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variant_attributes_AttributeOptionId",
                table: "variant_attributes",
                column: "AttributeOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_targets_VoucherId_TargetType_TargetId",
                table: "voucher_targets",
                columns: new[] { "VoucherId", "TargetType", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_OrderId",
                table: "voucher_usages",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_UserId",
                table: "voucher_usages",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_VoucherId",
                table: "voucher_usages",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_users_UserId",
                table: "voucher_users",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_users_VoucherId_UserId",
                table: "voucher_users",
                columns: new[] { "VoucherId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vouchers_Code",
                table: "vouchers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_ProductVariantId",
                table: "wishlist",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_UserId_ProductVariantId",
                table: "wishlist",
                columns: new[] { "UserId", "ProductVariantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campaign_categories");

            migrationBuilder.DropTable(
                name: "cart_items");

            migrationBuilder.DropTable(
                name: "category_specifications");

            migrationBuilder.DropTable(
                name: "category_variant_attributes");

            migrationBuilder.DropTable(
                name: "product_color_images");

            migrationBuilder.DropTable(
                name: "product_specifications");

            migrationBuilder.DropTable(
                name: "promotion_rules");

            migrationBuilder.DropTable(
                name: "promotion_targets");

            migrationBuilder.DropTable(
                name: "ratings");

            migrationBuilder.DropTable(
                name: "variant_attributes");

            migrationBuilder.DropTable(
                name: "voucher_targets");

            migrationBuilder.DropTable(
                name: "voucher_usages");

            migrationBuilder.DropTable(
                name: "voucher_users");

            migrationBuilder.DropTable(
                name: "wishlist");

            migrationBuilder.DropTable(
                name: "campaigns");

            migrationBuilder.DropTable(
                name: "specifications");

            migrationBuilder.DropTable(
                name: "promotions");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "attribute_options");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "product_variants");

            migrationBuilder.DropTable(
                name: "attributes");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "user_addresses");

            migrationBuilder.DropTable(
                name: "vouchers");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
