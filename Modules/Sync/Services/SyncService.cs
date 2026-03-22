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
using MyApi.Modules.Contacts.Models;
using MyApi.Modules.Documents.Models;
using MyApi.Modules.DynamicForms.Models;
using MyApi.Modules.Calendar.Models;
using MyApi.Modules.EmailAccounts.Models;
using MyApi.Modules.Installations.Models;
using MyApi.Modules.Articles.Models;
using MyApi.Modules.HR.Models;
using MyApi.Modules.Lookups.Models;
using MyApi.Modules.SupportTickets.Models;
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

        private async Task<int?> ResolveCurrentUserIdAsync(string currentUser)
        {
            if (string.IsNullOrWhiteSpace(currentUser)) return null;
            var normalized = currentUser.Trim().ToLowerInvariant();
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized && !u.IsDeleted);
            return user?.Id;
        }

        private static bool IsTicketOwnedByCurrentUser(SupportTicket ticket, string currentUser)
        {
            if (string.IsNullOrWhiteSpace(currentUser)) return false;
            if (string.IsNullOrWhiteSpace(ticket.UserEmail)) return false;
            return string.Equals(ticket.UserEmail.Trim(), currentUser.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private string? ResolveServerEntityKey(SyncOperationDto op, int? serverEntityId)
        {
            var entityType = NormalizeEntityType(op.EntityType, op.Endpoint);
            if (entityType == "email_account")
            {
                var key = ReadString(op.Payload, "id") ?? ReadString(op.Payload, "Id");
                if (!string.IsNullOrWhiteSpace(key)) return key;
            }
            if (entityType == "synced_email")
            {
                var ids = ParseSyncedEmailIdsFromPayloadOrEndpoint(op.Payload, op.Endpoint);
                if (ids.emailId.HasValue) return ids.emailId.Value.ToString();
            }
            if (entityType == "calendar_event")
            {
                var key = ReadString(op.Payload, "id") ?? ReadString(op.Payload, "Id");
                if (!string.IsNullOrWhiteSpace(key)) return key;
                if (!string.IsNullOrWhiteSpace(op.Endpoint))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(op.Endpoint, @"/events/([a-f0-9-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m.Success) return m.Groups[1].Value;
                }
            }
            return serverEntityId?.ToString();
        }

        public async Task<SyncPushResponseDto> PushAsync(SyncPushRequestDto request, string currentUser)
        {
            var currentUserId = await ResolveCurrentUserIdAsync(currentUser);
            if (!currentUserId.HasValue) throw new UnauthorizedAccessException("Unable to resolve current user.");
            if (request.Operations == null || request.Operations.Count == 0)
                return new SyncPushResponseDto();

            var response = new SyncPushResponseDto();
            var pending = new List<(int idx, SyncOperationDto op)>();
            var groupsWithDuplicateMembers = new HashSet<string>(StringComparer.Ordinal);

            // Failed operations persist a receipt with Status=rejected. On the next sync the client still
            // has the op in its queue and retries. Without this cleanup, the duplicate short-circuit below
            // would return duplicate for *any* existing receipt — including rejected — and the client
            // would drop the op from the queue without ever re-applying it.
            if (request.Operations.Count > 0)
            {
                var opIds = request.Operations.Select(o => o.OpId).Distinct().ToList();
                var rejectedRetry = await _context.Set<SyncOperationReceipt>()
                    .Where(r => r.DeviceId == request.DeviceId && opIds.Contains(r.OpId) && r.Status == "rejected")
                    .ToListAsync();
                if (rejectedRetry.Count > 0)
                {
                    _context.Set<SyncOperationReceipt>().RemoveRange(rejectedRetry);
                    await _context.SaveChangesAsync();
                }
            }

            for (var i = 0; i < request.Operations.Count; i++)
            {
                var op = request.Operations[i];
                var result = new SyncPushResultDto { OpId = op.OpId };
                if (!string.IsNullOrWhiteSpace(op.DeviceId) && !string.Equals(op.DeviceId, request.DeviceId, StringComparison.Ordinal))
                {
                    result.Status = "rejected";
                    result.Error = "Operation deviceId does not match request deviceId";
                    response.Results.Add(result);
                    continue;
                }
                var existing = await _context.Set<SyncOperationReceipt>()
                    .FirstOrDefaultAsync(r => r.DeviceId == request.DeviceId && r.OpId == op.OpId);
                if (existing != null)
                {
                    if (!string.IsNullOrWhiteSpace(op.TransactionGroupId))
                    {
                        groupsWithDuplicateMembers.Add(op.TransactionGroupId);
                    }
                    result.Status = "duplicate";
                    result.ServerEntityId = existing.ServerEntityId;
                    result.ServerEntityKey = existing.ServerEntityKey;
                    response.Results.Add(result);
                    continue;
                }
                pending.Add((i, op));
            }

            var grouped = pending
                .GroupBy(x => !string.IsNullOrWhiteSpace(x.op.TransactionGroupId) ? $"g:{x.op.TransactionGroupId}" : $"o:{x.op.OpId}")
                .Select(g => new
                {
                    Key = g.Key,
                    FirstIndex = g.Min(x => x.idx),
                    Items = g.OrderBy(x => x.idx).Select(x => x.op).ToList()
                })
                .OrderBy(g => g.FirstIndex)
                .ToList();

            foreach (var g in grouped)
            {
                if (g.Key.StartsWith("g:"))
                {
                    var groupId = g.Key.Substring(2);
                    if (groupsWithDuplicateMembers.Contains(groupId))
                    {
                        foreach (var op in g.Items)
                        {
                            response.Results.Add(new SyncPushResultDto
                            {
                                OpId = op.OpId,
                                Status = "rejected",
                                Error = "Transaction group contains already-applied operations; replay whole group from a fresh queue."
                            });
                        }
                        continue;
                    }
                }
                // Single operation path keeps old behavior.
                if (!g.Key.StartsWith("g:") || g.Items.Count <= 1)
                {
                    var op = g.Items[0];
                    var result = new SyncPushResultDto { OpId = op.OpId };
                    await using var tx = await _context.Database.BeginTransactionAsync();
                    var receipt = new SyncOperationReceipt
                    {
                        DeviceId = request.DeviceId,
                        OpId = op.OpId,
                        CreatedByUser = currentUser,
                        CreatedByUserId = currentUserId,
                        Status = "processing",
                        OperationJson = JsonSerializer.Serialize(op)
                    };
                    _context.Set<SyncOperationReceipt>().Add(receipt);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        await tx.RollbackAsync();
                        _context.Entry(receipt).State = EntityState.Detached;
                        var existingDup = await _context.Set<SyncOperationReceipt>()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(r => r.DeviceId == request.DeviceId && r.OpId == op.OpId);
                        result.Status = "duplicate";
                        result.ServerEntityId = existingDup?.ServerEntityId;
                        response.Results.Add(result);
                        continue;
                    }
                    try
                    {
                        result.ServerEntityId = await ApplyOperationWithConflictStrategyAsync(op, currentUser);
                        result.ServerEntityKey = ResolveServerEntityKey(op, result.ServerEntityId);
                        result.Status = "applied";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to apply sync operation {OpId}", op.OpId);
                        result.Status = "rejected";
                        result.Error = ex.Message;
                    }
                    receipt.Status = result.Status;
                    receipt.ServerEntityId = result.ServerEntityId;
                    receipt.ServerEntityKey = result.ServerEntityKey;
                    receipt.ResponseJson = JsonSerializer.Serialize(result);
                    await _context.SaveChangesAsync();
                    if (result.Status == "applied" || result.Status == "duplicate")
                    {
                        await tx.CommitAsync();
                    }
                    else
                    {
                        await tx.RollbackAsync();
                        _context.ChangeTracker.Clear();
                        _context.Set<SyncOperationReceipt>().Add(new SyncOperationReceipt
                        {
                            DeviceId = request.DeviceId,
                            OpId = op.OpId,
                            CreatedByUser = currentUser,
                            CreatedByUserId = currentUserId,
                            Status = result.Status,
                            ResponseJson = JsonSerializer.Serialize(result),
                            OperationJson = JsonSerializer.Serialize(op)
                        });
                        await _context.SaveChangesAsync();
                    }
                    response.Results.Add(result);
                    continue;
                }

                // Transaction group path: all-or-nothing apply.
                await using var groupTx = await _context.Database.BeginTransactionAsync();
                var groupResults = new List<SyncPushResultDto>();
                var reservedReceipts = new List<SyncOperationReceipt>();
                try
                {
                    foreach (var op in g.Items)
                    {
                        var receipt = new SyncOperationReceipt
                        {
                            DeviceId = request.DeviceId,
                            OpId = op.OpId,
                            CreatedByUser = currentUser,
                            CreatedByUserId = currentUserId,
                            Status = "processing",
                            OperationJson = JsonSerializer.Serialize(op)
                        };
                        _context.Set<SyncOperationReceipt>().Add(receipt);
                        reservedReceipts.Add(receipt);
                    }
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        await groupTx.RollbackAsync();
                        _context.ChangeTracker.Clear();
                        foreach (var op in g.Items)
                        {
                            var existingDup = await _context.Set<SyncOperationReceipt>()
                                .AsNoTracking()
                                .FirstOrDefaultAsync(r => r.DeviceId == request.DeviceId && r.OpId == op.OpId);
                            response.Results.Add(new SyncPushResultDto
                            {
                                OpId = op.OpId,
                                Status = "duplicate",
                                ServerEntityId = existingDup?.ServerEntityId,
                                ServerEntityKey = existingDup?.ServerEntityKey
                            });
                        }
                        continue;
                    }

                    foreach (var op in g.Items)
                    {
                        var r = new SyncPushResultDto
                        {
                            OpId = op.OpId,
                            Status = "applied",
                            ServerEntityId = await ApplyOperationWithConflictStrategyAsync(op, currentUser),
                        };
                        r.ServerEntityKey = ResolveServerEntityKey(op, r.ServerEntityId);
                        groupResults.Add(r);
                    }

                    foreach (var (receipt, r) in reservedReceipts.Zip(groupResults, (receipt, r) => (receipt, r)))
                    {
                        receipt.Status = r.Status;
                        receipt.ServerEntityId = r.ServerEntityId;
                        receipt.ServerEntityKey = r.ServerEntityKey;
                        receipt.ResponseJson = JsonSerializer.Serialize(r);
                    }
                    await _context.SaveChangesAsync();
                    await groupTx.CommitAsync();
                    response.Results.AddRange(groupResults);
                }
                catch (Exception ex)
                {
                    await groupTx.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    _logger.LogWarning(ex, "Failed transaction group {GroupKey}", g.Key);
                    foreach (var op in g.Items)
                    {
                        var r = new SyncPushResultDto
                        {
                            OpId = op.OpId,
                            Status = "rejected",
                            Error = $"Transaction group rolled back: {ex.Message}"
                        };
                        _context.Set<SyncOperationReceipt>().Add(new SyncOperationReceipt
                        {
                            DeviceId = request.DeviceId,
                            OpId = op.OpId,
                            CreatedByUser = currentUser,
                            CreatedByUserId = currentUserId,
                            Status = r.Status,
                            ResponseJson = JsonSerializer.Serialize(r),
                            OperationJson = JsonSerializer.Serialize(op)
                        });
                        response.Results.Add(r);
                    }
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        // Duplicate receipt rows may already exist from parallel request.
                        _context.ChangeTracker.Clear();
                    }
                }
            }

            return response;
        }

        private async Task<int?> ApplyOperationWithConflictStrategyAsync(SyncOperationDto op, string currentUser)
        {
            try
            {
                return await ApplyOperationAsync(op, currentUser);
            }
            catch (Exception ex)
            {
                var strategy = (op.ConflictStrategy ?? "reject").Trim().ToLowerInvariant();
                if (strategy == "last_write_wins")
                {
                    if (ex is KeyNotFoundException || ex is DbUpdateConcurrencyException)
                    {
                        return op.EntityId;
                    }
                }
                if (strategy == "merge" && !string.Equals(op.Operation, "delete", StringComparison.OrdinalIgnoreCase))
                {
                    var retry = new SyncOperationDto
                    {
                        OpId = op.OpId,
                        DeviceId = op.DeviceId,
                        EntityType = op.EntityType,
                        Operation = "upsert",
                        EntityId = op.EntityId,
                        ClientTempId = op.ClientTempId,
                        Payload = op.Payload,
                        ClientTimestamp = op.ClientTimestamp,
                        Method = op.Method,
                        Endpoint = op.Endpoint,
                        TransactionGroupId = op.TransactionGroupId,
                        ConflictStrategy = op.ConflictStrategy
                    };
                    return await ApplyOperationAsync(retry, currentUser);
                }
                throw;
            }
        }

        public async Task<SyncPullResponseDto> PullAsync(string? cursor, int limit, string currentUser, bool isAdmin)
        {
            var currentUserId = await ResolveCurrentUserIdAsync(currentUser);
            var effectiveLimit = Math.Max(1, Math.Min(limit, 500));
            DateTime since = DateTime.MinValue;
            long sinceId = 0;
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                if (cursor.Contains("|"))
                {
                    var parts = cursor.Split('|', 2);
                    if (!DateTime.TryParse(parts[0], null, DateTimeStyles.RoundtripKind, out since) || !long.TryParse(parts[1], out sinceId))
                        throw new ArgumentException("Invalid sync cursor format");
                }
                else
                {
                    if (!DateTime.TryParse(cursor, null, DateTimeStyles.RoundtripKind, out since))
                        throw new ArgumentException("Invalid sync cursor format");
                }
            }

            var changes = _context.Set<SyncChange>()
                .AsNoTracking()
                .Where(c => c.ChangedAt > since || (c.ChangedAt == since && c.Id > sinceId));

            if (!isAdmin)
            {
                if (!currentUserId.HasValue) throw new UnauthorizedAccessException("Unable to resolve current user.");
                var uid = currentUserId.Value;
                changes = changes.Where(c => c.ChangedByUserId == uid || (c.ChangedByUserId == null && c.ChangedBy == currentUser));
            }

            var rows = await changes
                .OrderBy(c => c.ChangedAt).ThenBy(c => c.Id)
                .Take(effectiveLimit)
                .ToListAsync();

            var dto = new SyncPullResponseDto
            {
                Changes = rows.Select(r => new SyncChangeDto
                {
                    Id = r.Id,
                    EntityType = r.EntityType,
                    EntityId = r.EntityId,
                    EntityKey = r.EntityKey,
                    Operation = r.Operation,
                    DataJson = r.DataJson,
                    ChangedAt = r.ChangedAt
                }).ToList(),
                NextCursor = rows.LastOrDefault() is SyncChange last
                    ? $"{last.ChangedAt:O}|{last.Id}"
                    : null,
                HasMore = rows.Count >= effectiveLimit
            };

            return dto;
        }

        public async Task<SyncHistoryResponseDto> GetHistoryAsync(SyncHistoryQueryDto query, string currentUser, bool isAdmin)
        {
            var currentUserId = await ResolveCurrentUserIdAsync(currentUser);
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 25 : Math.Min(query.PageSize, 200);

            var receipts = _context.Set<SyncOperationReceipt>().AsNoTracking().AsQueryable();
            if (!isAdmin)
            {
                if (!currentUserId.HasValue) throw new UnauthorizedAccessException("Unable to resolve current user.");
                var uid = currentUserId.Value;
                receipts = receipts.Where(r => r.CreatedByUserId == uid || (r.CreatedByUserId == null && r.CreatedByUser == currentUser));
            }
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
                    ServerEntityKey = r.ServerEntityKey,
                    EntityType = op?.EntityType,
                    Operation = op?.Operation,
                    Endpoint = op?.Endpoint,
                    Method = op?.Method,
                    TransactionGroupId = op?.TransactionGroupId,
                    ConflictStrategy = op?.ConflictStrategy,
                    CreatedAt = r.CreatedAt,
                    Error = result?.Error,
                    CanRetry = r.Status == "rejected" && !string.IsNullOrWhiteSpace(r.OperationJson),
                    OperationJson = (isAdmin && query.IncludePayloads) ? r.OperationJson : null,
                    ResponseJson = (isAdmin && query.IncludePayloads) ? r.ResponseJson : null
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

        public async Task<SyncPushResultDto> RetryAsync(SyncRetryRequestDto request, string currentUser, bool isAdmin)
        {
            var receipt = await _context.Set<SyncOperationReceipt>()
                .FirstOrDefaultAsync(r => r.DeviceId == request.DeviceId && r.OpId == request.OpId);
            if (receipt == null) throw new KeyNotFoundException("Sync receipt not found");
            if (!isAdmin && !string.Equals(receipt.CreatedByUser, currentUser, StringComparison.Ordinal))
                throw new KeyNotFoundException("Sync receipt not found");
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
                "contact" => await ApplyContactAsync(op, operation, currentUser),
                "installation" => await ApplyInstallationAsync(op, operation, currentUser),
                "article" => await ApplyArticleAsync(op, operation, currentUser),
                "stock_transaction" => await ApplyStockTransactionAsync(op, operation, currentUser),
                "hr_department" => await ApplyHrDepartmentAsync(op, operation, currentUser),
                "hr_attendance" => await ApplyHrAttendanceAsync(op, operation, currentUser),
                "document" => await ApplyDocumentAsync(op, operation, currentUser),
                "lookup_item" => await ApplyLookupItemAsync(op, operation, currentUser),
                "lookup_bulk" => await ApplyLookupBulkAsync(op, operation, currentUser),
                "currency" => await ApplyCurrencyAsync(op, operation, currentUser),
                "dynamic_form" => await ApplyDynamicFormAsync(op, operation, currentUser),
                "dynamic_form_response" => await ApplyDynamicFormResponseAsync(op, operation, currentUser),
                "calendar_event" => await ApplyCalendarEventAsync(op, operation, currentUser),
                "email_account" => await ApplyEmailAccountAsync(op, operation, currentUser),
                "synced_email" => await ApplySyncedEmailAsync(op, operation, currentUser),
                "project" => await ApplyProjectAsync(op, operation, currentUser),
                "daily_task" => await ApplyDailyTaskAsync(op, operation, currentUser),
                "task" => await ApplyTaskAsync(op, operation, currentUser),
                "offer" => await ApplyOfferAsync(op, operation, currentUser),
                "sale" => await ApplySaleAsync(op, operation, currentUser),
                "service_order" => await ApplyServiceOrderAsync(op, operation, currentUser),
                "dispatch" => await ApplyDispatchAsync(op, operation, currentUser),
                "support_ticket" => await ApplySupportTicketAsync(op, operation, currentUser),
                "support_ticket_comment" => await ApplySupportTicketCommentAsync(op, operation, currentUser),
                "support_ticket_link" => await ApplySupportTicketLinkAsync(op, operation, currentUser),
                "task_checklist" => await ApplyTaskChecklistAsync(op, operation, currentUser),
                "task_checklist_item" => await ApplyTaskChecklistItemAsync(op, operation, currentUser),
                _ => throw new InvalidOperationException($"Unsupported entityType '{entityType}' for offline sync")
            };
        }

        private string NormalizeEntityType(string? entityType, string? endpoint)
        {
            var value = (entityType ?? string.Empty).Trim().ToLowerInvariant();
            var ep = (endpoint ?? string.Empty).ToLowerInvariant();
            // Prefer specific sync entity types before broad "task"/"project"/etc checks.
            if (value == "support_ticket_comment") return "support_ticket_comment";
            if (value == "support_ticket_link") return "support_ticket_link";
            if (value == "support_ticket") return "support_ticket";
            if (value == "daily_task") return "daily_task";
            if (value == "task_checklist_item") return "task_checklist_item";
            if (value == "task_checklist") return "task_checklist";
            if (value == "lookup_bulk") return "lookup_bulk";
            if (value == "lookup_item") return "lookup_item";
            if (value == "currency") return "currency";
            if (value.Contains("dynamic_form_response") || (ep.Contains("dynamicforms") && ep.Contains("/responses") && !ep.Contains("/responses/count"))) return "dynamic_form_response";
            if (value.Contains("synced_email") || ep.Contains("/emails/")) return "synced_email";
            if (value.Contains("service")) return "service_order";
            if (value.Contains("dispatch")) return "dispatch";
            if (value.Contains("offer")) return "offer";
            if (value.Contains("sale")) return "sale";
            if (value.Contains("task")) return "task";
            if (value.Contains("project")) return "project";
            if (value.Contains("contact")) return "contact";
            if (value.Contains("installation")) return "installation";
            if (value.Contains("article")) return "article";
            if (value.Contains("stock_transaction")) return "stock_transaction";
            if (value.Contains("hr_department")) return "hr_department";
            if (value.Contains("hr_attendance")) return "hr_attendance";
            if (value.Contains("document")) return "document";
            if (value.Contains("dynamic")) return "dynamic_form";
            if (value.Contains("calendar")) return "calendar_event";
            if (value.Contains("email")) return "email_account";
            if (ep.Contains("daily-task") || ep.Contains("tasks/daily")) return "daily_task";
            if (ep.Contains("project-task") || ep.Contains("/tasks")) return "task";
            if (ep.Contains("projects")) return "project";
            if (ep.Contains("contacts")) return "contact";
            if (ep.Contains("installations")) return "installation";
            if (ep.Contains("stock-transactions")) return "stock_transaction";
            if (ep.Contains("articles")) return "article";
            if (ep.Contains("hr/departments")) return "hr_department";
            if (ep.Contains("hr/attendance")) return "hr_attendance";
            if (ep.Contains("documents")) return "document";
            if (ep.Contains("lookups/currencies")) return "currency";
            if (ep.Contains("lookups") && ep.Contains("/bulk")) return "lookup_bulk";
            if (ep.Contains("lookups")) return "lookup_item";
            if (ep.Contains("supporttickets") && ep.Contains("/comments")) return "support_ticket_comment";
            if (ep.Contains("supporttickets") && ep.Contains("/links")) return "support_ticket_link";
            if (ep.Contains("supporttickets")) return "support_ticket";
            if (ep.Contains("taskchecklists") && ep.Contains("/items")) return "task_checklist_item";
            if (ep.Contains("taskchecklists")) return "task_checklist";
            if (ep.Contains("dynamicforms") || ep.Contains("dynamic-forms")) return "dynamic_form";
            if (ep.Contains("calendar")) return "calendar_event";
            if (ep.Contains("email")) return "email_account";
            return value;
        }

        private async Task RecordChangeAsync(string entityType, int entityId, string operation, object payload, string currentUser)
        {
            var currentUserId = await ResolveCurrentUserIdAsync(currentUser);
            await RecordChangeAsync(entityType, entityId, null, operation, payload, currentUser, currentUserId);
        }

        private async Task RecordChangeAsync(string entityType, int entityId, string? entityKey, string operation, object payload, string currentUser, int? currentUserId)
        {
            _context.Set<SyncChange>().Add(new SyncChange
            {
                EntityType = entityType,
                EntityId = entityId,
                EntityKey = entityKey,
                Operation = operation,
                DataJson = JsonSerializer.Serialize(payload),
                ChangedBy = currentUser,
                ChangedByUserId = currentUserId
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

        private static bool? ReadBool(JsonElement? payload, string key)
        {
            if (payload is null || payload.Value.ValueKind != JsonValueKind.Object) return null;
            if (payload.Value.TryGetProperty(key, out var val))
            {
                if (val.ValueKind == JsonValueKind.True) return true;
                if (val.ValueKind == JsonValueKind.False) return false;
                if (val.ValueKind == JsonValueKind.String && bool.TryParse(val.GetString(), out var b)) return b;
            }
            return null;
        }

        private static DateTime? ReadDate(JsonElement? payload, string key)
        {
            if (payload is null || payload.Value.ValueKind != JsonValueKind.Object) return null;
            if (payload.Value.TryGetProperty(key, out var val))
            {
                if (val.ValueKind == JsonValueKind.String && DateTime.TryParse(val.GetString(), out var dt)) return dt;
            }
            return null;
        }

        private static decimal? ReadDecimal(JsonElement? payload, string key)
        {
            if (payload is null || payload.Value.ValueKind != JsonValueKind.Object) return null;
            if (payload.Value.TryGetProperty(key, out var val))
            {
                if (val.ValueKind == JsonValueKind.Number && val.TryGetDecimal(out var d)) return d;
                if (val.ValueKind == JsonValueKind.String && decimal.TryParse(val.GetString(), out var s)) return s;
            }
            return null;
        }

        private static TimeSpan? ReadTimeSpan(JsonElement? payload, string key)
        {
            if (payload is null || payload.Value.ValueKind != JsonValueKind.Object) return null;
            if (payload.Value.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String)
            {
                var s = val.GetString();
                if (!string.IsNullOrEmpty(s) && TimeSpan.TryParse(s, out var ts)) return ts;
            }
            return null;
        }

        private static Guid? ReadGuid(JsonElement? payload, string key)
        {
            if (payload is null || payload.Value.ValueKind != JsonValueKind.Object) return null;
            if (payload.Value.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String && Guid.TryParse(val.GetString(), out var g)) return g;
            return null;
        }

        private static string? ReadJson(JsonElement? payload, string key)
        {
            if (payload is null || payload.Value.ValueKind != JsonValueKind.Object) return null;
            if (payload.Value.TryGetProperty(key, out var val)) return val.GetRawText();
            return null;
        }

        private static int? ParseFormIdFromEndpoint(string? endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(endpoint, @"dynamicforms?/(\d+)/responses", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var id) ? id : null;
        }

        private static (Guid? accountId, Guid? emailId) ParseSyncedEmailIdsFromPayloadOrEndpoint(JsonElement? payload, string? endpoint)
        {
            var accountId = ReadGuid(payload, "accountId");
            var emailId = ReadGuid(payload, "emailId");
            if (accountId.HasValue && emailId.HasValue) return (accountId, emailId);
            var accStr = ReadString(payload, "accountId");
            var emStr = ReadString(payload, "emailId");
            if (!string.IsNullOrEmpty(accStr) && !string.IsNullOrEmpty(emStr) && Guid.TryParse(accStr, out var acc) && Guid.TryParse(emStr, out var em))
                return (acc, em);
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                var m = System.Text.RegularExpressions.Regex.Match(endpoint, @"email-accounts/([a-f0-9-]+)/emails/([a-f0-9-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success && Guid.TryParse(m.Groups[1].Value, out var a) && Guid.TryParse(m.Groups[2].Value, out var e))
                    return (a, e);
            }
            return (null, null);
        }

        private static int? ParseIdFromEndpoint(string? endpoint, string segment)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(endpoint, $@"{segment}/(\d+)(?:/|$|\?)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var id) ? id : null;
        }

        private static Guid? ParseGuidIdFromEndpoint(string? endpoint, string segment)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(endpoint, $@"{segment}/([a-f0-9-]+)(?:/|$|\?)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success) return null;
            return Guid.TryParse(match.Groups[1].Value, out var guid) ? guid : null;
        }

        private static string? ParseLookupTypeFromEndpoint(string? endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(endpoint, @"lookups/([^/?]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static int? ParseLookupItemIdFromEndpoint(string? endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) return null;
            // Matches /api/lookups/{type}/{id}
            var match = System.Text.RegularExpressions.Regex.Match(endpoint, @"lookups/[^/?]+/(\d+)(?:/|$|\?)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var id) ? id : null;
        }

        private async Task<int?> ApplyInstallationAsync(SyncOperationDto op, string operation, string user)
        {
            Installation? inst = null;
            var id = op.EntityId ?? ParseIdFromEndpoint(op.Endpoint, "installations");
            if (id.HasValue) inst = await _context.Installations.FirstOrDefaultAsync(x => x.Id == id.Value);
            if (inst == null && operation != "delete")
            {
                inst = new Installation
                {
                    InstallationNumber = ReadString(op.Payload, "installationNumber") ?? ReadString(op.Payload, "InstallationNumber") ?? $"INST-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    ContactId = ReadInt(op.Payload, "contactId") ?? ReadInt(op.Payload, "ContactId") ?? 0,
                    SiteAddress = ReadString(op.Payload, "siteAddress") ?? ReadString(op.Payload, "SiteAddress") ?? "Offline",
                    InstallationType = ReadString(op.Payload, "installationType") ?? ReadString(op.Payload, "InstallationType") ?? "general",
                    InstallationDate = ReadDate(op.Payload, "installationDate") ?? ReadDate(op.Payload, "InstallationDate") ?? DateTime.UtcNow,
                    Status = ReadString(op.Payload, "status") ?? ReadString(op.Payload, "Status") ?? "active",
                    CreatedBy = user
                };
                _context.Installations.Add(inst);
            }
            if (inst == null) return null;
            if (operation == "delete")
            {
                _context.Installations.Remove(inst);
            }
            else
            {
                inst.Name = ReadString(op.Payload, "name") ?? ReadString(op.Payload, "Name") ?? inst.Name;
                inst.Model = ReadString(op.Payload, "model") ?? ReadString(op.Payload, "Model") ?? inst.Model;
                inst.Manufacturer = ReadString(op.Payload, "manufacturer") ?? ReadString(op.Payload, "Manufacturer") ?? inst.Manufacturer;
                inst.Category = ReadString(op.Payload, "category") ?? ReadString(op.Payload, "Category") ?? inst.Category;
                inst.Type = ReadString(op.Payload, "type") ?? ReadString(op.Payload, "Type") ?? inst.Type;
                inst.SerialNumber = ReadString(op.Payload, "serialNumber") ?? ReadString(op.Payload, "SerialNumber") ?? inst.SerialNumber;
                inst.Matricule = ReadString(op.Payload, "matricule") ?? ReadString(op.Payload, "Matricule") ?? inst.Matricule;
                inst.SiteAddress = ReadString(op.Payload, "siteAddress") ?? ReadString(op.Payload, "SiteAddress") ?? inst.SiteAddress;
                inst.InstallationType = ReadString(op.Payload, "installationType") ?? ReadString(op.Payload, "InstallationType") ?? inst.InstallationType;
                inst.Status = ReadString(op.Payload, "status") ?? ReadString(op.Payload, "Status") ?? inst.Status;
                inst.Notes = ReadString(op.Payload, "notes") ?? ReadString(op.Payload, "Notes") ?? inst.Notes;
                inst.ContactId = ReadInt(op.Payload, "contactId") ?? ReadInt(op.Payload, "ContactId") ?? inst.ContactId;
                inst.WarrantyExpiry = ReadDate(op.Payload, "warrantyExpiry") ?? ReadDate(op.Payload, "WarrantyExpiry") ?? inst.WarrantyExpiry;
                inst.WarrantyFrom = ReadDate(op.Payload, "warrantyFrom") ?? ReadDate(op.Payload, "WarrantyFrom") ?? inst.WarrantyFrom;
                inst.ModifiedBy = user;
                inst.ModifiedDate = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("installation", inst.Id, operation, inst, user);
            return inst.Id;
        }

        private async Task<int?> ApplyArticleAsync(SyncOperationDto op, string operation, string user)
        {
            Article? article = null;
            var id = op.EntityId ?? ParseIdFromEndpoint(op.Endpoint, "articles");
            if (id.HasValue) article = await _context.Articles.FirstOrDefaultAsync(x => x.Id == id.Value && !x.IsDeleted);
            if (article == null && operation != "delete")
            {
                var lastArticle = await _context.Articles.OrderByDescending(a => a.Id).FirstOrDefaultAsync();
                var nextNum = (lastArticle?.Id ?? 0) + 1;
                var articleNumber = ReadString(op.Payload, "articleNumber") ?? ReadString(op.Payload, "ArticleNumber") ?? $"ART-{nextNum:D6}";
                article = new Article
                {
                    ArticleNumber = articleNumber,
                    Name = ReadString(op.Payload, "name") ?? ReadString(op.Payload, "Name") ?? "Offline Article",
                    Description = ReadString(op.Payload, "description") ?? ReadString(op.Payload, "Description"),
                    CategoryId = ReadInt(op.Payload, "categoryId") ?? ReadInt(op.Payload, "CategoryId"),
                    Unit = ReadString(op.Payload, "unit") ?? ReadString(op.Payload, "Unit") ?? "piece",
                    PurchasePrice = ReadDecimal(op.Payload, "purchasePrice") ?? ReadDecimal(op.Payload, "PurchasePrice") ?? 0,
                    SalesPrice = ReadDecimal(op.Payload, "salesPrice") ?? ReadDecimal(op.Payload, "SalesPrice") ?? 0,
                    StockQuantity = ReadDecimal(op.Payload, "stockQuantity") ?? ReadDecimal(op.Payload, "StockQuantity") ?? 0,
                    MinStockLevel = ReadDecimal(op.Payload, "minStockLevel") ?? ReadDecimal(op.Payload, "MinStockLevel"),
                    LocationId = ReadInt(op.Payload, "locationId") ?? ReadInt(op.Payload, "LocationId"),
                    GroupId = ReadInt(op.Payload, "groupId") ?? ReadInt(op.Payload, "GroupId"),
                    Supplier = ReadString(op.Payload, "supplier") ?? ReadString(op.Payload, "Supplier"),
                    Type = ReadString(op.Payload, "type") ?? ReadString(op.Payload, "Type") ?? "material",
                    Duration = ReadInt(op.Payload, "duration") ?? ReadInt(op.Payload, "Duration"),
                    TvaRate = ReadDecimal(op.Payload, "tvaRate") ?? ReadDecimal(op.Payload, "TvaRate") ?? 19,
                    IsActive = ReadBool(op.Payload, "isActive") ?? ReadBool(op.Payload, "IsActive") ?? true,
                    CreatedBy = user
                };
                _context.Articles.Add(article);
            }
            if (article == null) return null;
            if (operation == "delete") { article.IsDeleted = true; article.DeletedAt = DateTime.UtcNow; article.DeletedBy = user; }
            else
            {
                article.Name = ReadString(op.Payload, "name") ?? ReadString(op.Payload, "Name") ?? article.Name;
                article.Description = ReadString(op.Payload, "description") ?? ReadString(op.Payload, "Description") ?? article.Description;
                article.CategoryId = ReadInt(op.Payload, "categoryId") ?? ReadInt(op.Payload, "CategoryId") ?? article.CategoryId;
                article.Unit = ReadString(op.Payload, "unit") ?? ReadString(op.Payload, "Unit") ?? article.Unit;
                article.PurchasePrice = ReadDecimal(op.Payload, "purchasePrice") ?? ReadDecimal(op.Payload, "PurchasePrice") ?? article.PurchasePrice;
                article.SalesPrice = ReadDecimal(op.Payload, "salesPrice") ?? ReadDecimal(op.Payload, "SalesPrice") ?? article.SalesPrice;
                article.StockQuantity = ReadDecimal(op.Payload, "stockQuantity") ?? ReadDecimal(op.Payload, "StockQuantity") ?? article.StockQuantity;
                article.MinStockLevel = ReadDecimal(op.Payload, "minStockLevel") ?? ReadDecimal(op.Payload, "MinStockLevel") ?? article.MinStockLevel;
                article.LocationId = ReadInt(op.Payload, "locationId") ?? ReadInt(op.Payload, "LocationId") ?? article.LocationId;
                article.GroupId = ReadInt(op.Payload, "groupId") ?? ReadInt(op.Payload, "GroupId") ?? article.GroupId;
                article.Supplier = ReadString(op.Payload, "supplier") ?? ReadString(op.Payload, "Supplier") ?? article.Supplier;
                article.Type = ReadString(op.Payload, "type") ?? ReadString(op.Payload, "Type") ?? article.Type;
                article.IsActive = ReadBool(op.Payload, "isActive") ?? ReadBool(op.Payload, "IsActive") ?? article.IsActive;
                article.ModifiedBy = user;
                article.ModifiedDate = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("article", article.Id, operation, article, user);
            return article.Id;
        }

        private async Task<int?> ApplyStockTransactionAsync(SyncOperationDto op, string operation, string user)
        {
            if (operation == "delete") return null;
            var articleId = ReadInt(op.Payload, "articleId") ?? ReadInt(op.Payload, "ArticleId") ?? 0;
            var quantity = ReadDecimal(op.Payload, "quantity") ?? ReadDecimal(op.Payload, "Quantity") ?? 0;
            var reason = ReadString(op.Payload, "reason") ?? ReadString(op.Payload, "Reason") ?? "Offline sync";
            var notes = ReadString(op.Payload, "notes") ?? ReadString(op.Payload, "Notes");
            var txType = op.Endpoint?.Contains("/add") == true ? "add" : "remove";
            if (articleId <= 0 || quantity <= 0) throw new InvalidOperationException("articleId and quantity are required for stock transactions");
            var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == articleId && !a.IsDeleted);
            if (article == null) throw new KeyNotFoundException($"Article {articleId} not found");
            var prevStock = article.StockQuantity;
            decimal newStock;
            if (txType == "add") newStock = prevStock + quantity;
            else
            {
                if (prevStock < quantity) throw new InvalidOperationException($"Insufficient stock. Available: {prevStock}, requested: {quantity}");
                newStock = prevStock - quantity;
            }
            article.StockQuantity = newStock;
            article.ModifiedDate = DateTime.UtcNow;
            var tx = new StockTransaction
            {
                ArticleId = articleId,
                TransactionType = txType,
                Quantity = quantity,
                PreviousStock = prevStock,
                NewStock = newStock,
                Reason = reason,
                Notes = notes,
                ReferenceType = "manual",
                PerformedBy = user,
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<StockTransaction>().Add(tx);
            await _context.SaveChangesAsync();
            await RecordChangeAsync("stock_transaction", tx.Id, "create", tx, user);
            return tx.Id;
        }

        private async Task<int?> ApplyHrDepartmentAsync(SyncOperationDto op, string operation, string user)
        {
            HrDepartment? dept = null;
            var id = op.EntityId ?? ParseIdFromEndpoint(op.Endpoint, "departments");
            if (id.HasValue) dept = await _context.Set<HrDepartment>().FirstOrDefaultAsync(x => x.Id == id.Value && !x.IsDeleted);
            if (dept == null && operation != "delete")
            {
                dept = new HrDepartment
                {
                    Name = ReadString(op.Payload, "name") ?? ReadString(op.Payload, "Name") ?? "Offline Dept",
                    Code = ReadString(op.Payload, "code") ?? ReadString(op.Payload, "Code"),
                    ParentId = ReadInt(op.Payload, "parentId") ?? ReadInt(op.Payload, "ParentId"),
                    ManagerId = ReadInt(op.Payload, "managerId") ?? ReadInt(op.Payload, "ManagerId"),
                    Description = ReadString(op.Payload, "description") ?? ReadString(op.Payload, "Description"),
                    Position = ReadInt(op.Payload, "position") ?? ReadInt(op.Payload, "Position")
                };
                _context.Set<HrDepartment>().Add(dept);
            }
            if (dept == null) return null;
            if (operation == "delete") dept.IsDeleted = true;
            else
            {
                dept.Name = ReadString(op.Payload, "name") ?? ReadString(op.Payload, "Name") ?? dept.Name;
                dept.Code = ReadString(op.Payload, "code") ?? ReadString(op.Payload, "Code") ?? dept.Code;
                dept.ParentId = ReadInt(op.Payload, "parentId") ?? ReadInt(op.Payload, "ParentId") ?? dept.ParentId;
                dept.ManagerId = ReadInt(op.Payload, "managerId") ?? ReadInt(op.Payload, "ManagerId") ?? dept.ManagerId;
                dept.Description = ReadString(op.Payload, "description") ?? ReadString(op.Payload, "Description") ?? dept.Description;
                dept.Position = ReadInt(op.Payload, "position") ?? ReadInt(op.Payload, "Position") ?? dept.Position;
                dept.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("hr_department", dept.Id, operation, dept, user);
            return dept.Id;
        }

        private async Task<int?> ApplyHrAttendanceAsync(SyncOperationDto op, string operation, string user)
        {
            if (operation == "delete") return null;
            var userIdVal = ReadInt(op.Payload, "userId") ?? ReadInt(op.Payload, "UserId") ?? 0;
            var dateVal = ReadDate(op.Payload, "date") ?? ReadDate(op.Payload, "Date");
            if (userIdVal <= 0 || !dateVal.HasValue) throw new InvalidOperationException("userId and date are required for attendance");
            var date = dateVal.Value.Date;
            var existing = await _context.Set<HrAttendanceRecord>().FirstOrDefaultAsync(x => x.UserId == userIdVal && x.Date == date);
            TimeSpan? ParseTime(string? val)
            {
                if (string.IsNullOrEmpty(val)) return null;
                return TimeSpan.TryParse(val, out var ts) ? ts : null;
            }
            if (existing != null)
            {
                existing.CheckIn = ParseTime(ReadString(op.Payload, "checkIn") ?? ReadString(op.Payload, "CheckIn"));
                existing.CheckOut = ParseTime(ReadString(op.Payload, "checkOut") ?? ReadString(op.Payload, "CheckOut"));
                existing.BreakDuration = ReadInt(op.Payload, "breakDuration") ?? ReadInt(op.Payload, "BreakDuration") ?? existing.BreakDuration;
                existing.Source = ReadString(op.Payload, "source") ?? ReadString(op.Payload, "Source") ?? existing.Source;
                existing.HoursWorked = ReadDecimal(op.Payload, "hoursWorked") ?? ReadDecimal(op.Payload, "HoursWorked") ?? existing.HoursWorked;
                existing.OvertimeHours = ReadDecimal(op.Payload, "overtimeHours") ?? ReadDecimal(op.Payload, "OvertimeHours") ?? existing.OvertimeHours;
                existing.Status = ReadString(op.Payload, "status") ?? ReadString(op.Payload, "Status") ?? existing.Status;
                existing.Notes = ReadString(op.Payload, "notes") ?? ReadString(op.Payload, "Notes") ?? existing.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var rec = new HrAttendanceRecord
                {
                    UserId = userIdVal,
                    Date = date,
                    CheckIn = ParseTime(ReadString(op.Payload, "checkIn") ?? ReadString(op.Payload, "CheckIn")),
                    CheckOut = ParseTime(ReadString(op.Payload, "checkOut") ?? ReadString(op.Payload, "CheckOut")),
                    BreakDuration = ReadInt(op.Payload, "breakDuration") ?? ReadInt(op.Payload, "BreakDuration"),
                    Source = ReadString(op.Payload, "source") ?? ReadString(op.Payload, "Source") ?? "manual",
                    HoursWorked = ReadDecimal(op.Payload, "hoursWorked") ?? ReadDecimal(op.Payload, "HoursWorked"),
                    OvertimeHours = ReadDecimal(op.Payload, "overtimeHours") ?? ReadDecimal(op.Payload, "OvertimeHours"),
                    Status = ReadString(op.Payload, "status") ?? ReadString(op.Payload, "Status") ?? "present",
                    Notes = ReadString(op.Payload, "notes") ?? ReadString(op.Payload, "Notes")
                };
                _context.Set<HrAttendanceRecord>().Add(rec);
                await _context.SaveChangesAsync();
                await RecordChangeAsync("hr_attendance", rec.Id, "create", rec, user);
                return rec.Id;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("hr_attendance", existing!.Id, "upsert", existing, user);
            return existing.Id;
        }

        private async Task<int?> ApplyContactAsync(SyncOperationDto op, string operation, string user)
        {
            Contact? contact = null;
            if (op.EntityId.HasValue) contact = await _context.Contacts.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
            if (contact == null && operation != "delete")
            {
                contact = new Contact
                {
                    FirstName = ReadString(op.Payload, "firstName") ?? "Offline",
                    LastName = ReadString(op.Payload, "lastName") ?? "Contact",
                    Name = ReadString(op.Payload, "name") ?? $"{ReadString(op.Payload, "firstName") ?? "Offline"} {ReadString(op.Payload, "lastName") ?? "Contact"}",
                    CreatedBy = user
                };
                _context.Contacts.Add(contact);
            }
            if (contact == null) return null;
            if (operation == "delete") contact.IsDeleted = true;
            else
            {
                contact.FirstName = ReadString(op.Payload, "firstName") ?? contact.FirstName;
                contact.LastName = ReadString(op.Payload, "lastName") ?? contact.LastName;
                contact.Name = ReadString(op.Payload, "name") ?? contact.Name;
                contact.Email = ReadString(op.Payload, "email") ?? contact.Email;
                contact.Phone = ReadString(op.Payload, "phone") ?? contact.Phone;
                contact.Company = ReadString(op.Payload, "company") ?? contact.Company;
                contact.Type = ReadString(op.Payload, "type") ?? contact.Type;
                contact.Status = ReadString(op.Payload, "status") ?? contact.Status;
                contact.ModifiedBy = user;
                contact.ModifiedDate = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("contact", contact.Id, operation, contact, user);
            return contact.Id;
        }

        private async Task<int?> ApplyDocumentAsync(SyncOperationDto op, string operation, string user)
        {
            Document? doc = null;
            if (op.EntityId.HasValue) doc = await _context.Documents.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
            if (doc == null && operation != "delete")
            {
                doc = new Document
                {
                    FileName = ReadString(op.Payload, "fileName") ?? "offline-document.txt",
                    OriginalName = ReadString(op.Payload, "originalName") ?? "offline-document.txt",
                    FilePath = ReadString(op.Payload, "filePath") ?? "/offline",
                    FileSize = ReadInt(op.Payload, "fileSize") ?? 0,
                    ContentType = ReadString(op.Payload, "contentType") ?? "text/plain",
                    UploadedBy = user
                };
                _context.Documents.Add(doc);
            }
            if (doc == null) return null;
            if (operation == "delete")
            {
                _context.Documents.Remove(doc);
            }
            else
            {
                doc.Description = ReadString(op.Payload, "description") ?? doc.Description;
                doc.ModuleType = ReadString(op.Payload, "moduleType") ?? doc.ModuleType;
                doc.ModuleId = ReadString(op.Payload, "moduleId") ?? doc.ModuleId;
                doc.Tags = ReadString(op.Payload, "tags") ?? doc.Tags;
                doc.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("document", doc.Id, operation, doc, user);
            return doc.Id;
        }

        private async Task<int?> ApplyLookupItemAsync(SyncOperationDto op, string operation, string user)
        {
            LookupItem? item = null;
            var id = op.EntityId ?? ParseLookupItemIdFromEndpoint(op.Endpoint);
            var lookupType = ParseLookupTypeFromEndpoint(op.Endpoint);
            if (id.HasValue) item = await _context.LookupItems.FirstOrDefaultAsync(x => x.Id == id.Value && !x.IsDeleted);
            if (item == null && operation != "delete")
            {
                item = new LookupItem
                {
                    LookupType = lookupType,
                    Name = ReadString(op.Payload, "name") ?? ReadString(op.Payload, "Name") ?? "Offline Lookup",
                    Description = ReadString(op.Payload, "description") ?? ReadString(op.Payload, "Description"),
                    Color = ReadString(op.Payload, "color") ?? ReadString(op.Payload, "Color"),
                    IsActive = ReadBool(op.Payload, "isActive") ?? ReadBool(op.Payload, "IsActive") ?? true,
                    SortOrder = ReadInt(op.Payload, "sortOrder") ?? ReadInt(op.Payload, "SortOrder") ?? 0,
                    Category = ReadString(op.Payload, "category") ?? ReadString(op.Payload, "Category"),
                    Value = ReadString(op.Payload, "value") ?? ReadString(op.Payload, "Value"),
                    IsDefault = ReadBool(op.Payload, "isDefault") ?? ReadBool(op.Payload, "IsDefault") ?? false,
                    IsPaid = ReadBool(op.Payload, "isPaid") ?? ReadBool(op.Payload, "IsPaid"),
                    CreatedUser = user,
                    CreatedDate = DateTime.UtcNow
                };
                _context.LookupItems.Add(item);
            }
            if (item == null) return null;
            if (operation == "delete")
            {
                item.IsDeleted = true;
                item.IsActive = false;
                item.ModifyUser = user;
                item.ModifiedDate = DateTime.UtcNow;
                item.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                item.Name = ReadString(op.Payload, "name") ?? ReadString(op.Payload, "Name") ?? item.Name;
                item.Description = ReadString(op.Payload, "description") ?? ReadString(op.Payload, "Description") ?? item.Description;
                item.Color = ReadString(op.Payload, "color") ?? ReadString(op.Payload, "Color") ?? item.Color;
                item.IsActive = ReadBool(op.Payload, "isActive") ?? ReadBool(op.Payload, "IsActive") ?? item.IsActive;
                item.SortOrder = ReadInt(op.Payload, "sortOrder") ?? ReadInt(op.Payload, "SortOrder") ?? item.SortOrder;
                item.Category = ReadString(op.Payload, "category") ?? ReadString(op.Payload, "Category") ?? item.Category;
                item.Value = ReadString(op.Payload, "value") ?? ReadString(op.Payload, "Value") ?? item.Value;
                item.IsDefault = ReadBool(op.Payload, "isDefault") ?? ReadBool(op.Payload, "IsDefault") ?? item.IsDefault;
                item.IsPaid = ReadBool(op.Payload, "isPaid") ?? ReadBool(op.Payload, "IsPaid") ?? item.IsPaid;
                item.LookupType = lookupType ?? item.LookupType;
                item.ModifyUser = user;
                item.ModifiedDate = DateTime.UtcNow;
                item.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("lookup_item", item.Id, operation, item, user);
            return item.Id;
        }

        private async Task<int?> ApplyLookupBulkAsync(SyncOperationDto op, string operation, string user)
        {
            if (!op.Payload.HasValue || op.Payload.Value.ValueKind != JsonValueKind.Object) return null;
            var lookupType = ParseLookupTypeFromEndpoint(op.Endpoint);
            var payload = op.Payload.Value;
            if (operation == "delete")
            {
                if (payload.TryGetProperty("ids", out var ids) && ids.ValueKind == JsonValueKind.Array)
                {
                    foreach (var idEl in ids.EnumerateArray())
                    {
                        if (idEl.ValueKind != JsonValueKind.String && idEl.ValueKind != JsonValueKind.Number) continue;
                        if (!int.TryParse(idEl.ToString(), out var id)) continue;
                        var existing = await _context.LookupItems.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                        if (existing == null) continue;
                        existing.IsDeleted = true;
                        existing.IsActive = false;
                        existing.ModifyUser = user;
                        existing.ModifiedDate = DateTime.UtcNow;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }
                return null;
            }

            if (!payload.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) return null;
            foreach (var it in items.EnumerateArray())
            {
                if (operation == "upsert" && it.ValueKind == JsonValueKind.Object && it.TryGetProperty("id", out var idField) && it.TryGetProperty("data", out var dataField))
                {
                    var idParsed = int.TryParse(idField.ToString(), out var parsedId) ? parsedId : (int?)null;
                    var inner = new SyncOperationDto
                    {
                        EntityId = idParsed,
                        Endpoint = op.Endpoint,
                        Operation = "upsert",
                        Payload = dataField
                    };
                    await ApplyLookupItemAsync(inner, "upsert", user);
                }
                else if (it.ValueKind == JsonValueKind.Object)
                {
                    var inner = new SyncOperationDto
                    {
                        Endpoint = op.Endpoint,
                        Operation = "upsert",
                        Payload = it
                    };
                    await ApplyLookupItemAsync(inner, "upsert", user);
                }
            }
            return null;
        }

        private async Task<int?> ApplyCurrencyAsync(SyncOperationDto op, string operation, string user)
        {
            Currency? currency = null;
            var id = op.EntityId ?? ParseIdFromEndpoint(op.Endpoint, "currencies");
            if (id.HasValue) currency = await _context.Currencies.FirstOrDefaultAsync(x => x.Id == id.Value);
            if (currency == null && operation != "delete")
            {
                currency = new Currency
                {
                    Name = ReadString(op.Payload, "name") ?? ReadString(op.Payload, "Name") ?? "Offline Currency",
                    Symbol = ReadString(op.Payload, "symbol") ?? ReadString(op.Payload, "Symbol") ?? "$",
                    Code = ReadString(op.Payload, "code") ?? ReadString(op.Payload, "Code") ?? "XXX",
                    IsActive = ReadBool(op.Payload, "isActive") ?? ReadBool(op.Payload, "IsActive") ?? true,
                    IsDefault = ReadBool(op.Payload, "isDefault") ?? ReadBool(op.Payload, "IsDefault") ?? false,
                    SortOrder = ReadInt(op.Payload, "sortOrder") ?? ReadInt(op.Payload, "SortOrder") ?? 0,
                    CreatedUser = user,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Currencies.Add(currency);
            }
            if (currency == null) return null;
            if (operation == "delete")
            {
                _context.Currencies.Remove(currency);
            }
            else
            {
                currency.Name = ReadString(op.Payload, "name") ?? ReadString(op.Payload, "Name") ?? currency.Name;
                currency.Symbol = ReadString(op.Payload, "symbol") ?? ReadString(op.Payload, "Symbol") ?? currency.Symbol;
                currency.Code = ReadString(op.Payload, "code") ?? ReadString(op.Payload, "Code") ?? currency.Code;
                currency.IsActive = ReadBool(op.Payload, "isActive") ?? ReadBool(op.Payload, "IsActive") ?? currency.IsActive;
                currency.IsDefault = ReadBool(op.Payload, "isDefault") ?? ReadBool(op.Payload, "IsDefault") ?? currency.IsDefault;
                currency.SortOrder = ReadInt(op.Payload, "sortOrder") ?? ReadInt(op.Payload, "SortOrder") ?? currency.SortOrder;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("currency", currency.Id, operation, currency, user);
            return currency.Id;
        }

        private async Task<int?> ApplySupportTicketAsync(SyncOperationDto op, string operation, string user)
        {
            SupportTicket? ticket = null;
            var id = op.EntityId ?? ParseIdFromEndpoint(op.Endpoint, "SupportTickets");
            if (id.HasValue) ticket = await _context.SupportTickets.FirstOrDefaultAsync(x => x.Id == id.Value);
            if (ticket != null && !IsTicketOwnedByCurrentUser(ticket, user))
                throw new UnauthorizedAccessException("You are not allowed to modify this support ticket.");
            if (ticket == null && operation != "delete")
            {
                ticket = new SupportTicket
                {
                    Title = ReadString(op.Payload, "title") ?? ReadString(op.Payload, "Title") ?? "Offline Ticket",
                    Description = ReadString(op.Payload, "description") ?? ReadString(op.Payload, "Description") ?? string.Empty,
                    Urgency = ReadString(op.Payload, "urgency") ?? ReadString(op.Payload, "Urgency"),
                    Category = ReadString(op.Payload, "category") ?? ReadString(op.Payload, "Category"),
                    CurrentPage = ReadString(op.Payload, "currentPage") ?? ReadString(op.Payload, "CurrentPage"),
                    RelatedUrl = ReadString(op.Payload, "relatedUrl") ?? ReadString(op.Payload, "RelatedUrl"),
                    UserEmail = user,
                    Status = ReadString(op.Payload, "status") ?? ReadString(op.Payload, "Status") ?? "open",
                    Tenant = "default",
                    CreatedAt = DateTime.UtcNow
                };
                _context.SupportTickets.Add(ticket);
            }
            if (ticket == null) return null;
            if (operation == "delete")
            {
                _context.SupportTickets.Remove(ticket);
            }
            else
            {
                ticket.Status = ReadString(op.Payload, "status") ?? ReadString(op.Payload, "Status") ?? ticket.Status;
                ticket.Title = ReadString(op.Payload, "title") ?? ReadString(op.Payload, "Title") ?? ticket.Title;
                ticket.Description = ReadString(op.Payload, "description") ?? ReadString(op.Payload, "Description") ?? ticket.Description;
                ticket.Urgency = ReadString(op.Payload, "urgency") ?? ReadString(op.Payload, "Urgency") ?? ticket.Urgency;
                ticket.Category = ReadString(op.Payload, "category") ?? ReadString(op.Payload, "Category") ?? ticket.Category;
                ticket.CurrentPage = ReadString(op.Payload, "currentPage") ?? ReadString(op.Payload, "CurrentPage") ?? ticket.CurrentPage;
                ticket.RelatedUrl = ReadString(op.Payload, "relatedUrl") ?? ReadString(op.Payload, "RelatedUrl") ?? ticket.RelatedUrl;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("support_ticket", ticket.Id, operation, ticket, user);
            return ticket.Id;
        }

        private async Task<int?> ApplySupportTicketCommentAsync(SyncOperationDto op, string operation, string user)
        {
            if (operation == "delete") return null;
            var ticketId = ReadInt(op.Payload, "ticketId")
                ?? ReadInt(op.Payload, "TicketId")
                ?? ReadInt(op.Payload, "supportTicketId")
                ?? ParseIdFromEndpoint(op.Endpoint, "SupportTickets");
            if (!ticketId.HasValue) throw new InvalidOperationException("Ticket id is required for support ticket comments");
            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(x => x.Id == ticketId.Value);
            if (ticket == null) throw new KeyNotFoundException($"Support ticket '{ticketId.Value}' not found");
            if (!IsTicketOwnedByCurrentUser(ticket, user))
                throw new UnauthorizedAccessException("You are not allowed to comment on this support ticket.");
            var comment = new SupportTicketComment
            {
                SupportTicketId = ticketId.Value,
                Author = ReadString(op.Payload, "author") ?? ReadString(op.Payload, "Author") ?? user,
                AuthorEmail = ReadString(op.Payload, "authorEmail") ?? ReadString(op.Payload, "AuthorEmail"),
                Text = ReadString(op.Payload, "text") ?? ReadString(op.Payload, "Text") ?? string.Empty,
                IsInternal = ReadBool(op.Payload, "isInternal") ?? ReadBool(op.Payload, "IsInternal") ?? false,
                CreatedAt = DateTime.UtcNow
            };
            _context.SupportTicketComments.Add(comment);
            await _context.SaveChangesAsync();
            await RecordChangeAsync("support_ticket_comment", comment.Id, "create", comment, user);
            return comment.Id;
        }

        private async Task<int?> ApplySupportTicketLinkAsync(SyncOperationDto op, string operation, string user)
        {
            var sourceTicketId = ReadInt(op.Payload, "sourceTicketId")
                ?? ReadInt(op.Payload, "SourceTicketId")
                ?? ParseIdFromEndpoint(op.Endpoint, "SupportTickets");
            var targetTicketId = ReadInt(op.Payload, "targetTicketId") ?? ReadInt(op.Payload, "TargetTicketId");
            if (!sourceTicketId.HasValue) throw new InvalidOperationException("Source ticket id is required");
            var sourceTicket = await _context.SupportTickets.FirstOrDefaultAsync(x => x.Id == sourceTicketId.Value);
            if (sourceTicket == null) throw new KeyNotFoundException("Source support ticket not found");
            if (!IsTicketOwnedByCurrentUser(sourceTicket, user))
                throw new UnauthorizedAccessException("You are not allowed to link this support ticket.");
            if (operation == "delete")
            {
                var linkId = ParseIdFromEndpoint(op.Endpoint, "links");
                if (!linkId.HasValue) return null;
                var existing = await _context.SupportTicketLinks.FirstOrDefaultAsync(x => x.Id == linkId.Value && x.SourceTicketId == sourceTicketId.Value);
                if (existing == null) return null;
                _context.SupportTicketLinks.Remove(existing);
                await _context.SaveChangesAsync();
                await RecordChangeAsync("support_ticket_link", existing.Id, "delete", existing, user);
                return existing.Id;
            }
            if (!targetTicketId.HasValue) throw new InvalidOperationException("Target ticket id is required");
            var targetTicket = await _context.SupportTickets.FirstOrDefaultAsync(x => x.Id == targetTicketId.Value);
            if (targetTicket == null) throw new KeyNotFoundException("Target support ticket not found");
            if (!IsTicketOwnedByCurrentUser(targetTicket, user))
                throw new UnauthorizedAccessException("You are not allowed to link to this support ticket.");
            var link = new SupportTicketLink
            {
                SourceTicketId = sourceTicketId.Value,
                TargetTicketId = targetTicketId.Value,
                LinkType = ReadString(op.Payload, "linkType") ?? ReadString(op.Payload, "LinkType") ?? "related",
                CreatedAt = DateTime.UtcNow
            };
            _context.SupportTicketLinks.Add(link);
            await _context.SaveChangesAsync();
            await RecordChangeAsync("support_ticket_link", link.Id, "create", link, user);
            return link.Id;
        }

        private async Task<int?> ApplyTaskChecklistAsync(SyncOperationDto op, string operation, string user)
        {
            TaskChecklist? checklist = null;
            var id = op.EntityId ?? ParseIdFromEndpoint(op.Endpoint, "taskchecklists");
            if (id.HasValue) checklist = await _context.TaskChecklists.FirstOrDefaultAsync(x => x.Id == id.Value);
            if (checklist == null && operation != "delete")
            {
                checklist = new TaskChecklist
                {
                    Title = ReadString(op.Payload, "title") ?? ReadString(op.Payload, "Title") ?? "Offline checklist",
                    ProjectTaskId = ReadInt(op.Payload, "projectTaskId") ?? ReadInt(op.Payload, "ProjectTaskId"),
                    DailyTaskId = ReadInt(op.Payload, "dailyTaskId") ?? ReadInt(op.Payload, "DailyTaskId"),
                    SortOrder = ReadInt(op.Payload, "sortOrder") ?? ReadInt(op.Payload, "SortOrder") ?? 1,
                    CreatedBy = user,
                    CreatedDate = DateTime.UtcNow
                };
                _context.TaskChecklists.Add(checklist);
            }
            if (checklist == null) return null;
            if (operation == "delete")
            {
                _context.TaskChecklists.Remove(checklist);
            }
            else
            {
                checklist.Title = ReadString(op.Payload, "title") ?? ReadString(op.Payload, "Title") ?? checklist.Title;
                checklist.SortOrder = ReadInt(op.Payload, "sortOrder") ?? ReadInt(op.Payload, "SortOrder") ?? checklist.SortOrder;
                checklist.ModifiedBy = user;
                checklist.ModifiedDate = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("task_checklist", checklist.Id, operation, checklist, user);
            return checklist.Id;
        }

        private async Task<int?> ApplyTaskChecklistItemAsync(SyncOperationDto op, string operation, string user)
        {
            TaskChecklistItem? item = null;
            var id = op.EntityId ?? ParseIdFromEndpoint(op.Endpoint, "items");
            if (id.HasValue) item = await _context.TaskChecklistItems.FirstOrDefaultAsync(x => x.Id == id.Value);
            if (item == null && operation != "delete")
            {
                var checklistId = ReadInt(op.Payload, "checklistId") ?? ReadInt(op.Payload, "ChecklistId");
                if (!checklistId.HasValue) throw new InvalidOperationException("Checklist id is required");
                item = new TaskChecklistItem
                {
                    ChecklistId = checklistId.Value,
                    Title = ReadString(op.Payload, "title") ?? ReadString(op.Payload, "Title") ?? "Item",
                    IsCompleted = ReadBool(op.Payload, "isCompleted") ?? ReadBool(op.Payload, "IsCompleted") ?? false,
                    SortOrder = ReadInt(op.Payload, "sortOrder") ?? ReadInt(op.Payload, "SortOrder") ?? 1,
                    CreatedBy = user,
                    CreatedDate = DateTime.UtcNow
                };
                _context.TaskChecklistItems.Add(item);
            }
            if (item == null) return null;
            if (operation == "delete")
            {
                _context.TaskChecklistItems.Remove(item);
            }
            else
            {
                if (op.Endpoint?.Contains("/toggle") == true)
                {
                    var explicitState = ReadBool(op.Payload, "isCompleted") ?? ReadBool(op.Payload, "IsCompleted");
                    item.IsCompleted = explicitState ?? !item.IsCompleted;
                    item.CompletedAt = item.IsCompleted ? DateTime.UtcNow : null;
                }
                else
                {
                    item.Title = ReadString(op.Payload, "title") ?? ReadString(op.Payload, "Title") ?? item.Title;
                    item.IsCompleted = ReadBool(op.Payload, "isCompleted") ?? ReadBool(op.Payload, "IsCompleted") ?? item.IsCompleted;
                    item.SortOrder = ReadInt(op.Payload, "sortOrder") ?? ReadInt(op.Payload, "SortOrder") ?? item.SortOrder;
                }
                item.ModifiedBy = user;
                item.ModifiedDate = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("task_checklist_item", item.Id, operation, item, user);
            return item.Id;
        }

        private async Task<int?> ApplyDynamicFormAsync(SyncOperationDto op, string operation, string user)
        {
            DynamicForm? form = null;
            if (op.EntityId.HasValue) form = await _context.DynamicForms.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
            if (form == null && operation != "delete")
            {
                form = new DynamicForm
                {
                    NameEn = ReadString(op.Payload, "nameEn") ?? ReadString(op.Payload, "name") ?? "Offline Form",
                    NameFr = ReadString(op.Payload, "nameFr") ?? ReadString(op.Payload, "name") ?? "Formulaire Hors Ligne",
                    Fields = ReadString(op.Payload, "fields") ?? "[]",
                    CreatedBy = user
                };
                _context.DynamicForms.Add(form);
            }
            if (form == null) return null;
            if (operation == "delete") form.IsDeleted = true;
            else
            {
                form.NameEn = ReadString(op.Payload, "nameEn") ?? form.NameEn;
                form.NameFr = ReadString(op.Payload, "nameFr") ?? form.NameFr;
                form.DescriptionEn = ReadString(op.Payload, "descriptionEn") ?? form.DescriptionEn;
                form.DescriptionFr = ReadString(op.Payload, "descriptionFr") ?? form.DescriptionFr;
                form.Fields = ReadString(op.Payload, "fields") ?? form.Fields;
                form.Category = ReadString(op.Payload, "category") ?? form.Category;
                form.IsPublic = ReadBool(op.Payload, "isPublic") ?? form.IsPublic;
                form.PublicSlug = ReadString(op.Payload, "publicSlug") ?? form.PublicSlug;
                form.ModifiedBy = user;
                form.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("dynamic_form", form.Id, operation, form, user);
            return form.Id;
        }

        private async Task<int?> ApplyDynamicFormResponseAsync(SyncOperationDto op, string operation, string user)
        {
            if (operation == "delete") return null;
            var formId = ReadInt(op.Payload, "formId") ?? ReadInt(op.Payload, "FormId") ?? ParseFormIdFromEndpoint(op.Endpoint);
            if (!formId.HasValue || formId.Value <= 0)
                throw new InvalidOperationException("FormId is required for dynamic form response");
            var form = await _context.DynamicForms.FirstOrDefaultAsync(f => f.Id == formId.Value && !f.IsDeleted);
            if (form == null)
                throw new KeyNotFoundException($"Form with ID {formId} not found");
            var responsesJson = ReadJson(op.Payload, "responses") ?? ReadJson(op.Payload, "Responses") ?? "{}";
            var response = new DynamicFormResponse
            {
                FormId = form.Id,
                FormVersion = form.Version,
                EntityType = ReadString(op.Payload, "entityType") ?? ReadString(op.Payload, "EntityType"),
                EntityId = ReadString(op.Payload, "entityId") ?? ReadString(op.Payload, "EntityId"),
                Responses = responsesJson,
                Notes = ReadString(op.Payload, "notes") ?? ReadString(op.Payload, "Notes"),
                SubmitterName = ReadString(op.Payload, "submitterName") ?? ReadString(op.Payload, "SubmitterName"),
                SubmitterEmail = ReadString(op.Payload, "submitterEmail") ?? ReadString(op.Payload, "SubmitterEmail"),
                IsPublicSubmission = ReadBool(op.Payload, "isPublicSubmission") ?? ReadBool(op.Payload, "IsPublicSubmission") ?? false,
                SubmittedBy = user,
                SubmittedAt = DateTime.UtcNow
            };
            _context.Set<DynamicFormResponse>().Add(response);
            await _context.SaveChangesAsync();
            await RecordChangeAsync("dynamic_form_response", response.Id, "create", response, user);
            return response.Id;
        }

        private async Task<int?> ApplySyncedEmailAsync(SyncOperationDto op, string operation, string user)
        {
            var currentUserId = await ResolveCurrentUserIdAsync(user);
            if (!currentUserId.HasValue) throw new UnauthorizedAccessException("Unable to resolve current user.");
            var (accountId, emailId) = ParseSyncedEmailIdsFromPayloadOrEndpoint(op.Payload, op.Endpoint);
            if (!accountId.HasValue || !emailId.HasValue)
                throw new InvalidOperationException("accountId and emailId are required for synced email operations");
            var email = await _context.Set<SyncedEmail>()
                .FirstOrDefaultAsync(e => e.Id == emailId.Value && e.ConnectedEmailAccountId == accountId.Value);
            if (email == null)
                throw new KeyNotFoundException($"Synced email {emailId} not found");
            var account = await _context.ConnectedEmailAccounts.FirstOrDefaultAsync(a => a.Id == accountId.Value);
            if (account == null || account.UserId != currentUserId.Value)
                throw new UnauthorizedAccessException("You are not allowed to modify this synced email.");
            if (operation == "delete")
            {
                _context.Set<SyncedEmail>().Remove(email);
                await _context.SaveChangesAsync();
                await RecordChangeAsync("synced_email", 0, email.Id.ToString(), "delete", new { id = email.Id }, user, currentUserId);
                return null;
            }
            if (op.Endpoint?.Contains("/star") == true)
                email.IsStarred = !email.IsStarred;
            else if (op.Endpoint?.Contains("/read") == true)
                email.IsRead = !email.IsRead;
            await _context.SaveChangesAsync();
            await RecordChangeAsync("synced_email", 0, email.Id.ToString(), "upsert", email, user, currentUserId);
            return null;
        }

        private async Task<int?> ApplyCalendarEventAsync(SyncOperationDto op, string operation, string user)
        {
            CalendarEvent? evt = null;
            var eventId = ReadGuid(op.Payload, "id") ?? ReadGuid(op.Payload, "Id");
            if (!eventId.HasValue && !string.IsNullOrEmpty(op.Endpoint))
            {
                var m = System.Text.RegularExpressions.Regex.Match(op.Endpoint, @"/events/([a-f0-9-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success && Guid.TryParse(m.Groups[1].Value, out var g)) eventId = g;
            }
            if (eventId.HasValue)
                evt = await _context.CalendarEvents.FirstOrDefaultAsync(x => x.Id == eventId.Value);
            else
            {
                var endpointGuid = ParseGuidIdFromEndpoint(op.Endpoint, "events");
                if (endpointGuid.HasValue) evt = await _context.CalendarEvents.FirstOrDefaultAsync(x => x.Id == endpointGuid.Value);
            }
            if (evt == null && operation != "delete")
            {
                evt = new CalendarEvent
                {
                    Id = Guid.NewGuid(),
                    Title = ReadString(op.Payload, "title") ?? "Offline Event",
                    Start = ReadDate(op.Payload, "start") ?? DateTime.UtcNow,
                    End = ReadDate(op.Payload, "end") ?? DateTime.UtcNow.AddHours(1),
                    Type = ReadString(op.Payload, "type") ?? "meeting",
                    CreatedBy = Guid.Empty
                };
                _context.CalendarEvents.Add(evt);
            }
            if (evt == null) return null;
            if (operation == "delete")
            {
                _context.CalendarEvents.Remove(evt);
            }
            else
            {
                evt.Title = ReadString(op.Payload, "title") ?? evt.Title;
                evt.Description = ReadString(op.Payload, "description") ?? evt.Description;
                evt.Start = ReadDate(op.Payload, "start") ?? evt.Start;
                evt.End = ReadDate(op.Payload, "end") ?? evt.End;
                evt.Status = ReadString(op.Payload, "status") ?? evt.Status;
                evt.Priority = ReadString(op.Payload, "priority") ?? evt.Priority;
                evt.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("calendar_event", 0, evt.Id.ToString(), operation, evt, user, await ResolveCurrentUserIdAsync(user));
            return null;
        }

        private async Task<int?> ApplyEmailAccountAsync(SyncOperationDto op, string operation, string user)
        {
            var currentUserId = await ResolveCurrentUserIdAsync(user);
            if (!currentUserId.HasValue) throw new UnauthorizedAccessException("Unable to resolve current user.");
            ConnectedEmailAccount? account = null;
            var accountGuid = ReadGuid(op.Payload, "id") ?? ReadGuid(op.Payload, "Id") ?? ParseGuidIdFromEndpoint(op.Endpoint, "email-accounts");
            if (accountGuid.HasValue) account = await _context.ConnectedEmailAccounts.FirstOrDefaultAsync(x => x.Id == accountGuid.Value);
            if (account == null && operation != "delete")
            {
                account = new ConnectedEmailAccount
                {
                    UserId = currentUserId.Value,
                    Handle = ReadString(op.Payload, "handle") ?? ReadString(op.Payload, "email") ?? "offline@flowentra.local",
                    Provider = ReadString(op.Payload, "provider") ?? "custom",
                    AccessToken = ReadString(op.Payload, "accessToken") ?? "offline",
                    RefreshToken = ReadString(op.Payload, "refreshToken") ?? "offline"
                };
                _context.ConnectedEmailAccounts.Add(account);
            }
            if (account == null) return null;
            if (account.UserId != currentUserId.Value)
                throw new UnauthorizedAccessException("You are not allowed to modify this email account.");
            if (operation == "delete")
            {
                _context.ConnectedEmailAccounts.Remove(account);
            }
            else
            {
                account.Handle = ReadString(op.Payload, "handle") ?? account.Handle;
                account.Provider = ReadString(op.Payload, "provider") ?? account.Provider;
                account.SyncStatus = ReadString(op.Payload, "syncStatus") ?? account.SyncStatus;
                account.IsEmailSyncEnabled = ReadBool(op.Payload, "isEmailSyncEnabled") ?? account.IsEmailSyncEnabled;
                account.IsCalendarSyncEnabled = ReadBool(op.Payload, "isCalendarSyncEnabled") ?? account.IsCalendarSyncEnabled;
                account.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await RecordChangeAsync("email_account", 0, account.Id.ToString(), operation, account, user, currentUserId);
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
            task.RelatedEntityType = ReadString(op.Payload, "relatedEntityType") ?? ReadString(op.Payload, "RelatedEntityType") ?? task.RelatedEntityType;
            task.RelatedEntityId = ReadInt(op.Payload, "relatedEntityId") ?? ReadInt(op.Payload, "RelatedEntityId") ?? task.RelatedEntityId;
            task.DueDate = ReadDate(op.Payload, "dueDate") ?? ReadDate(op.Payload, "DueDate") ?? task.DueDate;
            task.AssignedUserId = ReadInt(op.Payload, "assignedUserId") ?? ReadInt(op.Payload, "AssignedUserId") ?? task.AssignedUserId;
            task.ModifiedBy = user;
            task.ModifiedDate = DateTime.UtcNow;

            if (task.Id == 0) _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();
            await RecordChangeAsync("task", task.Id, "upsert", task, user);
            return task.Id;
        }

        private async Task<int?> ApplyDailyTaskAsync(SyncOperationDto op, string operation, string user)
        {
            var id = op.EntityId ?? ParseIdFromEndpoint(op.Endpoint, "daily-task");
            if (operation == "delete" && id.HasValue)
            {
                var existing = await _context.DailyTasks.FirstOrDefaultAsync(x => x.Id == id.Value);
                if (existing != null) _context.DailyTasks.Remove(existing);
                await _context.SaveChangesAsync();
                await RecordChangeAsync("daily_task", id.Value, "delete", new { id }, user);
                return id;
            }

            DailyTask task;
            if (id.HasValue)
            {
                task = await _context.DailyTasks.FirstOrDefaultAsync(x => x.Id == id.Value) ?? new DailyTask { CreatedBy = user };
            }
            else
            {
                task = new DailyTask { CreatedBy = user };
            }

            task.Title = ReadString(op.Payload, "title") ?? ReadString(op.Payload, "Title") ?? task.Title ?? "Offline Task";
            task.Description = ReadString(op.Payload, "description") ?? ReadString(op.Payload, "Description") ?? task.Description;
            task.Status = ReadString(op.Payload, "status") ?? ReadString(op.Payload, "Status") ?? task.Status ?? "open";
            task.TaskType = ReadString(op.Payload, "taskType") ?? ReadString(op.Payload, "TaskType") ?? task.TaskType ?? "follow-up";
            task.RelatedEntityType = ReadString(op.Payload, "relatedEntityType") ?? ReadString(op.Payload, "RelatedEntityType") ?? task.RelatedEntityType;
            task.RelatedEntityId = ReadInt(op.Payload, "relatedEntityId") ?? ReadInt(op.Payload, "RelatedEntityId") ?? task.RelatedEntityId;
            task.DueDate = ReadDate(op.Payload, "dueDate") ?? ReadDate(op.Payload, "DueDate") ?? task.DueDate;
            task.AssignedUserId = ReadInt(op.Payload, "assignedUserId") ?? ReadInt(op.Payload, "AssignedUserId") ?? task.AssignedUserId;
            task.Priority = ReadString(op.Payload, "priority") ?? ReadString(op.Payload, "Priority") ?? task.Priority;

            if (task.Id == 0) _context.DailyTasks.Add(task);
            await _context.SaveChangesAsync();
            await RecordChangeAsync("daily_task", task.Id, "upsert", task, user);
            return task.Id;
        }

        private async Task<int?> ApplyOfferAsync(SyncOperationDto op, string operation, string user)
        {
            Offer? offer = null;
            if (op.EntityId.HasValue)
            {
                offer = await _context.Offers.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
            }
            if (offer == null && operation != "delete")
            {
                offer = new Offer
                {
                    ContactId = ReadInt(op.Payload, "contactId") ?? 0,
                    OfferNumber = ReadString(op.Payload, "offerNumber") ?? $"OFF-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    CreatedBy = user
                };
                _context.Offers.Add(offer);
            }
            if (offer == null) return null;
            if (operation == "delete") { offer.IsDeleted = true; }
            else
            {
                offer.Title = ReadString(op.Payload, "title") ?? offer.Title;
                offer.Description = ReadString(op.Payload, "description") ?? offer.Description;
                offer.Status = ReadString(op.Payload, "status") ?? offer.Status;
                offer.ContactId = ReadInt(op.Payload, "contactId") ?? offer.ContactId;
            }
            offer.ProjectId = ReadInt(op.Payload, "projectId") ?? offer.ProjectId;
            offer.ModifiedBy = user;
            offer.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await RecordChangeAsync("offer", offer.Id, operation, offer, user);
            return offer.Id;
        }

        private async Task<int?> ApplySaleAsync(SyncOperationDto op, string operation, string user)
        {
            Sale? sale = null;
            if (op.EntityId.HasValue)
            {
                sale = await _context.Sales.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
            }
            if (sale == null && operation != "delete")
            {
                sale = new Sale
                {
                    ContactId = ReadInt(op.Payload, "contactId") ?? 0,
                    SaleNumber = ReadString(op.Payload, "saleNumber") ?? $"SAL-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    CreatedBy = user
                };
                _context.Sales.Add(sale);
            }
            if (sale == null) return null;
            if (operation == "delete") { sale.IsDeleted = true; }
            else
            {
                sale.Title = ReadString(op.Payload, "title") ?? sale.Title;
                sale.Description = ReadString(op.Payload, "description") ?? sale.Description;
                sale.Status = ReadString(op.Payload, "status") ?? sale.Status;
                sale.ContactId = ReadInt(op.Payload, "contactId") ?? sale.ContactId;
            }
            sale.ProjectId = ReadInt(op.Payload, "projectId") ?? sale.ProjectId;
            sale.ModifiedBy = user;
            sale.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await RecordChangeAsync("sale", sale.Id, operation, sale, user);
            return sale.Id;
        }

        private async Task<int?> ApplyServiceOrderAsync(SyncOperationDto op, string operation, string user)
        {
            ServiceOrder? order = null;
            if (op.EntityId.HasValue)
            {
                order = await _context.ServiceOrders.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
            }
            if (order == null && operation != "delete")
            {
                order = new ServiceOrder
                {
                    ContactId = ReadInt(op.Payload, "contactId") ?? 0,
                    OrderNumber = ReadString(op.Payload, "orderNumber") ?? $"SO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    CreatedBy = user
                };
                _context.ServiceOrders.Add(order);
            }
            if (order == null) return null;
            if (operation == "delete") { order.IsDeleted = true; }
            else
            {
                order.Description = ReadString(op.Payload, "description") ?? order.Description;
                order.Status = ReadString(op.Payload, "status") ?? order.Status;
                order.Priority = ReadString(op.Payload, "priority") ?? order.Priority;
                order.ContactId = ReadInt(op.Payload, "contactId") ?? order.ContactId;
            }
            order.ProjectId = ReadInt(op.Payload, "projectId") ?? order.ProjectId;
            order.ModifiedBy = user;
            order.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await RecordChangeAsync("service_order", order.Id, operation, order, user);
            return order.Id;
        }

        private async Task<int?> ApplyDispatchAsync(SyncOperationDto op, string operation, string user)
        {
            Dispatch? dispatch = null;
            if (op.EntityId.HasValue)
            {
                dispatch = await _context.Dispatches.FirstOrDefaultAsync(x => x.Id == op.EntityId.Value);
            }
            if (dispatch == null && operation != "delete")
            {
                dispatch = new Dispatch
                {
                    ContactId = ReadInt(op.Payload, "contactId") ?? 0,
                    DispatchNumber = ReadString(op.Payload, "dispatchNumber") ?? $"DSP-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    SiteAddress = ReadString(op.Payload, "siteAddress") ?? "Offline address",
                    ScheduledDate = ReadDate(op.Payload, "scheduledDate") ?? DateTime.UtcNow,
                    CreatedBy = user
                };
                _context.Dispatches.Add(dispatch);
            }
            if (dispatch == null) return null;
            if (operation == "delete") { dispatch.IsDeleted = true; }
            else
            {
                dispatch.Status = ReadString(op.Payload, "status") ?? dispatch.Status;
                dispatch.Priority = ReadString(op.Payload, "priority") ?? dispatch.Priority;
                dispatch.ContactId = ReadInt(op.Payload, "contactId") ?? dispatch.ContactId;
                dispatch.SiteAddress = ReadString(op.Payload, "siteAddress") ?? dispatch.SiteAddress;
                dispatch.ScheduledDate = ReadDate(op.Payload, "scheduledDate") ?? dispatch.ScheduledDate;
            }
            dispatch.ProjectId = ReadInt(op.Payload, "projectId") ?? dispatch.ProjectId;
            dispatch.ModifiedBy = user;
            dispatch.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await RecordChangeAsync("dispatch", dispatch.Id, operation, dispatch, user);
            return dispatch.Id;
        }
    }
}
