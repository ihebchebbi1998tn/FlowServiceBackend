using Microsoft.EntityFrameworkCore;
using MyApi.Modules.Contacts.Models;
using MyApi.Modules.Shared.Data.Configurations;

namespace MyApi.Modules.Contacts.Data.Configurations
{
    public class ContactNoteConfiguration : IEntityConfiguration
    {
        public void Configure(ModelBuilder modelBuilder)
        {
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
        }
    }
}
