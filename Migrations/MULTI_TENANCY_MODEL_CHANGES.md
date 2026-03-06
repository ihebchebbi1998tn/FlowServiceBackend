# Multi-Tenancy: Remaining Model Changes Guide

## Pattern Applied to ALL Model Files

Every entity model (except `MainAdminUser` and `Tenant`) needs TWO changes:

### Change 1: Add `using MyApi.Infrastructure;` to imports
### Change 2: Add `: ITenantEntity` to class declaration and `public int TenantId { get; set; }` as first property

### Example (before):
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.SomeModule.Models
{
    [Table("SomeTable")]
    public class SomeEntity
    {
        [Key]
        public int Id { get; set; }
        // ...
    }
}
```

### Example (after):
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyApi.Infrastructure;                          // ← ADD THIS

namespace MyApi.Modules.SomeModule.Models
{
    [Table("SomeTable")]
    public class SomeEntity : ITenantEntity           // ← ADD : ITenantEntity
    {
        public int TenantId { get; set; }             // ← ADD THIS LINE
        
        [Key]
        public int Id { get; set; }
        // ...
    }
}
```

---

## ✅ Already Modified (in this PR):

- `Modules/Users/Models/User.cs`
- `Modules/Users/Models/UserPreferences.cs`
- `Modules/Users/Models/UserRole.cs`
- `Modules/Users/Models/UserSkill.cs`
- `Modules/Roles/Models/Role.cs`
- `Modules/Roles/Models/RolePermission.cs`
- `Modules/Roles/Models/RoleSkill.cs`
- `Modules/Skills/Models/Skill.cs`
- `Modules/Contacts/Models/Contact.cs`
- `Modules/Contacts/Models/ContactNote.cs`
- `Modules/Contacts/Models/ContactTag.cs`
- `Modules/Contacts/Models/ContactTagAssignment.cs`
- `Modules/Articles/Models/Article.cs`
- `Modules/Offers/Models/Offer.cs`
- `Modules/Sales/Models/Sale.cs`
- `Modules/ServiceOrders/Models/ServiceOrder.cs`
- `Modules/Preferences/Models/UserPreference.cs`

## ❌ Remaining Files to Modify (apply same pattern):

### Articles Module
- `Modules/Articles/Models/ArticleCategory.cs`
- `Modules/Articles/Models/Location.cs`
- `Modules/Articles/Models/InventoryTransaction.cs`
- `Modules/Articles/Models/StockTransaction.cs`

### Calendar Module
- `Modules/Calendar/Models/CalendarEvent.cs`
- `Modules/Calendar/Models/EventType.cs`
- `Modules/Calendar/Models/EventAttendee.cs`
- `Modules/Calendar/Models/EventReminder.cs`

### Email Accounts Module
- `Modules/EmailAccounts/Models/ConnectedEmailAccount.cs`
- `Modules/EmailAccounts/Models/CustomEmailAccount.cs`
- `Modules/EmailAccounts/Models/EmailBlocklistItem.cs`
- `Modules/EmailAccounts/Models/SyncedEmail.cs`
- `Modules/EmailAccounts/Models/SyncedEmailAttachment.cs`
- `Modules/EmailAccounts/Models/SyncedCalendarEvent.cs`

### Projects & Tasks Module
- `Modules/Projects/Models/Project.cs`
- `Modules/Projects/Models/ProjectColumn.cs`
- `Modules/Projects/Models/ProjectNote.cs`
- `Modules/Projects/Models/ProjectActivity.cs`
- `Modules/Projects/Models/ProjectTask.cs`
- `Modules/Projects/Models/DailyTask.cs`
- `Modules/Projects/Models/TaskComment.cs`
- `Modules/Projects/Models/TaskAttachment.cs`
- `Modules/Projects/Models/TaskTimeEntry.cs`
- `Modules/Projects/Models/TaskChecklist.cs`
- `Modules/Projects/Models/TaskChecklistItem.cs`
- `Modules/Projects/Models/RecurringTask.cs`
- `Modules/Projects/Models/RecurringTaskLog.cs`

### Lookups Module
- `Modules/Lookups/Models/LookupItem.cs`
- `Modules/Lookups/Models/Currency.cs`

### Offers Module (remaining)
- `Modules/Offers/Models/OfferItem.cs`
- `Modules/Offers/Models/OfferActivity.cs`

### Sales Module (remaining)
- `Modules/Sales/Models/SaleItem.cs`
- `Modules/Sales/Models/SaleActivity.cs`

### Installations Module
- `Modules/Installations/Models/Installation.cs`
- `Modules/Installations/Models/InstallationNote.cs`
- `Modules/Installations/Models/MaintenanceHistory.cs`

