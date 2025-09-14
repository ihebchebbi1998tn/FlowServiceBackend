using FlowServiceBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowServiceBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<MainAdminUser> MainAdminUsers { get; set; }
        public DbSet<UserPreferences> UserPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure MainAdminUser entity for PostgreSQL
            modelBuilder.Entity<MainAdminUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsActive);
                
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Country).IsRequired().HasMaxLength(2);
                entity.Property(e => e.Industry).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AccessToken).HasColumnType("text");
                entity.Property(e => e.RefreshToken).HasColumnType("text");
                entity.Property(e => e.CompanyName).HasMaxLength(255);
                entity.Property(e => e.CompanyWebsite).HasMaxLength(500);
                entity.Property(e => e.Preferences).HasColumnType("text");

                // Set default values
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);
            });

            // Configure UserPreferences entity for PostgreSQL
            modelBuilder.Entity<UserPreferences>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                
                entity.Property(e => e.Theme).IsRequired().HasMaxLength(20).HasDefaultValue("system");
                entity.Property(e => e.Language).IsRequired().HasMaxLength(5).HasDefaultValue("en");
                entity.Property(e => e.PrimaryColor).IsRequired().HasMaxLength(20).HasDefaultValue("blue");
                entity.Property(e => e.LayoutMode).IsRequired().HasMaxLength(20).HasDefaultValue("sidebar");
                entity.Property(e => e.DataView).IsRequired().HasMaxLength(10).HasDefaultValue("table");
                entity.Property(e => e.Timezone).HasMaxLength(100);
                entity.Property(e => e.DateFormat).IsRequired().HasMaxLength(20).HasDefaultValue("MM/DD/YYYY");
                entity.Property(e => e.TimeFormat).IsRequired().HasMaxLength(5).HasDefaultValue("12h");
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(5).HasDefaultValue("USD");
                entity.Property(e => e.NumberFormat).IsRequired().HasMaxLength(10).HasDefaultValue("comma");
                entity.Property(e => e.Notifications).HasColumnType("text").HasDefaultValue("{}");
                entity.Property(e => e.WorkArea).HasMaxLength(100);
                entity.Property(e => e.DashboardLayout).HasColumnType("text");
                entity.Property(e => e.QuickAccessItems).HasColumnType("text").HasDefaultValue("[]");

                // Set default values for boolean fields
                entity.Property(e => e.SidebarCollapsed).HasDefaultValue(false);
                entity.Property(e => e.CompactMode).HasDefaultValue(false);
                entity.Property(e => e.ShowTooltips).HasDefaultValue(true);
                entity.Property(e => e.AnimationsEnabled).HasDefaultValue(true);
                entity.Property(e => e.SoundEnabled).HasDefaultValue(true);
                entity.Property(e => e.AutoSave).HasDefaultValue(true);

                // Set default values for timestamps
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
                
                // Foreign key relationship
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
