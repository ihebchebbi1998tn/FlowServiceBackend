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
/// Special value "__all__" enables cross-company mode for MainAdminUsers.
/// Mutations require an additional X-Target-Tenant header specifying the
/// target tenant ID, so that writes are properly scoped.
/// </summary>
public class TenantMiddleware
{
    public const string TenantHeaderName = "X-Tenant";
    public const string TargetTenantHeaderName = "X-Target-Tenant";
    public const string ViewAllSentinel = "__all__";

    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenant = context.Request.Headers[TenantHeaderName].FirstOrDefault()?.Trim().ToLowerInvariant();

        if (string.Equals(tenant, ViewAllSentinel, StringComparison.OrdinalIgnoreCase))
        {
            // ── Cross-company "View All" mode ──
            // Only MainAdminUser may use this. Check JWT claim.
            var isMainAdmin = context.User?.Claims
                .Any(c => c.Type == "user_type" && c.Value == "MainAdminUser") == true;

            if (!isMainAdmin)
            {
                _logger.LogWarning("🚫 TENANT-MIDDLEWARE: Non-MainAdmin attempted __all__ mode");
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Only MainAdminUser can use view-all mode" });
                return;
            }

            var method = context.Request.Method.ToUpperInvariant();
            var isMutation = method is "POST" or "PUT" or "PATCH" or "DELETE";

            if (isMutation)
            {
                // Allow certain safe endpoints without X-Target-Tenant
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                var isSafeEndpoint = path.Contains("/api/tenants") || path.Contains("/api/auth");

                if (!isSafeEndpoint)
                {
                    // Mutations require X-Target-Tenant header
                    var targetTenantHeader = context.Request.Headers[TargetTenantHeaderName].FirstOrDefault();
                    
                    if (string.IsNullOrEmpty(targetTenantHeader) || !int.TryParse(targetTenantHeader, out var targetTenantId))
                    {
                        _logger.LogWarning("🚫 TENANT-MIDDLEWARE: Mutation in view-all mode without valid X-Target-Tenant header");
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync(new { error = "X-Target-Tenant header with a valid tenant ID is required for mutations in view-all mode." });
                        return;
                    }

                    // Validate tenant exists and is active
                    var validTenantId = TenantSlugCache.IsValidTenantId(targetTenantId);
                    if (!validTenantId)
                    {
                        _logger.LogWarning("🚫 TENANT-MIDDLEWARE: X-Target-Tenant {TenantId} is invalid or inactive", targetTenantId);
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsJsonAsync(new { error = $"Target tenant {targetTenantId} does not exist or is inactive." });
                        return;
                    }

                    // Scope this mutation to the target tenant
                    context.Items["Tenant"] = ViewAllSentinel;
                    context.Items["TenantId"] = targetTenantId;
                    context.Items["TenantViewAll"] = true;
                    context.Items["TenantTargetOverride"] = true;
                    _logger.LogDebug("🏢 TENANT-MIDDLEWARE: {Method} {Path} → VIEW-ALL mutation scoped to TenantId={TenantId}",
                        method, context.Request.Path, targetTenantId);

                    await _next(context);
                    return;
                }
            }

            // Read-only requests or safe endpoints: use -1 sentinel (no tenant filter)
            context.Items["Tenant"] = ViewAllSentinel;
            context.Items["TenantId"] = -1; // Sentinel for "all tenants"
            context.Items["TenantViewAll"] = true;
            _logger.LogDebug("🏢 TENANT-MIDDLEWARE: Request {Method} {Path} → VIEW-ALL mode (MainAdmin)",
                context.Request.Method, context.Request.Path);
        }
        else if (!string.IsNullOrEmpty(tenant))
        {
            context.Items["Tenant"] = tenant;
            var tenantId = TenantSlugCache.GetTenantId(tenant);
            context.Items["TenantId"] = tenantId;
            _logger.LogDebug("🏢 TENANT-MIDDLEWARE: Request {Method} {Path} → tenant='{Tenant}' (TenantId={TenantId})",
                context.Request.Method, context.Request.Path, tenant, tenantId);
        }
        else
        {
            context.Items["TenantId"] = 0; // Default tenant
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
    private static readonly Dictionary<string, string> _tenantConnections = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add tenant mappings here.
    };

    public static string? GetConnectionString(string? tenant)
    {
        if (string.IsNullOrEmpty(tenant))
            return null;

        if (_tenantConnections.TryGetValue(tenant, out var connStr))
            return connStr;

        var envKey = $"TENANT_{tenant.ToUpperInvariant()}_DATABASE_URL";
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        return null;
    }
}

/// <summary>
/// Resolves a tenant-specific public files base URL.
/// </summary>
public static class TenantPublicUrlResolver
{
    private static readonly Dictionary<string, string> _tenantPublicBases = new(StringComparer.OrdinalIgnoreCase)
    {
    };

    public static string? GetPublicBaseUrl(string? tenant)
    {
        if (string.IsNullOrEmpty(tenant)) return null;

        if (_tenantPublicBases.TryGetValue(tenant, out var baseUrl)) return baseUrl;

        var envKey = $"TENANT_{tenant.ToUpperInvariant()}_PUBLIC_BASE_URL";
        var envVal = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrEmpty(envVal)) return envVal;

        return null;
    }
}
