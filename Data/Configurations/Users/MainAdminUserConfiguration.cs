using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data.Configurations.Users
{
    public class MainAdminUserConfiguration : IEntityConfiguration
    {
        public void Configure(ModelBuilder modelBuilder)
        {
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
        }
    }
}