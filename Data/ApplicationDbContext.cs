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
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .HasPrincipalKey(u => u.Id)
                    .OnDelete(DeleteBehavior.Cascade);
            });
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
            }
        }
    }
}