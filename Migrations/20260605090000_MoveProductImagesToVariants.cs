using e_commerce_web_admin.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260605090000_MoveProductImagesToVariants")]
    public partial class MoveProductImagesToVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_product_color_images_products_ProductId",
                table: "product_color_images");

            migrationBuilder.RenameTable(
                name: "product_color_images",
                newName: "product_variant_images");

            migrationBuilder.RenameIndex(
                name: "IX_product_color_images_ProductId",
                table: "product_variant_images",
                newName: "IX_product_variant_images_ProductVariantId");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "product_variant_images",
                newName: "ProductVariantId");

            migrationBuilder.Sql("""
                DELETE pvi
                FROM product_variant_images AS pvi
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM product_variants AS pv
                    WHERE pv.ProductId = pvi.ProductVariantId
                );
                """);

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

            migrationBuilder.AddForeignKey(
                name: "FK_product_variant_images_product_variants_ProductVariantId",
                table: "product_variant_images",
                column: "ProductVariantId",
                principalTable: "product_variants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_product_variant_images_product_variants_ProductVariantId",
                table: "product_variant_images");

            migrationBuilder.Sql("""
                UPDATE pvi
                SET ProductVariantId = pv.ProductId
                FROM product_variant_images AS pvi
                INNER JOIN product_variants AS pv ON pv.Id = pvi.ProductVariantId;
                """);

            migrationBuilder.RenameTable(
                name: "product_variant_images",
                newName: "product_color_images");

            migrationBuilder.RenameIndex(
                name: "IX_product_variant_images_ProductVariantId",
                table: "product_color_images",
                newName: "IX_product_color_images_ProductId");

            migrationBuilder.RenameColumn(
                name: "ProductVariantId",
                table: "product_color_images",
                newName: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_product_color_images_products_ProductId",
                table: "product_color_images",
                column: "ProductId",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
