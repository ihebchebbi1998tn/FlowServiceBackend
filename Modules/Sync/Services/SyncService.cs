using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Modules.Dispatches.Models;
using MyApi.Modules.Offers.Models;
using MyApi.Modules.Projects.Models;
using MyApi.Modules.Sales.Models;
using MyApi.Modules.ServiceOrders.Models;
using MyApi.Modules.Sync.DTOs;
using MyApi.Modules.Sync.Models;

namespace MyApi.Modules.Sync.Services
{
    public class SyncService : ISyncService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SyncService> _logger;

        public SyncService(ApplicationDbContext context, ILogger<SyncService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SyncPushResponseDto> PushAsync(SyncPushRequestDto request, string currentUser)
        {
            var response = new SyncPushResponseDto();
            foreach (var op in request.Operations)
            {
                var result = new SyncPushResultDto { OpId = op.OpId };
                var existing = await _context.Set<SyncOperationReceipt>()
                    .FirstOrDefaultAsync(r => r.DeviceId == request.DeviceId && r.OpId == op.OpId);
                if (existing != null)
                {
                    result.Status = "duplicate";
                    result.ServerEntityId = existing.ServerEntityId;
                    response.Results.Add(result);
                    continue;
                }

                try
                {
                    result.ServerEntityId = await ApplyOperationAsync(op, currentUser);
                    result.Status = "applied";
                    _context.Set<SyncOperationReceipt>().Add(new SyncOperationReceipt
                    {
                        DeviceId = request.DeviceId,
                        OpId = op.OpId,
                        Status = result.Status,
                        ServerEntityId = result.ServerEntityId,
                        ResponseJson = JsonSerializer.Serialize(result),
                        OperationJson = JsonSerializer.Serialize(op)
                    });
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to apply sync operation {OpId}", op.OpId);
                    result.Status = "rejected";
                    result.Error = ex.Message;
                    _context.Set<SyncOperationReceipt>().Add(new SyncOperationReceipt
                    {
                        DeviceId = request.DeviceId,
                        OpId = op.OpId,
                        Status = result.Status,
                        ResponseJson = JsonSerializer.Serialize(result),
                        OperationJson = JsonSerializer.Serialize(op)
                    });
                    await _context.SaveChangesAsync();
                }

                response.Results.Add(result);
            }

            return response;
        }

        public async Task<SyncPullResponseDto> PullAsync(string? cursor, int limit)
        {
            DateTime since = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                DateTime.TryParse(cursor, null, DateTimeStyles.RoundtripKind, out since);
            }

            var rows = await _context.Set<SyncChange>()
                .AsNoTracking()
                .Where(c => c.ChangedAt > since)
                .OrderBy(c => c.ChangedAt)
                .Take(Math.Max(1, Math.Min(limit, 500)))
                .ToListAsync();

            var dto = new SyncPullResponseDto
            {
                Changes = rows.Select(r => new SyncChangeDto
                {
                    Id = r.Id,
                    EntityType = r.EntityType,
                    EntityId = r.EntityId,
                    Operation = r.Operation,
                    DataJson = r.DataJson,
                    ChangedAt = r.ChangedAt
                }).ToList(),
                NextCursor = rows.LastOrDefault()?.ChangedAt.ToString("O"),
                HasMore = rows.Count >= limit
            };

            return dto;
        }

