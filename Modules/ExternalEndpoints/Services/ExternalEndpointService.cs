using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApi.Data;
using MyApi.Modules.ExternalEndpoints.DTOs;
using MyApi.Modules.ExternalEndpoints.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MyApi.Modules.ExternalEndpoints.Services
{
    public class ExternalEndpointService : IExternalEndpointService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExternalEndpointService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory? _httpClientFactory;
        private readonly IWebhookForwardQueue? _forwardQueue;

        public ExternalEndpointService(ApplicationDbContext context, ILogger<ExternalEndpointService> logger,
            IServiceScopeFactory scopeFactory, IHttpClientFactory? httpClientFactory = null,
            IWebhookForwardQueue? forwardQueue = null)
        {
            _context = context;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _forwardQueue = forwardQueue;
        }

        // Plain key returned ONCE on create/regenerate. The DB only stores the hash.
        private const string HashPrefix = "h$";

        private static string GenerateApiKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return "ext_" + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..40];
        }

        private static string HashApiKey(string plain)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(plain));
            return HashPrefix + Convert.ToHexString(hash);
        }

        // Constant-time verification. Supports legacy plaintext rows during migration.
        private static bool VerifyApiKey(string stored, string provided)
        {
            if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(provided)) return false;
            string a = stored;
            string b = stored.StartsWith(HashPrefix) ? HashApiKey(provided) : provided;
            var aBytes = Encoding.UTF8.GetBytes(a);
            var bBytes = Encoding.UTF8.GetBytes(b);
            if (aBytes.Length != bBytes.Length) return false;
            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }

        private static string GenerateSlug(string name)
        {
            var slug = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            return slug + "-" + Guid.NewGuid().ToString("N")[..6];
        }

        // Mask the API key for list/get responses. The plain key is returned only
        // from CreateEndpointAsync, RegenerateKeyAsync, and the dedicated
        // RevealKeyAsync endpoint (which requires re-auth via [Authorize]).
        private static string MaskApiKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (key.Length <= 8) return new string('•', key.Length);
            var prefix = key.StartsWith("ext_") ? "ext_" : key[..Math.Min(4, key.Length)];
            var suffix = key[^4..];
            return $"{prefix}••••••••••••{suffix}";
        }

        private ExternalEndpointDto MapToDto(ExternalEndpoint e, int totalReceived = 0, int receivedToday = 0, DateTime? lastReceived = null, bool revealKey = false)
        {
            return new ExternalEndpointDto
            {
                Id = e.Id,
                Name = e.Name,
                Slug = e.Slug,
                Description = e.Description,
                ApiKey = revealKey ? e.ApiKey : MaskApiKey(e.ApiKey),
                IsActive = e.IsActive,
                AllowedMethods = e.AllowedMethods,
                AllowedOrigins = e.AllowedOrigins,
                ExpectedSchema = e.ExpectedSchema,
                ResponseTemplate = e.ResponseTemplate,
                WebhookForwardUrl = e.WebhookForwardUrl,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                CreatedBy = e.CreatedBy,
                TotalReceived = totalReceived,
                ReceivedToday = receivedToday,
                LastReceived = lastReceived
            };
        }

        // SSRF protection: reject webhook forwards to private/loopback/metadata IPs.
        private static (bool ok, string? error) ValidateWebhookUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return (true, null);
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return (false, "Invalid URL");
            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return (false, "URL must be http or https");
            var host = uri.Host.ToLowerInvariant();
            if (host == "localhost" || host == "0.0.0.0" || host.EndsWith(".local") || host.EndsWith(".internal"))
                return (false, "Loopback / internal hosts are not allowed");
            try
            {
                var addresses = System.Net.Dns.GetHostAddresses(host);
                foreach (var addr in addresses)
                {
                    var bytes = addr.GetAddressBytes();
                    if (System.Net.IPAddress.IsLoopback(addr)) return (false, "Loopback IPs are not allowed");
                    if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        // 10/8, 172.16/12, 192.168/16, 169.254/16 (link-local incl. AWS metadata)
                        if (bytes[0] == 10) return (false, "Private IPs are not allowed");
                        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return (false, "Private IPs are not allowed");
                        if (bytes[0] == 192 && bytes[1] == 168) return (false, "Private IPs are not allowed");
                        if (bytes[0] == 169 && bytes[1] == 254) return (false, "Link-local / metadata IPs are not allowed");
                        if (bytes[0] == 127) return (false, "Loopback IPs are not allowed");
                    }
                    else if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        // ::1 loopback already caught by IsLoopback. Block ULA fc00::/7,
                        // link-local fe80::/10, IPv4-mapped (::ffff:a.b.c.d) re-checked,
                        // and the IPv6 metadata range (fd00:ec2::254 / Azure fd00::/8).
                        if (addr.IsIPv6LinkLocal || addr.IsIPv6SiteLocal) return (false, "Link-local IPs are not allowed");
                        if ((bytes[0] & 0xFE) == 0xFC) return (false, "Unique-local IPs are not allowed");
                        if (addr.IsIPv4MappedToIPv6)
                        {
                            var v4 = addr.MapToIPv4().GetAddressBytes();
                            if (v4[0] == 10 || v4[0] == 127 ||
                                (v4[0] == 172 && v4[1] >= 16 && v4[1] <= 31) ||
                                (v4[0] == 192 && v4[1] == 168) ||
                                (v4[0] == 169 && v4[1] == 254))
                                return (false, "Mapped private IPs are not allowed");
                        }
                    }
                }
            }
            catch
            {
                return (false, "Could not resolve hostname");
            }
            return (true, null);
        }

        public async Task<PaginatedEndpointResponse> GetEndpointsAsync(string? search = null, string? status = null, int page = 1, int limit = 20)
        {
            var query = _context.ExternalEndpoints.AsNoTracking().Where(e => !e.IsDeleted);

            if (!string.IsNullOrEmpty(search))
            {
                var s = search.ToLower();
                query = query.Where(e => e.Name.ToLower().Contains(s) || e.Slug.ToLower().Contains(s));
            }

            if (status == "active") query = query.Where(e => e.IsActive);
            else if (status == "inactive") query = query.Where(e => !e.IsActive);

            var total = await query.CountAsync();
            var endpoints = await query.OrderByDescending(e => e.CreatedAt).Skip((page - 1) * limit).Take(limit).ToListAsync();

            var today = DateTime.UtcNow.Date;
            var endpointIds = endpoints.Select(e => e.Id).ToList();
            
            var logStats = await _context.ExternalEndpointLogs
                .Where(l => endpointIds.Contains(l.EndpointId))
                .GroupBy(l => l.EndpointId)
                .Select(g => new {
                    EndpointId = g.Key,
                    Total = g.Count(),
                    Today = g.Count(l => l.ReceivedAt >= today),
                    Last = g.Max(l => l.ReceivedAt)
                }).ToListAsync();

            var dtos = endpoints.Select(e =>
            {
                var stats = logStats.FirstOrDefault(s => s.EndpointId == e.Id);
                return MapToDto(e, stats?.Total ?? 0, stats?.Today ?? 0, stats?.Last);
            }).ToList();

            return new PaginatedEndpointResponse
            {
                Endpoints = dtos,
                Pagination = new PaginationInfo { Page = page, Limit = limit, Total = total, TotalPages = (int)Math.Ceiling(total / (double)limit) }
            };
        }

        public async Task<ExternalEndpointDto?> GetEndpointByIdAsync(int id)
        {
            var e = await _context.ExternalEndpoints.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (e == null) return null;

            var today = DateTime.UtcNow.Date;
            var stats = await _context.ExternalEndpointLogs.Where(l => l.EndpointId == id)
                .GroupBy(l => 1)
                .Select(g => new { Total = g.Count(), Today = g.Count(l => l.ReceivedAt >= today), Last = g.Max(l => (DateTime?)l.ReceivedAt) })
                .FirstOrDefaultAsync();

            return MapToDto(e, stats?.Total ?? 0, stats?.Today ?? 0, stats?.Last);
        }

        public async Task<ExternalEndpointDto> CreateEndpointAsync(CreateExternalEndpointDto dto, string userId)
        {
            // SSRF guard
            var (ok, err) = ValidateWebhookUrl(dto.WebhookForwardUrl);
            if (!ok) throw new ArgumentException($"Invalid webhook URL: {err}");

            var slug = string.IsNullOrEmpty(dto.Slug) ? GenerateSlug(dto.Name) : dto.Slug;

            // Slug must be unique GLOBALLY (the receive endpoint is public and
            // resolves a slug across all tenants, then disambiguates by API key).
            // IgnoreQueryFilters() so the tenant filter doesn't hide collisions
            // from other tenants — that would let two tenants reserve the same
            // slug and then trip a DB unique-index violation on insert.
            var exists = await _context.ExternalEndpoints
                .IgnoreQueryFilters()
                .AnyAsync(e => e.Slug == slug && !e.IsDeleted);
            if (exists) slug = slug + "-" + Guid.NewGuid().ToString("N")[..4];

            var plainKey = GenerateApiKey();
            var entity = new ExternalEndpoint
            {
                Name = dto.Name,
                Slug = slug,
                Description = dto.Description,
                ApiKey = HashApiKey(plainKey),
                IsActive = dto.IsActive,
                AllowedMethods = dto.AllowedMethods,
                AllowedOrigins = dto.AllowedOrigins,
                ExpectedSchema = dto.ExpectedSchema,
                ResponseTemplate = dto.ResponseTemplate,
                WebhookForwardUrl = dto.WebhookForwardUrl,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ExternalEndpoints.Add(entity);
            await _context.SaveChangesAsync();
            // Reveal the plain key ONCE on creation. After this it is unrecoverable.
            var dtoOut = MapToDto(entity);
            dtoOut.ApiKey = plainKey;
            return dtoOut;
        }

        public async Task<ExternalEndpointDto> UpdateEndpointAsync(int id, UpdateExternalEndpointDto dto, string userId)
        {
            var entity = await _context.ExternalEndpoints.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Endpoint not found");

            if (dto.WebhookForwardUrl != null)
            {
                var (ok, err) = ValidateWebhookUrl(dto.WebhookForwardUrl);
                if (!ok) throw new ArgumentException($"Invalid webhook URL: {err}");
            }

            if (dto.Name != null) entity.Name = dto.Name;
            if (dto.Description != null) entity.Description = dto.Description;
            if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
            if (dto.AllowedMethods != null) entity.AllowedMethods = dto.AllowedMethods;
            if (dto.AllowedOrigins != null) entity.AllowedOrigins = dto.AllowedOrigins;
            if (dto.ExpectedSchema != null) entity.ExpectedSchema = dto.ExpectedSchema;
            if (dto.ResponseTemplate != null) entity.ResponseTemplate = dto.ResponseTemplate;
            if (dto.WebhookForwardUrl != null) entity.WebhookForwardUrl = dto.WebhookForwardUrl;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (await GetEndpointByIdAsync(id))!;
        }

        public async Task<bool> DeleteEndpointAsync(int id, string userId)
        {
            var entity = await _context.ExternalEndpoints.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
            if (entity == null) return false;
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = userId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ExternalEndpointDto> RegenerateKeyAsync(int id, string userId)
        {
            var entity = await _context.ExternalEndpoints.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Endpoint not found");
            var plainKey = GenerateApiKey();
            entity.ApiKey = HashApiKey(plainKey);
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            var stats = await GetEndpointByIdAsync(id) ?? MapToDto(entity);
            stats.ApiKey = plainKey;
            return stats;
        }

        // Keys are hashed at rest. For legacy plaintext rows we still return the value
        // (one-time migration aid). New keys must be regenerated to be revealed.
        public async Task<string> RevealKeyAsync(int id)
        {
            var entity = await _context.ExternalEndpoints.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Endpoint not found");
            if (!entity.ApiKey.StartsWith(HashPrefix)) return entity.ApiKey;
            throw new InvalidOperationException("API key is hashed and cannot be revealed. Regenerate to obtain a new key.");
        }

        public async Task<ExternalEndpointStatsDto> GetStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var endpoints = _context.ExternalEndpoints.Where(e => !e.IsDeleted);
            return new ExternalEndpointStatsDto
            {
                TotalEndpoints = await endpoints.CountAsync(),
                ActiveEndpoints = await endpoints.CountAsync(e => e.IsActive),
                TotalReceivedToday = await _context.ExternalEndpointLogs.CountAsync(l => l.ReceivedAt >= today),
                TotalReceivedAll = await _context.ExternalEndpointLogs.CountAsync()
            };
        }

        // Logs
        public async Task<PaginatedLogResponse> GetLogsAsync(int endpointId, int page = 1, int limit = 20)
        {
            var query = _context.ExternalEndpointLogs.AsNoTracking().Where(l => l.EndpointId == endpointId);
            var total = await query.CountAsync();
            var logs = await query.OrderByDescending(l => l.ReceivedAt).Skip((page - 1) * limit).Take(limit).ToListAsync();
            return new PaginatedLogResponse
            {
                Logs = logs.Select(l => new ExternalEndpointLogDto
                {
                    Id = l.Id, EndpointId = l.EndpointId, Method = l.Method, Headers = l.Headers,
                    QueryString = l.QueryString, Body = l.Body, SourceIp = l.SourceIp,
                    StatusCode = l.StatusCode, ResponseBody = l.ResponseBody, ReceivedAt = l.ReceivedAt,
                    ProcessedAt = l.ProcessedAt, IsRead = l.IsRead
                }).ToList(),
                Pagination = new PaginationInfo { Page = page, Limit = limit, Total = total, TotalPages = (int)Math.Ceiling(total / (double)limit) }
            };
        }

        public async Task<ExternalEndpointLogDto?> GetLogByIdAsync(int endpointId, int logId)
        {
            var l = await _context.ExternalEndpointLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == logId && x.EndpointId == endpointId);
            if (l == null) return null;
            return new ExternalEndpointLogDto
            {
                Id = l.Id, EndpointId = l.EndpointId, Method = l.Method, Headers = l.Headers,
                QueryString = l.QueryString, Body = l.Body, SourceIp = l.SourceIp,
                StatusCode = l.StatusCode, ResponseBody = l.ResponseBody, ReceivedAt = l.ReceivedAt,
                ProcessedAt = l.ProcessedAt, IsRead = l.IsRead
            };
        }

        public async Task<bool> DeleteLogAsync(int endpointId, int logId)
        {
            var log = await _context.ExternalEndpointLogs.FirstOrDefaultAsync(l => l.Id == logId && l.EndpointId == endpointId);
            if (log == null) return false;
            _context.ExternalEndpointLogs.Remove(log);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClearLogsAsync(int endpointId)
        {
            var logs = await _context.ExternalEndpointLogs.Where(l => l.EndpointId == endpointId).ToListAsync();
            _context.ExternalEndpointLogs.RemoveRange(logs);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkLogAsReadAsync(int endpointId, int logId)
        {
            var log = await _context.ExternalEndpointLogs.FirstOrDefaultAsync(l => l.Id == logId && l.EndpointId == endpointId);
            if (log == null) return false;
            log.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        // Public receive — bypasses tenant filter by using raw slug lookup.
        // Slug uniqueness is per-tenant, so multiple tenants can share a slug.
        // We disambiguate by API key: only the endpoint whose ApiKey matches wins.
        public async Task<(int statusCode, string responseBody)> ReceiveAsync(string slug, string method, string? headers, string? queryString, string? body, string? sourceIp, string? apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return (401, "{\"error\":\"API key required\"}");

            var candidates = await _context.ExternalEndpoints
                .IgnoreQueryFilters()
                .Where(e => e.Slug == slug && !e.IsDeleted)
                .ToListAsync();

            if (candidates.Count == 0) return (404, "{\"error\":\"Endpoint not found\"}");

            // Pick the endpoint whose API key matches. Constant-time compare against
            // the (possibly hashed) stored value to prevent timing oracles. Iterate
            // over ALL candidates regardless of early hit so total work is uniform.
            ExternalEndpoint? endpoint = null;
            foreach (var c in candidates)
            {
                if (VerifyApiKey(c.ApiKey, apiKey)) endpoint = c;
            }
            if (endpoint == null) return (401, "{\"error\":\"Invalid API key\"}");
            if (!endpoint.IsActive) return (403, "{\"error\":\"Endpoint is inactive\"}");

            // Validate method
            var allowedMethods = endpoint.AllowedMethods.Split(',').Select(m => m.Trim().ToUpper()).ToList();
            if (!allowedMethods.Contains(method.ToUpper()))
                return (405, "{\"error\":\"Method not allowed\"}");

            // Store the log
            var log = new ExternalEndpointLog
            {
                TenantId = endpoint.TenantId,
                EndpointId = endpoint.Id,
                Method = method.ToUpper(),
                Headers = headers,
                QueryString = queryString,
                Body = body,
                SourceIp = sourceIp,
                StatusCode = 200,
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            var responseBody = endpoint.ResponseTemplate ?? "{\"success\":true,\"message\":\"Data received\"}";
            log.ResponseBody = responseBody;

            _context.ExternalEndpointLogs.Add(log);
            await _context.SaveChangesAsync();

            // Optional webhook forwarding — enqueue a durable job and signal the
            // background worker. The worker handles retries, backoff, and the
            // dead-letter state. This survives process restarts because the
            // WebhookForwardJobs table is the source of truth.
            if (!string.IsNullOrEmpty(endpoint.WebhookForwardUrl))
            {
                var (ok, _) = ValidateWebhookUrl(endpoint.WebhookForwardUrl);
                if (ok)
                {
                    var job = new WebhookForwardJob
                    {
                        TenantId = endpoint.TenantId,
                        EndpointId = endpoint.Id,
                        LogId = log.Id,
                        ForwardUrl = endpoint.WebhookForwardUrl!,
                        Body = body,
                        Status = "pending",
                        NextAttemptAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                    };
                    _context.WebhookForwardJobs.Add(job);
                    await _context.SaveChangesAsync();

                    // Best-effort wake-up: missing the signal is harmless because
                    // the worker also polls for due jobs on a timer.
                    if (_forwardQueue != null)
                    {
                        try { await _forwardQueue.EnqueueAsync(job.Id); }
                        catch (Exception ex) { _logger.LogDebug(ex, "Could not signal worker for job {JobId} (poll will pick it up)", job.Id); }
                    }
                }
                else
                {
                    _logger.LogWarning("Skipped webhook forward for endpoint {Slug}: URL failed SSRF validation", slug);
                }
            }

            return (200, responseBody);
        }
    }
}
