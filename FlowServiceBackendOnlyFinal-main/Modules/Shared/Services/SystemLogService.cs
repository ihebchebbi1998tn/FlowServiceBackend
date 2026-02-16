using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Modules.Shared.DTOs;
using MyApi.Modules.Shared.Models;
using System.Text.Json;

namespace MyApi.Modules.Shared.Services
{
    public class SystemLogService : ISystemLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemLogService> _logger;

        public SystemLogService(ApplicationDbContext context, ILogger<SystemLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private DbSet<SystemLog> SystemLogs => _context.Set<SystemLog>();

        public async Task<SystemLogListResponseDto> GetLogsAsync(SystemLogSearchRequestDto? searchRequest = null)
        {
            var query = SystemLogs.AsNoTracking().AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchRequest?.SearchTerm))
            {
                var term = searchRequest.SearchTerm.ToLower();
                query = query.Where(l => 
                    l.Message.ToLower().Contains(term) ||
                    l.Module.ToLower().Contains(term) ||
                    (l.Details != null && l.Details.ToLower().Contains(term)) ||
                    (l.UserName != null && l.UserName.ToLower().Contains(term)) ||
                    (l.EntityType != null && l.EntityType.ToLower().Contains(term))
                );
            }

            if (!string.IsNullOrEmpty(searchRequest?.Level) && searchRequest.Level != "all")
            {
                query = query.Where(l => l.Level == searchRequest.Level);
            }

            if (!string.IsNullOrEmpty(searchRequest?.Module))
            {
                query = query.Where(l => l.Module == searchRequest.Module);
            }

            if (!string.IsNullOrEmpty(searchRequest?.Action))
            {
                query = query.Where(l => l.Action == searchRequest.Action);
            }

            if (!string.IsNullOrEmpty(searchRequest?.UserId))
            {
                query = query.Where(l => l.UserId == searchRequest.UserId);
            }

            if (searchRequest?.StartDate.HasValue == true)
            {
                query = query.Where(l => l.Timestamp >= searchRequest.StartDate.Value);
            }

            if (searchRequest?.EndDate.HasValue == true)
            {
                query = query.Where(l => l.Timestamp <= searchRequest.EndDate.Value);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Pagination
            var pageNumber = searchRequest?.PageNumber ?? 1;
            var pageSize = searchRequest?.PageSize ?? 50;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(l => MapToDto(l))
                .ToListAsync();

            return new SystemLogListResponseDto
            {
                Logs = logs,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }

        public async Task<SystemLogDto?> GetLogByIdAsync(int id)
        {
            var log = await SystemLogs.FindAsync(id);
            return log != null ? MapToDto(log) : null;
        }

        public async Task<SystemLogDto> CreateLogAsync(CreateSystemLogRequestDto createDto, string? ipAddress = null)
        {
            var log = new SystemLog
            {
                Timestamp = DateTime.UtcNow,
                Level = ValidateLevel(createDto.Level),
                Message = createDto.Message,
                Module = createDto.Module,
                Action = ValidateAction(createDto.Action),
                UserId = createDto.UserId,
                UserName = createDto.UserName,
                EntityType = createDto.EntityType,
                EntityId = createDto.EntityId,
                Details = createDto.Details,
                IpAddress = ipAddress ?? createDto.IpAddress,
                UserAgent = createDto.UserAgent,
                Metadata = createDto.Metadata != null ? JsonSerializer.Serialize(createDto.Metadata) : null
            };

            SystemLogs.Add(log);
            await _context.SaveChangesAsync();

            return MapToDto(log);
        }

        public async Task<SystemLogStatisticsDto> GetStatisticsAsync()
        {
            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);
            var last7Days = now.AddDays(-7);

            var stats = await SystemLogs
                .AsNoTracking()
                .Where(l => l.Timestamp >= last7Days)
                .GroupBy(l => 1)
                .Select(g => new SystemLogStatisticsDto
                {
                    TotalLogs = g.Count(),
                    InfoCount = g.Count(l => l.Level == "info"),
                    WarningCount = g.Count(l => l.Level == "warning"),
                    ErrorCount = g.Count(l => l.Level == "error"),
                    SuccessCount = g.Count(l => l.Level == "success"),
                    Last24Hours = g.Count(l => l.Timestamp >= last24Hours),
                    Last7Days = g.Count()
                })
                .FirstOrDefaultAsync();

            return stats ?? new SystemLogStatisticsDto();
        }

