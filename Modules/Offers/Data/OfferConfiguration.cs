using Microsoft.EntityFrameworkCore;
using MyApi.Modules.Offers.Models;
using MyApi.Modules.Shared.Data.Configurations;

namespace MyApi.Modules.Offers.Data
{
    public class OfferConfiguration : IEntityConfiguration
    {
        public void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Offer>(entity =>
            {
                entity.ToTable("offers");
                entity.HasKey(o => o.Id);

                entity.Property(o => o.Title)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(o => o.ContactId)
                    .IsRequired();

                entity.Property(o => o.Status)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(o => o.Currency)
                    .IsRequired()
                    .HasMaxLength(3);

                entity.Property(o => o.Amount)
                    .HasColumnType("decimal(15,2)")
                    .IsRequired();

                entity.Property(o => o.Taxes)
                    .HasColumnType("decimal(15,2)");

                entity.Property(o => o.Discount)
                    .HasColumnType("decimal(15,2)");

                entity.Property(o => o.TotalAmount)
                    .HasColumnType("decimal(15,2)")
                    .HasComputedColumnSql("(amount + COALESCE(taxes, 0) - COALESCE(discount, 0))", stored: true);

                entity.Property(o => o.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");

                entity.Property(o => o.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");

                entity.HasIndex(o => o.ContactId)
                    .HasDatabaseName("idx_offers_contact_id");

                entity.HasIndex(o => o.Status)
                    .HasDatabaseName("idx_offers_status");

                entity.HasIndex(o => o.Category)
                    .HasDatabaseName("idx_offers_category");

                entity.HasIndex(o => o.CreatedAt)
                    .HasDatabaseName("idx_offers_created_at");

                entity.HasIndex(o => o.UpdatedAt)
                    .HasDatabaseName("idx_offers_updated_at");

                entity.HasIndex(o => o.AssignedTo)
                    .HasDatabaseName("idx_offers_assigned_to");

                entity.HasMany(o => o.Items)
                    .WithOne(i => i.Offer)
                    .HasForeignKey(i => i.OfferId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(o => o.Activities)
                    .WithOne(a => a.Offer)
                    .HasForeignKey(a => a.OfferId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

    public class OfferItemConfiguration : IEntityConfiguration
    {
        public void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OfferItem>(entity =>
            {
                entity.ToTable("offer_items");
                entity.HasKey(i => i.Id);

                entity.Property(i => i.ItemName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(i => i.Type)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(i => i.Quantity)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();

                entity.Property(i => i.UnitPrice)
                    .HasColumnType("decimal(15,2)")
                    .IsRequired();

                entity.Property(i => i.Discount)
                    .HasColumnType("decimal(15,2)");

                entity.Property(i => i.TotalPrice)
                    .HasColumnType("decimal(15,2)")
                    .HasComputedColumnSql(@"
                        (quantity * unit_price - COALESCE(
                            CASE 
                                WHEN discount_type = 'percentage' THEN (quantity * unit_price * discount / 100)
                                WHEN discount_type = 'fixed' THEN discount
                                ELSE 0
                            END, 0
                        ))
                    ", stored: true);

                entity.HasIndex(i => i.OfferId)
                    .HasDatabaseName("idx_offer_items_offer_id");

                entity.HasIndex(i => i.Type)
                    .HasDatabaseName("idx_offer_items_type");

                entity.HasIndex(i => i.ItemId)
                    .HasDatabaseName("idx_offer_items_item_id");
            });
        }
    }

    public class OfferActivityConfiguration : IEntityConfiguration
    {
        public void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OfferActivity>(entity =>
            {
                entity.ToTable("offer_activities");
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Type)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(a => a.Description)
                    .IsRequired();

                entity.Property(a => a.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("NOW()");

                entity.Property(a => a.CreatedByName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasIndex(a => a.OfferId)
                    .HasDatabaseName("idx_offer_activities_offer_id");

                entity.HasIndex(a => a.Type)
                    .HasDatabaseName("idx_offer_activities_type");

                entity.HasIndex(a => a.CreatedAt)
                    .HasDatabaseName("idx_offer_activities_created_at");
            });
        }
    }
}
