using Microsoft.EntityFrameworkCore;
using MyApi.Data;

namespace MyApi.Infrastructure;

/// <summary>
/// Middleware that reads the X-Tenant header from the request,
/// resolves a per-tenant connection string, and reconfigures
/// the ApplicationDbContext for the current request scope.
/// 
/// If no header is provided (or the tenant is unknown), the 
/// default connection string from DATABASE_URL / appsettings is used.
/// 
/// PERFORMANCE NOTES:
/// - Connection strings are cached in TenantDbContextFactory (parsed once).
/// - Default tenant (no header) skips all resolution → zero overhead.
/// - Npgsql pools connections per unique connection string automatically.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenant = context.Request.Headers["X-Tenant"].FirstOrDefault()?.Trim().ToLowerInvariant();

        if (!string.IsNullOrEmpty(tenant))
        {
            // Store tenant in HttpContext.Items so the scoped DbContext factory can read it
            context.Items["Tenant"] = tenant;
        }

        await _next(context);
    }
}

/// <summary>
/// Resolves connection strings per tenant.
/// Checks (in order):
///   1. In-memory dictionary (for hardcoded tenants)
///   2. Environment variable TENANT_{NAME}_DATABASE_URL
///   3. Returns null → caller uses default connection
/// </summary>
public static class TenantConnectionResolver
{
    // ─── Tenant → Connection String map ───
    // Key   = subdomain (lowercase), e.g. "demo", "client1"
    // Value = full PostgreSQL connection string or URI
    //
    // IMPORTANT: In production, move these to environment variables
    // or a config table. This dictionary is a simple starting point.
    private static readonly Dictionary<string, string> _tenantConnections = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add tenant mappings here. Examples:
        // ["demo"]    = "Host=...;Port=5432;Database=demo_db;Username=...;Password=...;SSL Mode=Require",
        // ["client1"] = "postgresql://user:pass@host:5432/client1_db?sslmode=require",
    };

    /// <summary>
    /// Returns the connection string for a given tenant, or null for default.
    /// </summary>
    public static string? GetConnectionString(string? tenant)
    {
        if (string.IsNullOrEmpty(tenant))
            return null;

        // 1. Check hardcoded dictionary
        if (_tenantConnections.TryGetValue(tenant, out var connStr))
            return connStr;

        // 2. Check environment variable: TENANT_DEMO_DATABASE_URL
        var envKey = $"TENANT_{tenant.ToUpperInvariant()}_DATABASE_URL";
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        // 3. Unknown tenant → use default
        return null;
    }
}
