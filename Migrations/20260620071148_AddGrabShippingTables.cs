using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddGrabShippingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormattedAddress",
                table: "user_addresses",
                type: "nvarchar(700)",
                maxLength: 700,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "user_addresses",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "user_addresses",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaceId",
                table: "user_addresses",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fulfillment_locations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProvinceCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ProvinceName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WardCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    WardName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DetailAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FormattedAddress = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fulfillment_locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shipment_quotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentLocationId = table.Column<long>(type: "bigint", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderQuoteId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Fee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    EstimatedDistanceMeters = table.Column<int>(type: "int", nullable: true),
                    EstimatedDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentLocationId = table.Column<long>(type: "bigint", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderDeliveryId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ProviderQuoteId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ProviderStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    TrackingUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PickupContactName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PickupPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PickupAddress = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    PickupLatitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    PickupLongitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    DropoffContactName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DropoffPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DropoffAddress = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    DropoffLatitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    DropoffLongitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    QuotedFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    EstimatedDistanceMeters = table.Column<int>(type: "int", nullable: true),
                    EstimatedDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    RequestedByStaffId = table.Column<long>(type: "bigint", nullable: true),
                    BookedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PickedUpAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shipments_fulfillment_locations_FulfillmentLocationId",
                        column: x => x.FulfillmentLocationId,
                        principalTable: "fulfillment_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipments_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipments_staff_RequestedByStaffId",
                        column: x => x.RequestedByStaffId,
                        principalTable: "staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipment_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    ProviderEventId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ProviderStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DriverPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    VehiclePlate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shipment_events_shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipment_packages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    WeightGrams = table.Column<int>(type: "int", nullable: true),
                    LengthCm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    WidthCm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    HeightCm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    DeclaredValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsFragile = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shipment_packages_shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "FormattedAddress", "Latitude", "Longitude", "PlaceId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "FormattedAddress", "Latitude", "Longitude", "PlaceId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "FormattedAddress", "Latitude", "Longitude", "PlaceId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 4L,
                columns: new[] { "FormattedAddress", "Latitude", "Longitude", "PlaceId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 5L,
                columns: new[] { "FormattedAddress", "Latitude", "Longitude", "PlaceId" },
                values: new object[] { null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_locations_IsActive",
                table: "fulfillment_locations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_locations_IsDefault",
                table: "fulfillment_locations",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_events_ProviderEventId",
                table: "shipment_events",
                column: "ProviderEventId",
                unique: true,
                filter: "[ProviderEventId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_events_ShipmentId",
                table: "shipment_events",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_packages_ShipmentId_Sequence",
                table: "shipment_packages",
                columns: new[] { "ShipmentId", "Sequence" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_shipments_FulfillmentLocationId",
                table: "shipments",
                column: "FulfillmentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_OrderId",
                table: "shipments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_Provider_ProviderDeliveryId",
                table: "shipments",
                columns: new[] { "Provider", "ProviderDeliveryId" },
                unique: true,
                filter: "[ProviderDeliveryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_RequestedByStaffId",
                table: "shipments",
                column: "RequestedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_Status",
                table: "shipments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipment_events");

            migrationBuilder.DropTable(
                name: "shipment_packages");

            migrationBuilder.DropTable(
                name: "shipment_quotes");

            migrationBuilder.DropTable(
                name: "shipments");

            migrationBuilder.DropTable(
                name: "fulfillment_locations");

            migrationBuilder.DropColumn(
                name: "FormattedAddress",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "PlaceId",
                table: "user_addresses");
        }
    }
}
