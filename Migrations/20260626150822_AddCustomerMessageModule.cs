using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerMessageModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_conversations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedStaffId = table.Column<long>(type: "bigint", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MessageCount = table.Column<int>(type: "int", nullable: false),
                    UnreadCustomerMessageCount = table.Column<int>(type: "int", nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastCustomerMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastStaffMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAiMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_conversations_staff_AssignedStaffId",
                        column: x => x.AssignedStaffId,
                        principalTable: "staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_conversations_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<long>(type: "bigint", nullable: false),
                    Sender = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsReadByAdmin = table.Column<bool>(type: "bit", nullable: false),
                    AiProvider = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    AiModel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AiPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiResponseId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    AiMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_customer_messages_customer_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "customer_conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_messages_staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_messages_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_conversations_AssignedStaffId",
                table: "customer_conversations",
                column: "AssignedStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_conversations_LastMessageAt",
                table: "customer_conversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_customer_conversations_UserId_Status",
                table: "customer_conversations",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_messages_ConversationId_CreatedAt",
                table: "customer_messages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_messages_Sender",
                table: "customer_messages",
                column: "Sender");

            migrationBuilder.CreateIndex(
                name: "IX_customer_messages_StaffId",
                table: "customer_messages",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_customer_messages_UserId",
                table: "customer_messages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_messages");

            migrationBuilder.DropTable(
                name: "customer_conversations");
        }
    }
}
