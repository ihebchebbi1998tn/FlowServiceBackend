using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data.Configurations.Contacts
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