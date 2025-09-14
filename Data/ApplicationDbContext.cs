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

            // Configure MainAdminUser entity
            modelBuilder.Entity<MainAdminUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Country).IsRequired().HasMaxLength(2);
                entity.Property(e => e.Industry).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AccessToken).HasMaxLength(500);
                entity.Property(e => e.RefreshToken).HasMaxLength(500);
                entity.Property(e => e.CompanyName).HasMaxLength(255);
                entity.Property(e => e.CompanyWebsite).HasMaxLength(500);
                entity.Property(e => e.Preferences).HasColumnType("nvarchar(max)");
            });

            // Configure UserPreferences entity
            modelBuilder.Entity<UserPreferences>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.Theme).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Language).IsRequired().HasMaxLength(5);
                entity.Property(e => e.PrimaryColor).IsRequired().HasMaxLength(20);
                entity.Property(e => e.LayoutMode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.DataView).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Timezone).HasMaxLength(100);
                entity.Property(e => e.DateFormat).IsRequired().HasMaxLength(20);
                entity.Property(e => e.TimeFormat).IsRequired().HasMaxLength(5);
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(5);
                entity.Property(e => e.NumberFormat).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Notifications).HasColumnType("nvarchar(max)");
                entity.Property(e => e.WorkArea).HasMaxLength(100);
                entity.Property(e => e.DashboardLayout).HasColumnType("nvarchar(max)");
                entity.Property(e => e.QuickAccessItems).HasColumnType("nvarchar(max)");
                
                // Foreign key relationship
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}