using Microsoft.EntityFrameworkCore;
using MyApi.Modules.Auth.Models;
using MyApi.Modules.Users.Models;
using MyApi.Modules.Roles.Models;
using MyApi.Modules.Skills.Models;
using MyApi.Modules.Contacts.Models;
using MyApi.Modules.Articles.Models;
using MyApi.Modules.Calendar.Models;
using MyApi.Modules.Projects.Models;
using MyApi.Modules.Lookups.Models;
using MyApi.Modules.Offers.Models;
using MyApi.Modules.Auth.Data.Configurations;
using MyApi.Modules.Users.Data.Configurations;
using MyApi.Modules.Roles.Data.Configurations;
using MyApi.Modules.Skills.Data.Configurations;
using MyApi.Modules.Contacts.Data.Configurations;
using MyApi.Modules.Offers.Data;
using MyApi.Data.SeedData;
using MyApi.Modules.Sales.Models;
using MyApi.Modules.Sales.Data;
using MyApi.Modules.Installations.Models;
using MyApi.Modules.Installations.Data;
using MyApi.Modules.Dispatches.Models;
using MyApi.Modules.Dispatches.Data;
using MyApi.Modules.ServiceOrders.Models;
using MyApi.Modules.ServiceOrders.Data;
using MyApi.Modules.Planning.Models;
using MyApi.Modules.Notifications.Models;
using MyApi.Modules.Notifications.Data;
using MyApi.Modules.Shared.Models;
using MyApi.Modules.Shared.Data.Configurations;
using MyApi.Modules.Preferences.Models;
using MyApi.Modules.DynamicForms.Models;
using MyApi.Modules.AiChat.Models;
using MyApi.Modules.WorkflowEngine.Models;
using MyApi.Modules.Documents.Models;
using MyApi.Modules.Signatures.Models;
using MyApi.Modules.WebsiteBuilder.Models;
using MyApi.Modules.WebsiteBuilder.Data.Configurations;
using MyApi.Modules.EmailAccounts.Models;

