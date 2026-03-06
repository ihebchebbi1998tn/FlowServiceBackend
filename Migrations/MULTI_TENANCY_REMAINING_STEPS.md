# Multi-Tenancy: Remaining Steps to Complete

## Status Summary

### ✅ COMPLETED
1. **SQL Migration Script** — `multi_tenancy_migration.sql` ready to execute on Neon
2. **ITenantEntity Interface** — `Infrastructure/ITenantEntity.cs` created
3. **Tenant Model** — `Modules/Tenants/Models/Tenant.cs` created
4. **TenantSlugCache** — `Infrastructure/TenantSlugCache.cs` created
5. **TenantsController** — `Modules/Tenants/Controllers/TenantsController.cs` created
6. **ApplicationDbContext** — Modified with Global Query Filters + SaveChanges override
7. **TenantMiddleware** — Modified for slug→TenantId resolution
8. **Program.cs** — Modified for cache init + SetTenantId on scoped DbContext
9. **Frontend tenantsApi** — `src/services/api/tenantsApi.ts` created
10. **TenantSwitcher** — `src/components/TenantSwitcher.tsx` created
11. **TenantManagement** — `src/modules/settings/components/TenantManagement.tsx` created
12. **Settings Page** — Companies tab integrated
13. **Translation Keys** — `nav.companies` added to en.json and fr.json

### ✅ Models with ITenantEntity Applied (35 files)
- `Users/Models/User.cs`
- `Users/Models/UserPreferences.cs`
- `Users/Models/UserRole.cs`
- `Users/Models/UserSkill.cs`
- `Roles/Models/Role.cs`
- `Roles/Models/RolePermission.cs`
- `Roles/Models/RoleSkill.cs`
- `Skills/Models/Skill.cs`
- `Contacts/Models/Contact.cs`
- `Contacts/Models/ContactNote.cs`
- `Contacts/Models/ContactTag.cs`
- `Contacts/Models/ContactTagAssignment.cs`
- `Articles/Models/Article.cs`
- `Articles/Models/ArticleCategory.cs`
- `Articles/Models/ArticleGroup.cs`
- `Articles/Models/Location.cs`
- `Articles/Models/InventoryTransaction.cs`
- `Articles/Models/StockTransaction.cs`
- `Calendar/Models/CalendarEvent.cs`
- `Calendar/Models/EventType.cs`
- `Calendar/Models/EventAttendee.cs`
- `Calendar/Models/EventReminder.cs`
- `EmailAccounts/Models/ConnectedEmailAccount.cs`
- `EmailAccounts/Models/CustomEmailAccount.cs`
- `EmailAccounts/Models/EmailBlocklistItem.cs`
- `EmailAccounts/Models/SyncedEmail.cs`
- `EmailAccounts/Models/SyncedEmailAttachment.cs`
- `EmailAccounts/Models/SyncedCalendarEvent.cs`
- `Projects/Models/Project.cs`
- `Projects/Models/ProjectColumn.cs`
- `Projects/Models/ProjectNote.cs`
- `Projects/Models/ProjectActivity.cs`
- `Offers/Models/Offer.cs`
- `Sales/Models/Sale.cs`
- `ServiceOrders/Models/ServiceOrder.cs`
- `Preferences/Models/UserPreference.cs`
- `Dispatches/Models/Dispatch.cs`
- `Dispatches/Models/Note.cs`
- `Installations/Models/Installation.cs`
- `Notifications/Models/Notification.cs`
- `Documents/Models/Document.cs`
- `Settings/Models/AppSettings.cs`
- `Planning/Models/UserStatusHistory.cs`

---

## ❌ REMAINING: Models Needing ITenantEntity (apply same pattern)

### Pattern to apply to each file:
1. Add `using MyApi.Infrastructure;` to imports
2. Add `: ITenantEntity` to class declaration
3. Add `public int TenantId { get; set; }` as first property

### Projects Module (remaining)
- `Projects/Models/ProjectTask.cs`
- `Projects/Models/DailyTask.cs`
- `Projects/Models/TaskComment.cs`
- `Projects/Models/TaskAttachment.cs`
- `Projects/Models/TaskTimeEntry.cs`
- `Projects/Models/TaskChecklist.cs` — **2 classes: TaskChecklist + TaskChecklistItem** (both need ITenantEntity)
- `Projects/Models/RecurringTask.cs` — **2 classes: RecurringTask + RecurringTaskLog** (both need ITenantEntity)

