using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddFifoInventoryCosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_batches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoodReceiptItemId = table.Column<long>(type: "bigint", nullable: false),
                    ProductVariantId = table.Column<long>(type: "bigint", nullable: false),
                    QuantityReceived = table.Column<int>(type: "int", nullable: false),
                    QuantityRemaining = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_batches_good_receipt_items_GoodReceiptItemId",
                        column: x => x.GoodReceiptItemId,
                        principalTable: "good_receipt_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_batches_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_item_cost_allocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    InventoryBatchId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item_cost_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_item_cost_allocations_inventory_batches_InventoryBatchId",
                        column: x => x.InventoryBatchId,
                        principalTable: "inventory_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_item_cost_allocations_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_batches_GoodReceiptItemId",
                table: "inventory_batches",
                column: "GoodReceiptItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_batches_ProductVariantId_ReceivedAt_Id",
                table: "inventory_batches",
                columns: new[] { "ProductVariantId", "ReceivedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_order_item_cost_allocations_InventoryBatchId",
                table: "order_item_cost_allocations",
                column: "InventoryBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_cost_allocations_OrderItemId",
                table: "order_item_cost_allocations",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_cost_allocations_OrderItemId_InventoryBatchId",
                table: "order_item_cost_allocations",
                columns: new[] { "OrderItemId", "InventoryBatchId" });

            migrationBuilder.Sql("""
                INSERT INTO inventory_batches (
                    GoodReceiptItemId,
                    ProductVariantId,
                    QuantityReceived,
                    QuantityRemaining,
                    UnitCost,
                    ReceivedAt,
                    CreatedAt,
                    UpdatedAt)
                SELECT
                    gri.Id,
                    gri.ProductVariantId,
                    gri.Quantity,
                    gri.Quantity,
                    gri.ImportPrice,
                    COALESCE(gr.UpdatedAt, gr.CreatedAt),
                    SYSUTCDATETIME(),
                    NULL
                FROM good_receipt_items AS gri
                INNER JOIN goods_receipts AS gr ON gr.Id = gri.GoodsReceiptId
                WHERE gr.Status = N'Approved'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM inventory_batches AS batch
                      WHERE batch.GoodReceiptItemId = gri.Id
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_item_cost_allocations");

            migrationBuilder.DropTable(
                name: "inventory_batches");
        }
    }
}
