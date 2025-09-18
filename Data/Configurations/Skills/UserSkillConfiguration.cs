using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data.Configurations.Skills
{
    public class UserSkillConfiguration : IEntityConfiguration
    {
        public void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSkill>(entity =>
            {
                entity.ToTable("UserSkills");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.SkillId });
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // Define relationships
                entity.HasOne(us => us.User)
                    .WithMany()
                    .HasForeignKey(us => us.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(us => us.Skill)
                    .WithMany(s => s.UserSkills)
                    .HasForeignKey(us => us.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}