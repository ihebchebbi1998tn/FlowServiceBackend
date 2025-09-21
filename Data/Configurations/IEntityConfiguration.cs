using Microsoft.EntityFrameworkCore;

namespace MyApi.Data.Configurations
{
    /// <summary>
    /// Base interface for entity configurations
    /// </summary>
    public interface IEntityConfiguration
    {
        void Configure(ModelBuilder modelBuilder);
    }
}