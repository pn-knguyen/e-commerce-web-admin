using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_orders_CreatedAt",
                table: "orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_orders_OrderStatus_PaymentStatus_CreatedAt",
                table: "orders",
                columns: new[] { "OrderStatus", "PaymentStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_PaymentMethodId_CreatedAt",
                table: "orders",
                columns: new[] { "PaymentMethodId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_UserId_CreatedAt",
                table: "orders",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_UserId_CreatedAt",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_PaymentMethodId_CreatedAt",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_OrderStatus_PaymentStatus_CreatedAt",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_CreatedAt",
                table: "orders");
        }
    }
}
