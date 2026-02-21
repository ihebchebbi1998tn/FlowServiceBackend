# Article Groups Migration: From Dedicated Table to Lookups System

## Overview
**Objective:** Consolidate article groups management by removing the separate `ArticleGroups` table and using the generic `Lookups` system instead (like ArticleCategories).

**Benefit:** Simpler architecture, less database overhead, consistent lookup handling across the application.

---

## Changes Made

### 1. **Database Schema Changes**

#### Migration Files Created:
- `FlowServiceBackendOnlyFinal-main/Database/Migrations/20260221_RemoveArticleGroupsTableUseLookups.sql` (SQL script)
- `FlowServiceBackendOnlyFinal-main/Migrations/20260221100001_RemoveArticleGroupsTableUseLookups.cs` (EF Core migration)

#### What the Migration Does:
1. ✅ Migrates existing data from `ArticleGroups` table to `LookupItems` with `LookupType='article-groups'`
2. ✅ Drops the `FK_Articles_ArticleGroups_GroupId` foreign key constraint
3. ✅ Drops the `IX_Articles_GroupId` index
4. ✅ Drops the `ArticleGroups` table entirely
5. ✅ Leaves `Articles.GroupId` column as a simple integer (no FK constraint)

**Result:** `Articles.GroupId` now points to `LookupItems.Id` entries where `LookupType='article-groups'`

---

### 2. **Backend Code Changes**

#### Article.cs Model
- **Added:** Comment explaining that `GroupId` now references `LookupItems` (lookups table)
- **No Breaking Changes:** The `GroupId` property remains exactly the same

```csharp
// GroupId now references LookupItems where LookupType='article-groups'
// This allows articles to use the generic Lookups system instead of a dedicated ArticleGroups table
public int? GroupId { get; set; }
```

#### ApplicationDbContext.cs
- **Commented Out:** `public DbSet<ArticleGroup> ArticleGroups { get; set; }`
- **Reason:** Table no longer exists after migration

```csharp
// NOTE: ArticleGroups is now replaced by generic Lookups system with LookupType='article-groups'
// Table has been dropped. Keep this commented for reference but don't use it.
// public DbSet<ArticleGroup> ArticleGroups { get; set; }
```

#### ArticleGroupsController.cs
- **Updated:** Documentation explaining it's now a wrapper around Lookups
- **No Functional Changes:** Already uses `ILookupService` (generic lookup service)

---

### 3. **Frontend Code Changes** (Already Fixed)

#### lookupsApi.ts
- **Created:** Dedicated `articlesGroupsApi` that calls `/api/articles/groups`
- **Mapping:** Response from backend Lookups API is transformed to `LookupItem[]`

```typescript
export const articleGroupsApi = articlesGroupsApi;
// Calls: GET /api/articles/groups → returns LookupItems with type='article-groups'
```

#### AddUnifiedArticle.tsx & EditUnifiedArticle.tsx
- **Behavior:** No changes needed - already uses `articleGroupsApi.getAll()`
- **Works the same:** Fetches groups and displays in dropdown

---

## Migration Steps

### Step 1: Apply EF Core Migration
```bash
cd FlowServiceBackend
dotnet ef database update
```

### Step 2: Or Apply SQL Migration Manually
If not using EF Core automation:
```bash
# Run the SQL script against your PostgreSQL database
psql -U postgres -d flowdb -f Database/Migrations/20260221_RemoveArticleGroupsTableUseLookups.sql
```

### Step 3: Verify Data
```sql
-- Verify groups were migrated to Lookups
SELECT COUNT(*) FROM "LookupItems" WHERE "LookupType" = 'article-groups';

-- Verify ArticleGroups table is gone
SELECT * FROM "ArticleGroups"; -- Should error: relation does not exist
```

### Step 4: Test Frontend
1. ✅ Load "Add Article" form - Group dropdown should display
2. ✅ Create new article with group - Should work without FK errors
3. ✅ Edit article and change group - Should update successfully
4. ✅ View article details - Group should display correctly

---

## Before & After

### BEFORE (Separate Table)
```
Articles.GroupId (int) 
    ↓ FK Constraint
    ↓
ArticleGroups table (dedicated)
    - Id (int)
    - Name (varchar)
    - Description (varchar)
    - IsActive (bool)
    - CreatedDate (datetime)
```

### AFTER (Generic Lookups)
```
Articles.GroupId (int)
    ↓ No FK constraint
    ↓
LookupItems table (generic)
    - Id (int)
    - LookupType = 'article-groups' (varchar)
    - Name (varchar)
    - Description (varchar)
    - IsActive (bool)
    - CreatedDate (datetime)
```

---

## Backward Compatibility

### Rollback Support
The EF Core migration includes a `Down()` method that recreates the `ArticleGroups` table if needed.

### Not Breaking Existing APIs
- `/api/articles/groups` endpoint works the same (now backed by Lookups)
- DTOs remain unchanged
- No client code needs to change

---

## Benefits

| Benefit | Details |
|---------|---------|
| **Consistency** | Uses same Lookups pattern as categories, statuses, etc. |
| **Simplicity** | Single lookup table instead of many similar small tables |
| **Flexibility** | Can add/remove/modify groups without model changes |
| **Performance** | Fewer FK constraints to check on inserts/updates |
| **Maintainability** | Centralized lookup management in Lookups module |

---

## Troubleshooting

### Issue: "FK constraint violation"
- **Cause:** GroupId value doesn't exist in Lookups
- **Solution:** Use correct group ID from `/api/articles/groups`

### Issue: "ArticleGroups table doesn't exist"
- **Cause:** Migration applied successfully
- **Solution:** This is expected - use Lookups system instead

### Issue: Old code still references ArticleGroup model
- **Solution:** Remove using statements for `using MyApi.Modules.Articles.Models;` and any `new ArticleGroup()` instantiations

---

## API Endpoints (Unchanged)

```
GET    /api/articles/groups           → Get all groups (from Lookups)
GET    /api/articles/groups/{id}      → Get specific group
POST   /api/articles/groups           → Create new group
PUT    /api/articles/groups/{id}      → Update group
DELETE /api/articles/groups/{id}      → Delete group
```

All endpoints continue to work as before, now backed by the Lookups system.