### Lookups Module
- `Lookups/Models/LookupItem.cs`
- `Lookups/Models/Currency.cs`

### Offers Module (remaining)
- `Offers/Models/OfferItem.cs`
- `Offers/Models/OfferActivity.cs`

### Sales Module (remaining)
- `Sales/Models/SaleItem.cs`
- `Sales/Models/SaleActivity.cs`

### Installations Module (remaining)
- `Installations/Models/InstallationNote.cs`
- `Installations/Models/MaintenanceHistory.cs`

### Dispatches Module (remaining)
- `Dispatches/Models/DispatchTechnician.cs`
- `Dispatches/Models/DispatchJob.cs`
- `Dispatches/Models/TimeEntry.cs`
- `Dispatches/Models/Expense.cs`
- `Dispatches/Models/MaterialUsage.cs`
- `Dispatches/Models/Attachment.cs`

### Service Orders Module (remaining)
- `ServiceOrders/Models/ServiceOrderJob.cs`
- `ServiceOrders/Models/ServiceOrderMaterial.cs`
- `ServiceOrders/Models/ServiceOrderTimeEntry.cs`
- `ServiceOrders/Models/ServiceOrderExpense.cs`
- `ServiceOrders/Models/ServiceOrderNote.cs`

### Planning Module (remaining)
- `Planning/Models/UserWorkingHours.cs` — already has file, add ITenantEntity
- `Planning/Models/UserLeave.cs` — already has file, add ITenantEntity
- `Planning/Models/DispatchHistory.cs` — already has file, add ITenantEntity

### Shared Module
- `Shared/Models/SystemLog.cs`
- `Shared/Models/EntityFormDocument.cs` — extends BaseEntityWithSoftDelete, add `, ITenantEntity`

### Preferences Module
- `Preferences/Models/PdfSettings.cs`

### Numbering Module
- `Numbering/Models/NumberingSettings.cs`
- `Numbering/Models/NumberSequence.cs`

### Dynamic Forms Module
- `DynamicForms/Models/DynamicForm.cs` — **2 classes: DynamicForm (extends BaseEntityWithStatus) + DynamicFormResponse** (both need ITenantEntity)

### Dashboards Module
- `Dashboards/Models/Dashboard.cs`

### AI Chat Module
- `AiChat/Models/AiConversation.cs`
- `AiChat/Models/AiMessage.cs`

### Workflow Engine Module
- `WorkflowEngine/Models/WorkflowDefinition.cs`
- `WorkflowEngine/Models/WorkflowTrigger.cs`
- `WorkflowEngine/Models/WorkflowExecution.cs`
- `WorkflowEngine/Models/WorkflowExecutionLog.cs`
- `WorkflowEngine/Models/WorkflowApproval.cs`
- `WorkflowEngine/Models/WorkflowProcessedEntity.cs`

### Signatures Module
- `Signatures/Models/UserSignature.cs`

### Website Builder Module
- `WebsiteBuilder/Models/WBSite.cs`
- `WebsiteBuilder/Models/WBPage.cs`
- `WebsiteBuilder/Models/WBPageVersion.cs`
- `WebsiteBuilder/Models/WBGlobalBlock.cs`
- `WebsiteBuilder/Models/WBGlobalBlockUsage.cs`
- `WebsiteBuilder/Models/WBBrandProfile.cs`
- `WebsiteBuilder/Models/WBFormSubmission.cs`
- `WebsiteBuilder/Models/WBMedia.cs`
- `WebsiteBuilder/Models/WBTemplate.cs`
- `WebsiteBuilder/Models/WBActivityLog.cs`

### User AI Settings Module
- `UserAiSettings/Models/UserAiKey.cs`
- `UserAiSettings/Models/UserAiPreference.cs`

### Payments Module
- `Payments/Models/PaymentModels.cs` — **4 classes: PaymentPlan, PaymentPlanInstallment, Payment, PaymentItemAllocation** (ALL 4 need ITenantEntity)

### Retenue Source Module
- `RetenueSource/Models/RSRecord.cs`
- `RetenueSource/Models/TEJExportLog.cs`

### Support Tickets Module
- `SupportTickets/Models/SupportTicket.cs` — **4 classes: SupportTicket, SupportTicketAttachment, SupportTicketComment, SupportTicketLink** (ALL 4 need ITenantEntity)

