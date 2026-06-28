using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCustomerMessageStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageCount",
                table: "customer_conversations");

            migrationBuilder.DropColumn(
                name: "UnreadCustomerMessageCount",
                table: "customer_conversations");

            migrationBuilder.Sql(
                "UPDATE customer_messages SET AiResponseId = NULL WHERE LTRIM(RTRIM(AiResponseId)) = N'';");
            migrationBuilder.Sql(
                """
                WITH duplicate_responses AS
                (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY AiResponseId ORDER BY Id) AS duplicate_number
                    FROM customer_messages
                    WHERE AiResponseId IS NOT NULL
                )
                UPDATE customer_messages
                SET AiResponseId = NULL
                WHERE Id IN
                (
                    SELECT Id
                    FROM duplicate_responses
                    WHERE duplicate_number > 1
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_customer_messages_AiResponseId",
                table: "customer_messages",
                column: "AiResponseId",
                unique: true,
                filter: "[AiResponseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_messages_ConversationId_Sender_IsReadByAdmin",
                table: "customer_messages",
                columns: new[] { "ConversationId", "Sender", "IsReadByAdmin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customer_messages_AiResponseId",
                table: "customer_messages");

            migrationBuilder.DropIndex(
                name: "IX_customer_messages_ConversationId_Sender_IsReadByAdmin",
                table: "customer_messages");

            migrationBuilder.AddColumn<int>(
                name: "MessageCount",
                table: "customer_conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnreadCustomerMessageCount",
                table: "customer_conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
