using Microsoft.EntityFrameworkCore;
using MyApi.Modules.Contacts.Models;
using MyApi.Modules.Shared.Data.Configurations;

namespace MyApi.Modules.Contacts.Data.Configurations
{
    public class ContactConfiguration : IEntityConfiguration
    {
        public void Configure(ModelBuilder modelBuilder)
        {
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
        }
    }
}
