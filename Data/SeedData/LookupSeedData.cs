using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data.SeedData
{
    public class LookupSeedData
    {
        public void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LookupItem>().HasData(
                // Contact Types
                new LookupItem { Id = 1, Category = "ContactType", Value = "individual", Description = "Individual Contact", SortOrder = 1, IsActive = true },
                new LookupItem { Id = 2, Category = "ContactType", Value = "company", Description = "Company Contact", SortOrder = 2, IsActive = true },
                
                // Contact Status
                new LookupItem { Id = 11, Category = "ContactStatus", Value = "active", Description = "Active Contact", SortOrder = 1, IsActive = true },
                new LookupItem { Id = 12, Category = "ContactStatus", Value = "inactive", Description = "Inactive Contact", SortOrder = 2, IsActive = true },
                new LookupItem { Id = 13, Category = "ContactStatus", Value = "prospect", Description = "Prospect", SortOrder = 3, IsActive = true },
                new LookupItem { Id = 14, Category = "ContactStatus", Value = "customer", Description = "Customer", SortOrder = 4, IsActive = true },
                
                // Article Categories  
                new LookupItem { Id = 21, Category = "ArticleCategory", Value = "hardware", Description = "Hardware", SortOrder = 1, IsActive = true },
                new LookupItem { Id = 22, Category = "ArticleCategory", Value = "software", Description = "Software", SortOrder = 2, IsActive = true },
                new LookupItem { Id = 23, Category = "ArticleCategory", Value = "service", Description = "Service", SortOrder = 3, IsActive = true },
                
                // Article Status
                new LookupItem { Id = 31, Category = "ArticleStatus", Value = "active", Description = "Active", SortOrder = 1, IsActive = true },
                new LookupItem { Id = 32, Category = "ArticleStatus", Value = "inactive", Description = "Inactive", SortOrder = 2, IsActive = true },
                new LookupItem { Id = 33, Category = "ArticleStatus", Value = "discontinued", Description = "Discontinued", SortOrder = 3, IsActive = true }
            );
        }
    }
}