### Dispatches Module
- `Modules/Dispatches/Models/Dispatch.cs`
- `Modules/Dispatches/Models/DispatchTechnician.cs`
- `Modules/Dispatches/Models/DispatchJob.cs`
- `Modules/Dispatches/Models/TimeEntry.cs`
- `Modules/Dispatches/Models/Expense.cs`
- `Modules/Dispatches/Models/MaterialUsage.cs`
- `Modules/Dispatches/Models/Attachment.cs`
- `Modules/Dispatches/Models/Note.cs`

### Service Orders Module (remaining)
- `Modules/ServiceOrders/Models/ServiceOrderJob.cs`
- `Modules/ServiceOrders/Models/ServiceOrderMaterial.cs`
- `Modules/ServiceOrders/Models/ServiceOrderTimeEntry.cs`
- `Modules/ServiceOrders/Models/ServiceOrderExpense.cs`
- `Modules/ServiceOrders/Models/ServiceOrderNote.cs`

### Planning Module
- `Modules/Planning/Models/UserWorkingHours.cs`
- `Modules/Planning/Models/UserLeave.cs`
- `Modules/Planning/Models/UserStatusHistory.cs`
- `Modules/Planning/Models/DispatchHistory.cs`

### Notifications & System
- `Modules/Notifications/Models/Notification.cs`
- `Modules/Shared/Models/SystemLog.cs`
- `Modules/Preferences/Models/PdfSettings.cs`

### Numbering Module
- `Modules/Numbering/Models/NumberingSettings.cs`
- `Modules/Numbering/Models/NumberSequence.cs`

### Dynamic Forms Module
- `Modules/DynamicForms/Models/DynamicForm.cs`
- `Modules/DynamicForms/Models/DynamicFormResponse.cs`

### Dashboards Module
- `Modules/Dashboards/Models/Dashboard.cs`

### Shared Module
- `Modules/Shared/Models/EntityFormDocument.cs`

### AI Chat Module
- `Modules/AiChat/Models/AiConversation.cs`
- `Modules/AiChat/Models/AiMessage.cs`

### Workflow Engine Module
- `Modules/WorkflowEngine/Models/WorkflowDefinition.cs`
- `Modules/WorkflowEngine/Models/WorkflowTrigger.cs`
- `Modules/WorkflowEngine/Models/WorkflowExecution.cs`
- `Modules/WorkflowEngine/Models/WorkflowExecutionLog.cs`
- `Modules/WorkflowEngine/Models/WorkflowApproval.cs`
- `Modules/WorkflowEngine/Models/WorkflowProcessedEntity.cs`

### Documents & Signatures Module
- `Modules/Documents/Models/Document.cs`
- `Modules/Signatures/Models/UserSignature.cs`

### Website Builder Module
- `Modules/WebsiteBuilder/Models/WBSite.cs`
- `Modules/WebsiteBuilder/Models/WBPage.cs`
- `Modules/WebsiteBuilder/Models/WBPageVersion.cs`
- `Modules/WebsiteBuilder/Models/WBGlobalBlock.cs`
- `Modules/WebsiteBuilder/Models/WBGlobalBlockUsage.cs`
- `Modules/WebsiteBuilder/Models/WBBrandProfile.cs`
- `Modules/WebsiteBuilder/Models/WBFormSubmission.cs`
- `Modules/WebsiteBuilder/Models/WBMedia.cs`
- `Modules/WebsiteBuilder/Models/WBTemplate.cs`
- `Modules/WebsiteBuilder/Models/WBActivityLog.cs`

### User AI Settings Module
- `Modules/UserAiSettings/Models/UserAiKey.cs`
- `Modules/UserAiSettings/Models/UserAiPreference.cs`

### Payments Module
- `Modules/Payments/Models/PaymentModels.cs` (has 4 classes — add ITenantEntity to ALL 4: Payment, PaymentPlan, PaymentPlanInstallment, PaymentItemAllocation)

### Retenue Source Module
- `Modules/RetenueSource/Models/RSRecord.cs`
- `Modules/RetenueSource/Models/TEJExportLog.cs`

### Support Tickets Module
- `Modules/SupportTickets/Models/SupportTicket.cs`
- `Modules/SupportTickets/Models/SupportTicketAttachment.cs`
- `Modules/SupportTickets/Models/SupportTicketComment.cs`
- `Modules/SupportTickets/Models/SupportTicketLink.cs`

### Settings Module
- `Modules/Settings/Models/AppSettings.cs`

---

## Quick Find-and-Replace Commands (for IDE)

In your IDE, you can use these regex patterns across the Models directories:

**Find:** `public class (\w+)\s*\n\s*\{`
**Replace:** `public class $1 : ITenantEntity\n    {\n        public int TenantId { get; set; }\n`

Then add `using MyApi.Infrastructure;` to each file's imports.

## Notes
- `MainAdminUser` does NOT get ITenantEntity (it's shared across all tenants)
- `Tenant` does NOT get ITenantEntity (it IS the tenant registry)
- The `TenantDbContextFactory` creates contexts for database-per-tenant — still works alongside single-DB filters
