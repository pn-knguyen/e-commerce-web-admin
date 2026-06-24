using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentProviderAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderDropoffDistrictCode",
                table: "shipments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderDropoffDistrictName",
                table: "shipments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderDropoffProvinceCode",
                table: "shipments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderDropoffProvinceName",
                table: "shipments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderDropoffWardCode",
                table: "shipments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderDropoffWardName",
                table: "shipments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPickupDistrictCode",
                table: "shipments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPickupDistrictName",
                table: "shipments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPickupProvinceCode",
                table: "shipments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPickupProvinceName",
                table: "shipments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPickupWardCode",
                table: "shipments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderPickupWardName",
                table: "shipments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderDropoffDistrictCode",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ProviderDropoffDistrictName",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ProviderDropoffProvinceCode",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ProviderDropoffProvinceName",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ProviderDropoffWardCode",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ProviderDropoffWardName",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ProviderPickupDistrictCode",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ProviderPickupDistrictName",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ProviderPickupProvinceCode",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ProviderPickupProvinceName",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ProviderPickupWardCode",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ProviderPickupWardName",
                table: "shipments");
        }
    }
}
