using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShipmentQuotesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipment_quotes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shipment_quotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FulfillmentLocationId = table.Column<long>(type: "bigint", nullable: true),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    EstimatedDistanceMeters = table.Column<int>(type: "int", nullable: true),
                    EstimatedDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Fee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderQuoteId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shipment_quotes_fulfillment_locations_FulfillmentLocationId",
                        column: x => x.FulfillmentLocationId,
                        principalTable: "fulfillment_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_quotes_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shipment_quotes_FulfillmentLocationId",
                table: "shipment_quotes",
                column: "FulfillmentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_quotes_OrderId",
                table: "shipment_quotes",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_quotes_Provider_ProviderQuoteId",
                table: "shipment_quotes",
                columns: new[] { "Provider", "ProviderQuoteId" },
                unique: true,
                filter: "[ProviderQuoteId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_quotes_Status",
                table: "shipment_quotes",
                column: "Status");
        }
    }
}
