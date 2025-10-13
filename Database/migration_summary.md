# MyApi Database Migration Summary

## Overview
This document provides a comprehensive overview of all database migrations and the current schema structure for the MyApi project.

## Migration History

| Migration File | Date | Description |
|---|---|---|
| `20250914190000_InitialCreatePostgreSQL.cs` | 2025-09-14 | Initial creation of MainAdminUsers and UserPreferences tables |
| `20240915000001_AddRolesTables.cs` | 2024-09-15 | Added Roles and UserRoles tables |
| `20250915130000_CreateUsersTable.cs` | 2025-09-15 | Created regular Users table |
| `20240916000001_AddSkillsTables.cs` | 2024-09-16 | Added Skills and UserSkills tables |
| `20240916000002_AddRoleSkillsTable.cs` | 2024-09-16 | Added RoleSkills junction table |
| `20240918000001_AddContactsModule.cs` | 2024-09-18 | Complete contacts management system |
| `20250918090000_AddArticlesTable.cs` | 2025-09-18 | Articles table for materials and services |
| `20250918100000_AddCalendarTables.cs` | 2025-09-18 | Calendar system with events, types, attendees, reminders |
| `20250918110000_AddLookupsTable.cs` | 2025-09-18 | Lookup items and currencies tables |

## Database Schema

### 1. User Management

#### MainAdminUsers
- **Purpose**: Primary admin users table
- **Key Fields**: Id, Email, PasswordHash, FirstName, LastName, Company info
- **Features**: JWT token storage, preferences, audit trails
- **Indexes**: Email (unique), CreatedAt, IsActive

#### UserPreferences  
- **Purpose**: User-specific application preferences
- **Key Fields**: UserId, Theme, Language, Layout settings
- **Features**: UI customization, localization, dashboard layout
- **Relationship**: One-to-one with MainAdminUsers

#### Users
- **Purpose**: Regular users table (separate from admin users)
- **Key Fields**: Id, Email, PasswordHash, Role, audit fields
- **Features**: Token management, role-based access
- **Indexes**: Email (unique)

### 2. Role & Permission System

#### Roles
- **Purpose**: Define user roles and permissions
- **Key Fields**: Id, Name, Description, audit fields
- **Features**: Soft delete, hierarchical structure support
- **Indexes**: Name (unique)

#### UserRoles
- **Purpose**: Junction table for user-role assignments
- **Key Fields**: UserId, RoleId, AssignedBy, AssignedAt
- **Features**: Many-to-many relationship, assignment tracking
- **Relationships**: MainAdminUsers ↔ Roles

### 3. Skills Management

#### Skills
- **Purpose**: Define available skills and competencies
- **Key Fields**: Id, Name, Category, Level, Description
- **Features**: Categorized skills, soft delete
- **Indexes**: Name (unique), Category

#### UserSkills
- **Purpose**: User skill assignments with proficiency
- **Key Fields**: UserId, SkillId, ProficiencyLevel, YearsOfExperience
- **Features**: Skill assessment, certifications tracking
- **Relationships**: Users ↔ Skills

#### RoleSkills
- **Purpose**: Required skills for specific roles
- **Key Fields**: RoleId, SkillId, AssignedBy, Notes
- **Features**: Role-based skill requirements
- **Relationships**: Roles ↔ Skills

### 4. Contact Management

#### Contacts
- **Purpose**: Client and business contact management
- **Key Fields**: Id, Name, Email, Company, Position, Status
- **Features**: Contact categorization, favorites, soft delete
- **Indexes**: Email, Name, Status, Type, CreatedAt, IsDeleted

#### ContactTags
- **Purpose**: Tagging system for contacts
- **Key Fields**: Id, Name, Color, Description
- **Features**: Color-coded tags, soft delete
- **Indexes**: Name (unique where not deleted), IsDeleted

#### ContactNotes
- **Purpose**: Notes and comments for contacts
- **Key Fields**: Id, ContactId, Content, CreatedBy
- **Features**: Audit trail, rich text support
- **Relationships**: Many-to-one with Contacts

#### ContactTagAssignments
- **Purpose**: Junction table for contact-tag relationships
- **Key Fields**: ContactId, TagId, AssignedBy, AssignedAt
- **Features**: Many-to-many relationship, assignment tracking
- **Indexes**: Unique constraint on (ContactId, TagId)

### 5. Articles (Materials & Services)

