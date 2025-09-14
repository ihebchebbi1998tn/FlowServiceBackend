using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowServiceBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LayoutMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataView = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateFormat = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TimeFormat = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    NumberFormat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Notifications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SidebarCollapsed = table.Column<bool>(type: "bit", nullable: false),
                    CompactMode = table.Column<bool>(type: "bit", nullable: false),
                    ShowTooltips = table.Column<bool>(type: "bit", nullable: false),
                    AnimationsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SoundEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AutoSave = table.Column<bool>(type: "bit", nullable: false),
                    WorkArea = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DashboardLayout = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuickAccessItems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
        }
    }
}