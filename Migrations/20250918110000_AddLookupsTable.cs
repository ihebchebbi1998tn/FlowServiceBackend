using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create LookupItems table
            migrationBuilder.CreateTable(
                name: "LookupItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LookupType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModifyUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: true),
                    DefaultDuration = table.Column<int>(type: "integer", nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: true),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupItems", x => x.Id);
                });

            // Create Currencies table
            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModifyUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_LookupItems_LookupType_IsDeleted",
                table: "LookupItems",
                columns: new[] { "LookupType", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_LookupItems_SortOrder",
                table: "LookupItems",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_SortOrder",
                table: "Currencies",
                column: "SortOrder");

            // Insert seed data
            var createdAt = DateTime.UtcNow;

            // Article Categories
            migrationBuilder.InsertData(
                table: "LookupItems",
                columns: new[] { "Id", "Name", "LookupType", "IsActive", "SortOrder", "CreatedUser", "CreatedAt", "IsDeleted" },
                values: new object[,]
                {
                    { "general", "General", "article-category", true, 0, "system", createdAt, false },
                    { "hardware", "Hardware", "article-category", true, 1, "system", createdAt, false },
                    { "software", "Software", "article-category", true, 2, "system", createdAt, false },
                    { "accessories", "Accessories", "article-category", true, 3, "system", createdAt, false }
                });

            // Task Statuses
            migrationBuilder.InsertData(
                table: "LookupItems",
                columns: new[] { "Id", "Name", "Color", "Description", "LookupType", "IsActive", "SortOrder", "CreatedUser", "CreatedAt", "IsDeleted" },
                values: new object[,]
                {
                    { "todo", "To Do", "#64748b", "Tasks to be started", "task-status", true, 0, "system", createdAt, false },
                    { "progress", "In Progress", "#3b82f6", "Currently working on", "task-status", true, 1, "system", createdAt, false },
                    { "review", "Review", "#f59e0b", "Ready for review", "task-status", true, 2, "system", createdAt, false },
                    { "done", "Done", "#10b981", "Completed tasks", "task-status", true, 3, "system", createdAt, false }
                });

            // Currencies
            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "Name", "Symbol", "Code", "IsActive", "IsDefault", "SortOrder", "CreatedUser", "CreatedAt", "IsDeleted" },
                values: new object[,]
                {
                    { "USD", "USD ($)", "$", "USD", true, true, 0, "system", createdAt, false },
                    { "EUR", "EUR (€)", "€", "EUR", true, false, 1, "system", createdAt, false },
                    { "GBP", "GBP (£)", "£", "GBP", true, false, 2, "system", createdAt, false },
                    { "TND", "TND (د.ت)", "د.ت", "TND", true, false, 3, "system", createdAt, false }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LookupItems");
            migrationBuilder.DropTable(name: "Currencies");
        }
    }
}
