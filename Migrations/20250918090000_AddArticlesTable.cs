using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddArticlesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),

                    SKU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: true),
                    MinStock = table.Column<int>(type: "integer", nullable: true),
                    CostPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    SellPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),

                    BasePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Duration = table.Column<int>(type: "integer", nullable: true),
                    SkillsRequired = table.Column<string>(type: "text", nullable: true),
                    MaterialsNeeded = table.Column<string>(type: "text", nullable: true),
                    PreferredUsers = table.Column<string>(type: "text", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    EstimatedDuration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MaterialsIncluded = table.Column<bool>(type: "boolean", nullable: true),
                    WarrantyCoverage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ServiceArea = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Inclusions = table.Column<string>(type: "text", nullable: true),
                    AddOnServices = table.Column<string>(type: "text", nullable: true),

                    LastUsed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),

                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                });

            // Check constraint for Type
            migrationBuilder.AddCheckConstraint(
                name: "CK_Articles_Type",
                table: "Articles",
                sql: "\"Type\" IN ('material','service')");

            // Indexes
            migrationBuilder.CreateIndex(name: "IX_Articles_Type", table: "Articles", column: "Type");
            migrationBuilder.CreateIndex(name: "IX_Articles_Category", table: "Articles", column: "Category");
            migrationBuilder.CreateIndex(name: "IX_Articles_Status", table: "Articles", column: "Status");
            migrationBuilder.CreateIndex(name: "IX_Articles_CreatedAt", table: "Articles", column: "CreatedAt");
            migrationBuilder.CreateIndex(name: "IX_Articles_IsActive", table: "Articles", column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Articles");
        }
    }
}