        public async Task<SyncHistoryResponseDto> GetHistoryAsync(SyncHistoryQueryDto query)
        {
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 25 : Math.Min(query.PageSize, 200);

            var receipts = _context.Set<SyncOperationReceipt>().AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var status = query.Status.Trim().ToLowerInvariant();
                receipts = receipts.Where(r => r.Status.ToLower() == status);
            }
            if (!string.IsNullOrWhiteSpace(query.DeviceId))
            {
                receipts = receipts.Where(r => r.DeviceId == query.DeviceId);
            }
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLowerInvariant();
                receipts = receipts.Where(r => r.OpId.ToLower().Contains(search) || r.DeviceId.ToLower().Contains(search));
            }

            var total = await receipts.CountAsync();
            var rows = await receipts
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = rows.Select(r =>
            {
                SyncOperationDto? op = null;
                SyncPushResultDto? result = null;
                try { if (!string.IsNullOrWhiteSpace(r.OperationJson)) op = JsonSerializer.Deserialize<SyncOperationDto>(r.OperationJson); } catch { }
                try { if (!string.IsNullOrWhiteSpace(r.ResponseJson)) result = JsonSerializer.Deserialize<SyncPushResultDto>(r.ResponseJson); } catch { }
                return new SyncHistoryItemDto
                {
                    OpId = r.OpId,
                    DeviceId = r.DeviceId,
                    Status = r.Status,
                    ServerEntityId = r.ServerEntityId,
                    EntityType = op?.EntityType,
                    Operation = op?.Operation,
                    Endpoint = op?.Endpoint,
                    CreatedAt = r.CreatedAt,
                    Error = result?.Error,
                    CanRetry = r.Status == "rejected" && !string.IsNullOrWhiteSpace(r.OperationJson)
                };
            }).ToList();

            return new SyncHistoryResponseDto
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<SyncPushResultDto> RetryAsync(SyncRetryRequestDto request, string currentUser)
        {
            var receipt = await _context.Set<SyncOperationReceipt>()
                .FirstOrDefaultAsync(r => r.DeviceId == request.DeviceId && r.OpId == request.OpId);
            if (receipt == null) throw new KeyNotFoundException("Sync receipt not found");
            if (string.IsNullOrWhiteSpace(receipt.OperationJson))
                throw new InvalidOperationException("Operation payload is not available for retry");

            var original = JsonSerializer.Deserialize<SyncOperationDto>(receipt.OperationJson);
            if (original == null) throw new InvalidOperationException("Invalid operation payload");

            original.OpId = $"{original.OpId}-retry-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            original.ClientTimestamp = DateTime.UtcNow;
            original.DeviceId = request.DeviceId;

            var push = await PushAsync(new SyncPushRequestDto
            {
                DeviceId = request.DeviceId,
                Operations = new System.Collections.Generic.List<SyncOperationDto> { original }
            }, currentUser);

            return push.Results.FirstOrDefault() ?? new SyncPushResultDto
            {
                OpId = original.OpId,
                Status = "rejected",
                Error = "Retry failed"
            };
        }

        private async Task<int?> ApplyOperationAsync(SyncOperationDto op, string currentUser)
        {
            var entityType = NormalizeEntityType(op.EntityType, op.Endpoint);
            var operation = op.Operation?.ToLowerInvariant() ?? "upsert";
            return entityType switch
            {
                "project" => await ApplyProjectAsync(op, operation, currentUser),
                "task" => await ApplyTaskAsync(op, operation, currentUser),
                "offer" => await ApplyOfferAsync(op, operation, currentUser),
                "sale" => await ApplySaleAsync(op, operation, currentUser),
                "service_order" => await ApplyServiceOrderAsync(op, operation, currentUser),
                "dispatch" => await ApplyDispatchAsync(op, operation, currentUser),
                _ => throw new InvalidOperationException($"Unsupported entityType '{entityType}'")
            };
        }

        private string NormalizeEntityType(string? entityType, string? endpoint)
        {
            var value = (entityType ?? string.Empty).Trim().ToLowerInvariant();
            if (value.Contains("service")) return "service_order";
            if (value.Contains("dispatch")) return "dispatch";
            if (value.Contains("offer")) return "offer";
            if (value.Contains("sale")) return "sale";
            if (value.Contains("task")) return "task";
            if (value.Contains("project")) return "project";
            var ep = (endpoint ?? string.Empty).ToLowerInvariant();
            if (ep.Contains("project-task") || ep.Contains("/tasks")) return "task";
            if (ep.Contains("projects")) return "project";
            return value;
        }

        private async Task RecordChangeAsync(string entityType, int entityId, string operation, object payload, string currentUser)
        {
            _context.Set<SyncChange>().Add(new SyncChange
            {
                EntityType = entityType,
                EntityId = entityId,
                Operation = operation,
                DataJson = JsonSerializer.Serialize(payload),
                ChangedBy = currentUser
            });
            await _context.SaveChangesAsync();
        }

        private static string? ReadString(JsonElement? payload, string key)
        {
            if (payload is null || payload.Value.ValueKind != JsonValueKind.Object) return null;
            if (payload.Value.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String) return val.GetString();
            return null;
        }

        private static int? ReadInt(JsonElement? payload, string key)
        {
            if (payload is null || payload.Value.ValueKind != JsonValueKind.Object) return null;
            if (payload.Value.TryGetProperty(key, out var val))
            {
                if (val.ValueKind == JsonValueKind.Number && val.TryGetInt32(out var i)) return i;
                if (val.ValueKind == JsonValueKind.String && int.TryParse(val.GetString(), out var s)) return s;
            }
            return null;
        }

        private async Task<int?> ApplyProjectAsync(SyncOperationDto op, string operation, string user)
        {
            if (operation == "delete" && op.EntityId.HasValue)
            {
                var existing = await _context.Projects.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
                if (existing != null) _context.Projects.Remove(existing);
                await _context.SaveChangesAsync();
                await RecordChangeAsync("project", op.EntityId.Value, "delete", new { id = op.EntityId }, user);
                return op.EntityId;
            }

            Project project;
            if (op.EntityId.HasValue)
            {
                project = await _context.Projects.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value) ?? new Project { CreatedBy = user };
            }
            else
            {
                project = new Project { CreatedBy = user };
            }

            project.Name = ReadString(op.Payload, "name") ?? project.Name ?? "Offline Project";
            project.Description = ReadString(op.Payload, "description") ?? project.Description;
            project.Status = ReadString(op.Payload, "status") ?? project.Status;
            project.ModifiedBy = user;
            project.ModifiedDate = DateTime.UtcNow;

            if (project.Id == 0) _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            await RecordChangeAsync("project", project.Id, "upsert", project, user);
            return project.Id;
        }

        private async Task<int?> ApplyTaskAsync(SyncOperationDto op, string operation, string user)
        {
            if (operation == "delete" && op.EntityId.HasValue)
            {
                var existing = await _context.ProjectTasks.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
                if (existing != null) _context.ProjectTasks.Remove(existing);
                await _context.SaveChangesAsync();
                await RecordChangeAsync("task", op.EntityId.Value, "delete", new { id = op.EntityId }, user);
                return op.EntityId;
            }

            ProjectTask task;
            if (op.EntityId.HasValue)
            {
                task = await _context.ProjectTasks.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value) ?? new ProjectTask { CreatedBy = user };
            }
            else
            {
                task = new ProjectTask { CreatedBy = user };
            }

            task.Title = ReadString(op.Payload, "title") ?? task.Title ?? "Offline Task";
            task.Description = ReadString(op.Payload, "description") ?? task.Description;
            task.Status = ReadString(op.Payload, "status") ?? task.Status;
            task.TaskType = ReadString(op.Payload, "taskType") ?? task.TaskType;
            task.RelatedEntityId = ReadInt(op.Payload, "relatedEntityId") ?? task.RelatedEntityId;
            task.ModifiedBy = user;
            task.ModifiedDate = DateTime.UtcNow;

            if (task.Id == 0) _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();
            await RecordChangeAsync("task", task.Id, "upsert", task, user);
            return task.Id;
        }

        private async Task<int?> ApplyOfferAsync(SyncOperationDto op, string operation, string user)
        {
            if (!op.EntityId.HasValue) return null;
            var offer = await _context.Offers.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
            if (offer == null) return null;
            if (operation == "delete") offer.IsDeleted = true;
            offer.ProjectId = ReadInt(op.Payload, "projectId") ?? offer.ProjectId;
            offer.ModifiedBy = user;
            offer.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await RecordChangeAsync("offer", offer.Id, operation, offer, user);
            return offer.Id;
        }

        private async Task<int?> ApplySaleAsync(SyncOperationDto op, string operation, string user)
        {
            if (!op.EntityId.HasValue) return null;
            var sale = await _context.Sales.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
            if (sale == null) return null;
            if (operation == "delete") sale.IsDeleted = true;
            sale.ProjectId = ReadInt(op.Payload, "projectId") ?? sale.ProjectId;
            sale.ModifiedBy = user;
            sale.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await RecordChangeAsync("sale", sale.Id, operation, sale, user);
            return sale.Id;
        }

        private async Task<int?> ApplyServiceOrderAsync(SyncOperationDto op, string operation, string user)
        {
            if (!op.EntityId.HasValue) return null;
            var order = await _context.ServiceOrders.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
            if (order == null) return null;
            if (operation == "delete") order.IsDeleted = true;
            order.ProjectId = ReadInt(op.Payload, "projectId") ?? order.ProjectId;
            order.ModifiedBy = user;
            order.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await RecordChangeAsync("service_order", order.Id, operation, order, user);
            return order.Id;
        }

        private async Task<int?> ApplyDispatchAsync(SyncOperationDto op, string operation, string user)
        {
            if (!op.EntityId.HasValue) return null;
            var dispatch = await _context.Dispatches.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
            if (dispatch == null) return null;
            if (operation == "delete") dispatch.IsDeleted = true;
            dispatch.ProjectId = ReadInt(op.Payload, "projectId") ?? dispatch.ProjectId;
            dispatch.ModifiedBy = user;
            dispatch.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await RecordChangeAsync("dispatch", dispatch.Id, operation, dispatch, user);
            return dispatch.Id;
        }
    }
}
