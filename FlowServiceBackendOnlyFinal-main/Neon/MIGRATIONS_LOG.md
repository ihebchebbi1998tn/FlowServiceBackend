# Database Migrations Log

This file tracks all database schema changes applied to the production database.

## Migration History

| Date | File | Description | Status |
|------|------|-------------|--------|
| 2025-01-XX | 01-09_*.sql | Initial schema setup | ✅ Applied |
| 2025-12-02 | 10_fix_mainadminusers_schema.sql | Fix missing columns in MainAdminUsers | ⏳ Pending |
| 2025-12-02 | 11_fix_all_schema_mismatches.sql | Fix schema mismatches across all tables | ⏳ Pending |
| 2025-12-02 | 12_fix_token_columns.sql | Change AccessToken/RefreshToken to TEXT | ⏳ Pending |
| 2025-12-02 | **13_complete_schema_setup.sql** | **Complete schema setup for all modules** | ⏳ **RUN THIS** |

---

## Latest Migration: 13_complete_schema_setup.sql

### What This Migration Does:

1. **Contacts Module**
   - Creates `Contacts` table (PascalCase)
   - Creates `ContactNotes` table
   - Creates `ContactTags` table
   - Creates `ContactTagAssignments` table
   - Adds all necessary indexes

2. **Articles Module**
   - Creates `Articles` table (PascalCase)
   - Creates `ArticleCategories` table
   - Creates `Locations` table
   - Creates `InventoryTransactions` table

3. **Lookups Module**
   - Creates `LookupItems` table
   - Creates `Currencies` table
   - **Inserts default lookup data:**
     - Priorities (low, medium, high, urgent)
     - Task Statuses
     - Project Statuses
     - Project Types
     - Event Types
     - Article Categories
     - Article Statuses
     - Service Categories
     - Countries (sample)
     - Technician Statuses
     - Leave Types
     - Offer Statuses
   - **Inserts default currencies** (USD, EUR, GBP, CAD, AUD)

4. **Installations Module**
   - Creates `Installations` table
   - Creates `MaintenanceHistory` table

5. **Token Fix**
   - Converts `AccessToken` and `RefreshToken` columns to TEXT type

### How to Apply:

1. Go to **Neon Console** → **SQL Editor**
2. Copy the entire contents of `13_complete_schema_setup.sql`
3. Execute the SQL
4. Verify with the SELECT queries at the end

### Expected Output After Running:

```
table_name
-----------------
Articles
ArticleCategories
Contacts
ContactNotes
ContactTags
ContactTagAssignments
Currencies
DailyTasks
Installations
InventoryTransactions
Locations
LookupItems
MainAdminUsers
MaintenanceHistory
ProjectColumns
Projects
ProjectTasks
Roles
RoleSkills
Skills
TaskAttachments
TaskComments
UserPreferences
Users
UserSkills
```

```
lookuptype          | count
--------------------|------
article-category    | 4
article-status      | 3
country             | 4
event-type          | 3
leave-type          | 3
offer-status        | 4
priority            | 4
project-status      | 4
project-type        | 3
service-category    | 3
task-status         | 3
technician-status   | 3
```

---

## Troubleshooting

### Column does not exist error
Run the latest migration file to add missing columns.

### Column already exists error
Migrations use `IF NOT EXISTS` - safe to re-run.

### Type mismatch error
Check the column type matches between C# model and database.

### 500 Internal Server Error
The database tables don't exist. Run migration `13_complete_schema_setup.sql`.

---

## Schema Reference

### Key Tables

| Table | Module | Description |
|-------|--------|-------------|
| MainAdminUsers | Auth | Admin user accounts |
| Users | Users | Regular users managed by admins |
| Roles | Roles | User roles |
| Skills | Skills | Skills that can be assigned |
| Contacts | Contacts | Contact records |
| ContactTags | Contacts | Tags for contacts |
| Articles | Articles | Materials and services |
| LookupItems | Lookups | All lookup data |
| Currencies | Lookups | Currency definitions |
| Projects | Projects | Project records |
| ProjectTasks | Projects | Tasks within projects |
| Installations | Installations | Installation records |

---

## After Running Migration

After successfully running the migration:

1. **Redeploy your backend** on Render
2. **Run the API tests** to verify everything works
3. Update this file to mark migration as ✅ Applied
