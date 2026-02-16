# Fresh Database Setup - Complete Instructions

## 🎯 Complete Fresh Start for Neon Database

This guide provides a complete fresh start approach to setting up your database with all 50 tables.

## 📋 Prerequisites

- Neon database account
- PostgreSQL connection details
- Database admin access

## 🔄 Step-by-Step Process

### Step 1: Clean the Database Completely

Run the cleanup script on your Neon database:

```bash
# Connect to your Neon database
psql "postgresql://neondb_owner:npg_4xqdPl6UNbYu@ep-weathered-voice-a4hjp1xs-pooler.us-east-1.aws.neon.tech/neondb?sslmode=require"

# Run the cleanup script
\i Database/cleanup_database.sql
```

This will drop all existing tables in the correct order.

### Step 2: Apply the Fresh Migration

After cleaning the database, run the migration:

```bash
# In your project root
dotnet ef database update

# Or if the app is running, restart it - migrations will auto-apply
```

### Step 3: Verify Database Setup

```sql
-- Check all tables were created
SELECT tablename 
FROM pg_tables 
WHERE schemaname = 'public' 
ORDER BY tablename;

-- Should return 50 tables:
-- 1. MainAdminUsers
-- 2. Users
-- 3. UserPreferences
-- 4. Roles
-- 5. Skills
-- 6. UserRoles
-- 7. RoleSkills
-- 8. UserSkills
-- 9. Contacts
-- 10. ContactNotes
-- 11. ContactTags
-- 12. ContactTagAssignments
-- 13. Locations
-- 14. ArticleCategories
-- 15. Articles
-- 16. InventoryTransactions
-- 17. EventTypes
-- 18. CalendarEvents
-- 19. EventAttendees
-- 20. EventReminders
-- 21. Offers
-- 22. OfferItems
-- 23. OfferActivities
-- 24. Sales
-- 25. SaleItems
-- 26. SaleActivities
-- 27. Installations
-- 28. MaintenanceHistory
-- 29. ServiceOrders
-- 30. ServiceOrderJobs
-- 31. Projects
-- 32. ProjectColumns
-- 33. ProjectTasks
-- 34. DailyTasks
-- 35. TaskComments
-- 36. TaskAttachments
-- 37. Dispatches
-- 38. DispatchTechnicians
-- 39. TimeEntries
-- 40. MaterialUsage
-- 41. Expenses
-- 42. Notes
-- 43. Attachments
-- 44. TechnicianWorkingHours
-- 45. TechnicianLeave
-- 46. TechnicianStatusHistory
-- 47. DispatchHistory
-- 48. LookupItems
-- 49. Currencies
-- 50. __EFMigrationsHistory
```

### Step 4: Verify Foreign Keys

```sql
-- Check foreign key constraints
SELECT
    tc.table_name, 
    tc.constraint_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM information_schema.table_constraints AS tc 
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY' 
ORDER BY tc.table_name;
```

## 🚀 What This Migration Creates

### Core Tables (11)
- **MainAdminUsers**: Main admin authentication
- **Users**: Regular user accounts
- **UserPreferences**: User settings
- **Roles**: User roles
- **Skills**: Available skills
- **UserRoles**: User-role assignments
- **RoleSkills**: Role-skill requirements
- **UserSkills**: User skill proficiencies
- **LookupItems**: Lookup data
- **Currencies**: Currency definitions

### Contact Management (4)
- **Contacts**: Customer/contact information
- **ContactNotes**: Notes for contacts
- **ContactTags**: Tag definitions
- **ContactTagAssignments**: Tag assignments

### Inventory Management (4)
- **Locations**: Storage locations
- **ArticleCategories**: Product categories
- **Articles**: Products/inventory items
- **InventoryTransactions**: Stock movements

### Calendar (4)
- **EventTypes**: Event categories
- **CalendarEvents**: Scheduled events
- **EventAttendees**: Event participants
- **EventReminders**: Event notifications

### Sales & Offers (6)
- **Offers**: Price quotations
- **OfferItems**: Offer line items
- **OfferActivities**: Offer history
- **Sales**: Sales orders
- **SaleItems**: Sale line items
- **SaleActivities**: Sales history

### Installations (2)
- **Installations**: Installation records
- **MaintenanceHistory**: Maintenance logs

### Service Orders (2)
- **ServiceOrders**: Service requests
- **ServiceOrderJobs**: Individual jobs

### Project Management (6)
- **Projects**: Project records
- **ProjectColumns**: Kanban columns
- **ProjectTasks**: Project tasks
- **DailyTasks**: Daily todo items
- **TaskComments**: Task discussions
- **TaskAttachments**: Task files

### Dispatch Management (7)
- **Dispatches**: Field service dispatches
- **DispatchTechnicians**: Assigned technicians
- **TimeEntries**: Time tracking
- **MaterialUsage**: Materials used
- **Expenses**: Dispatch expenses
- **Notes**: Dispatch notes
- **Attachments**: Dispatch files

### Planning (4)
- **TechnicianWorkingHours**: Work schedules
- **TechnicianLeave**: Leave requests
- **TechnicianStatusHistory**: Status tracking
- **DispatchHistory**: Dispatch status log

## 🔍 Troubleshooting

### If Migration Fails

1. **Check connection string**:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "postgresql://neondb_owner:npg_4xqdPl6UNbYu@ep-weathered-voice-a4hjp1xs-pooler.us-east-1.aws.neon.tech/neondb?sslmode=require"
     }
   }
   ```

2. **Verify database is clean**:
   ```sql
   SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';
   -- Should return 0 after cleanup
   ```

3. **Check migration history**:
   ```bash
   dotnet ef migrations list
   ```

4. **Force re-apply migration**:
   ```bash
   dotnet ef database update --force
   ```

### If Tables Are Missing

Re-run the cleanup and migration:
```bash
psql "postgresql://..." -f Database/cleanup_database.sql
dotnet ef database update
```

## ✅ Success Indicators

- ✅ 50 tables created successfully
- ✅ All foreign key constraints in place
- ✅ All indexes created
- ✅ API starts without errors
- ✅ Swagger UI loads correctly
- ✅ All endpoints accessible

## 📝 Notes

- This migration creates ALL 50 tables from scratch
- All tables use proper PascalCase naming
- All foreign keys are properly configured
- All indexes are in place for performance
- Default values and constraints are set correctly

## 🎉 Next Steps

After successful setup:
1. Create your first admin user via API
2. Test all endpoints in Swagger
3. Begin adding data
4. Set up automated backups on Neon dashboard
