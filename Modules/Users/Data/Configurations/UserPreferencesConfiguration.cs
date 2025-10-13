using Microsoft.EntityFrameworkCore;
using MyApi.Modules.Users.Models;
using MyApi.Modules.Auth.Models;
using MyApi.Modules.Shared.Data.Configurations;

namespace MyApi.Modules.Users.Data.Configurations
{
    public class UserPreferencesConfiguration : IEntityConfiguration
    {
        public void Configure(ModelBuilder modelBuilder)
        {
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
        }
    }
}