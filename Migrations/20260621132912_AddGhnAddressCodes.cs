using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddGhnAddressCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DistrictCode",
                table: "user_addresses",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DistrictName",
                table: "user_addresses",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DistrictCode",
                table: "fulfillment_locations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DistrictName",
                table: "fulfillment_locations",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "DistrictCode", "DistrictName" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "DistrictCode", "DistrictName" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "DistrictCode", "DistrictName" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 4L,
                columns: new[] { "DistrictCode", "DistrictName" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 5L,
                columns: new[] { "DistrictCode", "DistrictName" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistrictCode",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "DistrictName",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "DistrictCode",
                table: "fulfillment_locations");

            migrationBuilder.DropColumn(
                name: "DistrictName",
                table: "fulfillment_locations");
        }
    }
}
