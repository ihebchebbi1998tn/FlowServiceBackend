using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data
{
    /// <summary>
    /// Legacy configurations that need to be refactored
    /// TODO: Move these to separate configuration files
    /// </summary>
    public partial class ApplicationDbContext
    {
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

            // Event Type configuration - abbreviated for space
            modelBuilder.Entity<EventType>(entity =>
            {
                entity.ToTable("event_types");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Color).IsRequired().HasMaxLength(7);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                
                // Seed data - keeping essential data only
                entity.HasData(
                    new EventType { Id = "meeting", Name = "Meeting", Description = "General meetings and discussions", Color = "#3B82F6", IsDefault = true, IsActive = true },
                    new EventType { Id = "appointment", Name = "Appointment", Description = "Client appointments", Color = "#10B981", IsDefault = false, IsActive = true }
                );
            });

            // Event Attendee and Reminder configurations - simplified
            modelBuilder.Entity<EventAttendee>(entity =>
            {
                entity.ToTable("event_attendees");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                
                entity.HasOne(ea => ea.CalendarEvent)
                    .WithMany(ce => ce.Attendees)
                    .HasForeignKey(ea => ea.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EventReminder>(entity =>
            {
                entity.ToTable("event_reminders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                
                entity.HasOne(er => er.CalendarEvent)
                    .WithMany(ce => ce.Reminders)
                    .HasForeignKey(er => er.EventId)
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
                entity.HasIndex(e => new { e.Category, e.Value }).IsUnique();
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.SortOrder);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Configure Currency entity
            modelBuilder.Entity<Currency>(entity =>
            {
                entity.ToTable("Currencies");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });
        }
    }
}
