using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApi.Data;
using MyApi.Modules.WorkflowEngine.Models;
using MyApi.Modules.WorkflowEngine.Services;
using System.Text.Json;

namespace MyApi.Modules.WorkflowEngine.Controllers
{
    /// <summary>
    /// Manual “run now” endpoint for the state-based workflow polling cycle.
    ///
    /// The frontend workflow builder calls this to reconcile entity states immediately
    /// (instead of waiting for the 5-minute background polling interval).
    /// </summary>
    [ApiController]
    [Route("api/workflow-reconciliation")]
    [Authorize]
    public class WorkflowReconciliationController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WorkflowReconciliationController> _logger;

        // Must match the UI expectations (src/services/api/workflowApi.ts)
        public class ReconciliationResultDto
        {
            public int OffersFixed { get; set; }
            public int SalesFixed { get; set; }
            public int ServiceOrdersFixed { get; set; }
            public int DispatchesFixed { get; set; }
            public int TotalFixed { get; set; }
            public string Message { get; set; } = "";
        }

        public class ReconciliationStatusDto
        {
            public bool Enabled { get; set; }
            public int IntervalMinutes { get; set; }
            public string Description { get; set; } = "";
        }

        public WorkflowReconciliationController(
            IServiceProvider serviceProvider,
            ILogger<WorkflowReconciliationController> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Runs one full polling cycle immediately.
        /// This is effectively “poll now”.
        /// </summary>
        [HttpPost("run")]
        public async Task<ActionResult<ReconciliationResultDto>> Run(CancellationToken cancellationToken)
        {
            var startedAt = DateTime.UtcNow;

            _logger.LogInformation("[WORKFLOW-RECONCILE] Manual run requested at {Time}", startedAt);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IWorkflowNotificationService>();
            var graphExecutor = scope.ServiceProvider.GetRequiredService<IWorkflowGraphExecutor>();

            // Get all active triggers with their workflows (same criteria as WorkflowPollingService)
            var triggers = await db.WorkflowTriggers
                .Include(t => t.Workflow)
                .Where(t => t.IsActive
                    && t.Workflow != null
                    && t.Workflow.IsActive
                    && !t.Workflow.IsDeleted)
                .ToListAsync(cancellationToken);

            int totalProcessed = 0;
            int totalTriggered = 0;

            int offersFixed = 0;
            int salesFixed = 0;
            int serviceOrdersFixed = 0;
            int dispatchesFixed = 0;

            foreach (var trigger in triggers)
            {
                try
                {
                    var (processed, triggered) = await ProcessTriggerAsync(
                        db,
                        trigger,
                        graphExecutor,
                        notificationService,
                        cancellationToken);

                    totalProcessed += processed;
                    totalTriggered += triggered;

                    // “Fixed” counts are a best-effort metric for UI only:
                    // count how many executions were triggered by entity type.
                    switch (trigger.EntityType?.ToLower())
                    {
                        case "offer":
                            offersFixed += triggered;
                            break;
                        case "sale":
                            salesFixed += triggered;
                            break;
                        case "service_order":
                            serviceOrdersFixed += triggered;
                            break;
                        case "dispatch":
                            dispatchesFixed += triggered;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[WORKFLOW-RECONCILE] Error processing trigger {TriggerId} (Node: {NodeId})",
                        trigger.Id,
                        trigger.NodeId);
                }
            }

            var endedAt = DateTime.UtcNow;

            var result = new ReconciliationResultDto
            {
                OffersFixed = offersFixed,
                SalesFixed = salesFixed,
                ServiceOrdersFixed = serviceOrdersFixed,
                DispatchesFixed = dispatchesFixed,
                TotalFixed = totalTriggered,
                Message = $"Reconciliation complete in {(endedAt - startedAt).TotalSeconds:F1}s. Entities checked: {totalProcessed}. Workflows triggered: {totalTriggered}."
            };

            _logger.LogInformation("[WORKFLOW-RECONCILE] {Message}", result.Message);

            return Ok(result);
        }

        /// <summary>
        /// Exposes the background polling configuration so the UI can display it.
        /// </summary>
        [HttpGet("status")]
        public ActionResult<ReconciliationStatusDto> Status()
        {
            // Keep in sync with WorkflowPollingService (TimeSpan.FromMinutes(5))
            return Ok(new ReconciliationStatusDto
            {
                Enabled = true,
                IntervalMinutes = 5,
                Description = "State-based workflow polling: checks current entity status and triggers workflows (same as background service)."
            });
        }

        private async Task<(int processed, int triggered)> ProcessTriggerAsync(
            ApplicationDbContext db,
            WorkflowTrigger trigger,
            IWorkflowGraphExecutor graphExecutor,
            IWorkflowNotificationService notificationService,
            CancellationToken cancellationToken)
        {
            int processed = 0;
            int triggered = 0;

            var matchingEntities = await GetEntitiesWithStatusAsync(db, trigger.EntityType, trigger.ToStatus, cancellationToken);

            foreach (var entity in matchingEntities)
            {
                processed++;

                var alreadyProcessed = await db.Set<WorkflowProcessedEntity>()
                    .AnyAsync(p => p.TriggerId == trigger.Id
                            && p.EntityType == trigger.EntityType
                            && p.EntityId == entity.Id
                            && p.ProcessedStatus == entity.Status,
                        cancellationToken);

                if (alreadyProcessed)
                {
                    continue;
                }

                WorkflowExecution? execution = null;
                int? executionId = null;

                try
                {
                    execution = await CreateExecutionAsync(db, trigger, entity, notificationService, cancellationToken);
                    executionId = execution.Id;

                    // Mark as processed BEFORE executing to prevent race conditions
                    var processedRecord = new WorkflowProcessedEntity
                    {
                        TriggerId = trigger.Id,
                        EntityType = trigger.EntityType,
                        EntityId = entity.Id,
                        ProcessedStatus = entity.Status,
                        ProcessedAt = DateTime.UtcNow,
                        ExecutionId = execution.Id
                    };
                    db.Set<WorkflowProcessedEntity>().Add(processedRecord);
                    await db.SaveChangesAsync(cancellationToken);

                    var context = new WorkflowExecutionContext
                    {
                        WorkflowId = trigger.WorkflowId,
                        ExecutionId = execution.Id,
                        TriggerEntityType = trigger.EntityType,
                        TriggerEntityId = entity.Id,
                        UserId = "manual-reconcile",
                        Variables = new Dictionary<string, object?>
                        {
                            ["oldStatus"] = entity.Status,
                            ["newStatus"] = entity.Status,
                            ["entityId"] = entity.Id,
                            ["entityType"] = trigger.EntityType,
                            ["triggerSource"] = "manual-reconcile",
                            ["additionalContext"] = entity.Context
                        }
                    };

                    await PopulateRelatedEntityIdsAsync(db, trigger.EntityType, entity.Id, entity.Context, context.Variables, cancellationToken);

                    var execResult = await graphExecutor.ExecuteGraphAsync(
                        trigger.WorkflowId,
                        execution.Id,
                        trigger.NodeId,
                        context);

                    execution.Status = execResult.FinalStatus;
                    execution.Error = Truncate(execResult.Error, 1000);
                    if (execResult.FinalStatus == "completed" || execResult.FinalStatus == "failed")
                    {
                        execution.CompletedAt = DateTime.UtcNow;
                    }

                    // Persist execution result (defensive: schema mismatches / failed entity updates can poison ChangeTracker)
                    try
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx,
                            "[WORKFLOW-RECONCILE] Failed to persist execution status for execution #{ExecutionId}. Retrying with a clean ChangeTracker.",
                            execution.Id);

                        var execId = execution.Id;
                        var finalStatus = execution.Status;
                        var finalError = execution.Error;
                        var completedAt = execution.CompletedAt;

                        db.ChangeTracker.Clear();

                        var freshExecution = await db.WorkflowExecutions
                            .FirstOrDefaultAsync(e => e.Id == execId, cancellationToken);

                        if (freshExecution != null)
                        {
                            freshExecution.Status = finalStatus;
                            freshExecution.Error = finalError;
                            if (finalStatus == "completed" || finalStatus == "failed")
                            {
                                freshExecution.CompletedAt = completedAt ?? DateTime.UtcNow;
                            }

                            await db.SaveChangesAsync(cancellationToken);
                        }
                    }

                    await notificationService.NotifyExecutionCompletedAsync(
                        trigger.WorkflowId,
                        execution.Id,
                        execResult.FinalStatus,
                        execResult.NodesExecuted,
                        execResult.NodesFailed);

                    triggered++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[WORKFLOW-RECONCILE] Failed to execute workflow for {EntityType} #{EntityId}",
                        trigger.EntityType,
                        entity.Id);

                    // Best-effort cleanup:
                    // 1) mark execution failed
                    // 2) remove processed marker so the entity can be retried after config/schema fixes
                    try
                    {
                        var now = DateTime.UtcNow;

                        db.ChangeTracker.Clear();

                        var markers = await db.Set<WorkflowProcessedEntity>()
                            .Where(p => p.TriggerId == trigger.Id
                                && p.EntityType == trigger.EntityType
                                && p.EntityId == entity.Id
                                && p.ProcessedStatus == entity.Status)
                            .ToListAsync(cancellationToken);

                        if (markers.Any())
                        {
                            db.Set<WorkflowProcessedEntity>().RemoveRange(markers);
                        }

                        if (executionId.HasValue)
                        {
                            var exec = await db.WorkflowExecutions
                                .FirstOrDefaultAsync(e => e.Id == executionId.Value, cancellationToken);

                            if (exec != null)
                            {
                                exec.Status = "failed";
                                exec.Error = Truncate(ex.Message, 1000);
                                exec.CompletedAt = now;
                            }
                        }

                        await db.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogWarning(innerEx,
                            "[WORKFLOW-RECONCILE] Failed to persist failure/cleanup for {EntityType} #{EntityId}",
                            trigger.EntityType,
                            entity.Id);
                    }
                }
            }

            return (processed, triggered);
        }

        private async Task<List<EntityStatusInfo>> GetEntitiesWithStatusAsync(
            ApplicationDbContext db,
            string entityType,
            string? targetStatus,
            CancellationToken cancellationToken)
        {
            var results = new List<EntityStatusInfo>();

            switch (entityType.ToLower())
            {
                case "dispatch":
                    var dispatches = await db.Dispatches
                        .AsNoTracking()
                        .Where(d => !d.IsDeleted && (targetStatus == null || d.Status == targetStatus))
                        .Select(d => new { d.Id, d.Status, d.ServiceOrderId, d.JobId })
                        .ToListAsync(cancellationToken);

                    results.AddRange(dispatches.Select(d => new EntityStatusInfo
                    {
                        Id = d.Id,
                        Status = d.Status ?? "",
                        Context = new { d.ServiceOrderId, d.JobId }
                    }));
                    break;

                case "service_order":
                    var serviceOrders = await db.ServiceOrders
                        .AsNoTracking()
                        .Where(so => targetStatus == null || so.Status == targetStatus)
                        .Select(so => new { so.Id, so.Status, so.SaleId })
                        .ToListAsync(cancellationToken);

                    results.AddRange(serviceOrders.Select(so => new EntityStatusInfo
                    {
                        Id = so.Id,
                        Status = so.Status ?? "",
                        Context = new { so.SaleId }
                    }));
                    break;

                case "sale":
                    var sales = await db.Sales
                        .AsNoTracking()
                        .Where(s => targetStatus == null || s.Status == targetStatus)
                        .Select(s => new { s.Id, s.Status, s.OfferId })
                        .ToListAsync(cancellationToken);

                    results.AddRange(sales.Select(s => new EntityStatusInfo
                    {
                        Id = s.Id,
                        Status = s.Status ?? "",
                        Context = new { s.OfferId }
                    }));
                    break;

                case "offer":
                    var offers = await db.Offers
                        .AsNoTracking()
                        .Where(o => targetStatus == null || o.Status == targetStatus)
                        .Select(o => new { o.Id, o.Status })
                        .ToListAsync(cancellationToken);

                    results.AddRange(offers.Select(o => new EntityStatusInfo
                    {
                        Id = o.Id,
                        Status = o.Status ?? "",
                        Context = null
                    }));
                    break;
            }

            return results;
        }

        private async Task<WorkflowExecution> CreateExecutionAsync(
            ApplicationDbContext db,
            WorkflowTrigger trigger,
            EntityStatusInfo entity,
            IWorkflowNotificationService notificationService,
            CancellationToken cancellationToken)
        {
            var execution = new WorkflowExecution
            {
                WorkflowId = trigger.WorkflowId,
                TriggerEntityType = trigger.EntityType,
                TriggerEntityId = entity.Id,
                Status = "running",
                CurrentNodeId = trigger.NodeId,
                Context = JsonSerializer.Serialize(new
                {
                    entityType = trigger.EntityType,
                    entityId = entity.Id,
                    currentStatus = entity.Status,
                    triggerSource = "manual-reconcile",
                    triggeredAt = DateTime.UtcNow,
                    additionalContext = entity.Context
                }),
                StartedAt = DateTime.UtcNow,
                TriggeredBy = User?.Identity?.Name ?? "manual-reconcile"
            };

            db.WorkflowExecutions.Add(execution);
            await db.SaveChangesAsync(cancellationToken);

            await notificationService.NotifyExecutionStartedAsync(
                trigger.WorkflowId,
                execution.Id,
                trigger.EntityType,
                entity.Id,
                "manual-reconcile");

            // Log trigger node as completed
            var triggerLog = new WorkflowExecutionLog
            {
                ExecutionId = execution.Id,
                NodeId = trigger.NodeId,
                NodeType = "status-trigger",
                Status = "completed",
                Input = JsonSerializer.Serialize(new { currentStatus = entity.Status, source = "manual-reconcile" }),
                Output = JsonSerializer.Serialize(new { triggered = true }),
                Timestamp = DateTime.UtcNow
            };

            db.WorkflowExecutionLogs.Add(triggerLog);
            await db.SaveChangesAsync(cancellationToken);

            await notificationService.NotifyNodeCompletedAsync(
                trigger.WorkflowId,
                execution.Id,
                trigger.NodeId,
                "status-trigger",
                true,
                null,
                JsonSerializer.Serialize(new { triggered = true, source = "manual-reconcile" }));

            return execution;
        }

        private async Task PopulateRelatedEntityIdsAsync(
            ApplicationDbContext db,
            string entityType,
            int entityId,
            object? entityContext,
            Dictionary<string, object?> variables,
            CancellationToken cancellationToken)
        {
            try
            {
                switch (entityType.ToLower())
                {
                    case "dispatch":
                        // Dispatch → ServiceOrder → Sale → Offer
                        var dispatch = await db.Dispatches.FindAsync(new object[] { entityId }, cancellationToken);
                        if (dispatch?.ServiceOrderId != null)
                        {
                            variables["serviceOrderId"] = dispatch.ServiceOrderId.Value;

                            var so = await db.ServiceOrders.FindAsync(new object[] { dispatch.ServiceOrderId.Value }, cancellationToken);
                            if (so?.SaleId != null && int.TryParse(so.SaleId, out var saleId))
                            {
                                variables["saleId"] = saleId;

                                var sale = await db.Sales.FindAsync(new object[] { saleId }, cancellationToken);
                                if (sale?.OfferId != null && int.TryParse(sale.OfferId, out var offerId))
                                {
                                    variables["offerId"] = offerId;
                                }
                            }
                        }
                        break;

                    case "service_order":
                        // ServiceOrder → Sale → Offer
                        var serviceOrder = await db.ServiceOrders.FindAsync(new object[] { entityId }, cancellationToken);
                        if (serviceOrder?.SaleId != null && int.TryParse(serviceOrder.SaleId, out var soSaleId))
                        {
                            variables["saleId"] = soSaleId;

                            var soSale = await db.Sales.FindAsync(new object[] { soSaleId }, cancellationToken);
                            if (soSale?.OfferId != null && int.TryParse(soSale.OfferId, out var soOfferId))
                            {
                                variables["offerId"] = soOfferId;
                            }
                        }
                        break;

                    case "sale":
                        // Sale → Offer + ServiceOrder
                        var sale2 = await db.Sales.FindAsync(new object[] { entityId }, cancellationToken);
                        if (sale2?.OfferId != null && int.TryParse(sale2.OfferId, out var saleOfferId))
                        {
                            variables["offerId"] = saleOfferId;
                        }

                        var relatedSo = await db.ServiceOrders
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.SaleId == entityId.ToString(), cancellationToken);

                        if (relatedSo != null)
                        {
                            variables["serviceOrderId"] = relatedSo.Id;
                        }
                        break;

                    case "offer":
                        // Offer → Sale → ServiceOrder
                        var relatedSale = await db.Sales
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.OfferId == entityId.ToString(), cancellationToken);

                        if (relatedSale != null)
                        {
                            variables["saleId"] = relatedSale.Id;

                            var offerSo = await db.ServiceOrders
                                .AsNoTracking()
                                .FirstOrDefaultAsync(s => s.SaleId == relatedSale.Id.ToString(), cancellationToken);

                            if (offerSo != null)
                            {
                                variables["serviceOrderId"] = offerSo.Id;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[WORKFLOW-RECONCILE] Failed to pre-populate related entity IDs for {EntityType}#{EntityId}",
                    entityType,
                    entityId);
            }
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private class EntityStatusInfo
        {
            public int Id { get; set; }
            public string Status { get; set; } = "";
            public object? Context { get; set; }
        }
    }
}
