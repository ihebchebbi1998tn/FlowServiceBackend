# Neon Database SQL Scripts

This folder contains PostgreSQL-compatible SQL scripts for creating all database tables needed for the FlowService backend application.

## 📁 File Structure

The scripts are organized by module for better maintainability:

### Initial Setup (01-09)
1. **01_auth_and_users.sql** - Authentication and user management tables
2. **02_roles_and_skills.sql** - Roles, skills, and their relationships
3. **03_contacts.sql** - Contact management (CRM)
4. **04_articles.sql** - Materials and services catalog
5. **05_offers.sql** - Offers/quotes management
6. **06_sales.sql** - Sales pipeline management
7. **07_projects.sql** - Project and task management
8. **08_calendar.sql** - Calendar and event management
9. **09_lookups.sql** - Lookup tables and currencies

### Migrations (10+)
10. **10_fix_mainadminusers_schema.sql** - Fix missing columns in MainAdminUsers table

See **MIGRATIONS_LOG.md** for detailed migration history and schema reference.

## 🚀 Execution Order

**IMPORTANT:** Execute the scripts in numerical order (01 through 09) to ensure proper foreign key relationships.

\`\`\`bash
# Execute in order:
psql -U your_username -d your_database -f 01_auth_and_users.sql
psql -U your_username -d your_database -f 02_roles_and_skills.sql
psql -U your_username -d your_database -f 03_contacts.sql
psql -U your_username -d your_database -f 04_articles.sql
psql -U your_username -d your_database -f 05_offers.sql
psql -U your_username -d your_database -f 06_sales.sql
psql -U your_username -d your_database -f 07_projects.sql
psql -U your_username -d your_database -f 08_calendar.sql
psql -U your_username -d your_database -f 09_lookups.sql
\`\`\`

## 🔗 Using with Neon

### Option 1: Neon Console
1. Log in to your Neon account at https://console.neon.tech
2. Select your project and database
3. Go to the SQL Editor
4. Copy and paste each script in order
5. Execute each script

### Option 2: Neon CLI
\`\`\`bash
# Install Neon CLI
npm install -g neonctl

# Connect to your database
neonctl connection-string <project-id>

# Execute scripts
psql "your-connection-string" -f 01_auth_and_users.sql
# ... repeat for all scripts
\`\`\`

### Option 3: Connection String
\`\`\`bash
# Get your connection string from Neon console
export DATABASE_URL="postgresql://user:password@host/database?sslmode=require"

# Execute all scripts
for file in *.sql; do
  psql $DATABASE_URL -f $file
done
\`\`\`

## 📊 Database Schema Overview

### Total Tables: 33

**Auth & Users (3 tables)**
- MainAdminUsers, Users, UserPreferences

**Roles & Skills (5 tables)**
- Roles, Skills, UserRoles, UserSkills, RoleSkills

**Contacts (4 tables)**
- Contacts, ContactNotes, ContactTags, ContactTagAssignments

**Articles (4 tables)**
- articles, article_categories, locations, inventory_transactions

**Offers (3 tables)**
- offers, offer_items, offer_activities

**Sales (3 tables)**
- sales, sale_items, sale_activities

**Projects (6 tables)**
- Projects, ProjectColumns, ProjectTasks, DailyTasks, TaskComments, TaskAttachments

**Calendar (4 tables)**
- calendar_events, event_types, event_attendees, event_reminders

**Lookups (2 tables)**
- LookupItems, Currencies

## 🔑 Key Features

- **Foreign Key Constraints**: Proper relationships between tables
- **Indexes**: Optimized for common queries
- **Generated Columns**: Automatic calculation of totals
- **Default Values**: Sensible defaults for all fields
- **Timestamps**: Automatic tracking of creation and updates
- **Soft Deletes**: IsDeleted flags for data retention
- **JSONB Support**: Flexible data storage for complex fields

## ⚠️ Important Notes

1. **Idempotent Scripts**: All scripts use `IF NOT EXISTS` clauses, making them safe to run multiple times
2. **Data Loss**: These scripts only CREATE tables. They won't drop existing data
3. **Permissions**: Ensure your database user has CREATE TABLE privileges
4. **Neon Compatibility**: All scripts are PostgreSQL 14+ compatible (Neon's version)

## 🔄 Updating the Schema

If you need to modify the schema:
1. Create a new migration file (e.g., `10_migration_name.sql`)
2. Use `ALTER TABLE` statements instead of `CREATE TABLE`
3. Document the changes in this README

## 📝 Entity Framework Integration

These scripts match the Entity Framework models in the C# backend. After running these scripts, your EF Core migrations should recognize the existing schema.

To sync EF Core with the database:
\`\`\`bash
dotnet ef migrations add InitialCreate
dotnet ef database update
\`\`\`

## 🆘 Troubleshooting

**Error: relation already exists**
- This is normal if tables already exist. The scripts are idempotent.

**Error: foreign key constraint**
- Ensure you're running scripts in the correct order (01-09)

**Error: permission denied**
- Check your database user has CREATE privileges

**Connection timeout**
- Neon databases may sleep after inactivity. Wait a moment and retry.

## 📞 Support

For issues related to:
- **Neon Platform**: https://neon.tech/docs
- **PostgreSQL**: https://www.postgresql.org/docs/
- **Application Schema**: Contact the development team
