using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAddressSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "user_addresses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "user_addresses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 1L,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 2L,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 3L,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 4L,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "user_addresses",
                keyColumn: "Id",
                keyValue: 5L,
                column: "DeletedAt",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_user_addresses_UserId_IsDeleted",
                table: "user_addresses",
                columns: new[] { "UserId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_addresses_UserId_IsDeleted",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "user_addresses");
        }
    }
}
