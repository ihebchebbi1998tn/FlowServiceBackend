using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<MainAdminUser> MainAdminUsers { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }
        public DbSet<User> Users { get; set; }
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

        // Lookups Module
        public DbSet<LookupItem> LookupItems { get; set; }
        public DbSet<Currency> Currencies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // MainAdminUser configuration
            modelBuilder.Entity<MainAdminUser>(entity =>
            {
                entity.ToTable("MainAdminUsers");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsActive);
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);
                entity.Property(e => e.OnboardingCompleted)
                    .HasDefaultValue(false);
            });

            // UserPreferences configuration
            modelBuilder.Entity<UserPreferences>(entity =>
            {
                entity.ToTable("UserPreferences");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("NOW()");
                
                // Default values
                entity.Property(e => e.Theme).HasDefaultValue("system");
                entity.Property(e => e.Language).HasDefaultValue("en");
                entity.Property(e => e.PrimaryColor).HasDefaultValue("blue");
                entity.Property(e => e.LayoutMode).HasDefaultValue("sidebar");
                entity.Property(e => e.DataView).HasDefaultValue("table");
                entity.Property(e => e.DateFormat).HasDefaultValue("MM/DD/YYYY");
                entity.Property(e => e.TimeFormat).HasDefaultValue("12h");
                entity.Property(e => e.Currency).HasDefaultValue("USD");
                entity.Property(e => e.NumberFormat).HasDefaultValue("comma");
                entity.Property(e => e.Notifications).HasDefaultValue("{}");
                entity.Property(e => e.QuickAccessItems).HasDefaultValue("[]");
                entity.Property(e => e.SidebarCollapsed).HasDefaultValue(false);
                entity.Property(e => e.CompactMode).HasDefaultValue(false);
                entity.Property(e => e.ShowTooltips).HasDefaultValue(true);
                entity.Property(e => e.AnimationsEnabled).HasDefaultValue(true);
                entity.Property(e => e.SoundEnabled).HasDefaultValue(true);
                entity.Property(e => e.AutoSave).HasDefaultValue(true);

                // Explicit relationship configuration
                entity.HasOne(e => e.User)
                    .WithOne()
                    .HasForeignKey<UserPreferences>(e => e.UserId)
                    .HasPrincipalKey<MainAdminUser>(u => u.Id)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // all other entity configurations
            ConfigureUserEntities(modelBuilder);
            ConfigureContactEntities(modelBuilder);
            ConfigureArticleEntities(modelBuilder);
            ConfigureCalendarEntities(modelBuilder);
            ConfigureLookupEntities(modelBuilder);
            
            // Seed data
            SeedLookupData(modelBuilder);
            SeedCurrencies(modelBuilder);
        }

        private void ConfigureUserEntities(ModelBuilder modelBuilder)
        {
            // Users configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsDeleted);
                
                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);
                entity.Property(e => e.IsDeleted)
                    .HasDefaultValue(false);
            });

            // Configure Role entity
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            });

            // Configure UserRole entity
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.RoleId });
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // Define relationships
                entity.HasOne(ur => ur.User)
                    .WithMany()
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Skill entity
            modelBuilder.Entity<Skill>(entity =>
            {
                entity.ToTable("Skills");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Category);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            });

            // Configure UserSkill entity
            modelBuilder.Entity<UserSkill>(entity =>
            {
                entity.ToTable("UserSkills");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.SkillId });
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // Define relationships
                entity.HasOne(us => us.User)
                    .WithMany()
                    .HasForeignKey(us => us.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(us => us.Skill)
                    .WithMany(s => s.UserSkills)
                    .HasForeignKey(us => us.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure RoleSkill entity
            modelBuilder.Entity<RoleSkill>(entity =>
            {
                entity.ToTable("RoleSkills");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.RoleId, e.SkillId });
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // Define relationships
                entity.HasOne(rs => rs.Role)
                    .WithMany()
                    .HasForeignKey(rs => rs.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rs => rs.Skill)
                    .WithMany()
                    .HasForeignKey(rs => rs.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureContactEntities(ModelBuilder modelBuilder)
        {
            // Configure Contact entity
            modelBuilder.Entity<Contact>(entity =>
            {
                entity.ToTable("Contacts");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsDeleted);
                
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.Status).HasDefaultValue("active");
                entity.Property(e => e.Type).HasDefaultValue("individual");
                entity.Property(e => e.Favorite).HasDefaultValue(false);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            });

            // Configure ContactNote entity
            modelBuilder.Entity<ContactNote>(entity =>
            {
                entity.ToTable("ContactNotes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ContactId);
                entity.HasIndex(e => e.CreatedAt);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

                entity.HasOne(n => n.Contact)
                    .WithMany(c => c.Notes)
                    .HasForeignKey(n => n.ContactId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ContactTag entity
            modelBuilder.Entity<ContactTag>(entity =>
            {
                entity.ToTable("ContactTags");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name)
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");
                entity.HasIndex(e => e.IsDeleted);
                
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.Color).HasDefaultValue("#3b82f6");
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            });

            // Configure ContactTagAssignment entity
            modelBuilder.Entity<ContactTagAssignment>(entity =>
            {
                entity.ToTable("ContactTagAssignments");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ContactId);
                entity.HasIndex(e => e.TagId);
                entity.HasIndex(e => new { e.ContactId, e.TagId }).IsUnique();
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("NOW()");

                entity.HasOne(ta => ta.Contact)
                    .WithMany(c => c.TagAssignments)
                    .HasForeignKey(ta => ta.ContactId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ta => ta.Tag)
                    .WithMany(t => t.ContactAssignments)
                    .HasForeignKey(ta => ta.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureArticleEntities(ModelBuilder modelBuilder)
        {
            // Configure Article entity
            modelBuilder.Entity<Article>(entity =>
            {
                entity.ToTable("Articles", t => 
                {
                    t.HasCheckConstraint("CK_Articles_Type", "\"Type\" IN ('material','service')");
                });
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.Id)
                      .ValueGeneratedNever();

                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

                entity.Property(e => e.SKU).HasMaxLength(50);
                entity.Property(e => e.Supplier).HasMaxLength(200);
                entity.Property(e => e.Location).HasMaxLength(200);
                entity.Property(e => e.SubLocation).HasMaxLength(200);

                entity.Property(e => e.EstimatedDuration).HasMaxLength(100);
                entity.Property(e => e.WarrantyCoverage).HasMaxLength(200);
                entity.Property(e => e.ServiceArea).HasMaxLength(100);
                entity.Property(e => e.LastUsedBy).HasMaxLength(100);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ModifiedBy).HasMaxLength(100);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });
        }

        private void ConfigureCalendarEntities(ModelBuilder modelBuilder)
        {
            // Calendar Event configuration
            modelBuilder.Entity<CalendarEvent>(entity =>
            {
                entity.ToTable("calendar_events", t => 
                {
                    t.HasCheckConstraint("CK_calendar_events_Status", "\"Status\" IN ('scheduled', 'confirmed', 'cancelled', 'completed')");
                    t.HasCheckConstraint("CK_calendar_events_Priority", "\"Priority\" IN ('low', 'medium', 'high', 'urgent')");
                    t.HasCheckConstraint("CK_calendar_events_RelatedType", "related_type IN ('contact', 'sale', 'offer', 'project', 'service_order')");
                });
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Priority).IsRequired().HasMaxLength(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
                
                entity.HasIndex(e => e.Start);
                entity.HasIndex(e => e.End);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ContactId);
                entity.HasIndex(e => e.RelatedType);
                entity.HasIndex(e => e.RelatedId);

                // Foreign key relationships
                entity.HasOne(e => e.Contact)
                    .WithMany()
                    .HasForeignKey(e => e.ContactId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.EventTypeNavigation)
                    .WithMany(et => et.CalendarEvents)
                    .HasForeignKey(e => e.Type)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Event Type configuration
            modelBuilder.Entity<EventType>(entity =>
            {
                entity.ToTable("event_types");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                
                entity.HasData(
                    new EventType { Id = "meeting", Name = "Meeting", Description = "General meetings and discussions", Color = "#3B82F6", IsDefault = true, IsActive = true },
                    new EventType { Id = "appointment", Name = "Appointment", Description = "Client appointments", Color = "#10B981", IsDefault = false, IsActive = true },
                    new EventType { Id = "call", Name = "Phone Call", Description = "Scheduled phone calls", Color = "#F59E0B", IsDefault = false, IsActive = true },
                    new EventType { Id = "task", Name = "Task", Description = "Task reminders and deadlines", Color = "#EF4444", IsDefault = false, IsActive = true },
                    new EventType { Id = "event", Name = "Event", Description = "Special events and occasions", Color = "#8B5CF6", IsDefault = false, IsActive = true },
                    new EventType { Id = "reminder", Name = "Reminder", Description = "General reminders", Color = "#6B7280", IsDefault = false, IsActive = true }
                );
            });

            // Event Attendee configuration
            modelBuilder.Entity<EventAttendee>(entity =>
            {
                entity.ToTable("event_attendees", t => 
                {
                    t.HasCheckConstraint("CK_event_attendees_Status", "\"Status\" IN ('pending', 'accepted', 'declined', 'tentative')");
                });
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Status).HasDefaultValue("pending").HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.HasIndex(e => e.EventId);

                entity.HasOne(e => e.CalendarEvent)
                    .WithMany(ce => ce.EventAttendees)
                    .HasForeignKey(e => e.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Event Reminder configuration
            modelBuilder.Entity<EventReminder>(entity =>
            {
                entity.ToTable("event_reminders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.HasIndex(e => e.EventId);
                entity.HasIndex(e => e.MinutesBefore);

                entity.HasOne(e => e.CalendarEvent)
                    .WithMany(ce => ce.EventReminders)
                    .HasForeignKey(e => e.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureLookupEntities(ModelBuilder modelBuilder)
        {
            // Configure LookupItem entity
            modelBuilder.Entity<LookupItem>(entity =>
            {
                entity.ToTable("LookupItems");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.LookupType);
                entity.HasIndex(e => new { e.LookupType, e.Name }).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsDeleted);
                
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.SortOrder).HasDefaultValue(0);
            });

            // Configure Currency entity
            modelBuilder.Entity<Currency>(entity =>
            {
                entity.ToTable("Currencies");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsDeleted);
                
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);
                entity.Property(e => e.SortOrder).HasDefaultValue(0);
            });
        }

        private static void SeedLookupData(ModelBuilder modelBuilder)
        {
            var createdAt = DateTime.UtcNow;
            var lookupItems = new List<LookupItem>();

            // Article Categories
            lookupItems.AddRange(new[]
            {
                new LookupItem { Id = "electronics", Name = "Electronics", Color = "#3B82F6", LookupType = "article-category", IsActive = true, SortOrder = 0, CreatedUser = "system", CreatedAt = createdAt },
                new LookupItem { Id = "tools", Name = "Tools", Color = "#EF4444", LookupType = "article-category", IsActive = true, SortOrder = 1, CreatedUser = "system", CreatedAt = createdAt },
                new LookupItem { Id = "materials", Name = "Materials", Color = "#10B981", LookupType = "article-category", IsActive = true, SortOrder = 2, CreatedUser = "system", CreatedAt = createdAt }
            });

            // Priorities
            lookupItems.AddRange(new[]
            {
                new LookupItem { Id = "low", Name = "Low", Color = "#10B981", LookupType = "priority", IsActive = true, SortOrder = 0, CreatedUser = "system", CreatedAt = createdAt, Level = 1 },
                new LookupItem { Id = "medium", Name = "Medium", Color = "#F59E0B", LookupType = "priority", IsActive = true, SortOrder = 1, CreatedUser = "system", CreatedAt = createdAt, Level = 2 },
                new LookupItem { Id = "high", Name = "High", Color = "#EF4444", LookupType = "priority", IsActive = true, SortOrder = 2, CreatedUser = "system", CreatedAt = createdAt, Level = 3 },
                new LookupItem { Id = "urgent", Name = "Urgent", Color = "#ef4444", LookupType = "priority", IsActive = true, SortOrder = 3, CreatedUser = "system", CreatedAt = createdAt, Level = 4 }
            });

            // Technician Statuses
            lookupItems.AddRange(new[]
            {
                new LookupItem { Id = "available", Name = "Available", Color = "#10B981", Description = "Available for assignments", LookupType = "technician-status", IsActive = true, SortOrder = 0, CreatedUser = "system", CreatedAt = createdAt, IsAvailable = true },
                new LookupItem { Id = "busy", Name = "Busy", Color = "#F59E0B", Description = "Currently busy / assigned", LookupType = "technician-status", IsActive = true, SortOrder = 1, CreatedUser = "system", CreatedAt = createdAt, IsAvailable = false }
            });

            modelBuilder.Entity<LookupItem>().HasData(lookupItems);
        }

        private static void SeedCurrencies(ModelBuilder modelBuilder)
        {
            var createdAt = DateTime.UtcNow;
            var currencies = new[]
            {
                new Currency { Id = "USD", Name = "USD ($)", Symbol = "$", Code = "USD", IsActive = true, IsDefault = true, SortOrder = 0, CreatedUser = "system", CreatedAt = createdAt },
                new Currency { Id = "EUR", Name = "EUR (€)", Symbol = "€", Code = "EUR", IsActive = true, IsDefault = false, SortOrder = 1, CreatedUser = "system", CreatedAt = createdAt },
                new Currency { Id = "GBP", Name = "GBP (£)", Symbol = "£", Code = "GBP", IsActive = true, IsDefault = false, SortOrder = 2, CreatedUser = "system", CreatedAt = createdAt },
                new Currency { Id = "TND", Name = "TND (د.ت)", Symbol = "د.ت", Code = "TND", IsActive = true, IsDefault = false, SortOrder = 3, CreatedUser = "system", CreatedAt = createdAt }
            };

            modelBuilder.Entity<Currency>().HasData(currencies);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is MainAdminUser adminUser)
                {
                    if (entry.State == EntityState.Modified)
                    {
                        adminUser.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity is UserPreferences preferences)
                {
                    if (entry.State == EntityState.Modified)
                    {
                        preferences.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity is Contact contact)
                {
                    if (entry.State == EntityState.Modified)
                    {
                        contact.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity is Article article)
                {
                    if (entry.State == EntityState.Modified)
                    {
                        article.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity is LookupItem lookup)
                {
                    if (entry.State == EntityState.Modified)
                    {
                        lookup.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.Entity is Currency currency)
                {
                    if (entry.State == EntityState.Modified)
                    {
                        currency.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }
    }
}