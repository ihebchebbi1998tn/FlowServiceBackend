using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FlowServiceBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreatePostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MainAdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RefreshToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CompanyWebsite = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Preferences = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainAdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "system"),
                    Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "en"),
                    PrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "blue"),
                    LayoutMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "sidebar"),
                    DataView = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "table"),
                    Timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateFormat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "MM/DD/YYYY"),
                    TimeFormat = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "12h"),
                    Currency = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "USD"),
                    NumberFormat = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "comma"),
                    Notifications = table.Column<string>(type: "text", nullable: true, defaultValue: "{}"),
                    SidebarCollapsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CompactMode = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ShowTooltips = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AnimationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SoundEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AutoSave = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    WorkArea = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DashboardLayout = table.Column<string>(type: "text", nullable: true),
                    QuickAccessItems = table.Column<string>(type: "text", nullable: true, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_MainAdminUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "MainAdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MainAdminUsers_CreatedAt",
                table: "MainAdminUsers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MainAdminUsers_Email",
                table: "MainAdminUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MainAdminUsers_IsActive",
                table: "MainAdminUsers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "MainAdminUsers");
        }
    }
}