using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyApi.Data;
using MyApi.Modules.WorkflowEngine.Models;
using System.Text.Json;

namespace MyApi.Modules.WorkflowEngine.Services
{
    /// <summary>
    /// Background service that polls entity states every 5 minutes
    /// and triggers workflows based on CURRENT status (state-based),
    /// not just status transitions (event-based).
    /// </summary>
    public class WorkflowPollingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WorkflowPollingService> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(5);

        public WorkflowPollingService(
            IServiceProvider serviceProvider,
            ILogger<WorkflowPollingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[WORKFLOW-POLLING] Starting workflow polling service. Interval: {Interval} minutes", 
                _pollingInterval.TotalMinutes);

            // Wait a bit before first run to let the app fully start
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollAndTriggerWorkflowsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[WORKFLOW-POLLING] Error during polling cycle");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }

            _logger.LogInformation("[WORKFLOW-POLLING] Workflow polling service stopped");
        }

        private async Task PollAndTriggerWorkflowsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[WORKFLOW-POLLING] ═══════════════════════════════════════════════════════════════");
            _logger.LogInformation("[WORKFLOW-POLLING] Starting polling cycle at {Time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IWorkflowNotificationService>();
            var graphExecutor = scope.ServiceProvider.GetRequiredService<IWorkflowGraphExecutor>();

            // Get all active triggers with their workflows
            var triggers = await db.WorkflowTriggers
                .Include(t => t.Workflow)
                .Where(t => t.IsActive 
                    && t.Workflow != null 
                    && t.Workflow.IsActive 
                    && !t.Workflow.IsDeleted)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("[WORKFLOW-POLLING] Found {Count} active triggers to check", triggers.Count);

            int totalProcessed = 0;
            int totalTriggered = 0;

            foreach (var trigger in triggers)
            {
                try
                {
                    var (processed, triggered) = await ProcessTriggerAsync(
                        db, trigger, graphExecutor, notificationService, cancellationToken);
                    totalProcessed += processed;
                    totalTriggered += triggered;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[WORKFLOW-POLLING] Error processing trigger {TriggerId} (Node: {NodeId})", 
                        trigger.Id, trigger.NodeId);
                }
            }

            _logger.LogInformation("[WORKFLOW-POLLING] Polling cycle complete. Entities checked: {Processed}, Workflows triggered: {Triggered}",
                totalProcessed, totalTriggered);
            _logger.LogInformation("[WORKFLOW-POLLING] ═══════════════════════════════════════════════════════════════");
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

            _logger.LogInformation(
                "[WORKFLOW-POLLING] Processing trigger {TriggerId}: {EntityType} -> '{ToStatus}' (from: '{FromStatus}')",
                trigger.Id, trigger.EntityType, trigger.ToStatus ?? "ANY", trigger.FromStatus ?? "ANY");

            // Get entities that match the trigger's toStatus (current status check)
            var matchingEntities = await GetEntitiesWithStatusAsync(db, trigger.EntityType, trigger.ToStatus, cancellationToken);
            
            _logger.LogInformation(
                "[WORKFLOW-POLLING] Found {Count} {EntityType} entities with status '{Status}'",
                matchingEntities.Count, trigger.EntityType, trigger.ToStatus ?? "ANY");

            foreach (var entity in matchingEntities)
            {
                processed++;

                // Check if this entity has already been processed by this trigger for this status
                var alreadyProcessed = await db.Set<WorkflowProcessedEntity>()
                    .AnyAsync(p => p.TriggerId == trigger.Id 
                        && p.EntityType == trigger.EntityType 
                        && p.EntityId == entity.Id 
                        && p.ProcessedStatus == entity.Status, 
                        cancellationToken);

                if (alreadyProcessed)
                {
                    _logger.LogDebug(
                        "[WORKFLOW-POLLING] Skipping {EntityType} #{EntityId} - already processed for status '{Status}'",
                        trigger.EntityType, entity.Id, entity.Status);
                    continue;
                }

                _logger.LogInformation(
                    "[WORKFLOW-POLLING] 🚀 Triggering workflow for {EntityType} #{EntityId} (Status: '{Status}')",
                    trigger.EntityType, entity.Id, entity.Status);

                WorkflowExecution? execution = null;
                int? executionId = null;

                try
                {
                    // Create workflow execution
                    execution = await CreateExecutionAsync(db, trigger, entity, notificationService);
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

                    // Execute the workflow graph
                    var context = new WorkflowExecutionContext
                    {
                        WorkflowId = trigger.WorkflowId,
                        ExecutionId = execution.Id,
                        TriggerEntityType = trigger.EntityType,
                        TriggerEntityId = entity.Id,
                        UserId = "system-polling",
                        Variables = new Dictionary<string, object?>
                        {
                            ["oldStatus"] = entity.Status, // For state-based, old = current (we don't know the real old)
                            ["newStatus"] = entity.Status,
                            ["entityId"] = entity.Id,
                            ["entityType"] = trigger.EntityType,
                            ["triggerSource"] = "polling",
                            ["additionalContext"] = entity.Context
                        }
                    };

                    // Pre-populate related entity IDs in context for faster lookups
                    await PopulateRelatedEntityIdsAsync(db, trigger.EntityType, entity.Id, entity.Context, context.Variables);

                    var result = await graphExecutor.ExecuteGraphAsync(
                        trigger.WorkflowId,
                        execution.Id,
                        trigger.NodeId,
                        context);

                    // Update execution with result
                    execution.Status = result.FinalStatus;
                    execution.Error = Truncate(result.Error, 1000);
                    if (result.FinalStatus == "completed" || result.FinalStatus == "failed")
                    {
                        execution.CompletedAt = DateTime.UtcNow;
                    }

                    // IMPORTANT: workflow nodes may have attempted entity updates that failed (schema mismatch, etc.)
                    // If the DbContext is in a bad state, saving the execution update can throw.
                    try
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx,
                            "[WORKFLOW-POLLING] Failed to persist execution status for execution #{ExecutionId}. Retrying with a clean ChangeTracker.",
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
                        result.FinalStatus,
                        result.NodesExecuted,
                        result.NodesFailed);

                    _logger.LogInformation(
                        "[WORKFLOW-POLLING] ✅ Workflow completed for {EntityType} #{EntityId}: {Status} ({Executed} nodes executed)",
                        trigger.EntityType, entity.Id, result.FinalStatus, result.NodesExecuted);

                    triggered++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[WORKFLOW-POLLING] ❌ Failed to execute workflow for {EntityType} #{EntityId}",
                        trigger.EntityType, entity.Id);

                    // Best-effort cleanup:
                    // 1) mark execution failed (so debug console doesn't show perpetual 'running')
                    // 2) remove processed marker so polling can retry after config/schema fixes
                    try
                    {
                        var now = DateTime.UtcNow;

                        db.ChangeTracker.Clear();

                        // Remove processed marker(s) so this status can be retried later
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
                            "[WORKFLOW-POLLING] Failed to persist failure/cleanup for {EntityType} #{EntityId}",
                            trigger.EntityType, entity.Id);
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
            IWorkflowNotificationService notificationService)
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
                    triggerSource = "polling",
                    triggeredAt = DateTime.UtcNow,
                    additionalContext = entity.Context
                }),
                StartedAt = DateTime.UtcNow,
                TriggeredBy = "system-polling"
            };

            db.WorkflowExecutions.Add(execution);
            await db.SaveChangesAsync();

            await notificationService.NotifyExecutionStartedAsync(
                trigger.WorkflowId,
                execution.Id,
                trigger.EntityType,
                entity.Id,
                "system-polling");

            // Log trigger node as completed
            var triggerLog = new WorkflowExecutionLog
            {
                ExecutionId = execution.Id,
                NodeId = trigger.NodeId,
                NodeType = "status-trigger",
                Status = "completed",
                Input = JsonSerializer.Serialize(new { currentStatus = entity.Status, source = "polling" }),
                Output = JsonSerializer.Serialize(new { triggered = true }),
                Timestamp = DateTime.UtcNow
            };
            db.WorkflowExecutionLogs.Add(triggerLog);
            await db.SaveChangesAsync();

            await notificationService.NotifyNodeCompletedAsync(
                trigger.WorkflowId,
                execution.Id,
                trigger.NodeId,
                "status-trigger",
                true,
                null,
                JsonSerializer.Serialize(new { triggered = true, source = "polling" }));

            return execution;
        }
        
        /// <summary>
        /// Pre-populate related entity IDs in the context for faster lookups during workflow execution.
        /// This resolves the full entity chain upfront.
        /// </summary>
        private async Task PopulateRelatedEntityIdsAsync(
            ApplicationDbContext db,
            string entityType,
            int entityId,
            object? entityContext,
            Dictionary<string, object?> variables)
        {
            try
            {
                switch (entityType.ToLower())
                {
                    case "dispatch":
                        // Dispatch → ServiceOrder → Sale → Offer
                        var dispatch = await db.Dispatches.FindAsync(entityId);
                        if (dispatch?.ServiceOrderId != null)
                        {
                            variables["serviceOrderId"] = dispatch.ServiceOrderId.Value;
                            _logger.LogInformation("[WORKFLOW-POLLING] Pre-populated serviceOrderId={Id}", dispatch.ServiceOrderId.Value);
                            
                            var so = await db.ServiceOrders.FindAsync(dispatch.ServiceOrderId.Value);
                            if (so?.SaleId != null && int.TryParse(so.SaleId, out var saleId))
                            {
                                variables["saleId"] = saleId;
                                _logger.LogInformation("[WORKFLOW-POLLING] Pre-populated saleId={Id}", saleId);
                                
                                var sale = await db.Sales.FindAsync(saleId);
                                if (sale?.OfferId != null && int.TryParse(sale.OfferId, out var offerId))
                                {
                                    variables["offerId"] = offerId;
                                    _logger.LogInformation("[WORKFLOW-POLLING] Pre-populated offerId={Id}", offerId);
                                }
                            }
                        }
                        break;
                        
                    case "service_order":
                        // ServiceOrder → Sale → Offer
                        var serviceOrder = await db.ServiceOrders.FindAsync(entityId);
                        if (serviceOrder?.SaleId != null && int.TryParse(serviceOrder.SaleId, out var soSaleId))
                        {
                            variables["saleId"] = soSaleId;
                            _logger.LogInformation("[WORKFLOW-POLLING] Pre-populated saleId={Id}", soSaleId);
                            
                            var soSale = await db.Sales.FindAsync(soSaleId);
                            if (soSale?.OfferId != null && int.TryParse(soSale.OfferId, out var soOfferId))
                            {
                                variables["offerId"] = soOfferId;
                                _logger.LogInformation("[WORKFLOW-POLLING] Pre-populated offerId={Id}", soOfferId);
                            }
                        }
                        break;
                        
                    case "sale":
                        // Sale → Offer
                        var sale2 = await db.Sales.FindAsync(entityId);
                        if (sale2?.OfferId != null && int.TryParse(sale2.OfferId, out var saleOfferId))
                        {
                            variables["offerId"] = saleOfferId;
                            _logger.LogInformation("[WORKFLOW-POLLING] Pre-populated offerId={Id}", saleOfferId);
                        }
                        // Sale → ServiceOrder (if exists)
                        var relatedSo = await db.ServiceOrders.FirstOrDefaultAsync(s => s.SaleId == entityId.ToString());
                        if (relatedSo != null)
                        {
                            variables["serviceOrderId"] = relatedSo.Id;
                            _logger.LogInformation("[WORKFLOW-POLLING] Pre-populated serviceOrderId={Id}", relatedSo.Id);
                        }
                        break;
                        
                    case "offer":
                        // Offer → Sale → ServiceOrder
                        var relatedSale = await db.Sales.FirstOrDefaultAsync(s => s.OfferId == entityId.ToString());
                        if (relatedSale != null)
                        {
                            variables["saleId"] = relatedSale.Id;
                            _logger.LogInformation("[WORKFLOW-POLLING] Pre-populated saleId={Id}", relatedSale.Id);
                            
                            var offerSo = await db.ServiceOrders.FirstOrDefaultAsync(s => s.SaleId == relatedSale.Id.ToString());
                            if (offerSo != null)
                            {
                                variables["serviceOrderId"] = offerSo.Id;
                                _logger.LogInformation("[WORKFLOW-POLLING] Pre-populated serviceOrderId={Id}", offerSo.Id);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[WORKFLOW-POLLING] Failed to pre-populate related entity IDs for {EntityType}#{EntityId}",
                    entityType, entityId);
            }
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>
        /// Internal class to hold entity status information
        /// </summary>
        private class EntityStatusInfo
        {
            public int Id { get; set; }
            public string Status { get; set; } = "";
            public object? Context { get; set; }
        }
    }
}
