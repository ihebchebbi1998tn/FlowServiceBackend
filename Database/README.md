# MyApi Database Management

## 📋 Overview

This directory contains database management scripts and documentation for the MyApi project. The database uses PostgreSQL and Entity Framework Core migrations.

## 📁 Directory Structure

\`\`\`
FlowServiceBackend/Database/
├── complete_database_recreation.sql    # Complete database recreation script
├── migration_summary.md                 # Detailed migration history and schema documentation  
├── README.md                           # This file
└── scripts/                            # Additional utility scripts (future)
\`\`\`

## 🚀 Quick Start

### Option 1: Complete Database Recreation (Recommended for Development)

**⚠️ WARNING: This will delete all existing data!**

\`\`\`sql
-- Connect to PostgreSQL
psql -h your_host -d your_database -U your_username

-- Run the complete recreation script
\i complete_database_recreation.sql
\`\`\`

### Option 2: Entity Framework Migrations (Production)

\`\`\`bash
# Update database using EF migrations
cd FlowServiceBackend
dotnet ef database update

# Create new migration (when needed)
dotnet ef migrations add YourMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove
\`\`\`

## 📊 Database Schema Overview

### Core Tables (19 tables total)

| Module | Tables | Description |
|--------|--------|-------------|
| **User Management** | MainAdminUsers, UserPreferences, Users | Admin and regular user management with preferences |
| **Access Control** | Roles, UserRoles | Role-based access control system |
| **Skills** | Skills, UserSkills, RoleSkills | Skill management and assignments |
| **Contacts** | Contacts, ContactTags, ContactNotes, ContactTagAssignments | Complete contact management system |
| **Inventory** | Articles | Materials and services catalog |
| **Calendar** | calendar_events, event_types, event_attendees, event_reminders | Full calendar and event management |
| **Configuration** | LookupItems, Currencies | System-wide lookup data and multi-currency support |

## 🔧 Key Features

### ✅ Data Integrity
- Foreign key constraints with proper cascade rules
- Check constraints for enumerated values  
- Unique constraints for business rules
- Soft delete support on major entities

### ⚡ Performance
- Strategic indexing on frequently queried columns
- Composite indexes for complex queries
- UUID primary keys for distributed architecture
- Efficient JSONB usage for semi-structured data

### 🌍 Internationalization
- Multi-language support
- Multi-currency system
- Timezone-aware timestamps (TIMESTAMPTZ)

### 📈 Scalability
- Flexible lookup system for easy configuration
- Extensible user preferences
- Modular table design
- JSONB for flexible data structures

## 🛡️ Migration History

| Date | Migration | Purpose |
|------|-----------|---------|
| 2025-09-14 | InitialCreatePostgreSQL | Base admin users and preferences |
| 2024-09-15 | AddRolesTables | Role-based access control |
| 2025-09-15 | CreateUsersTable | Regular users table |
| 2024-09-16 | AddSkillsTables | Skills management system |
| 2024-09-16 | AddRoleSkillsTable | Role-skill relationships |
| 2024-09-18 | AddContactsModule | Complete contact management |
| 2025-09-18 | AddArticlesTable | Materials and services catalog |
| 2025-09-18 | AddCalendarTables | Calendar and event system |
| 2025-09-18 | AddLookupsTable | Lookup data and currencies |

## 📋 Verification Checklist

After running database scripts, verify:

\`\`\`sql
-- ✅ Check all tables exist (should return 19 tables)
SELECT COUNT(*) as total_tables 
FROM information_schema.tables 
WHERE table_schema = 'public';

-- ✅ Check seed data loaded
SELECT 'event_types' as table_name, COUNT(*) as rows FROM event_types
UNION ALL
SELECT 'LookupItems', COUNT(*) FROM "LookupItems"  
UNION ALL
SELECT 'Currencies', COUNT(*) FROM "Currencies";

-- ✅ Check indexes created
SELECT COUNT(*) as total_indexes 
FROM pg_indexes 
WHERE schemaname = 'public';

-- ✅ Check foreign key constraints
SELECT COUNT(*) as foreign_keys 
FROM information_schema.table_constraints 
WHERE constraint_type = 'FOREIGN KEY' 
AND table_schema = 'public';
\`\`\`

Expected Results:
- **Tables**: 19 total tables
- **Seed Data**: 6 event types, 8 lookup items, 4 currencies
- **Indexes**: 35+ indexes for performance
- **Foreign Keys**: 15+ foreign key relationships

## 🔥 Troubleshooting

### Common Issues

#### 1. Permission Errors
\`\`\`sql
-- Grant necessary permissions
GRANT ALL PRIVILEGES ON DATABASE your_database TO your_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO your_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO your_user;
\`\`\`

#### 2. Connection Issues
\`\`\`bash
# Check PostgreSQL is running
sudo systemctl status postgresql

# Test connection
psql -h localhost -d your_database -U your_user -c "SELECT version();"
\`\`\`

#### 3. Entity Framework Issues
\`\`\`bash
# Clear EF cache
dotnet ef migrations list
dotnet clean
dotnet build

# Reset migrations (development only!)
dotnet ef database drop --force
dotnet ef database update
\`\`\`

#### 4. Case Sensitivity Issues
PostgreSQL uses quoted identifiers for mixed-case table names:
\`\`\`sql
-- ✅ Use quotes for mixed case
SELECT * FROM "MainAdminUsers";

-- ❌ This won't work
SELECT * FROM MainAdminUsers;
\`\`\`

## 🚨 Safety Guidelines

### Development Environment
- ✅ Safe to use complete recreation script
- ✅ Can drop and recreate database anytime
- ✅ Use for testing and development

### Production Environment  
- ⚠️ **NEVER** use complete recreation script
- ✅ Only use Entity Framework migrations
- ✅ Always backup before migrations
- ✅ Test migrations in staging first

### Backup Commands
\`\`\`bash
# Create backup
pg_dump -h localhost -U username -d database_name > backup.sql

# Restore backup
psql -h localhost -U username -d database_name < backup.sql
\`\`\`

## 📞 Support

### For Database Issues:
1. Check the migration_summary.md for detailed schema documentation
2. Verify your PostgreSQL version (requires 12+)
3. Ensure proper permissions and connection settings
4. Check application logs for Entity Framework errors

### For Development:
- Use the complete recreation script for clean development setups
- Always run in development environment first
- Keep migration files in version control
- Document any manual schema changes

## 🎯 Best Practices

1. **Always backup** before running database scripts
2. **Test in development** before applying to production  
3. **Use migrations** for production changes
4. **Monitor performance** after schema changes
5. **Keep documentation updated** when adding new tables
6. **Follow naming conventions** established in existing schema
7. **Use appropriate data types** for PostgreSQL optimization

---

🚀 **Ready to go?** Run the complete recreation script in your development environment and start building!