namespace MyApi.Data
{
    public partial class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Users Module
        public DbSet<MainAdminUser> MainAdminUsers { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<User> Users { get; set; }
        
        // Roles & Skills Module  
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<UserSkill> UserSkills { get; set; }
        public DbSet<RoleSkill> RoleSkills { get; set; }
        
        // Contacts Module
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ContactNote> ContactNotes { get; set; }
        public DbSet<ContactTag> ContactTags { get; set; }
        public DbSet<ContactTagAssignment> ContactTagAssignments { get; set; }

        // Articles Module (Materials & Services)
        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleCategory> ArticleCategories { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }

        // Calendar entities
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<EventAttendee> EventAttendees { get; set; }
        public DbSet<EventReminder> EventReminders { get; set; }

        // Email Accounts Module (Gmail/Outlook OAuth)
        public DbSet<ConnectedEmailAccount> ConnectedEmailAccounts { get; set; }
        public DbSet<EmailBlocklistItem> EmailBlocklistItems { get; set; }

        // Tasks Module
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectColumn> ProjectColumns { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<DailyTask> DailyTasks { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskAttachment> TaskAttachments { get; set; }
        public DbSet<TaskTimeEntry> TaskTimeEntries { get; set; }
        public DbSet<TaskChecklist> TaskChecklists { get; set; }
        public DbSet<TaskChecklistItem> TaskChecklistItems { get; set; }
        public DbSet<RecurringTask> RecurringTasks { get; set; }
        public DbSet<RecurringTaskLog> RecurringTaskLogs { get; set; }

        // Lookups Module
        public DbSet<LookupItem> LookupItems { get; set; }
        public DbSet<Currency> Currencies { get; set; }

        // Offers Module
        public DbSet<Offer> Offers { get; set; }
        public DbSet<OfferItem> OfferItems { get; set; }
        public DbSet<OfferActivity> OfferActivities { get; set; }

        // Sales Module
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<SaleActivity> SaleActivities { get; set; }

        // Installations Module
        public DbSet<Installation> Installations { get; set; }
        public DbSet<InstallationNote> InstallationNotes { get; set; }
        public DbSet<MaintenanceHistory> MaintenanceHistories { get; set; }

    // Dispatches Module
    public DbSet<Dispatch> Dispatches { get; set; }
    public DbSet<DispatchTechnician> DispatchTechnicians { get; set; }
    public DbSet<TimeEntry> TimeEntries { get; set; }
    public DbSet<Expense> DispatchExpenses { get; set; }
    public DbSet<MaterialUsage> DispatchMaterials { get; set; }
    public DbSet<Attachment> DispatchAttachments { get; set; }
    public DbSet<Note> DispatchNotes { get; set; }

        public DbSet<ServiceOrder> ServiceOrders { get; set; }
        public DbSet<ServiceOrderJob> ServiceOrderJobs { get; set; }
        public DbSet<ServiceOrderMaterial> ServiceOrderMaterials { get; set; }
        public DbSet<ServiceOrderTimeEntry> ServiceOrderTimeEntries { get; set; }
        public DbSet<ServiceOrderExpense> ServiceOrderExpenses { get; set; }
        public DbSet<ServiceOrderNote> ServiceOrderNotes { get; set; }

        // Planning Module
        public DbSet<UserWorkingHours> UserWorkingHours { get; set; }
        public DbSet<UserLeave> UserLeaves { get; set; }
        public DbSet<UserStatusHistory> UserStatusHistory { get; set; }
        public DbSet<DispatchHistory> DispatchHistory { get; set; }

        // Notifications Module
        public DbSet<Notification> Notifications { get; set; }

        // System Logs Module
        public DbSet<SystemLog> SystemLogs { get; set; }

        // PDF Settings Module (Global settings)
        public DbSet<PdfSettings> PdfSettings { get; set; }


        // Dynamic Forms Module
        public DbSet<DynamicForm> DynamicForms { get; set; }
        public DbSet<DynamicFormResponse> DynamicFormResponses { get; set; }

        // Entity Form Documents (shared - for offers/sales)
        public DbSet<MyApi.Modules.Shared.Models.EntityFormDocument> EntityFormDocuments { get; set; }

        // AI Chat Module
        public DbSet<AiConversation> AiConversations { get; set; }
        public DbSet<AiMessage> AiMessages { get; set; }

        // Workflow Engine Module
        public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }
        public DbSet<WorkflowTrigger> WorkflowTriggers { get; set; }
        public DbSet<WorkflowExecution> WorkflowExecutions { get; set; }
        public DbSet<WorkflowExecutionLog> WorkflowExecutionLogs { get; set; }
        public DbSet<WorkflowApproval> WorkflowApprovals { get; set; }
        public DbSet<WorkflowProcessedEntity> WorkflowProcessedEntities { get; set; }

        // Documents Module
        public DbSet<Document> Documents { get; set; }

        // Signatures Module
        public DbSet<UserSignature> UserSignatures { get; set; }

        // Website Builder Module
        public DbSet<WBSite> WBSites { get; set; }
        public DbSet<WBPage> WBPages { get; set; }
        public DbSet<WBPageVersion> WBPageVersions { get; set; }
        public DbSet<WBGlobalBlock> WBGlobalBlocks { get; set; }
        public DbSet<WBGlobalBlockUsage> WBGlobalBlockUsages { get; set; }
        public DbSet<WBBrandProfile> WBBrandProfiles { get; set; }
        public DbSet<WBFormSubmission> WBFormSubmissions { get; set; }
        public DbSet<WBMedia> WBMedia { get; set; }
        public DbSet<WBTemplate> WBTemplates { get; set; }
        public DbSet<WBActivityLog> WBActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations
            ApplyEntityConfigurations(modelBuilder);
            
            // Apply seed data
            ApplySeedData(modelBuilder);
        }

        private void ApplyEntityConfigurations(ModelBuilder modelBuilder)
        {
            // Auth & Users domain configurations
            new MyApi.Modules.Auth.Data.Configurations.MainAdminUserConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Users.Data.Configurations.UserPreferencesConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Users.Data.Configurations.UserConfiguration().Configure(modelBuilder);
            
            // Roles domain configurations
            new MyApi.Modules.Roles.Data.Configurations.RoleConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Roles.Data.Configurations.UserRoleConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Roles.Data.Configurations.RolePermissionConfiguration().Configure(modelBuilder);
            
            // Skills domain configurations
            new MyApi.Modules.Skills.Data.Configurations.SkillConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Skills.Data.Configurations.UserSkillConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Roles.Data.Configurations.RoleSkillConfiguration().Configure(modelBuilder);
            
            // Contacts domain configurations
            new MyApi.Modules.Contacts.Data.Configurations.ContactConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Contacts.Data.Configurations.ContactNoteConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Contacts.Data.Configurations.ContactTagConfiguration().Configure(modelBuilder);
            
            // Offers domain configurations
            new MyApi.Modules.Offers.Data.OfferConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Offers.Data.OfferItemConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Offers.Data.OfferActivityConfiguration().Configure(modelBuilder);
            
            // Sales domain configurations
            new MyApi.Modules.Sales.Data.SaleConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Sales.Data.SaleItemConfiguration().Configure(modelBuilder);
            new MyApi.Modules.Sales.Data.SaleActivityConfiguration().Configure(modelBuilder);

            modelBuilder.ApplyConfiguration(new InstallationConfiguration());
            modelBuilder.ApplyConfiguration(new MaintenanceHistoryConfiguration());
            // Dispatches domain configurations
            modelBuilder.ApplyConfiguration(new MyApi.Modules.Dispatches.Data.DispatchConfiguration());
            modelBuilder.ApplyConfiguration(new MyApi.Modules.Dispatches.Data.DispatchTechnicianConfiguration());
            modelBuilder.ApplyConfiguration(new MyApi.Modules.Dispatches.Data.TimeEntryConfiguration());
            modelBuilder.ApplyConfiguration(new MyApi.Modules.Dispatches.Data.ExpenseConfiguration());
            modelBuilder.ApplyConfiguration(new MyApi.Modules.Dispatches.Data.MaterialUsageConfiguration());
            modelBuilder.ApplyConfiguration(new MyApi.Modules.Dispatches.Data.AttachmentConfiguration());
            modelBuilder.ApplyConfiguration(new MyApi.Modules.Dispatches.Data.NoteConfiguration());
            
            modelBuilder.ApplyConfiguration(new ServiceOrderConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceOrderJobConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceOrderMaterialConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceOrderTimeEntryConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceOrderExpenseConfiguration());
            
            // Notifications configuration
            new NotificationConfiguration().Configure(modelBuilder);
            
            // System Logs configuration
            new SystemLogConfiguration().Configure(modelBuilder);
            
            // Entity Form Documents configuration (Status as lowercase string)
            modelBuilder.ApplyConfiguration(new EntityFormDocumentConfiguration());
            
            // Configure remaining entities
            ConfigureArticleEntities(modelBuilder);
            ConfigureCalendarEntities(modelBuilder);
            ConfigureLookupEntities(modelBuilder);
            ConfigureTasksEntities(modelBuilder);
            ConfigurePlanningEntities(modelBuilder);

            // Website Builder Module configurations
            new WBSiteConfiguration().Configure(modelBuilder);
            new WBPageConfiguration().Configure(modelBuilder);
            new WBPageVersionConfiguration().Configure(modelBuilder);
            new WBGlobalBlockConfiguration().Configure(modelBuilder);
            new WBGlobalBlockUsageConfiguration().Configure(modelBuilder);
            new WBBrandProfileConfiguration().Configure(modelBuilder);
            new WBFormSubmissionConfiguration().Configure(modelBuilder);
            new WBMediaConfiguration().Configure(modelBuilder);
            new WBTemplateConfiguration().Configure(modelBuilder);
            new WBActivityLogConfiguration().Configure(modelBuilder);
        }

        private void ConfigureArticleEntities(ModelBuilder modelBuilder)
        {
            // Article entity configuration - matches actual Article model
            modelBuilder.Entity<Article>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ArticleNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.Unit).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PurchasePrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SalesPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.StockQuantity).HasColumnType("decimal(18,2)");
                entity.Property(e => e.MinStockLevel).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Supplier).HasMaxLength(200);
            });

            // ArticleCategory entity configuration - matches actual model
            modelBuilder.Entity<ArticleCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            // Location entity configuration - matches actual model
            modelBuilder.Entity<Location>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            // InventoryTransaction entity configuration
            modelBuilder.Entity<InventoryTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne<Article>()
                    .WithMany()
                    .HasForeignKey(e => e.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // StockTransaction entity configuration
            modelBuilder.Entity<StockTransaction>(entity =>
            {
                entity.ToTable("stock_transactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PreviousStock).HasColumnType("decimal(18,2)");
                entity.Property(e => e.NewStock).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Reason).HasMaxLength(255);
                entity.Property(e => e.ReferenceType).HasMaxLength(50);
                entity.Property(e => e.ReferenceId).HasMaxLength(50);
                entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
                entity.Property(e => e.PerformedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PerformedByName).HasMaxLength(200);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.HasOne(e => e.Article)
                    .WithMany()
                    .HasForeignKey(e => e.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureCalendarEntities(ModelBuilder modelBuilder)
        {
            // Calendar Event configuration
            modelBuilder.Entity<CalendarEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
            });

            // Event Type configuration
            modelBuilder.Entity<EventType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            });

            // Event Attendee configuration
            modelBuilder.Entity<EventAttendee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne<CalendarEvent>()
                    .WithMany()
                    .HasForeignKey(e => e.EventId);
            });

            // Event Reminder configuration
            modelBuilder.Entity<EventReminder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne<CalendarEvent>()
                    .WithMany()
                    .HasForeignKey(e => e.EventId);
            });
        }

        private void ConfigureLookupEntities(ModelBuilder modelBuilder)
        {
            // LookupItem configuration
            modelBuilder.Entity<LookupItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LookupType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedUser).IsRequired().HasMaxLength(100);
            });

            // Currency configuration
            modelBuilder.Entity<Currency>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(3);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Symbol).IsRequired().HasMaxLength(10);
            });
        }

        private void ConfigureTasksEntities(ModelBuilder modelBuilder)
        {
            // Project entity configuration - matches actual Project model
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Priority).HasMaxLength(20);
                entity.Property(e => e.TeamMembers).HasMaxLength(1000);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ModifiedBy).HasMaxLength(100);
                
                entity.HasOne(e => e.Contact)
                    .WithMany()
                    .HasForeignKey(e => e.ContactId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Project Column entity configuration - matches actual ProjectColumn model
            modelBuilder.Entity<ProjectColumn>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Color).HasMaxLength(7);
                
                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Columns)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Project Task entity configuration - simplified to match actual database
            modelBuilder.Entity<ProjectTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Priority).HasMaxLength(10);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.ModifiedBy).HasMaxLength(255);
                
                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Column)
                    .WithMany(c => c.Tasks)
                    .HasForeignKey(e => e.ColumnId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Daily Task entity configuration - simplified to match actual database
            modelBuilder.Entity<DailyTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Priority).HasMaxLength(10);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
            });

            // Task Comment entity configuration - simplified to match actual database
            modelBuilder.Entity<TaskComment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Comment).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
            });

            // Task Attachment entity configuration
            modelBuilder.Entity<TaskAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UploadedBy).IsRequired().HasMaxLength(100);
                
                entity.HasOne(e => e.ProjectTask)
                    .WithMany()
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Task Time Entry entity configuration
            modelBuilder.Entity<TaskTimeEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserName).HasMaxLength(100);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.Duration).HasColumnType("decimal(18,2)");
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.WorkType).IsRequired().HasMaxLength(50).HasDefaultValue("work");
                entity.Property(e => e.ApprovalStatus).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
                entity.Property(e => e.ApprovalNotes).HasColumnType("text");
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                entity.HasOne(e => e.ProjectTask)
                    .WithMany()
                    .HasForeignKey(e => e.ProjectTaskId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.DailyTask)
                    .WithMany()
                    .HasForeignKey(e => e.DailyTaskId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.ApprovedBy)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedById)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(e => e.ProjectTaskId);
                entity.HasIndex(e => e.DailyTaskId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.StartTime);
                entity.HasIndex(e => e.ApprovalStatus);
            });

            // Task Checklist entity configuration
            modelBuilder.Entity<TaskChecklist>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasColumnType("text");
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                entity.HasOne(e => e.ProjectTask)
                    .WithMany()
                    .HasForeignKey(e => e.ProjectTaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.DailyTask)
                    .WithMany()
                    .HasForeignKey(e => e.DailyTaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ProjectTaskId);
                entity.HasIndex(e => e.DailyTaskId);
            });

            // Task Checklist Item entity configuration
            modelBuilder.Entity<TaskChecklistItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.Property(e => e.CompletedByName).HasMaxLength(100);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                entity.HasOne(e => e.Checklist)
                    .WithMany(c => c.Items)
                    .HasForeignKey(e => e.ChecklistId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CompletedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CompletedById)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.ChecklistId);
            });

            // Recurring Task entity configuration
            modelBuilder.Entity<RecurringTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RecurrenceType).IsRequired().HasMaxLength(50).HasDefaultValue("daily");
                entity.Property(e => e.DaysOfWeek).HasMaxLength(50);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                entity.HasOne(e => e.ProjectTask)
                    .WithMany()
                    .HasForeignKey(e => e.ProjectTaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.DailyTask)
                    .WithMany()
                    .HasForeignKey(e => e.DailyTaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ProjectTaskId);
                entity.HasIndex(e => e.DailyTaskId);
                entity.HasIndex(e => e.NextOccurrence);
                entity.HasIndex(e => e.IsActive);
            });

            // Recurring Task Log entity configuration
            modelBuilder.Entity<RecurringTaskLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("created");
                entity.Property(e => e.Notes).HasMaxLength(500);

                entity.HasOne(e => e.RecurringTask)
                    .WithMany()
                    .HasForeignKey(e => e.RecurringTaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.RecurringTaskId);
                entity.HasIndex(e => e.GeneratedDate);
            });

            // AI Conversation entity configuration
            modelBuilder.Entity<AiConversation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Title).HasMaxLength(500).HasDefaultValue("New Conversation");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.IsPinned).HasDefaultValue(false);
                entity.Property(e => e.IsArchived).HasDefaultValue(false);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.UserId, e.IsDeleted });
            });

            // AI Message entity configuration
            modelBuilder.Entity<AiMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Feedback).HasMaxLength(20);

                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => e.CreatedAt);
            });
        }

        /// <summary>
        /// Planning module entities: UserLeave, UserWorkingHours, UserStatusHistory
        /// IMPORTANT: These tables intentionally have NO foreign key to "Users" because
        /// user_id can reference either MainAdminUsers (id=1) or Users (id>=2).
        /// We must explicitly tell EF Core to ignore the convention-based FK.
        /// </summary>
        private void ConfigurePlanningEntities(ModelBuilder modelBuilder)
        {
            // UserLeave - no FK on UserId (supports both MainAdminUsers and Users)
            modelBuilder.Entity<UserLeave>(entity =>
            {
                entity.ToTable("user_leaves");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.LeaveType).HasColumnName("leave_type").HasMaxLength(100).IsRequired();
                entity.Property(e => e.StartDate).HasColumnName("start_date").IsRequired();
                entity.Property(e => e.EndDate).HasColumnName("end_date").IsRequired();
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("approved");
                entity.Property(e => e.Reason).HasColumnName("reason");
                entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
                entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                // Explicitly ignore convention-based FK to Users table
                entity.Ignore("User");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.StartDate, e.EndDate });
                entity.HasIndex(e => e.Status);
            });

            // UserWorkingHours - no FK on UserId (supports both MainAdminUsers and Users)
            modelBuilder.Entity<UserWorkingHours>(entity =>
            {
                entity.ToTable("user_working_hours");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week").IsRequired();
                entity.Property(e => e.StartTime).HasColumnName("start_time").IsRequired();
                entity.Property(e => e.EndTime).HasColumnName("end_time").IsRequired();
                entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
                entity.Property(e => e.EffectiveUntil).HasColumnName("effective_until");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.DayOfWeek);
                entity.HasIndex(e => new { e.UserId, e.DayOfWeek }).IsUnique();
            });

            // UserStatusHistory - no FK on UserId (supports both MainAdminUsers and Users)
            modelBuilder.Entity<UserStatusHistory>(entity =>
            {
                entity.ToTable("user_status_history");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.NewStatus).HasColumnName("new_status").HasMaxLength(50).IsRequired();
                entity.Property(e => e.PreviousStatus).HasColumnName("previous_status").HasMaxLength(50);
                entity.Property(e => e.Reason).HasColumnName("reason");
                entity.Property(e => e.ChangedAt).HasColumnName("changed_at");
                entity.Property(e => e.ChangedBy).HasColumnName("changed_by");

                entity.HasIndex(e => e.UserId);
            });
        }

        private void ApplySeedData(ModelBuilder modelBuilder)
        {
            // Apply seed data from separate files
            new LookupSeedData().Seed(modelBuilder);
            new CurrencySeedData().Seed(modelBuilder);
        }
    }
}