        public async Task<List<string>> GetModulesAsync()
        {
            return await SystemLogs
                .AsNoTracking()
                .Select(l => l.Module)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
        }

        public async Task<CleanupResultDto> CleanupOldLogsAsync(int daysOld = 7)
        {
            List<SystemLog> logsToDelete;
            
            if (daysOld == 0)
            {
                // Delete ALL logs
                logsToDelete = await SystemLogs.ToListAsync();
            }
            else
            {
                // Delete logs older than specified days
                var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
                logsToDelete = await SystemLogs
                    .Where(l => l.Timestamp < cutoffDate)
                    .ToListAsync();
            }

            var count = logsToDelete.Count;
            
            if (count > 0)
            {
                SystemLogs.RemoveRange(logsToDelete);
                await _context.SaveChangesAsync();
            }

            var message = daysOld == 0 
                ? $"Successfully deleted all {count} logs"
                : $"Successfully deleted {count} logs older than {daysOld} days";

            _logger.LogInformation("Cleaned up {Count} system logs (daysOld={Days})", count, daysOld);

            return new CleanupResultDto
            {
                DeletedCount = count,
                Message = message
            };
        }

        // Quick logging methods
        public Task LogInfoAsync(string message, string module, string action = "other", string? userId = null, string? userName = null, string? entityType = null, string? entityId = null, string? details = null)
            => CreateQuickLogAsync("info", message, module, action, userId, userName, entityType, entityId, details);

        public Task LogWarningAsync(string message, string module, string action = "other", string? userId = null, string? userName = null, string? entityType = null, string? entityId = null, string? details = null)
            => CreateQuickLogAsync("warning", message, module, action, userId, userName, entityType, entityId, details);

        public Task LogErrorAsync(string message, string module, string action = "other", string? userId = null, string? userName = null, string? entityType = null, string? entityId = null, string? details = null)
            => CreateQuickLogAsync("error", message, module, action, userId, userName, entityType, entityId, details);

        public Task LogSuccessAsync(string message, string module, string action = "other", string? userId = null, string? userName = null, string? entityType = null, string? entityId = null, string? details = null)
            => CreateQuickLogAsync("success", message, module, action, userId, userName, entityType, entityId, details);

        private async Task CreateQuickLogAsync(string level, string message, string module, string action, string? userId, string? userName, string? entityType, string? entityId, string? details)
        {
            try
            {
                var log = new SystemLog
                {
                    Timestamp = DateTime.UtcNow,
                    Level = level,
                    Message = message,
                    Module = module,
                    Action = action,
                    UserId = userId,
                    UserName = userName,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details
                };

                SystemLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Don't throw - logging should never break the main flow
                _logger.LogWarning(ex, "Failed to create system log entry");
            }
        }

        private static SystemLogDto MapToDto(SystemLog log)
        {
            return new SystemLogDto
            {
                Id = log.Id,
                Timestamp = log.Timestamp,
                Level = log.Level,
                Message = log.Message,
                Module = log.Module,
                Action = log.Action,
                UserId = log.UserId,
                UserName = log.UserName,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Details = log.Details,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                Metadata = !string.IsNullOrEmpty(log.Metadata) 
                    ? JsonSerializer.Deserialize<object>(log.Metadata) 
                    : null
            };
        }

        private static string ValidateLevel(string level)
        {
            var validLevels = new[] { "info", "warning", "error", "success" };
            return validLevels.Contains(level.ToLower()) ? level.ToLower() : "info";
        }

        private static string ValidateAction(string action)
        {
            var validActions = new[] { "create", "read", "update", "delete", "login", "logout", "export", "import", "other" };
            return validActions.Contains(action.ToLower()) ? action.ToLower() : "other";
        }
    }
}