#### Articles
- **Purpose**: Inventory and service catalog management
- **Key Fields**: Id (UUID), Name, Type, Category, Status
- **Features**: 
  - Dual-purpose: Materials (inventory) and Services
  - Material fields: SKU, Stock, Pricing, Supplier
  - Service fields: Duration, Skills, Hourly rates
- **Constraints**: Type CHECK constraint ('material', 'service')
- **Indexes**: Type, Category, Status, CreatedAt, IsActive

### 6. Calendar System

#### event_types
- **Purpose**: Define event categories and types
- **Key Fields**: Id, Name, Color, Description
- **Features**: Color-coded event types, default type support
- **Seed Data**: meeting, appointment, call, task, event, reminder

#### calendar_events
- **Purpose**: Calendar events and appointments
- **Key Fields**: Id (UUID), Title, Start, End, Type, Status, Priority
- **Features**: 
  - All-day events support
  - JSONB for attendees and reminders
  - Related entity linking (contacts, projects, etc.)
  - Privacy settings
- **Constraints**: Status and Priority CHECK constraints
- **Indexes**: Start, End, Type, Status, contact_id, related fields

#### event_attendees
- **Purpose**: Event participant management
- **Key Fields**: Id (UUID), event_id, Email, Name, Status
- **Features**: RSVP tracking, response management
- **Constraints**: Status CHECK constraint
- **Relationships**: Many-to-one with calendar_events

#### event_reminders
- **Purpose**: Event notification system
- **Key Fields**: Id (UUID), event_id, Type, minutes_before
- **Features**: Multiple reminder types (email, notification, SMS)
- **Constraints**: Type CHECK constraint
- **Relationships**: Many-to-one with calendar_events

### 7. Lookup System

#### LookupItems
- **Purpose**: Configurable dropdown and reference data
- **Key Fields**: Id, Name, LookupType, SortOrder, Color
- **Features**: 
  - Multi-purpose lookup system
  - Color support for UI elements
  - Hierarchical data support (Level field)
  - Custom properties for specific lookup types
- **Seed Data**: article-category, task-status
- **Indexes**: (LookupType, IsDeleted), SortOrder

#### Currencies
- **Purpose**: Multi-currency support
- **Key Fields**: Id, Name, Symbol, Code
- **Features**: Default currency selection, sort ordering
- **Seed Data**: USD, EUR, GBP, TND
- **Indexes**: Code (unique), SortOrder

## Key Features

### 1. Audit Trails
- CreatedAt/UpdatedAt timestamps on all major tables
- CreatedBy/ModifiedBy user tracking
- Soft delete support (IsDeleted flags)

### 2. Data Integrity
- Foreign key constraints with CASCADE deletes where appropriate
- CHECK constraints for enumerated values
- Unique constraints for business rules

### 3. Performance Optimization
- Strategic indexing on frequently queried columns
- Composite indexes for complex queries
- UUID primary keys for distributed systems

### 4. Flexibility
- JSONB fields for semi-structured data
- Extensible lookup system
- Configurable user preferences

### 5. Internationalization
- Multi-language support in user preferences
- Multiple currency support
- Timezone-aware timestamps

## Usage Instructions

### Running the Complete Recreation Script
\`\`\`bash
# Connect to your PostgreSQL database
psql -h your_host -d your_database -U your_username

# Run the complete recreation script
\i /path/to/FlowServiceBackend/Database/complete_database_recreation.sql
\`\`\`

### Verification
After running the script, verify the installation:
\`\`\`sql
-- Check all tables exist
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

-- Check seed data
SELECT COUNT(*) as event_types FROM event_types;
SELECT COUNT(*) as lookup_items FROM "LookupItems";
SELECT COUNT(*) as currencies FROM "Currencies";
\`\`\`

## Notes

- **PostgreSQL Specific**: This schema uses PostgreSQL-specific features (JSONB, UUID, etc.)
- **Case Sensitivity**: Table names use mixed case as per Entity Framework conventions
- **Data Loss Warning**: The complete recreation script will drop all existing data
- **Migration Safety**: Always backup your database before running migration scripts
- **Environment**: Test thoroughly in development before applying to production

## Future Considerations

1. **Partitioning**: Consider partitioning large tables like calendar_events by date
2. **Archiving**: Implement archiving strategy for historical data
3. **Monitoring**: Add database monitoring for performance optimization
4. **Backup**: Implement automated backup strategies
5. **Scaling**: Consider read replicas for reporting queries
