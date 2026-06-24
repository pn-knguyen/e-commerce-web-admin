using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenShipmentConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SELECT [Id]
                INTO #DuplicateOpenShipments
                FROM (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER (
                            PARTITION BY [OrderId], [Provider]
                            ORDER BY [CreatedAt] DESC, [Id] DESC) AS [RowNumber]
                    FROM [shipments]
                    WHERE [ProviderDeliveryId] IS NULL
                ) AS [RankedOpenShipments]
                WHERE [RowNumber] > 1;

                DELETE FROM [shipment_events]
                WHERE [ShipmentId] IN (SELECT [Id] FROM #DuplicateOpenShipments);

                DELETE FROM [shipment_packages]
                WHERE [ShipmentId] IN (SELECT [Id] FROM #DuplicateOpenShipments);

                DELETE FROM [shipments]
                WHERE [Id] IN (SELECT [Id] FROM #DuplicateOpenShipments);

                DROP TABLE #DuplicateOpenShipments;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_shipments_OrderId_Provider_Open",
                table: "shipments",
                columns: new[] { "OrderId", "Provider" },
                unique: true,
                filter: "[ProviderDeliveryId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shipments_OrderId_Provider_Open",
                table: "shipments");
        }
    }
}
