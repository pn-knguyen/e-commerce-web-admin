using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleDefaultLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH [RankedDefaults] AS (
                    SELECT
                        [Id],
                        ROW_NUMBER() OVER (
                            ORDER BY [IsActive] DESC, [UpdatedAt] DESC, [CreatedAt] DESC, [Id] DESC) AS [RowNumber]
                    FROM [fulfillment_locations]
                    WHERE [IsDefault] = 1
                )
                UPDATE [location]
                SET [IsDefault] = 0
                FROM [fulfillment_locations] AS [location]
                INNER JOIN [RankedDefaults] AS [ranked] ON [ranked].[Id] = [location].[Id]
                WHERE [ranked].[RowNumber] > 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_fulfillment_locations_IsDefault",
                table: "fulfillment_locations");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_locations_IsDefault",
                table: "fulfillment_locations",
                column: "IsDefault",
                unique: true,
                filter: "[IsDefault] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_fulfillment_locations_IsDefault",
                table: "fulfillment_locations");

            migrationBuilder.CreateIndex(
                name: "IX_fulfillment_locations_IsDefault",
                table: "fulfillment_locations",
                column: "IsDefault");
        }
    }
}
