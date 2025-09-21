using Microsoft.EntityFrameworkCore;
using MyApi.Models;
using MyApi.Data.Configurations;
using MyApi.Data.Configurations.Users;
using MyApi.Data.Configurations.Roles;
using MyApi.Data.Configurations.Skills;
using MyApi.Data.Configurations.Contacts;
using MyApi.Data.SeedData;

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
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<UserSkill> UserSkills { get; set; }
        public DbSet<RoleSkill> RoleSkills { get; set; }
        
        // Contacts Module
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ContactNote> ContactNotes { get; set; }
        public DbSet<ContactTag> ContactTags { get; set; }
        public DbSet<ContactTagAssignment> ContactTagAssignments { get; set; }

        // Articles (Materials & Services)
        public DbSet<Article> Articles { get; set; }

        // Calendar entities
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<EventAttendee> EventAttendees { get; set; }
        public DbSet<EventReminder> EventReminders { get; set; }

        // Tasks Module
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectColumn> ProjectColumns { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<DailyTask> DailyTasks { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskAttachment> TaskAttachments { get; set; }

        // Lookups Module
        public DbSet<LookupItem> LookupItems { get; set; }
        public DbSet<Currency> Currencies { get; set; }

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
            // Users domain configurations
            new MainAdminUserConfiguration().Configure(modelBuilder);
            new UserPreferencesConfiguration().Configure(modelBuilder);
            new UserConfiguration().Configure(modelBuilder);
            
            // Roles domain configurations
            new RoleConfiguration().Configure(modelBuilder);
            new UserRoleConfiguration().Configure(modelBuilder);
            
            // Skills domain configurations
            new SkillConfiguration().Configure(modelBuilder);
            new UserSkillConfiguration().Configure(modelBuilder);
            new RoleSkillConfiguration().Configure(modelBuilder);
            
            // Contacts domain configurations
            new ContactConfiguration().Configure(modelBuilder);
            new ContactNoteConfiguration().Configure(modelBuilder);
            new ContactTagConfiguration().Configure(modelBuilder);
            
            // TODO: Add other domain configurations (Articles, Calendar, Lookups)
            // These can be moved to separate configuration files following the same pattern
            ConfigureArticleEntities(modelBuilder);
            ConfigureCalendarEntities(modelBuilder);
            ConfigureLookupEntities(modelBuilder);
            ConfigureTasksEntities(modelBuilder);
        }

        private void ConfigureArticleEntities(ModelBuilder modelBuilder)
        {
            // Article entity configuration
            modelBuilder.Entity<Article>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CostPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SellPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.BasePrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.HourlyRate).HasColumnType("decimal(18,2)");
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
                entity.Property(e => e.Symbol).IsRequired().HasMaxLength(5);
            });
        }

        private void ConfigureTasksEntities(ModelBuilder modelBuilder)
        {
            // Project entity configuration
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.OwnerName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.TeamMembers).HasMaxLength(1000);
                entity.Property(e => e.Budget).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Currency).HasMaxLength(3);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Priority).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Tags).HasMaxLength(1000);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.ModifiedBy).HasMaxLength(255);
                
                entity.HasOne(e => e.Contact)
                    .WithMany()
                    .HasForeignKey(e => e.ContactId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Project Column entity configuration
            modelBuilder.Entity<ProjectColumn>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
                
                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Columns)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Project Task entity configuration
            modelBuilder.Entity<ProjectTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.AssigneeName).HasMaxLength(255);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Priority).IsRequired().HasMaxLength(10);
                entity.Property(e => e.EstimatedHours).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ActualHours).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Tags).HasMaxLength(1000);
                entity.Property(e => e.Attachments).HasMaxLength(2000);
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
                
                entity.HasOne(e => e.Contact)
                    .WithMany()
                    .HasForeignKey(e => e.ContactId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                entity.HasOne(e => e.ParentTask)
                    .WithMany(t => t.SubTasks)
                    .HasForeignKey(e => e.ParentTaskId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Daily Task entity configuration
            modelBuilder.Entity<DailyTask>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Priority).IsRequired().HasMaxLength(10);
                entity.Property(e => e.EstimatedHours).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ActualHours).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Tags).HasMaxLength(1000);
                entity.Property(e => e.Attachments).HasMaxLength(2000);
                entity.Property(e => e.CreatedBy).HasMaxLength(255);
                entity.Property(e => e.ModifiedBy).HasMaxLength(255);
            });

            // Task Comment entity configuration
            modelBuilder.Entity<TaskComment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(255);
                
                entity.HasOne(e => e.ProjectTask)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(e => e.ProjectTaskId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.DailyTask)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(e => e.DailyTaskId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Task Attachment entity configuration
            modelBuilder.Entity<TaskAttachment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FileUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.MimeType).HasMaxLength(100);
                entity.Property(e => e.UploadedByName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Caption).HasMaxLength(500);
                
                entity.HasOne(e => e.ProjectTask)
                    .WithMany(t => t.TaskAttachments)
                    .HasForeignKey(e => e.ProjectTaskId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.DailyTask)
                    .WithMany(t => t.TaskAttachments)
                    .HasForeignKey(e => e.DailyTaskId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ApplySeedData(ModelBuilder modelBuilder)
        {
            new LookupSeedData().Seed(modelBuilder);
            new CurrencySeedData().Seed(modelBuilder);
        }
    }
}