using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerConversationChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "customer_conversations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_customer_conversations_UserId_Channel_LastMessageAt",
                table: "customer_conversations",
                columns: new[] { "UserId", "Channel", "LastMessageAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_conversations_UserId_Channel_LastMessageAt",
                table: "customer_conversations");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "customer_conversations");
        }
    }
}
