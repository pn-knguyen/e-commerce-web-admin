using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerMessageClientMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientMessageId",
                table: "customer_messages",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_messages_StaffId_Sender_ClientMessageId",
                table: "customer_messages",
                columns: new[] { "StaffId", "Sender", "ClientMessageId" },
                unique: true,
                filter: "[ClientMessageId] IS NOT NULL AND [StaffId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_messages_UserId_Sender_ClientMessageId",
                table: "customer_messages",
                columns: new[] { "UserId", "Sender", "ClientMessageId" },
                unique: true,
                filter: "[ClientMessageId] IS NOT NULL AND [UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_messages_StaffId_Sender_ClientMessageId",
                table: "customer_messages");

            migrationBuilder.DropIndex(
                name: "IX_customer_messages_UserId_Sender_ClientMessageId",
                table: "customer_messages");

            migrationBuilder.DropColumn(
                name: "ClientMessageId",
                table: "customer_messages");
        }
    }
}
