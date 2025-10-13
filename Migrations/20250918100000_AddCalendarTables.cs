using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create event_types table
            migrationBuilder.CreateTable(
                name: "event_types",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_types", x => x.Id);
                });

            // Create calendar_events table
            migrationBuilder.CreateTable(
                name: "calendar_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    End = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    all_day = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Attendees = table.Column<string>(type: "jsonb", nullable: true),
                    related_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    related_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    Reminders = table.Column<string>(type: "jsonb", nullable: true),
                    Recurring = table.Column<string>(type: "jsonb", nullable: true),
                    is_private = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calendar_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_calendar_events_Contacts_contact_id",
                        column: x => x.contact_id,
                        principalTable: "Contacts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_calendar_events_event_types_Type",
                        column: x => x.Type,
                        principalTable: "event_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.CheckConstraint("CK_calendar_events_Status", "\"Status\" IN ('scheduled', 'confirmed', 'cancelled', 'completed')");
                    table.CheckConstraint("CK_calendar_events_Priority", "\"Priority\" IN ('low', 'medium', 'high', 'urgent')");
                    table.CheckConstraint("CK_calendar_events_RelatedType", "related_type IN ('contact', 'sale', 'offer', 'project', 'service_order')");
                });

            // Create event_attendees table
            migrationBuilder.CreateTable(
                name: "event_attendees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    Response = table.Column<string>(type: "text", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_attendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_attendees_calendar_events_event_id",
                        column: x => x.event_id,
                        principalTable: "calendar_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.CheckConstraint("CK_event_attendees_Status", "\"Status\" IN ('pending', 'accepted', 'declined', 'tentative')");
                });

            // Create event_reminders table
            migrationBuilder.CreateTable(
                name: "event_reminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "email"),
                    minutes_before = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_reminders_calendar_events_event_id",
                        column: x => x.event_id,
                        principalTable: "calendar_events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.CheckConstraint("CK_event_reminders_Type", "\"Type\" IN ('email', 'notification', 'sms')");
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_Start",
                table: "calendar_events",
                column: "Start");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_End",
                table: "calendar_events",
                column: "End");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_Type",
                table: "calendar_events",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_Status",
                table: "calendar_events",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_contact_id",
                table: "calendar_events",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_related_type",
                table: "calendar_events",
                column: "related_type");

            migrationBuilder.CreateIndex(
                name: "IX_calendar_events_related_id",
                table: "calendar_events",
                column: "related_id");

            migrationBuilder.CreateIndex(
                name: "IX_event_attendees_event_id",
                table: "event_attendees",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_event_reminders_event_id",
                table: "event_reminders",
                column: "event_id");

            // Insert default event types
            migrationBuilder.InsertData(
                table: "event_types",
                columns: new[] { "Id", "Name", "Description", "Color", "is_default", "is_active" },
                values: new object[,]
                {
                    { "meeting", "Meeting", "General meetings and discussions", "#3B82F6", true, true },
                    { "appointment", "Appointment", "Client appointments", "#10B981", false, true },
                    { "call", "Phone Call", "Scheduled phone calls", "#F59E0B", false, true },
                    { "task", "Task", "Task reminders and deadlines", "#EF4444", false, true },
                    { "event", "Event", "Special events and occasions", "#8B5CF6", false, true },
                    { "reminder", "Reminder", "General reminders", "#6B7280", false, true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "event_reminders");
            migrationBuilder.DropTable(name: "event_attendees");
            migrationBuilder.DropTable(name: "calendar_events");
            migrationBuilder.DropTable(name: "event_types");
        }
    }
}
