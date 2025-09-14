using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace FlowServiceBackend.Migrations
{
    /// <summary>
    /// Initial database migration for Main_AdminUser table
    /// </summary>
    public partial class InitialCreate : Migration
    {
        /// <summary>
        /// Up migration - creates the Main_AdminUser table
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MainAdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Industry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CompanyWebsite = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Preferences = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainAdminUsers", x => x.Id);
                });

            // Create unique index on Email
            migrationBuilder.CreateIndex(
                name: "IX_MainAdminUsers_Email",
                table: "MainAdminUsers",
                column: "Email",
                unique: true);

            // Create index on IsActive for performance
            migrationBuilder.CreateIndex(
                name: "IX_MainAdminUsers_IsActive",
                table: "MainAdminUsers",
                column: "IsActive");

            // Create index on CreatedAt for performance
            migrationBuilder.CreateIndex(
                name: "IX_MainAdminUsers_CreatedAt",
                table: "MainAdminUsers",
                column: "CreatedAt");
        }

        /// <summary>
        /// Down migration - drops the Main_AdminUser table
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MainAdminUsers");
        }
    }
}