using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Modules.ExternalEndpoints.DTOs;
using MyApi.Modules.ExternalEndpoints.Models;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MyApi.Modules.ExternalEndpoints.Services
{
    public class ExternalEndpointService : IExternalEndpointService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExternalEndpointService> _logger;

        public ExternalEndpointService(ApplicationDbContext context, ILogger<ExternalEndpointService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private static string GenerateApiKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return "ext_" + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..40];
        }

        private static string GenerateSlug(string name)
        {
            var slug = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
            return slug + "-" + Guid.NewGuid().ToString("N")[..6];
        }

        private ExternalEndpointDto MapToDto(ExternalEndpoint e, int totalReceived = 0, int receivedToday = 0, DateTime? lastReceived = null)
        {
            return new ExternalEndpointDto
            {
                Id = e.Id,
                Name = e.Name,
                Slug = e.Slug,
                Description = e.Description,
                ApiKey = e.ApiKey,
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
            var slug = string.IsNullOrEmpty(dto.Slug) ? GenerateSlug(dto.Name) : dto.Slug;
            
            // Ensure slug is unique within tenant
            var exists = await _context.ExternalEndpoints.AnyAsync(e => e.Slug == slug && !e.IsDeleted);
            if (exists) slug = slug + "-" + Guid.NewGuid().ToString("N")[..4];

            var entity = new ExternalEndpoint
            {
                Name = dto.Name,
                Slug = slug,
                Description = dto.Description,
                ApiKey = GenerateApiKey(),
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
            return MapToDto(entity);
        }

        public async Task<ExternalEndpointDto> UpdateEndpointAsync(int id, UpdateExternalEndpointDto dto, string userId)
        {
            var entity = await _context.ExternalEndpoints.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Endpoint not found");

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
            entity.ApiKey = GenerateApiKey();
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (await GetEndpointByIdAsync(id))!;
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

        // Public receive — bypasses tenant filter by using raw slug lookup
        public async Task<(int statusCode, string responseBody)> ReceiveAsync(string slug, string method, string? headers, string? queryString, string? body, string? sourceIp, string? apiKey)
        {
            // Must query without tenant filter — use a direct query
            var endpoint = await _context.ExternalEndpoints
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Slug == slug && !e.IsDeleted);

            if (endpoint == null) return (404, "{\"error\":\"Endpoint not found\"}");
            if (!endpoint.IsActive) return (403, "{\"error\":\"Endpoint is inactive\"}");

            // Validate API key
            if (string.IsNullOrEmpty(apiKey) || apiKey != endpoint.ApiKey)
                return (401, "{\"error\":\"Invalid API key\"}");

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

            // Optional webhook forwarding
            if (!string.IsNullOrEmpty(endpoint.WebhookForwardUrl))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var client = new HttpClient();
                        var content = new StringContent(body ?? "", System.Text.Encoding.UTF8, "application/json");
                        await client.PostAsync(endpoint.WebhookForwardUrl, content);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to forward webhook for endpoint {Slug}", slug);
                    }
                });
            }

            return (200, responseBody);
        }
    }
}
