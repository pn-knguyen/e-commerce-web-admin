using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class MoveVariantColorToProductVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "product_variants",
                type: "varchar(7)",
                unicode: false,
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorName",
                table: "product_variants",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE pv
SET
    ColorName = COALESCE(
        NULLIF(LTRIM(RTRIM(pv.ColorName)), ''),
        NULLIF(LTRIM(RTRIM(attributeColor.Label)), ''),
        NULLIF(LTRIM(RTRIM(src.Color)), '')),
    ColorHex = CASE
        WHEN src.Color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'
            THEN UPPER(src.Color)
        ELSE pv.ColorHex
    END
FROM product_variants pv
OUTER APPLY (
    SELECT TOP (1) Color
    FROM product_variant_images pvi
    WHERE pvi.ProductVariantId = pv.Id
      AND NULLIF(LTRIM(RTRIM(pvi.Color)), '') IS NOT NULL
    ORDER BY pvi.Position, pvi.Id
) src
OUTER APPLY (
    SELECT TOP (1) ao.Label
    FROM variant_attributes va
    INNER JOIN attribute_options ao ON ao.Id = va.AttributeOptionId
    INNER JOIN attributes a ON a.Id = ao.AttributeId
    WHERE va.ProductVariantId = pv.Id
      AND a.Code = 'color'
    ORDER BY ao.Id
) attributeColor
WHERE src.Color IS NOT NULL OR attributeColor.Label IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "product_variant_images");

            migrationBuilder.UpdateData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "ColorHex", "ColorName" },
                values: new object[] { "#111827", "Black Titanium" });

            migrationBuilder.UpdateData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "ColorHex", "ColorName" },
                values: new object[] { "#71717A", "Titanium Gray" });

            migrationBuilder.UpdateData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "ColorHex", "ColorName" },
                values: new object[] { "#D6D3D1", "Platinum Silver" });

            migrationBuilder.UpdateData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: 4L,
                columns: new[] { "ColorHex", "ColorName" },
                values: new object[] { "#FFFFFF", "White" });

            migrationBuilder.UpdateData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: 5L,
                columns: new[] { "ColorHex", "ColorName" },
                values: new object[] { "#111827", "Core Black" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "product_variant_images",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
UPDATE pvi
SET Color = COALESCE(NULLIF(LTRIM(RTRIM(pv.ColorName)), ''), NULLIF(LTRIM(RTRIM(pv.ColorHex)), ''), '')
FROM product_variant_images pvi
INNER JOIN product_variants pv ON pv.Id = pvi.ProductVariantId;");

            migrationBuilder.UpdateData(
                table: "product_variant_images",
                keyColumn: "Id",
                keyValue: 1L,
                column: "Color",
                value: "Black Titanium");

            migrationBuilder.UpdateData(
                table: "product_variant_images",
                keyColumn: "Id",
                keyValue: 2L,
                column: "Color",
                value: "Titanium Gray");

            migrationBuilder.UpdateData(
                table: "product_variant_images",
                keyColumn: "Id",
                keyValue: 3L,
                column: "Color",
                value: "Platinum Silver");

            migrationBuilder.UpdateData(
                table: "product_variant_images",
                keyColumn: "Id",
                keyValue: 4L,
                column: "Color",
                value: "White");

            migrationBuilder.UpdateData(
                table: "product_variant_images",
                keyColumn: "Id",
                keyValue: 5L,
                column: "Color",
                value: "Core Black");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "ColorName",
                table: "product_variants");
        }
    }
}