---

## ❌ REMAINING: Raw SQL Query Fixes

These bypass EF Core Global Query Filters and need manual `AND \"TenantId\" = @tenantId` filtering:

### 1. SaleService.cs (~line 522)
**Current:**
```csharp
await _context.Database.ExecuteSqlRawAsync(
    "UPDATE \\\"ServiceOrders\\\" SET \\\"SaleId\\\" = NULL WHERE \\\"SaleId\\\" = @p0", sale.Id);
```
**Fix:**
```csharp
await _context.Database.ExecuteSqlRawAsync(
    "UPDATE \\\"ServiceOrders\\\" SET \\\"SaleId\\\" = NULL WHERE \\\"SaleId\\\" = @p0 AND \\\"TenantId\\\" = @p1", 
    sale.Id, _context.GetTenantId());
```

### 2. StockTransactionService.cs (~line 126)
**Current:**
```csharp
var article = await _context.Articles
    .FromSqlRaw("SELECT * FROM \\\"Articles\\\" WHERE \\\"Id\\\" = {0} FOR UPDATE", articleId)
    .FirstOrDefaultAsync();
```
**Fix:**
```csharp
var article = await _context.Articles
    .FromSqlRaw("SELECT * FROM \\\"Articles\\\" WHERE \\\"Id\\\" = {0} AND \\\"TenantId\\\" = {1} FOR UPDATE", 
        articleId, _context.GetTenantId())
    .FirstOrDefaultAsync();
```

### 3. AuthService.cs (~line 395) — SELECT setval(...) — NO FIX NEEDED (sequence reset, not tenant-scoped)

---

## ❌ REMAINING: AuthService Company Logo per Tenant

### File: `Auth/Services/AuthService.cs`
**Method:** `GetCompanyLogoUrlAsync()`

**Current:** Reads from `MainAdminUsers.CompanyLogoUrl`

**Fix:**
```csharp
public async Task<string?> GetCompanyLogoUrlAsync()
{
    // Check if there's a current tenant with its own logo
    var tenantId = _context.GetTenantId();
    if (tenantId > 0)
    {
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == tenantId)
            .Select(t => t.CompanyLogoUrl)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrEmpty(tenant)) return tenant;
    }
    
    // Fallback to MainAdmin's company logo
    var mainAdminUser = await _context.MainAdminUsers.FirstOrDefaultAsync();
    return mainAdminUser?.CompanyLogoUrl;
}
```

---

## ❌ REMAINING: Additional Considerations

### 1. Search for any other raw SQL queries
Run this across the entire backend:
```bash
grep -rn "ExecuteSqlRaw\|FromSqlRaw\|SqlQuery" --include="*.cs" FlowServiceBackendOnlyFinal-main/Modules/
```
Each result needs manual `AND \"TenantId\"` filtering.

### 2. Verify ApplicationDbContext.OnModelCreating
Ensure ALL DbSet<T> properties in ApplicationDbContext.cs match the entity models that implement ITenantEntity. The reflection-based approach should handle this automatically, but verify by checking:
```bash
grep -n "public DbSet<" FlowServiceBackendOnlyFinal-main/Data/ApplicationDbContext.cs
```

### 3. Test Sequence
1. Execute `multi_tenancy_migration.sql` on Neon
2. Deploy updated backend
3. Verify existing data loads (TenantId=0 = default)
4. Create a second tenant via POST /api/Tenants
5. Switch to new tenant via TenantSwitcher
6. Verify data isolation (new tenant sees empty data)
7. Create data in new tenant
8. Switch back — verify original data still intact

### 4. Frontend: Sidebar company info
When a non-default tenant is active, update the sidebar header to show that tenant's company name/logo instead of the MainAdmin's.

### 5. Login flow enhancement
After MainAdmin login, if multiple tenants exist and no tenant is selected, auto-select the default tenant or show a company picker.

---

## Quick IDE Regex for Remaining Files

For each remaining file, apply this change:

**Step 1 - Add import (top of file):**
```
using MyApi.Infrastructure;
```

**Step 2 - Add interface to class:**
Find: `public class (\w+)(\s*:\s*\w+)?`
Replace: `public class $1$2, ITenantEntity` (or `: ITenantEntity` if no base class)

**Step 3 - Add property (first line inside class body):**
```csharp
public int TenantId { get; set; }
```
