using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using Npgsql;
using System.Collections.Concurrent;

namespace MyApi.Infrastructure;

/// <summary>
/// Factory that creates ApplicationDbContext instances with tenant-specific
/// connection strings. Registered as a singleton for performance — caches
/// normalized connection strings so they're only parsed once per tenant.
/// </summary>
public interface ITenantDbContextFactory
{
    ApplicationDbContext CreateDbContext(string? tenant);
    string GetConnectionString(string? tenant);
}

public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly string _defaultConnectionString;
    private readonly ILogger<TenantDbContextFactory> _logger;
    private readonly bool _isDevelopment;

    // ── Connection string cache: tenant → normalized conn string ──
    // Avoids re-parsing env vars & URIs on every request.
    private readonly ConcurrentDictionary<string, string> _connCache = new(StringComparer.OrdinalIgnoreCase);

    public TenantDbContextFactory(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<TenantDbContextFactory> logger)
    {
        _logger = logger;
        _isDevelopment = environment.IsDevelopment();

        // Resolve default connection string the same way Program.cs does
        var raw = Environment.GetEnvironmentVariable("DATABASE_URL") ??
                  configuration.GetConnectionString("DefaultConnection") ?? "";

        _defaultConnectionString = NormalizePgUrl(raw);
    }

    /// <summary>
    /// Returns the final connection string for a tenant (cached).
    /// Returns the default connection string when tenant is null or unknown.
    /// </summary>
    public string GetConnectionString(string? tenant)
    {
        if (string.IsNullOrEmpty(tenant))
            return _defaultConnectionString;

        return _connCache.GetOrAdd(tenant, t =>
        {
            var tenantConn = TenantConnectionResolver.GetConnectionString(t);
            if (tenantConn != null)
            {
                _logger.LogInformation("🏢 Tenant '{Tenant}' → tenant-specific DB resolved", t);
                return NormalizePgUrl(tenantConn);
            }

            _logger.LogInformation("🏢 Tenant '{Tenant}' → no specific DB, using default", t);
            return _defaultConnectionString;
        });
    }

    public ApplicationDbContext CreateDbContext(string? tenant)
    {
        var connStr = GetConnectionString(tenant);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connStr, npgsql =>
                npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null));

        if (_isDevelopment)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Converts postgres:// URIs to Npgsql connection strings and applies pool settings.
    /// </summary>
    private static string NormalizePgUrl(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;

        NpgsqlConnectionStringBuilder builder;

        if (raw.StartsWith("postgres://") || raw.StartsWith("postgresql://"))
        {
            var uri = new Uri(raw);
            var userInfo = uri.UserInfo?.Split(':', 2) ?? Array.Empty<string>();
            builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "",
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
                Database = uri.AbsolutePath?.TrimStart('/') ?? "",
                SslMode = SslMode.Require,
            };

            // Preserve query params (e.g. ?channel_binding=require)
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            foreach (var kv in queryParams)
            {
                try { builder[kv.Key] = kv.Value.ToString(); } catch { }
            }
        }
        else
        {
            builder = new NpgsqlConnectionStringBuilder(raw);
        }

        // Apply pool settings for performance
        builder.MaxPoolSize = 50;
        builder.MinPoolSize = 5;

        return builder.ToString();
    }
}
