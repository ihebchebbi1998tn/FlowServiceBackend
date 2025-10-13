using Microsoft.EntityFrameworkCore;
using MyApi.Modules.Lookups.Models;

namespace MyApi.Data.SeedData
{
    public class LookupSeedData
    {
        public void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LookupItem>().HasData(
                // Contact Types
                new LookupItem { Id = "CT001", LookupType = "ContactType", Name = "individual", Description = "Individual Contact", SortOrder = 1, IsActive = true, CreatedUser = "system" },
                new LookupItem { Id = "CT002", LookupType = "ContactType", Name = "company", Description = "Company Contact", SortOrder = 2, IsActive = true, CreatedUser = "system" },
                
                // Contact Status
                new LookupItem { Id = "CS001", LookupType = "ContactStatus", Name = "active", Description = "Active Contact", SortOrder = 1, IsActive = true, CreatedUser = "system" },
                new LookupItem { Id = "CS002", LookupType = "ContactStatus", Name = "inactive", Description = "Inactive Contact", SortOrder = 2, IsActive = true, CreatedUser = "system" },
                new LookupItem { Id = "CS003", LookupType = "ContactStatus", Name = "prospect", Description = "Prospect", SortOrder = 3, IsActive = true, CreatedUser = "system" },
                new LookupItem { Id = "CS004", LookupType = "ContactStatus", Name = "customer", Description = "Customer", SortOrder = 4, IsActive = true, CreatedUser = "system" },
                
                // Article Categories  
                new LookupItem { Id = "AC001", LookupType = "ArticleCategory", Name = "hardware", Description = "Hardware", SortOrder = 1, IsActive = true, CreatedUser = "system" },
                new LookupItem { Id = "AC002", LookupType = "ArticleCategory", Name = "software", Description = "Software", SortOrder = 2, IsActive = true, CreatedUser = "system" },
                new LookupItem { Id = "AC003", LookupType = "ArticleCategory", Name = "service", Description = "Service", SortOrder = 3, IsActive = true, CreatedUser = "system" },
                
                // Article Status
                new LookupItem { Id = "AS001", LookupType = "ArticleStatus", Name = "active", Description = "Active", SortOrder = 1, IsActive = true, CreatedUser = "system" },
                new LookupItem { Id = "AS002", LookupType = "ArticleStatus", Name = "inactive", Description = "Inactive", SortOrder = 2, IsActive = true, CreatedUser = "system" },
                new LookupItem { Id = "AS003", LookupType = "ArticleStatus", Name = "discontinued", Description = "Discontinued", SortOrder = 3, IsActive = true, CreatedUser = "system" }
            );
        }
    }
}
