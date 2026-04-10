using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApi.Modules.ExternalEndpoints.Models;

namespace MyApi.Modules.ExternalEndpoints.Database
{
    public class ExternalEndpointConfiguration : IEntityTypeConfiguration<ExternalEndpoint>
    {
        public void Configure(EntityTypeBuilder<ExternalEndpoint> builder)
        {
            builder.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique().HasFilter("\"IsDeleted\" = false");
            builder.HasMany(e => e.Logs).WithOne(l => l.Endpoint).HasForeignKey(l => l.EndpointId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ExternalEndpointLogConfiguration : IEntityTypeConfiguration<ExternalEndpointLog>
    {
        public void Configure(EntityTypeBuilder<ExternalEndpointLog> builder)
        {
            builder.HasIndex(l => l.EndpointId);
            builder.HasIndex(l => l.ReceivedAt);
        }
    }
}
