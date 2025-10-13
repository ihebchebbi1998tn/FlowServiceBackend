using Microsoft.EntityFrameworkCore;
using MyApi.Modules.Lookups.Models;

namespace MyApi.Data.SeedData
{
    public class CurrencySeedData
    {
        public void Seed(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Currency>().HasData(
                new Currency { Id = "USD", Code = "USD", Name = "US Dollar", Symbol = "$", IsActive = true },
                new Currency { Id = "EUR", Code = "EUR", Name = "Euro", Symbol = "€", IsActive = true },
                new Currency { Id = "GBP", Code = "GBP", Name = "British Pound", Symbol = "£", IsActive = true },
                new Currency { Id = "CAD", Code = "CAD", Name = "Canadian Dollar", Symbol = "C$", IsActive = true },
                new Currency { Id = "AUD", Code = "AUD", Name = "Australian Dollar", Symbol = "A$", IsActive = true }
            );
        }
    }
}
