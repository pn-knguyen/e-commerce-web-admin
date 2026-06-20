using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace e_commerce_web_admin.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffIdentitySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_goods_receipts_users_ApprovedBy",
                table: "goods_receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_goods_receipts_users_CreatedBy",
                table: "goods_receipts");

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[role_permissions]', N'U') IS NOT NULL
                    DROP TABLE [role_permissions];
                """);

            migrationBuilder.CreateTable(
                name: "staff",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AvatarImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "staff_roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "staff_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_claims_staff_UserId",
                        column: x => x.UserId,
                        principalTable: "staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_logins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_staff_logins_staff_UserId",
                        column: x => x.UserId,
                        principalTable: "staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_tokens",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_staff_tokens_staff_UserId",
                        column: x => x.UserId,
                        principalTable: "staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_role_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_role_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_role_claims_staff_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "staff_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_user_roles",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_staff_user_roles_staff_UserId",
                        column: x => x.UserId,
                        principalTable: "staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_user_roles_staff_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "staff_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[users]', N'U') IS NOT NULL
                BEGIN
                    SET IDENTITY_INSERT [staff] ON;

                    INSERT INTO [staff] (
                        [Id],
                        [FullName],
                        [IsActive],
                        [AvatarImage],
                        [CreatedAt],
                        [UpdatedAt],
                        [UserName],
                        [NormalizedUserName],
                        [Email],
                        [NormalizedEmail],
                        [EmailConfirmed],
                        [PasswordHash],
                        [SecurityStamp],
                        [ConcurrencyStamp],
                        [PhoneNumber],
                        [PhoneNumberConfirmed],
                        [TwoFactorEnabled],
                        [LockoutEnabled],
                        [AccessFailedCount])
                    SELECT
                        [Id],
                        COALESCE(NULLIF([FullName], N''), NULLIF([Username], N''), CONCAT(N'Staff ', [Id])),
                        COALESCE([IsActive], CAST(1 AS bit)),
                        [AvatarImage],
                        COALESCE([CreatedAt], SYSUTCDATETIME()),
                        [UpdatedAt],
                        COALESCE(NULLIF([Username], N''), CONCAT(N'staff', [Id])),
                        UPPER(COALESCE(NULLIF([Username], N''), CONCAT(N'staff', [Id]))),
                        COALESCE(NULLIF([Email], N''), CONCAT(N'staff', [Id], N'@ecommerce.local')),
                        UPPER(COALESCE(NULLIF([Email], N''), CONCAT(N'staff', [Id], N'@ecommerce.local'))),
                        CAST(1 AS bit),
                        NULL,
                        CONVERT(nvarchar(36), NEWID()),
                        CONVERT(nvarchar(36), NEWID()),
                        [Phone],
                        CAST(0 AS bit),
                        CAST(0 AS bit),
                        CAST(1 AS bit),
                        0
                    FROM [users] AS [u]
                    WHERE (
                            [u].[Role] IN (N'Admin', N'Manager', N'Staff')
                            OR EXISTS (
                                SELECT 1
                                FROM [goods_receipts] AS [gr]
                                WHERE [gr].[CreatedBy] = [u].[Id]
                                    OR [gr].[ApprovedBy] = [u].[Id])
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM [staff] AS [s]
                            WHERE [s].[Id] = [u].[Id]);

                    SET IDENTITY_INSERT [staff] OFF;

                END
                """);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "staff",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "staff",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_staff_claims_UserId",
                table: "staff_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_logins_UserId",
                table: "staff_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_role_claims_RoleId",
                table: "staff_role_claims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "staff_roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_staff_user_roles_RoleId",
                table: "staff_user_roles",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_goods_receipts_staff_ApprovedBy",
                table: "goods_receipts",
                column: "ApprovedBy",
                principalTable: "staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_goods_receipts_staff_CreatedBy",
                table: "goods_receipts",
                column: "CreatedBy",
                principalTable: "staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_goods_receipts_staff_ApprovedBy",
                table: "goods_receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_goods_receipts_staff_CreatedBy",
                table: "goods_receipts");

            migrationBuilder.DropTable(
                name: "staff_claims");

            migrationBuilder.DropTable(
                name: "staff_logins");

            migrationBuilder.DropTable(
                name: "staff_role_claims");

            migrationBuilder.DropTable(
                name: "staff_tokens");

            migrationBuilder.DropTable(
                name: "staff_user_roles");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropTable(
                name: "staff_roles");

            migrationBuilder.AddForeignKey(
                name: "FK_goods_receipts_users_ApprovedBy",
                table: "goods_receipts",
                column: "ApprovedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_goods_receipts_users_CreatedBy",
                table: "goods_receipts",
                column: "CreatedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
