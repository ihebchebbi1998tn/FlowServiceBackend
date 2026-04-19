using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;

namespace MyApi.Infrastructure;

/// <summary>
/// In-memory cache mapping tenant slugs to their numeric TenantId.
/// Populated at startup and refreshed on tenant CRUD operations.
/// 
/// IMPORTANT: TenantId in data tables does NOT equal Tenant.Id.
/// - TenantId = 0 means "default tenant" (existing data before multi-tenancy)
/// - TenantId = Tenant.Id for all other tenants
/// - The "default" tenant (Slug="default", IsDefault=true) maps to TenantId = 0
/// </summary>
public static class TenantSlugCache
{
    private static readonly ConcurrentDictionary<string, int> _slugToId = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize(ApplicationDbContext db)
    {
        try
        {
            // Use IgnoreQueryFilters since Tenant table doesn't have TenantId filter
            var tenants = db.Tenants
                .Where(t => t.IsActive)
                .Select(t => new { t.Slug, t.Id, t.IsDefault })
                .ToList();

            _slugToId.Clear();

            // We no longer force the "default" tenant to map to TenantId = 0.
            // If the user chooses a different default company, we must respect its actual ID
            // so that its isolated data is loaded correctly. 
            // Only the very first automatic tenant (if specifically handled elsewhere) 
            // or the fallback system uses 0. Here, every stored tenant maps to its real database ID.
            foreach (var t in tenants)
            {
                _slugToId[t.Slug] = t.Id;
            }
        }
        catch (Exception ex)
        {
            // Surface the underlying Postgres error so we can tell whether it's
            // a missing table (pre-migration) vs. a real DB failure (quota, auth, network).
            var connStr = db.Database.GetConnectionString() ?? "";
            string host = "unknown", name = "unknown";
            try
            {
                var csb = new Npgsql.NpgsqlConnectionStringBuilder(connStr);
                host = csb.Host ?? "unknown";
                name = csb.Database ?? "unknown";
            }
            catch { }

            if (ex is Npgsql.PostgresException pg)
            {
                Console.WriteLine($"[TenantSlugCache] ❌ Postgres error on host='{host}' db='{name}' → SqlState={pg.SqlState} Message={pg.MessageText}");
            }
            else
            {
                Console.WriteLine($"[TenantSlugCache] ❌ Could not initialize on host='{host}' db='{name}': {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Refresh the cache (call after tenant CRUD operations).
    /// </summary>
    public static void Refresh(ApplicationDbContext db)
    {
        Initialize(db);
    }

    /// <summary>
    /// Get the numeric TenantId for a slug. Returns 0 (default) if unknown.
    /// </summary>
    public static int GetTenantId(string? slug)
    {
        if (string.IsNullOrEmpty(slug))
            return 0;

        if (_slugToId.TryGetValue(slug, out var id))
            return id;

        // Unknown slug → default tenant
        return 0;
    }

    /// <summary>
    /// Check if a slug exists in the cache.
    /// </summary>
    public static bool HasTenant(string slug) => _slugToId.ContainsKey(slug);

    /// <summary>
    /// Check if a numeric tenant ID exists and is active in the cache.
    /// Used by TenantMiddleware to validate X-Target-Tenant header.
    /// </summary>
    public static bool IsValidTenantId(int tenantId) => _slugToId.Values.Contains(tenantId) || tenantId == 0;
}