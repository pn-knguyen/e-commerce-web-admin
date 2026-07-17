using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryLedgerProfitTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageCost",
                table: "product_variants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "order_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "FulfillmentLocationId",
                table: "goods_receipts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inventory_balances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentLocationId = table.Column<long>(type: "bigint", nullable: true),
                    OnHandQuantity = table.Column<int>(type: "int", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "int", nullable: false),
                    AverageCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_balances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_balances_fulfillment_locations_FulfillmentLocationId",
                        column: x => x.FulfillmentLocationId,
                        principalTable: "fulfillment_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_balances_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_stock_lots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentLocationId = table.Column<long>(type: "bigint", nullable: true),
                    GoodReceiptItemId = table.Column<long>(type: "bigint", nullable: true),
                    LotCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "int", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_stock_lots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_stock_lots_fulfillment_locations_FulfillmentLocationId",
                        column: x => x.FulfillmentLocationId,
                        principalTable: "fulfillment_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_stock_lots_good_receipt_items_GoodReceiptItemId",
                        column: x => x.GoodReceiptItemId,
                        principalTable: "good_receipt_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_stock_lots_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductVariantId = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentLocationId = table.Column<long>(type: "bigint", nullable: true),
                    StockLotId = table.Column<long>(type: "bigint", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    QuantityDelta = table.Column<int>(type: "int", nullable: false),
                    ReservedQuantityDelta = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_movements_fulfillment_locations_FulfillmentLocationId",
                        column: x => x.FulfillmentLocationId,
                        principalTable: "fulfillment_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_inventory_stock_lots_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "inventory_stock_lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_product_variants_ProductVariantId",
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
                    StockLotId = table.Column<long>(type: "bigint", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item_cost_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_item_cost_allocations_inventory_stock_lots_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "inventory_stock_lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_item_cost_allocations_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "order_items",
                keyColumn: "Id",
                keyValue: 1L,
                column: "UnitCost",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "order_items",
                keyColumn: "Id",
                keyValue: 2L,
                column: "UnitCost",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "order_items",
                keyColumn: "Id",
                keyValue: 3L,
                column: "UnitCost",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "order_items",
                keyColumn: "Id",
                keyValue: 4L,
                column: "UnitCost",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "order_items",
                keyColumn: "Id",
                keyValue: 5L,
                column: "UnitCost",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: 1L,
                column: "AverageCost",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: 2L,
                column: "AverageCost",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: 3L,
                column: "AverageCost",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: 4L,
                column: "AverageCost",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "product_variants",
                keyColumn: "Id",
                keyValue: 5L,
                column: "AverageCost",
                value: 0m);

            migrationBuilder.Sql(
                """
                DECLARE @defaultLocationId bigint;

                SELECT TOP (1) @defaultLocationId = Id
                FROM fulfillment_locations
                WHERE IsActive = CAST(1 AS bit)
                ORDER BY IsDefault DESC, Name ASC;

                IF @defaultLocationId IS NOT NULL
                BEGIN
                    UPDATE goods_receipts
                    SET FulfillmentLocationId = @defaultLocationId
                    WHERE FulfillmentLocationId IS NULL
                      AND Status = N'Approved';
                END;

                ;WITH VariantCosts AS
                (
                    SELECT
                        gri.ProductVariantId,
                        CAST(SUM(CAST(gri.Quantity AS decimal(18, 2)) * gri.ImportPrice)
                            / NULLIF(SUM(CAST(gri.Quantity AS decimal(18, 2))), 0) AS decimal(18, 2)) AS AverageCost
                    FROM good_receipt_items AS gri
                    INNER JOIN goods_receipts AS gr ON gr.Id = gri.GoodsReceiptId
                    WHERE gr.Status = N'Approved'
                    GROUP BY gri.ProductVariantId
                )
                UPDATE pv
                SET AverageCost = vc.AverageCost
                FROM product_variants AS pv
                INNER JOIN VariantCosts AS vc ON vc.ProductVariantId = pv.Id;

                UPDATE oi
                SET UnitCost = pv.AverageCost
                FROM order_items AS oi
                INNER JOIN product_variants AS pv ON pv.Id = oi.ProductVariantId
                WHERE oi.UnitCost = 0;

                INSERT INTO inventory_stock_lots
                (
                    ProductVariantId,
                    FulfillmentLocationId,
                    GoodReceiptItemId,
                    LotCode,
                    ReceivedQuantity,
                    RemainingQuantity,
                    UnitCost,
                    ReceivedAt,
                    CreatedAt,
                    UpdatedAt
                )
                SELECT
                    pv.Id,
                    @defaultLocationId,
                    NULL,
                    CONCAT(N'OPEN-', pv.Id),
                    pv.Quantity,
                    pv.Quantity,
                    pv.AverageCost,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME(),
                    NULL
                FROM product_variants AS pv
                WHERE pv.Quantity > 0
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM inventory_stock_lots AS lot
                      WHERE lot.LotCode = CONCAT(N'OPEN-', pv.Id)
                  );

                INSERT INTO inventory_balances
                (
                    ProductVariantId,
                    FulfillmentLocationId,
                    OnHandQuantity,
                    ReservedQuantity,
                    AverageCost,
                    UpdatedAt
                )
                SELECT
                    pv.Id,
                    @defaultLocationId,
                    pv.Quantity,
                    0,
                    pv.AverageCost,
                    SYSUTCDATETIME()
                FROM product_variants AS pv
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM inventory_balances AS balance
                    WHERE balance.ProductVariantId = pv.Id
                      AND
                      (
                          balance.FulfillmentLocationId = @defaultLocationId
                          OR (balance.FulfillmentLocationId IS NULL AND @defaultLocationId IS NULL)
                      )
                );

                INSERT INTO inventory_movements
                (
                    ProductVariantId,
                    FulfillmentLocationId,
                    StockLotId,
                    Type,
                    QuantityDelta,
                    ReservedQuantityDelta,
                    UnitCost,
                    TotalCost,
                    ReferenceType,
                    ReferenceId,
                    Note,
                    OccurredAt,
                    CreatedAt
                )
                SELECT
                    lot.ProductVariantId,
                    lot.FulfillmentLocationId,
                    lot.Id,
                    N'Adjustment',
                    lot.ReceivedQuantity,
                    0,
                    lot.UnitCost,
                    lot.ReceivedQuantity * lot.UnitCost,
                    N'SystemBackfill',
                    lot.ProductVariantId,
                    N'Opening inventory balance created by inventory ledger migration.',
                    lot.CreatedAt,
                    lot.CreatedAt
                FROM inventory_stock_lots AS lot
                WHERE lot.LotCode LIKE N'OPEN-%'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM inventory_movements AS movement
                      WHERE movement.StockLotId = lot.Id
                        AND movement.ReferenceType = N'SystemBackfill'
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_FulfillmentLocationId",
                table: "goods_receipts",
                column: "FulfillmentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_FulfillmentLocationId",
                table: "inventory_balances",
                column: "FulfillmentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_ProductVariantId_FulfillmentLocationId",
                table: "inventory_balances",
                columns: new[] { "ProductVariantId", "FulfillmentLocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_FulfillmentLocationId",
                table: "inventory_movements",
                column: "FulfillmentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_ProductVariantId_OccurredAt",
                table: "inventory_movements",
                columns: new[] { "ProductVariantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_ReferenceType_ReferenceId",
                table: "inventory_movements",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_StockLotId",
                table: "inventory_movements",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_stock_lots_FulfillmentLocationId",
                table: "inventory_stock_lots",
                column: "FulfillmentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_stock_lots_GoodReceiptItemId",
                table: "inventory_stock_lots",
                column: "GoodReceiptItemId",
                unique: true,
                filter: "[GoodReceiptItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_stock_lots_LotCode",
                table: "inventory_stock_lots",
                column: "LotCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_stock_lots_ProductVariantId",
                table: "inventory_stock_lots",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_cost_allocations_OrderItemId",
                table: "order_item_cost_allocations",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_cost_allocations_StockLotId",
                table: "order_item_cost_allocations",
                column: "StockLotId");

            migrationBuilder.AddForeignKey(
                name: "FK_goods_receipts_fulfillment_locations_FulfillmentLocationId",
                table: "goods_receipts",
                column: "FulfillmentLocationId",
                principalTable: "fulfillment_locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_goods_receipts_fulfillment_locations_FulfillmentLocationId",
                table: "goods_receipts");

            migrationBuilder.DropTable(
                name: "inventory_balances");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "order_item_cost_allocations");

            migrationBuilder.DropTable(
                name: "inventory_stock_lots");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipts_FulfillmentLocationId",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "AverageCost",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "FulfillmentLocationId",
                table: "goods_receipts");
        }
    }
}
