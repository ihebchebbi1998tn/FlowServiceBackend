using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Modules.WorkflowEngine.Models;
using MyApi.Modules.Offers.Models;
using MyApi.Modules.Sales.Models;
using MyApi.Modules.ServiceOrders.Models;
using MyApi.Modules.Dispatches.Models;
using System.Diagnostics;
using System.Text.Json;

namespace MyApi.Modules.WorkflowEngine.Services
{
    public class WorkflowNodeExecutor : IWorkflowNodeExecutor
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<WorkflowNodeExecutor> _logger;
        private readonly IBusinessWorkflowService _businessWorkflowService;

        public WorkflowNodeExecutor(
            ApplicationDbContext db,
            ILogger<WorkflowNodeExecutor> logger,
            IBusinessWorkflowService businessWorkflowService)
        {
            _db = db;
            _logger = logger;
            _businessWorkflowService = businessWorkflowService;
        }

        public async Task<NodeExecutionResult> ExecuteNodeAsync(
            int executionId,
            WorkflowNode node,
            WorkflowExecutionContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new NodeExecutionResult();

            try
            {
                _logger.LogInformation(
                    "Executing node {NodeId} of type {NodeType} for execution {ExecutionId}",
                    node.Id, node.Type, executionId);

                // Route to appropriate handler based on node type
                // Get the actionType from node data for entity nodes
                var actionType = GetNodeDataString(node, "actionType")?.ToLower() ?? "";
                
                result = node.Type.ToLower() switch
                {
                    // Trigger nodes (already fired, just pass through)
                    var t when t.Contains("status-trigger") => await ExecuteTriggerNodeAsync(node, context),
                    var t when t.Contains("trigger") => await ExecuteTriggerNodeAsync(node, context),
                    
                    // Business process nodes (explicit create- prefixed)
                    var t when t.Contains("create-offer") => await ExecuteCreateOfferAsync(node, context),
                    var t when t.Contains("create-sale") => await ExecuteCreateSaleAsync(node, context),
                    var t when t.Contains("create-service") => await ExecuteCreateServiceOrderAsync(node, context),
                    var t when t.Contains("create-dispatch") => await ExecuteCreateDispatchAsync(node, context),
                    
                    // Status update nodes (explicit update- prefixed)
                    var t when t.Contains("update-status") || t.Contains("update-") => await ExecuteUpdateStatusAsync(node, context),
                    
                    // Plain entity names used by frontend action nodes
                    // These are nodes with type "sale", "service-order", "dispatch", "offer"
                    // that have actionType "create" or "update-status" in their data
                    "offer" when actionType == "create" => await ExecuteCreateOfferAsync(node, context),
                    "sale" when actionType == "create" => await ExecuteCreateSaleAsync(node, context),
                    "service-order" when actionType == "create" => await ExecuteCreateServiceOrderAsync(node, context),
                    "service_order" when actionType == "create" => await ExecuteCreateServiceOrderAsync(node, context),
                    "dispatch" when actionType == "create" => await ExecuteCreateDispatchAsync(node, context),
                    // Plain entity names with update-status action
                    "offer" when actionType.Contains("update") => await ExecuteUpdateStatusAsync(node, context),
                    "sale" when actionType.Contains("update") => await ExecuteUpdateStatusAsync(node, context),
                    "service-order" when actionType.Contains("update") => await ExecuteUpdateStatusAsync(node, context),
                    "service_order" when actionType.Contains("update") => await ExecuteUpdateStatusAsync(node, context),
                    "dispatch" when actionType.Contains("update") => await ExecuteUpdateStatusAsync(node, context),
                    // Plain entity names without actionType - default to create
                    "offer" => await ExecuteCreateOfferAsync(node, context),
                    "sale" => await ExecuteCreateSaleAsync(node, context),
                    "service-order" or "service_order" => await ExecuteCreateServiceOrderAsync(node, context),
                    "dispatch" => await ExecuteCreateDispatchAsync(node, context),
                    
                    // Contact node - lookup/pass through contact info
                    "contact" => await ExecuteContactNodeAsync(node, context),
                    
                    // Condition nodes
                    var t when t.Contains("condition") || t.Contains("if-") => await ExecuteConditionNodeAsync(node, context),
                    var t when t.Contains("switch") => await ExecuteSwitchNodeAsync(node, context),
                    
                    // Action nodes
                    var t when t.Contains("send-email") || t.Contains("email") => await ExecuteSendEmailAsync(node, context),
                    var t when t.Contains("send-sms") || t.Contains("sms") => await ExecuteSendSmsAsync(node, context),
                    var t when t.Contains("notification") => await ExecuteNotificationAsync(node, context),
                    
                    // Integration nodes
                    var t when t.Contains("api") || t.Contains("http") => await ExecuteApiCallAsync(node, context),
                    var t when t.Contains("webhook") => await ExecuteWebhookAsync(node, context),
                    
                    // Approval nodes
                    var t when t.Contains("approval") => await ExecuteApprovalAsync(node, context, executionId),
                    
                    // Delay nodes
                    var t when t.Contains("delay") || t.Contains("wait") => await ExecuteDelayAsync(node, context),
                    
                    // AI nodes
                    var t when t.Contains("ai-") || t.Contains("llm") => await ExecuteAiNodeAsync(node, context),
                    
                    // Loop nodes
                    var t when t.Contains("loop") => await ExecuteLoopNodeAsync(node, context),
                    
                    // Calculation nodes
                    var t when t.Contains("calculate") || t.Contains("math") => await ExecuteCalculationAsync(node, context),
                    var t when t.Contains("set-variable") || t.Contains("assign") => await ExecuteSetVariableAsync(node, context),
                    
                    // Database nodes
                    var t when t.Contains("database") || t.Contains("db-") => await ExecuteDatabaseOperationAsync(node, context),
                    
                    // Parallel node
                    var t when t.Contains("parallel") => await ExecuteParallelNodeAsync(node, context),
                    
                    // Try-catch node
                    var t when t.Contains("try-catch") || t.Contains("error") => await ExecuteTryCatchNodeAsync(node, context),
                    
                    // Scheduled trigger (already fired, pass through like other triggers)
                    var t when t.Contains("scheduled") => await ExecuteTriggerNodeAsync(node, context),
                    
                    // Default - just pass through
                    _ => await ExecuteDefaultNodeAsync(node, context)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing node {NodeId} of type {NodeType}", node.Id, node.Type);
                result.Success = false;
                result.Status = "failed";
                result.Error = ex.Message;
            }

            stopwatch.Stop();
            result.DurationMs = (int)stopwatch.ElapsedMilliseconds;

            return result;
        }

        public async Task<bool> EvaluateConditionAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var result = await ExecuteConditionNodeAsync(node, context);
            return result.SelectedBranch == "true" || result.SelectedBranch == "yes";
        }

        #region Trigger Nodes

        private Task<NodeExecutionResult> ExecuteTriggerNodeAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            // Trigger nodes have already fired - they just pass data through
            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["triggered"] = true,
                    ["entityType"] = context.TriggerEntityType,
                    ["entityId"] = context.TriggerEntityId
                }
            });
        }

        #endregion

        #region Business Process Nodes

        private Task<NodeExecutionResult> ExecuteCreateOfferAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            // This would create an offer based on node configuration
            // For now, log and return success
            _logger.LogInformation("Create Offer node executed for context {EntityType}:{EntityId}", 
                context.TriggerEntityType, context.TriggerEntityId);

            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "create_offer",
                    ["status"] = "simulated"
                }
            });
        }

        private async Task<NodeExecutionResult> ExecuteCreateSaleAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            // If triggered by an offer, create sale from offer
            if (context.TriggerEntityType == "offer")
            {
                var saleId = await _businessWorkflowService.HandleOfferAcceptedAsync(
                    context.TriggerEntityId, 
                    context.UserId);

                if (saleId.HasValue)
                {
                    return new NodeExecutionResult
                    {
                        Success = true,
                        Status = "completed",
                        CreatedEntityId = saleId.Value,
                        CreatedEntityType = "sale",
                        Output = new Dictionary<string, object?>
                        {
                            ["action"] = "create_sale",
                            ["saleId"] = saleId.Value,
                            ["fromOfferId"] = context.TriggerEntityId
                        }
                    };
                }
            }

            return new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "create_sale",
                    ["status"] = "skipped",
                    ["reason"] = "No offer context or sale already exists"
                }
            };
        }

        private async Task<NodeExecutionResult> ExecuteCreateServiceOrderAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            // If triggered by a sale, create service order
            if (context.TriggerEntityType == "sale")
            {
                // Extract service order config from context if available
                object? serviceOrderConfig = null;
                if (context.Variables?.TryGetValue("additionalContext", out var additionalContext) == true && additionalContext != null)
                {
                    try
                    {
                        // The additionalContext may contain serviceOrderConfig
                        var contextJson = System.Text.Json.JsonSerializer.Serialize(additionalContext);
                        var contextDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(contextJson);
                        
                        if (contextDict != null && 
                            (contextDict.TryGetValue("serviceOrderConfig", out var configVal) || 
                             contextDict.TryGetValue("ServiceOrderConfig", out configVal)))
                        {
                            serviceOrderConfig = configVal;
                            _logger.LogInformation("ExecuteCreateServiceOrderAsync: Found serviceOrderConfig in context");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "ExecuteCreateServiceOrderAsync: Failed to parse additionalContext");
                    }
                }
                
                var serviceOrderId = await _businessWorkflowService.HandleSaleInProgressAsync(
                    context.TriggerEntityId, 
                    context.UserId,
                    serviceOrderConfig);

                if (serviceOrderId.HasValue)
                {
                    return new NodeExecutionResult
                    {
                        Success = true,
                        Status = "completed",
                        CreatedEntityId = serviceOrderId.Value,
                        CreatedEntityType = "service_order",
                        Output = new Dictionary<string, object?>
                        {
                            ["action"] = "create_service_order",
                            ["serviceOrderId"] = serviceOrderId.Value,
                            ["fromSaleId"] = context.TriggerEntityId,
                            ["usedConfig"] = serviceOrderConfig != null
                        }
                    };
                }
            }

            return new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "create_service_order",
                    ["status"] = "skipped",
                    ["reason"] = "No sale context or no service items"
                }
            };
        }

        private async Task<NodeExecutionResult> ExecuteCreateDispatchAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            // If triggered by a service order, create dispatches
            if (context.TriggerEntityType == "service_order")
            {
                var dispatchIds = await _businessWorkflowService.HandleServiceOrderScheduledAsync(
                    context.TriggerEntityId, 
                    context.UserId);

                var ids = dispatchIds.ToList();
                if (ids.Any())
                {
                    return new NodeExecutionResult
                    {
                        Success = true,
                        Status = "completed",
                        CreatedEntityId = ids.First(),
                        CreatedEntityType = "dispatch",
                        Output = new Dictionary<string, object?>
                        {
                            ["action"] = "create_dispatch",
                            ["dispatchIds"] = ids,
                            ["count"] = ids.Count,
                            ["fromServiceOrderId"] = context.TriggerEntityId
                        }
                    };
                }
            }

            return new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "create_dispatch",
                    ["status"] = "skipped",
                    ["reason"] = "No service order context or dispatches already exist"
                }
            };
        }

        private async Task<NodeExecutionResult> ExecuteUpdateStatusAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            _logger.LogInformation(
                "[WORKFLOW-UPDATE-STATUS] Executing update-status node: {NodeId} (Label: {Label}, Type: {Type})",
                node.Id, node.Label, node.Type);
            
            // Try explicit entityType from node data first, then infer from node.Type, then fall back to trigger context
            var entityType = GetNodeDataString(node, "entityType") 
                          ?? InferEntityTypeFromNodeType(node.Type) 
                          ?? context.TriggerEntityType;
            // Check multiple locations for newStatus (top-level and nested in config)
            var newStatus = GetNodeDataString(node, "newStatus") 
                         ?? GetNodeDataString(node, "status")
                         ?? GetConfigString(node, "newStatus")
                         ?? GetConfigString(node, "status");

            _logger.LogInformation(
                "[WORKFLOW-UPDATE-STATUS] Parsed: EntityType='{EntityType}', NewStatus='{NewStatus}', TriggerContext={TriggerType}#{TriggerId}",
                entityType, newStatus ?? "NULL", context.TriggerEntityType, context.TriggerEntityId);

            if (string.IsNullOrEmpty(newStatus))
            {
                _logger.LogError(
                    "[WORKFLOW-UPDATE-STATUS] No status specified in node {NodeId}. Cannot update.",
                    node.Id);
                return new NodeExecutionResult
                {
                    Success = false,
                    Status = "failed",
                    Error = "No status specified in update-status node"
                };
            }

            // CRITICAL: Resolve the correct entity ID for the target entity type
            // If we're updating a different entity than what triggered the workflow,
            // look up the correct ID from context variables (set by earlier create nodes)
            var entityId = context.TriggerEntityId;
            
            if (entityType != context.TriggerEntityType)
            {
                _logger.LogInformation(
                    "[WORKFLOW-UPDATE-STATUS] Target entity type '{TargetType}' differs from trigger type '{TriggerType}'. Resolving related ID...",
                    entityType, context.TriggerEntityType);
                
                // Check if a previous node created this entity type and stored its ID
                var createdKey = $"created_{entityType}_id";
                if (context.Variables.TryGetValue(createdKey, out var createdId) && createdId != null)
                {
                    if (createdId is int intId) entityId = intId;
                    else if (createdId is double dblId) entityId = (int)dblId;
                    else if (int.TryParse(createdId.ToString(), out var parsedId)) entityId = parsedId;
                    
                    _logger.LogInformation(
                        "[WORKFLOW-UPDATE-STATUS] Found created {EntityType} ID in context: {EntityId}",
                        entityType, entityId);
                }
                else
                {
                    // Try to find the related entity through DB relationships
                    var resolvedId = await ResolveRelatedEntityIdAsync(context.TriggerEntityType, context.TriggerEntityId, entityType);
                    _logger.LogInformation(
                        "[WORKFLOW-UPDATE-STATUS] Resolved {EntityType} ID via DB: {ResolvedId} (from {TriggerType}#{TriggerId})",
                        entityType, resolvedId, context.TriggerEntityType, context.TriggerEntityId);
                    entityId = resolvedId;
                }
            }

            _logger.LogInformation(
                "[WORKFLOW-UPDATE-STATUS] Updating {EntityType} #{EntityId} to status '{NewStatus}'",
                entityType, entityId, newStatus);

            // Update the entity status
            var success = entityType switch
            {
                "offer" => await UpdateOfferStatusAsync(entityId, newStatus, context.UserId),
                "sale" => await UpdateSaleStatusAsync(entityId, newStatus, context.UserId),
                "service_order" => await UpdateServiceOrderStatusAsync(entityId, newStatus, context.UserId),
                "dispatch" => await UpdateDispatchStatusAsync(entityId, newStatus, context.UserId),
                _ => false
            };

            return new NodeExecutionResult
            {
                Success = success,
                Status = success ? "completed" : "failed",
                Error = success ? null : $"Failed to update {entityType} #{entityId} status to {newStatus}",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "update_status",
                    ["entityType"] = entityType,
                    ["entityId"] = entityId,
                    ["newStatus"] = newStatus
                }
            };
        }
        
        /// <summary>
        /// Gets a string value directly from node.data.config.{key}
        /// </summary>
        private string? GetConfigString(WorkflowNode node, string key)
        {
            if (node.Data.TryGetValue("config", out var configValue) && configValue is JsonElement configEl && configEl.ValueKind == JsonValueKind.Object)
            {
                if (configEl.TryGetProperty(key, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String) return prop.GetString();
                    if (prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Object && prop.ValueKind != JsonValueKind.Array)
                        return prop.ToString();
                }
            }
            return null;
        }

        #endregion

        #region Condition Nodes

        private async Task<NodeExecutionResult> ExecuteConditionNodeAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            _logger.LogInformation(
                "[WORKFLOW-CONDITION] Evaluating condition node: {NodeId} (Label: {Label})",
                node.Id, node.Label);
            
            // Log all node data for debugging
            _logger.LogInformation(
                "[WORKFLOW-CONDITION] Node data keys: [{Keys}]",
                string.Join(", ", node.Data.Keys));
            
            // Check multiple locations where condition fields could be stored:
            // 1. Top-level: node.data.field (from handlePanelConfigSave spreading)
            // 2. In config: node.data.config.field (NodeConfigPanel saves flat config)
            // 3. In config.condition: node.data.config.condition.field (ConditionalConfigModal saves nested)
            // 4. In config.conditionData: node.data.config.conditionData.field (seed data / DB format)
            var field = GetNodeDataString(node, "field") 
                     ?? GetNodeDataString(node, "conditionField")
                     ?? GetConditionNestedString(node, "field");
            var operatorType = GetNodeDataString(node, "operator") 
                            ?? GetConditionNestedString(node, "operator") 
                            ?? "equals";
            var expectedValue = GetNodeDataValue(node, "value") 
                             ?? GetNodeDataValue(node, "expectedValue")
                             ?? GetConditionNestedValue(node, "value");
            var checkField = GetConditionNestedString(node, "checkField"); // For array operations like all_match

            _logger.LogInformation(
                "[WORKFLOW-CONDITION] Parsed condition: Field='{Field}', Operator='{Operator}', ExpectedValue='{Expected}', CheckField='{CheckField}'",
                field ?? "NULL", operatorType, expectedValue ?? "NULL", checkField ?? "NULL");

            if (string.IsNullOrEmpty(field))
            {
                _logger.LogWarning(
                    "[WORKFLOW-CONDITION] No field specified for condition node {NodeId}. Defaulting to TRUE branch.",
                    node.Id);
                return new NodeExecutionResult
                {
                    Success = true,
                    Status = "completed",
                    SelectedBranch = "true", // Default to true branch if no condition
                    Output = new Dictionary<string, object?> { ["conditionResult"] = true }
                };
            }

            // Special handling for collection-based conditions
            bool result;
            object? actualValue;
            
            // Check if this is a collection field like "sale.items", "serviceOrder.dispatches", "sale.serviceOrders", "offer.sales"
            var fieldLower = field.ToLower();
            var isCollectionField = field.Contains('.') && 
                (fieldLower.EndsWith(".items") || fieldLower.EndsWith(".dispatches") || 
                 fieldLower.EndsWith(".serviceorders") || fieldLower.EndsWith(".service_orders") ||
                 fieldLower.EndsWith(".sales") || fieldLower.EndsWith(".jobs"));
            var isCollectionOperator = operatorType.ToLower() == "all_match" || 
                                       operatorType.ToLower() == "any_match" ||
                                       operatorType.ToLower() == "contains";
            
            if (isCollectionField && isCollectionOperator)
            {
                // For collection fields with collection operators, evaluate the collection directly
                result = await EvaluateCollectionConditionAsync(field, operatorType, expectedValue?.ToString(), checkField, context);
                actualValue = result; // The result is already the boolean
            }
            else
            {
                // Get the actual value from context or entity
                actualValue = await GetFieldValueAsync(field, context);
                // Evaluate the condition
                result = EvaluateCondition(actualValue, operatorType, expectedValue);
            }

            _logger.LogInformation(
                "[WORKFLOW-CONDITION] Condition evaluated: {Field} {Operator} {Expected} = {Result} (actual: {Actual}) -> Branch: {Branch}",
                field, operatorType, expectedValue, result, actualValue, result ? "YES/TRUE" : "NO/FALSE");

            return new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                SelectedBranch = result ? "yes" : "no",
                Output = new Dictionary<string, object?>
                {
                    ["conditionResult"] = result,
                    ["selectedBranch"] = result ? "yes" : "no",
                    ["field"] = field,
                    ["operator"] = operatorType,
                    ["expectedValue"] = expectedValue,
                    ["actualValue"] = actualValue
                }
            };
        }
        
        /// <summary>
        /// Evaluates collection conditions like all_match or any_match
        /// Example: serviceOrder.dispatches all_match status=technically_completed
        /// </summary>
        private async Task<bool> EvaluateCollectionConditionAsync(
            string field, 
            string operatorType, 
            string? expectedValue, 
            string? checkField,
            WorkflowExecutionContext context)
        {
            try
            {
                // Parse the field path (e.g., "serviceOrder.dispatches")
                if (!field.Contains('.'))
                    return false;
                    
                var parts = field.Split('.', 2);
                var entityPrefix = parts[0].ToLower();
                var collectionField = parts[1].ToLower();
                
                // Resolve the entity ID
                int entityId;
                // entityType tracked for future logging/extension
                
                if (entityPrefix == "serviceorder" || entityPrefix == "service_order")
                {
                    _ = "service_order"; // entityType for future use
                    // If triggered by dispatch, get the parent service order
                    if (context.TriggerEntityType == "dispatch")
                    {
                        var dispatch = await _db.Dispatches.FindAsync(context.TriggerEntityId);
                        if (dispatch?.ServiceOrderId == null) return false;
                        entityId = dispatch.ServiceOrderId.Value;
                    }
                    else if (context.TriggerEntityType == "service_order")
                    {
                        entityId = context.TriggerEntityId;
                    }
                    else
                    {
                        // Try to resolve from context variables
                        if (context.Variables.TryGetValue("created_service_order_id", out var soId) && soId is int id)
                            entityId = id;
                        else
                            entityId = await ResolveRelatedEntityIdAsync(context.TriggerEntityType, context.TriggerEntityId, "service_order");
                    }
                }
                else if (entityPrefix == "sale")
                {
                    _ = "sale"; // entityType for future use
                    // If triggered by sale, use the trigger entity ID directly
                    if (context.TriggerEntityType == "sale")
                    {
                        entityId = context.TriggerEntityId;
                    }
                    else if (context.TriggerEntityType == "service_order")
                    {
                        // Resolve sale from service order
                        entityId = await ResolveRelatedEntityIdAsync("service_order", context.TriggerEntityId, "sale");
                    }
                    else if (context.TriggerEntityType == "dispatch")
                    {
                        // Resolve sale from dispatch (via service order)
                        entityId = await ResolveRelatedEntityIdAsync("dispatch", context.TriggerEntityId, "sale");
                    }
                    else
                    {
                        // Try to resolve from context variables
                        if (context.Variables.TryGetValue("created_sale_id", out var saleIdVar) && saleIdVar is int sid)
                            entityId = sid;
                        else
                            entityId = context.TriggerEntityId; // Fallback
                    }
                    
                    _logger.LogInformation(
                        "EvaluateCollectionCondition: Resolved sale ID {SaleId} for sale.items check (trigger: {TriggerType}#{TriggerId})",
                        entityId, context.TriggerEntityType, context.TriggerEntityId);
                }
                else
                {
                    _logger.LogWarning(
                        "EvaluateCollectionCondition: Unknown entity prefix '{EntityPrefix}' in field '{Field}'",
                        entityPrefix, field);
                    return false; // Unknown entity type
                }
                
                // Handle the collection field
                if (collectionField == "dispatches")
                {
                    var dispatches = await _db.Dispatches
                        .Where(d => d.ServiceOrderId == entityId && !d.IsDeleted)
                        .ToListAsync();
                    
                    if (!dispatches.Any())
                    {
                        _logger.LogInformation("EvaluateCollectionCondition: No dispatches found for service order {ServiceOrderId}", entityId);
                        return false; // No items means condition fails
                    }
                    
                    // DYNAMIC: Get target statuses from the expectedValue (workflow configuration)
                    // The user configures what statuses to check in the workflow node
                    // Example: "technically_completed,completed" or just "completed"
                    var targetStatuses = !string.IsNullOrEmpty(expectedValue) 
                        ? expectedValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                       .Select(s => s.Trim().ToLower())
                                       .ToArray()
                        : Array.Empty<string>();
                    
                    if (targetStatuses.Length == 0)
                    {
                        _logger.LogWarning("EvaluateCollectionCondition: No target statuses specified in workflow config for all_match check");
                        return false;
                    }
                    
                    var fieldToCheck = checkField?.ToLower() ?? "status";
                    
                    if (operatorType.ToLower() == "all_match")
                    {
                        if (fieldToCheck == "status")
                        {
                            var allMatch = dispatches.All(d => targetStatuses.Contains(d.Status?.ToLower() ?? ""));
                            _logger.LogInformation(
                                "EvaluateCollectionCondition: all_match check - {MatchCount}/{Total} dispatches match target statuses [{TargetStatuses}]. Result: {Result}",
                                dispatches.Count(d => targetStatuses.Contains(d.Status?.ToLower() ?? "")),
                                dispatches.Count,
                                string.Join(", ", targetStatuses),
                                allMatch);
                            return allMatch;
                        }
                    }
                    else if (operatorType.ToLower() == "any_match")
                    {
                        if (fieldToCheck == "status")
                        {
                            var anyMatch = dispatches.Any(d => targetStatuses.Contains(d.Status?.ToLower() ?? ""));
                            _logger.LogInformation(
                                "EvaluateCollectionCondition: any_match check - dispatches match target statuses [{TargetStatuses}]. Result: {Result}",
                                string.Join(", ", targetStatuses),
                                anyMatch);
                            return anyMatch;
                        }
                    }
                }
                else if (collectionField == "items")
                {
                    // Handle sale.items contains <type> check (e.g., "service", "article", "material")
                    if (entityPrefix == "sale")
                    {
                        var expectedType = expectedValue?.ToLower() ?? "service";
                        
                        _logger.LogInformation(
                            "EvaluateCollectionCondition: Checking if sale {SaleId} contains items of type '{ExpectedType}'",
                            entityId, expectedType);
                        
                        var hasItems = await _db.SaleItems.AnyAsync(si => 
                            si.SaleId == entityId && 
                            (
                                (si.Type != null && si.Type.ToLower() == expectedType) ||
                                (expectedType == "service" && si.RequiresServiceOrder)
                            ));
                        
                        _logger.LogInformation(
                            "EvaluateCollectionCondition: Sale {SaleId} contains items of type '{ExpectedType}': {Result}",
                            entityId, expectedType, hasItems);
                        
                        return hasItems;
                    }
                }
                else if (collectionField == "serviceorders" || collectionField == "service_orders")
                {
                    // Handle sale.serviceOrders all_match/any_match <status> check
                    if (entityPrefix == "sale")
                    {
                        var serviceOrders = await _db.ServiceOrders
                            .Where(so => so.SaleId == entityId.ToString())
                            .ToListAsync();
                        
                        if (!serviceOrders.Any())
                        {
                            _logger.LogInformation("EvaluateCollectionCondition: No service orders found for sale {SaleId}", entityId);
                            return false;
                        }
                        
                        var targetStatuses = !string.IsNullOrEmpty(expectedValue) 
                            ? expectedValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim().ToLower())
                                           .ToArray()
                            : Array.Empty<string>();
                        
                        if (targetStatuses.Length == 0) return false;
                        
                        var fieldToCheck = checkField?.ToLower() ?? "status";
                        
                        if (operatorType.ToLower() == "all_match")
                        {
                            if (fieldToCheck == "status")
                            {
                                var allMatch = serviceOrders.All(so => targetStatuses.Contains(so.Status?.ToLower() ?? ""));
                                _logger.LogInformation(
                                    "EvaluateCollectionCondition: all_match check - {MatchCount}/{Total} service orders match [{Statuses}]. Result: {Result}",
                                    serviceOrders.Count(so => targetStatuses.Contains(so.Status?.ToLower() ?? "")),
                                    serviceOrders.Count,
                                    string.Join(", ", targetStatuses),
                                    allMatch);
                                return allMatch;
                            }
                        }
                        else if (operatorType.ToLower() == "any_match" || operatorType.ToLower() == "contains")
                        {
                            if (fieldToCheck == "status")
                            {
                                var anyMatch = serviceOrders.Any(so => targetStatuses.Contains(so.Status?.ToLower() ?? ""));
                                _logger.LogInformation(
                                    "EvaluateCollectionCondition: any_match check - service orders match [{Statuses}]. Result: {Result}",
                                    string.Join(", ", targetStatuses),
                                    anyMatch);
                                return anyMatch;
                            }
                        }
                    }
                }
                else if (collectionField == "sales")
                {
                    // Handle offer.sales all_match/any_match <status> check
                    if (entityPrefix == "offer")
                    {
                        var sales = await _db.Sales
                            .Where(s => s.OfferId == entityId.ToString())
                            .ToListAsync();
                        
                        if (!sales.Any())
                        {
                            _logger.LogInformation("EvaluateCollectionCondition: No sales found for offer {OfferId}", entityId);
                            return false;
                        }
                        
                        var targetStatuses = !string.IsNullOrEmpty(expectedValue) 
                            ? expectedValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim().ToLower())
                                           .ToArray()
                            : Array.Empty<string>();
                        
                        if (targetStatuses.Length == 0) return false;
                        
                        var fieldToCheck = checkField?.ToLower() ?? "status";
                        
                        if (operatorType.ToLower() == "all_match")
                        {
                            if (fieldToCheck == "status")
                            {
                                var allMatch = sales.All(s => targetStatuses.Contains(s.Status?.ToLower() ?? ""));
                                _logger.LogInformation(
                                    "EvaluateCollectionCondition: all_match check - {MatchCount}/{Total} sales match [{Statuses}]. Result: {Result}",
                                    sales.Count(s => targetStatuses.Contains(s.Status?.ToLower() ?? "")),
                                    sales.Count,
                                    string.Join(", ", targetStatuses),
                                    allMatch);
                                return allMatch;
                            }
                        }
                        else if (operatorType.ToLower() == "any_match" || operatorType.ToLower() == "contains")
                        {
                            if (fieldToCheck == "status")
                            {
                                var anyMatch = sales.Any(s => targetStatuses.Contains(s.Status?.ToLower() ?? ""));
                                _logger.LogInformation(
                                    "EvaluateCollectionCondition: any_match check - sales match [{Statuses}]. Result: {Result}",
                                    string.Join(", ", targetStatuses),
                                    anyMatch);
                                return anyMatch;
                            }
                        }
                    }
                }
                else if (collectionField == "jobs")
                {
                    // Handle serviceOrder.jobs all_match/any_match <status> check
                    if (entityPrefix == "serviceorder" || entityPrefix == "service_order")
                    {
                        var jobs = await _db.ServiceOrderJobs
                            .Where(j => j.ServiceOrderId == entityId)
                            .ToListAsync();
                        
                        if (!jobs.Any())
                        {
                            _logger.LogInformation("EvaluateCollectionCondition: No jobs found for service order {ServiceOrderId}", entityId);
                            return false;
                        }
                        
                        var targetStatuses = !string.IsNullOrEmpty(expectedValue) 
                            ? expectedValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim().ToLower())
                                           .ToArray()
                            : Array.Empty<string>();
                        
                        if (targetStatuses.Length == 0) return false;
                        
                        var fieldToCheck = checkField?.ToLower() ?? "status";
                        
                        if (operatorType.ToLower() == "all_match")
                        {
                            if (fieldToCheck == "status")
                            {
                                var allMatch = jobs.All(j => targetStatuses.Contains(j.Status?.ToLower() ?? ""));
                                _logger.LogInformation(
                                    "EvaluateCollectionCondition: all_match check - {MatchCount}/{Total} jobs match [{Statuses}]. Result: {Result}",
                                    jobs.Count(j => targetStatuses.Contains(j.Status?.ToLower() ?? "")),
                                    jobs.Count,
                                    string.Join(", ", targetStatuses),
                                    allMatch);
                                return allMatch;
                            }
                        }
                        else if (operatorType.ToLower() == "any_match" || operatorType.ToLower() == "contains")
                        {
                            if (fieldToCheck == "status")
                            {
                                var anyMatch = jobs.Any(j => targetStatuses.Contains(j.Status?.ToLower() ?? ""));
                                _logger.LogInformation(
                                    "EvaluateCollectionCondition: any_match check - jobs match [{Statuses}]. Result: {Result}",
                                    string.Join(", ", targetStatuses),
                                    anyMatch);
                                return anyMatch;
                            }
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EvaluateCollectionConditionAsync failed for field {Field}", field);
                return false;
            }
        }

        private async Task<NodeExecutionResult> ExecuteSwitchNodeAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var field = GetNodeDataString(node, "field") ?? GetNodeDataString(node, "switchField");
            
            if (string.IsNullOrEmpty(field))
            {
                return new NodeExecutionResult
                {
                    Success = false,
                    Status = "failed",
                    Error = "No field specified for switch node"
                };
            }

            var actualValue = await GetFieldValueAsync(field, context);
            var valueStr = actualValue?.ToString()?.ToLower() ?? "default";

            return new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                SelectedCase = valueStr,
                Output = new Dictionary<string, object?>
                {
                    ["switchField"] = field,
                    ["switchValue"] = actualValue,
                    ["selectedCase"] = valueStr
                }
            };
        }

        #endregion

        #region Action Nodes

        private Task<NodeExecutionResult> ExecuteSendEmailAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var to = GetNodeDataString(node, "to") ?? GetNodeDataString(node, "recipient") ?? GetNodeDataString(node, "recipientType");
            var subject = GetNodeDataString(node, "subject") ?? GetNodeDataString(node, "emailSubject");
            var template = GetNodeDataString(node, "template") ?? GetNodeDataString(node, "emailTemplate");

            _logger.LogInformation(
                "Email action: To={To}, Subject={Subject}, Template={Template}",
                to, subject, template);

            // TODO: Integrate with email service
            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "send_email",
                    ["to"] = to,
                    ["subject"] = subject,
                    ["template"] = template,
                    ["status"] = "simulated"
                }
            });
        }

        private Task<NodeExecutionResult> ExecuteSendSmsAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var to = GetNodeDataString(node, "to") ?? GetNodeDataString(node, "phone");
            var message = GetNodeDataString(node, "message");

            _logger.LogInformation("SMS action: To={To}", to);

            // TODO: Integrate with SMS service
            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "send_sms",
                    ["to"] = to,
                    ["status"] = "simulated"
                }
            });
        }

        private Task<NodeExecutionResult> ExecuteNotificationAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var title = GetNodeDataString(node, "title") ?? GetNodeDataString(node, "notificationTitle");
            var message = GetNodeDataString(node, "message") ?? GetNodeDataString(node, "notificationMessage");
            var recipients = GetNodeDataString(node, "recipients") ?? GetNodeDataString(node, "recipientType");

            _logger.LogInformation("Notification action: Title={Title}", title);

            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "notification",
                    ["title"] = title,
                    ["message"] = message,
                    ["status"] = "simulated"
                }
            });
        }

        #endregion

        #region Integration Nodes

        private Task<NodeExecutionResult> ExecuteApiCallAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var url = GetNodeDataString(node, "url");
            var method = GetNodeDataString(node, "method") ?? "GET";

            _logger.LogInformation("API call: {Method} {Url}", method, url);

            // TODO: Actually make HTTP call
            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "api_call",
                    ["url"] = url,
                    ["method"] = method,
                    ["status"] = "simulated"
                }
            });
        }

        private Task<NodeExecutionResult> ExecuteWebhookAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var url = GetNodeDataString(node, "url") ?? GetNodeDataString(node, "webhookUrl");

            _logger.LogInformation("Webhook action: Url={Url}", url);

            // TODO: Actually send webhook
            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "webhook",
                    ["url"] = url,
                    ["status"] = "simulated"
                }
            });
        }

        #endregion

        #region Approval Nodes

        private async Task<NodeExecutionResult> ExecuteApprovalAsync(WorkflowNode node, WorkflowExecutionContext context, int executionId)
        {
            var title = GetNodeDataString(node, "title") ?? "Approval Required";
            var message = GetNodeDataString(node, "message");
            var approverRole = GetNodeDataString(node, "approverRole") ?? "manager";
            var timeoutHours = GetNodeDataInt(node, "timeoutHours") ?? 24;

            // Create approval request
            var approval = new WorkflowApproval
            {
                ExecutionId = executionId,
                NodeId = node.Id,
                Title = title,
                Message = message,
                ApproverRole = approverRole,
                Status = "pending",
                TimeoutHours = timeoutHours,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(timeoutHours)
            };

            _db.WorkflowApprovals.Add(approval);
            await _db.SaveChangesAsync();

            return new NodeExecutionResult
            {
                Success = true,
                Status = "waiting_approval",
                ShouldStop = true, // Stop execution until approved
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "approval",
                    ["approvalId"] = approval.Id,
                    ["title"] = title,
                    ["approverRole"] = approverRole,
                    ["expiresAt"] = approval.ExpiresAt
                }
            };
        }

        #endregion

        #region AI Nodes

        private Task<NodeExecutionResult> ExecuteAiNodeAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var model = GetNodeDataString(node, "model") ?? "gpt-4";
            var prompt = GetNodeDataString(node, "prompt");
            var aiType = node.Type.ToLower();

            _logger.LogInformation("AI node executed: Type={AiType}, Model={Model}", aiType, model);

            // TODO: Integrate with actual AI/LLM service
            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = aiType.Contains("email") ? "ai_email_write" : "ai_analyze",
                    ["model"] = model,
                    ["prompt"] = prompt,
                    ["result"] = aiType.Contains("email") 
                        ? "AI-generated email content (simulated)" 
                        : "AI analysis result (simulated)",
                    ["status"] = "simulated"
                }
            });
        }

        #endregion

        #region Loop Nodes

        private Task<NodeExecutionResult> ExecuteLoopNodeAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var loopType = GetNodeDataString(node, "loopType") ?? "for";
            var iterations = GetNodeDataInt(node, "iterations") ?? 1;

            _logger.LogInformation("Loop node executed: Type={LoopType}, Iterations={Iterations}", loopType, iterations);

            // Store loop metadata in context so downstream nodes can reference it
            context.Variables[$"{node.Id}_iteration"] = 0;
            context.Variables[$"{node.Id}_maxIterations"] = iterations;
            context.Variables[$"{node.Id}_loopType"] = loopType;

            // Note: Actual loop re-execution is handled by the graph executor
            // which checks if a node has loop semantics and re-queues children
            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "loop",
                    ["loopType"] = loopType,
                    ["iterations"] = iterations,
                    ["status"] = "loop_started"
                }
            });
        }

        #endregion

        #region Parallel & TryCatch Nodes

        private Task<NodeExecutionResult> ExecuteParallelNodeAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var maxConcurrency = GetNodeDataInt(node, "maxConcurrency") ?? 5;
            var waitForAll = GetNodeDataString(node, "waitForAll")?.ToLower() != "false";

            _logger.LogInformation("Parallel node executed: MaxConcurrency={Max}, WaitForAll={Wait}", maxConcurrency, waitForAll);

            // Store parallel settings in context for the graph executor
            context.Variables[$"{node.Id}_maxConcurrency"] = maxConcurrency;
            context.Variables[$"{node.Id}_waitForAll"] = waitForAll;

            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "parallel",
                    ["maxConcurrency"] = maxConcurrency,
                    ["waitForAll"] = waitForAll
                }
            });
        }

        private Task<NodeExecutionResult> ExecuteTryCatchNodeAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var retryCount = GetNodeDataInt(node, "retryCount") ?? 0;
            var onErrorAction = GetNodeDataString(node, "onErrorAction") ?? "stop";

            _logger.LogInformation("TryCatch node executed: RetryCount={Retry}, OnError={OnError}", retryCount, onErrorAction);

            context.Variables[$"{node.Id}_retryCount"] = retryCount;
            context.Variables[$"{node.Id}_onErrorAction"] = onErrorAction;

            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "try_catch",
                    ["retryCount"] = retryCount,
                    ["onErrorAction"] = onErrorAction
                }
            });
        }

        #region Contact Node

        private async Task<NodeExecutionResult> ExecuteContactNodeAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            // Contact nodes either look up or pass through contact info
            var contactId = GetNodeDataInt(node, "contactId") ?? context.TriggerEntityId;
            
            _logger.LogInformation("Contact node executed for contact #{ContactId}", contactId);

            // Try to load contact data from the trigger entity if it references a contact
            int? resolvedContactId = null;
            if (context.TriggerEntityType == "offer")
            {
                var offer = await _db.Offers.FindAsync(context.TriggerEntityId);
                resolvedContactId = offer?.ContactId;
            }
            else if (context.TriggerEntityType == "sale")
            {
                var sale = await _db.Sales.FindAsync(context.TriggerEntityId);
                resolvedContactId = sale?.ContactId;
            }
            else if (context.TriggerEntityType == "service_order")
            {
                var so = await _db.ServiceOrders.FindAsync(context.TriggerEntityId);
                resolvedContactId = so?.ContactId;
            }
            else if (context.TriggerEntityType == "dispatch")
            {
                var dispatch = await _db.Dispatches.FindAsync(context.TriggerEntityId);
                resolvedContactId = dispatch?.ContactId;
            }

            if (resolvedContactId.HasValue)
            {
                context.Variables["contact_id"] = resolvedContactId.Value;
            }

            return new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "contact_lookup",
                    ["contactId"] = resolvedContactId ?? contactId,
                    ["resolved"] = resolvedContactId.HasValue
                }
            };
        }

        #endregion


        private async Task<NodeExecutionResult> ExecuteDelayAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var delayMs = GetNodeDataInt(node, "delayMs") ?? GetNodeDataInt(node, "delay") ?? 1000;
            
            // For actual production, this would schedule a job
            // For now, we simulate a short delay
            await Task.Delay(Math.Min(delayMs, 5000)); // Cap at 5 seconds for safety

            return new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "delay",
                    ["delayMs"] = delayMs
                }
            };
        }

        private Task<NodeExecutionResult> ExecuteCalculationAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var expression = GetNodeDataString(node, "expression");
            var resultVar = GetNodeDataString(node, "resultVariable") ?? "result";

            // TODO: Implement expression evaluation
            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "calculate",
                    ["expression"] = expression,
                    ["result"] = 0 // Placeholder
                }
            });
        }

        private Task<NodeExecutionResult> ExecuteSetVariableAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var name = GetNodeDataString(node, "name") ?? GetNodeDataString(node, "variableName");
            var value = GetNodeDataValue(node, "value");

            if (!string.IsNullOrEmpty(name))
            {
                context.Variables[name] = value;
            }

            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "set_variable",
                    ["name"] = name,
                    ["value"] = value
                }
            });
        }

        private Task<NodeExecutionResult> ExecuteDatabaseOperationAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            var operation = GetNodeDataString(node, "operation") ?? "read";
            var table = GetNodeDataString(node, "table");

            _logger.LogInformation("Database operation: {Operation} on {Table}", operation, table);

            // TODO: Implement database operations
            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["action"] = "database",
                    ["operation"] = operation,
                    ["table"] = table,
                    ["status"] = "simulated"
                }
            });
        }

        private Task<NodeExecutionResult> ExecuteDefaultNodeAsync(WorkflowNode node, WorkflowExecutionContext context)
        {
            _logger.LogInformation("Default execution for node type: {NodeType}", node.Type);

            return Task.FromResult(new NodeExecutionResult
            {
                Success = true,
                Status = "completed",
                Output = new Dictionary<string, object?>
                {
                    ["nodeType"] = node.Type,
                    ["status"] = "passed_through"
                }
            });
        }

        #endregion

        #region Helper Methods

        private string? GetNodeDataString(WorkflowNode node, string key)
        {
            // Check top-level data first
            if (node.Data.TryGetValue(key, out var value))
            {
                if (value is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.String) return jsonElement.GetString();
                    if (jsonElement.ValueKind != JsonValueKind.Null && jsonElement.ValueKind != JsonValueKind.Object && jsonElement.ValueKind != JsonValueKind.Array)
                        return jsonElement.ToString();
                }
                else if (value != null)
                {
                    return value.ToString();
                }
            }
            
            // Fall back to nested "config" object (frontend stores configs inside node.data.config)
            if (node.Data.TryGetValue("config", out var configValue) && configValue is JsonElement configEl && configEl.ValueKind == JsonValueKind.Object)
            {
                if (configEl.TryGetProperty(key, out var configProp))
                {
                    if (configProp.ValueKind == JsonValueKind.String) return configProp.GetString();
                    if (configProp.ValueKind != JsonValueKind.Null && configProp.ValueKind != JsonValueKind.Object && configProp.ValueKind != JsonValueKind.Array)
                        return configProp.ToString();
                }
            }
            
            return null;
        }

        private object? GetNodeDataValue(WorkflowNode node, string key)
        {
            // Check top-level data first
            if (node.Data.TryGetValue(key, out var value))
            {
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.ValueKind switch
                    {
                        JsonValueKind.String => jsonElement.GetString(),
                        JsonValueKind.Number => jsonElement.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => jsonElement.ToString()
                    };
                }
                return value;
            }
            
            // Fall back to nested "config" object
            if (node.Data.TryGetValue("config", out var configValue) && configValue is JsonElement configEl && configEl.ValueKind == JsonValueKind.Object)
            {
                if (configEl.TryGetProperty(key, out var configProp))
                {
                    return configProp.ValueKind switch
                    {
                        JsonValueKind.String => configProp.GetString(),
                        JsonValueKind.Number => configProp.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => configProp.ToString()
                    };
                }
            }
            
            return null;
        }

        private int? GetNodeDataInt(WorkflowNode node, string key)
        {
            var value = GetNodeDataValue(node, key);
            if (value is int i) return i;
            if (value is double d) return (int)d;
            if (value is string s && int.TryParse(s, out var parsed)) return parsed;
            return null;
        }

        /// <summary>
        /// Reads from config.condition.{key} - used by ConditionalConfigModal which stores
        /// if-else conditions as { condition: { field, operator, value } }
        /// </summary>
        private string? GetConditionNestedString(WorkflowNode node, string key)
        {
            if (node.Data.TryGetValue("config", out var configValue) && configValue is JsonElement configEl && configEl.ValueKind == JsonValueKind.Object)
            {
                // Check config.condition.{key} (ConditionalConfigModal format)
                if (configEl.TryGetProperty("condition", out var conditionEl) && conditionEl.ValueKind == JsonValueKind.Object)
                {
                    if (conditionEl.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                    {
                        return prop.GetString();
                    }
                }
                // Also check config.conditionData.{key} (seed data / DB format)
                if (configEl.TryGetProperty("conditionData", out var conditionDataEl) && conditionDataEl.ValueKind == JsonValueKind.Object)
                {
                    if (conditionDataEl.TryGetProperty(key, out var prop2) && prop2.ValueKind == JsonValueKind.String)
                    {
                        return prop2.GetString();
                    }
                }
            }
            return null;
        }

        private object? GetConditionNestedValue(WorkflowNode node, string key)
        {
            if (node.Data.TryGetValue("config", out var configValue) && configValue is JsonElement configEl && configEl.ValueKind == JsonValueKind.Object)
            {
                // Check config.condition.{key} (ConditionalConfigModal format)
                if (configEl.TryGetProperty("condition", out var conditionEl) && conditionEl.ValueKind == JsonValueKind.Object)
                {
                    if (conditionEl.TryGetProperty(key, out var prop))
                    {
                        return prop.ValueKind switch
                        {
                            JsonValueKind.String => prop.GetString(),
                            JsonValueKind.Number => prop.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => prop.ToString()
                        };
                    }
                }
                // Also check config.conditionData.{key} (seed data / DB format)
                if (configEl.TryGetProperty("conditionData", out var conditionDataEl) && conditionDataEl.ValueKind == JsonValueKind.Object)
                {
                    if (conditionDataEl.TryGetProperty(key, out var prop2))
                    {
                        return prop2.ValueKind switch
                        {
                            JsonValueKind.String => prop2.GetString(),
                            JsonValueKind.Number => prop2.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => prop2.ToString()
                        };
                    }
                }
            }
            return null;
        }


        private async Task<object?> GetFieldValueAsync(string field, WorkflowExecutionContext context)
        {
            _logger.LogInformation(
                "[WORKFLOW-FIELD] GetFieldValueAsync: field='{Field}', TriggerEntity={TriggerType}#{TriggerId}",
                field, context.TriggerEntityType, context.TriggerEntityId);
            
            // Check context variables first
            if (context.Variables.TryGetValue(field, out var varValue))
            {
                _logger.LogInformation("[WORKFLOW-FIELD] Found in context variables: {Value}", varValue);
                return varValue;
            }

            // Check node outputs
            if (context.NodeOutputs.TryGetValue(field, out var outputValue))
            {
                _logger.LogInformation("[WORKFLOW-FIELD] Found in node outputs: {Value}", outputValue);
                return outputValue;
            }

            // Strip entity prefix from dotted paths (e.g. "sale.hasServiceItems" → "hasServiceItems")
            var resolvedField = field;
            var resolvedEntityType = context.TriggerEntityType;
            
            if (field.Contains('.'))
            {
                var parts = field.Split('.', 2);
                var prefix = parts[0].ToLower();
                resolvedField = parts[1];
                
                // Override entity type if prefix specifies a different entity
                resolvedEntityType = prefix switch
                {
                    "offer" => "offer",
                    "sale" => "sale",
                    "serviceorder" or "service_order" => "service_order",
                    "dispatch" => "dispatch",
                    _ => context.TriggerEntityType
                };
                
                _logger.LogInformation(
                    "[WORKFLOW-FIELD] Parsed dotted path: prefix='{Prefix}' → entityType='{EntityType}', field='{Field}'",
                    prefix, resolvedEntityType, resolvedField);
            }

            // CRITICAL: Resolve the correct entity ID for the target entity type
            // If the dotted path references a different entity than the trigger, look up the right ID
            var resolvedEntityId = context.TriggerEntityId;
            if (resolvedEntityType != context.TriggerEntityType)
            {
                _logger.LogInformation(
                    "[WORKFLOW-FIELD] Need to resolve {TargetType} ID from {TriggerType}#{TriggerId}",
                    resolvedEntityType, context.TriggerEntityType, context.TriggerEntityId);
                
                var createdKey = $"created_{resolvedEntityType}_id";
                if (context.Variables.TryGetValue(createdKey, out var createdId) && createdId != null)
                {
                    if (createdId is int intId) resolvedEntityId = intId;
                    else if (createdId is double dblId) resolvedEntityId = (int)dblId;
                    else if (int.TryParse(createdId.ToString(), out var parsedId)) resolvedEntityId = parsedId;
                    
                    _logger.LogInformation("[WORKFLOW-FIELD] Found created ID in context: {EntityId}", resolvedEntityId);
                }
                else
                {
                    resolvedEntityId = await ResolveRelatedEntityIdAsync(context.TriggerEntityType, context.TriggerEntityId, resolvedEntityType);
                    _logger.LogInformation("[WORKFLOW-FIELD] Resolved via DB relationship: {EntityId}", resolvedEntityId);
                }
            }

            _logger.LogInformation(
                "[WORKFLOW-FIELD] Fetching {EntityType}#{EntityId}.{Field}",
                resolvedEntityType, resolvedEntityId, resolvedField);

            // Get from entity using the resolved ID
            var result = resolvedEntityType switch
            {
                "offer" => await GetOfferFieldAsync(resolvedEntityId, resolvedField),
                "sale" => await GetSaleFieldAsync(resolvedEntityId, resolvedField),
                "service_order" => await GetServiceOrderFieldAsync(resolvedEntityId, resolvedField),
                "dispatch" => await GetDispatchFieldAsync(resolvedEntityId, resolvedField),
                _ => null
            };
            
            _logger.LogInformation("[WORKFLOW-FIELD] Result: {Result}", result);
            return result;
        }

        private bool EvaluateCondition(object? actual, string op, object? expected)
        {
            var actualStr = actual?.ToString()?.ToLower() ?? "";
            var expectedStr = expected?.ToString()?.ToLower() ?? "";

            return op.ToLower() switch
            {
                "equals" or "==" or "eq" => actualStr == expectedStr,
                "not_equals" or "!=" or "neq" => actualStr != expectedStr,
                "contains" => actualStr.Contains(expectedStr),
                "not_contains" => !actualStr.Contains(expectedStr),
                "starts_with" => actualStr.StartsWith(expectedStr),
                "ends_with" => actualStr.EndsWith(expectedStr),
                "greater_than" or ">" or "gt" => CompareNumbers(actual, expected) > 0,
                "less_than" or "<" or "lt" => CompareNumbers(actual, expected) < 0,
                "greater_or_equal" or ">=" or "gte" => CompareNumbers(actual, expected) >= 0,
                "less_or_equal" or "<=" or "lte" => CompareNumbers(actual, expected) <= 0,
                "is_empty" => string.IsNullOrEmpty(actualStr),
                "is_not_empty" => !string.IsNullOrEmpty(actualStr),
                "is_true" => actualStr == "true" || actualStr == "1",
                "is_false" => actualStr == "false" || actualStr == "0",
                // For all_match / any_match, actual should already be pre-computed as bool
                "all_match" => actualStr == "true" || actualStr == "1",
                "any_match" => actualStr == "true" || actualStr == "1",
                _ => actualStr == expectedStr
            };
        }

        private int CompareNumbers(object? a, object? b)
        {
            if (double.TryParse(a?.ToString(), out var numA) && double.TryParse(b?.ToString(), out var numB))
            {
                return numA.CompareTo(numB);
            }
            return string.Compare(a?.ToString(), b?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private async Task<object?> GetOfferFieldAsync(int id, string field)
        {
            var offer = await _db.Offers.FindAsync(id);
            if (offer == null) return null;

            return field.ToLower() switch
            {
                "status" => offer.Status,
                "totalamount" or "total" => offer.TotalAmount,
                "grandtotal" => offer.GrandTotal,
                "contactid" => offer.ContactId,
                "validuntil" => offer.ValidUntil,
                _ => null
            };
        }

        private async Task<object?> GetSaleFieldAsync(int id, string field)
        {
            var sale = await _db.Sales.FindAsync(id);
            if (sale == null) return null;

            return field.ToLower() switch
            {
                "status" => sale.Status,
                "totalamount" or "total" => sale.TotalAmount,
                "grandtotal" => sale.GrandTotal,
                "contactid" => sale.ContactId,
                "offerid" => sale.OfferId,
                // Check if sale has service items (items with Type == "service")
                "hasserviceitems" or "hasservices" => await _db.SaleItems
                    .AnyAsync(si => si.SaleId == id && si.Type == "service"),
                _ => null
            };
        }

        private async Task<object?> GetServiceOrderFieldAsync(int id, string field)
        {
            var serviceOrder = await _db.ServiceOrders.FindAsync(id);
            if (serviceOrder == null) return null;

            return field.ToLower() switch
            {
                "status" => serviceOrder.Status,
                "priority" => serviceOrder.Priority,
                "contactid" => serviceOrder.ContactId,
                "saleid" => serviceOrder.SaleId,
                "totalamount" => serviceOrder.TotalAmount,
                // Check if all dispatches for this service order are completed
                "alldispatchescompleted" or "alldone" or "dispatches" => await CheckAllDispatchesCompletedAsync(id),
                _ => null
            };
        }
        
        private async Task<bool> CheckAllDispatchesCompletedAsync(int serviceOrderId)
        {
            var dispatches = await _db.Dispatches
                .Where(d => d.ServiceOrderId == serviceOrderId && !d.IsDeleted)
                .Select(d => new { d.Id, d.Status })
                .ToListAsync();
            
            _logger.LogInformation(
                "[WORKFLOW-CHECK] CheckAllDispatchesCompleted for ServiceOrder #{ServiceOrderId}: Found {Count} dispatches",
                serviceOrderId, dispatches.Count);
            
            if (!dispatches.Any())
            {
                _logger.LogInformation("[WORKFLOW-CHECK] No dispatches found → returning FALSE");
                return false; // No dispatches means not completed
            }
            
            var completedStatuses = new[] { "technically_completed", "completed" };
            
            foreach (var d in dispatches)
            {
                var isComplete = completedStatuses.Contains(d.Status?.ToLower() ?? "");
                _logger.LogInformation(
                    "[WORKFLOW-CHECK] Dispatch #{DispatchId}: Status='{Status}', IsComplete={IsComplete}",
                    d.Id, d.Status, isComplete);
            }
            
            var allCompleted = dispatches.All(d => completedStatuses.Contains(d.Status?.ToLower() ?? ""));
            _logger.LogInformation("[WORKFLOW-CHECK] All dispatches completed: {Result}", allCompleted);
            
            return allCompleted;
        }

        private async Task<object?> GetDispatchFieldAsync(int id, string field)
        {
            var dispatch = await _db.Dispatches.FindAsync(id);
            if (dispatch == null) return null;

            return field.ToLower() switch
            {
                "status" => dispatch.Status,
                "priority" => dispatch.Priority,
                "contactid" => dispatch.ContactId,
                "serviceorderid" => dispatch.ServiceOrderId,
                "scheduleddate" => dispatch.ScheduledDate,
                _ => null
            };
        }

        /// <summary>
        /// Infer entity type from the node's business type string (e.g., "update-service-order-status" → "service_order")
        /// </summary>
        private static string? InferEntityTypeFromNodeType(string nodeType)
        {
            var lower = nodeType.ToLower();
            if (lower.Contains("service-order") || lower.Contains("service_order")) return "service_order";
            if (lower.Contains("dispatch")) return "dispatch";
            if (lower.Contains("sale")) return "sale";
            if (lower.Contains("offer")) return "offer";
            return null;
        }

        /// <summary>
        /// Resolves a related entity ID by traversing DB relationships.
        /// E.g., from dispatch → service_order, or sale → service_order.
        /// Supports multi-hop resolution (e.g., dispatch → service_order → sale)
        /// </summary>
        private async Task<int> ResolveRelatedEntityIdAsync(string fromEntityType, int fromEntityId, string toEntityType)
        {
            _logger.LogInformation(
                "[WORKFLOW-RESOLVE] Resolving {ToEntity} from {FromEntity}#{FromId}",
                toEntityType, fromEntityType, fromEntityId);
            
            try
            {
                // dispatch → service_order: look up dispatch.ServiceOrderId
                if (fromEntityType == "dispatch" && toEntityType == "service_order")
                {
                    var dispatch = await _db.Dispatches.FindAsync(fromEntityId);
                    var result = dispatch?.ServiceOrderId ?? 0;
                    _logger.LogInformation("[WORKFLOW-RESOLVE] dispatch→service_order: Found ServiceOrderId={Id}", result);
                    return result > 0 ? result : fromEntityId;
                }
                
                // dispatch → sale: go through service_order
                if (fromEntityType == "dispatch" && toEntityType == "sale")
                {
                    var dispatch = await _db.Dispatches.FindAsync(fromEntityId);
                    if (dispatch?.ServiceOrderId != null)
                    {
                        var so = await _db.ServiceOrders.FindAsync(dispatch.ServiceOrderId);
                        if (so?.SaleId != null && int.TryParse(so.SaleId, out var saleId))
                        {
                            _logger.LogInformation("[WORKFLOW-RESOLVE] dispatch→sale (via SO): Found SaleId={Id}", saleId);
                            return saleId;
                        }
                    }
                }
                
                // dispatch → offer: go through service_order → sale
                if (fromEntityType == "dispatch" && toEntityType == "offer")
                {
                    var dispatch = await _db.Dispatches.FindAsync(fromEntityId);
                    if (dispatch?.ServiceOrderId != null)
                    {
                        var so = await _db.ServiceOrders.FindAsync(dispatch.ServiceOrderId);
                        if (so?.SaleId != null && int.TryParse(so.SaleId, out var saleId))
                        {
                            var sale = await _db.Sales.FindAsync(saleId);
                            if (sale?.OfferId != null && int.TryParse(sale.OfferId, out var offerId))
                            {
                                _logger.LogInformation("[WORKFLOW-RESOLVE] dispatch→offer (via SO→Sale): Found OfferId={Id}", offerId);
                                return offerId;
                            }
                        }
                    }
                }
                
                // service_order → sale: look up service_order.SaleId
                if (fromEntityType == "service_order" && toEntityType == "sale")
                {
                    var so = await _db.ServiceOrders.FindAsync(fromEntityId);
                    if (so?.SaleId != null && int.TryParse(so.SaleId, out var saleId))
                    {
                        _logger.LogInformation("[WORKFLOW-RESOLVE] service_order→sale: Found SaleId={Id}", saleId);
                        return saleId;
                    }
                }
                
                // service_order → offer: go through sale
                if (fromEntityType == "service_order" && toEntityType == "offer")
                {
                    var so = await _db.ServiceOrders.FindAsync(fromEntityId);
                    if (so?.SaleId != null && int.TryParse(so.SaleId, out var saleId))
                    {
                        var sale = await _db.Sales.FindAsync(saleId);
                        if (sale?.OfferId != null && int.TryParse(sale.OfferId, out var offerId))
                        {
                            _logger.LogInformation("[WORKFLOW-RESOLVE] service_order→offer (via Sale): Found OfferId={Id}", offerId);
                            return offerId;
                        }
                    }
                }
                
                // sale → service_order: find SO with this SaleId
                if (fromEntityType == "sale" && toEntityType == "service_order")
                {
                    var so = await _db.ServiceOrders.FirstOrDefaultAsync(s => s.SaleId == fromEntityId.ToString());
                    if (so != null)
                    {
                        _logger.LogInformation("[WORKFLOW-RESOLVE] sale→service_order: Found ServiceOrderId={Id}", so.Id);
                        return so.Id;
                    }
                }
                
                // sale → dispatch: go through service_order
                if (fromEntityType == "sale" && toEntityType == "dispatch")
                {
                    var so = await _db.ServiceOrders.FirstOrDefaultAsync(s => s.SaleId == fromEntityId.ToString());
                    if (so != null)
                    {
                        var dispatch = await _db.Dispatches.FirstOrDefaultAsync(d => d.ServiceOrderId == so.Id && !d.IsDeleted);
                        if (dispatch != null)
                        {
                            _logger.LogInformation("[WORKFLOW-RESOLVE] sale→dispatch (via SO): Found DispatchId={Id}", dispatch.Id);
                            return dispatch.Id;
                        }
                    }
                }
                
                // service_order → dispatch: find first dispatch for this SO
                if (fromEntityType == "service_order" && toEntityType == "dispatch")
                {
                    var dispatch = await _db.Dispatches.FirstOrDefaultAsync(d => d.ServiceOrderId == fromEntityId && !d.IsDeleted);
                    if (dispatch != null)
                    {
                        _logger.LogInformation("[WORKFLOW-RESOLVE] service_order→dispatch: Found DispatchId={Id}", dispatch.Id);
                        return dispatch.Id;
                    }
                }
                
                // offer → sale: find sale from this offer
                if (fromEntityType == "offer" && toEntityType == "sale")
                {
                    var sale = await _db.Sales.FirstOrDefaultAsync(s => s.OfferId == fromEntityId.ToString());
                    if (sale != null)
                    {
                        _logger.LogInformation("[WORKFLOW-RESOLVE] offer→sale: Found SaleId={Id}", sale.Id);
                        return sale.Id;
                    }
                }
                
                // offer → service_order: go through sale
                if (fromEntityType == "offer" && toEntityType == "service_order")
                {
                    var sale = await _db.Sales.FirstOrDefaultAsync(s => s.OfferId == fromEntityId.ToString());
                    if (sale != null)
                    {
                        var so = await _db.ServiceOrders.FirstOrDefaultAsync(s => s.SaleId == sale.Id.ToString());
                        if (so != null)
                        {
                            _logger.LogInformation("[WORKFLOW-RESOLVE] offer→service_order (via Sale): Found ServiceOrderId={Id}", so.Id);
                            return so.Id;
                        }
                    }
                }
                
                // offer → dispatch: go through sale → service_order
                if (fromEntityType == "offer" && toEntityType == "dispatch")
                {
                    var sale = await _db.Sales.FirstOrDefaultAsync(s => s.OfferId == fromEntityId.ToString());
                    if (sale != null)
                    {
                        var so = await _db.ServiceOrders.FirstOrDefaultAsync(s => s.SaleId == sale.Id.ToString());
                        if (so != null)
                        {
                            var dispatch = await _db.Dispatches.FirstOrDefaultAsync(d => d.ServiceOrderId == so.Id && !d.IsDeleted);
                            if (dispatch != null)
                            {
                                _logger.LogInformation("[WORKFLOW-RESOLVE] offer→dispatch (via Sale→SO): Found DispatchId={Id}", dispatch.Id);
                                return dispatch.Id;
                            }
                        }
                    }
                }
                
                // sale → offer: look up sale.OfferId
                if (fromEntityType == "sale" && toEntityType == "offer")
                {
                    var sale = await _db.Sales.FindAsync(fromEntityId);
                    if (sale?.OfferId != null && int.TryParse(sale.OfferId, out var offerId))
                    {
                        _logger.LogInformation("[WORKFLOW-RESOLVE] sale→offer: Found OfferId={Id}", offerId);
                        return offerId;
                    }
                }
                
                _logger.LogWarning("[WORKFLOW-RESOLVE] No relationship found for {FromEntity}→{ToEntity}", fromEntityType, toEntityType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WORKFLOW-RESOLVE] Failed to resolve {ToEntity} from {FromEntity}#{FromId}", 
                    toEntityType, fromEntityType, fromEntityId);
            }
            
            // Fallback: return fromEntityId but log a clear warning
            // This will likely cause the update to fail gracefully (entity not found at that ID for the target type)
            _logger.LogWarning(
                "[WORKFLOW-RESOLVE] ⚠️ No relationship found from {FromEntity}#{FromId} → {ToEntity}. " +
                "Falling back to fromEntityId={FromId} which may target the WRONG entity. " +
                "Check workflow configuration and entity relationships.",
                fromEntityType, fromEntityId, toEntityType, fromEntityId);
            return fromEntityId;
        }

        private async Task<bool> UpdateOfferStatusAsync(int id, string newStatus, string? userId)
        {
            var offer = await _db.Offers.FindAsync(id);
            if (offer == null) return false;

            offer.Status = newStatus;
            offer.ModifiedDate = DateTime.UtcNow;
            offer.ModifiedBy = userId;
            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<bool> UpdateSaleStatusAsync(int id, string newStatus, string? userId)
        {
            var sale = await _db.Sales.FindAsync(id);
            if (sale == null) return false;

            sale.Status = newStatus;
            sale.ModifiedDate = DateTime.UtcNow;
            sale.ModifiedBy = userId;
            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<bool> UpdateServiceOrderStatusAsync(int id, string newStatus, string? userId)
        {
            _logger.LogInformation(
                "[WORKFLOW-UPDATE-SO] Attempting to update ServiceOrder #{Id} to status '{NewStatus}' (by user: {UserId})",
                id, newStatus, userId ?? "system");
            
            var serviceOrder = await _db.ServiceOrders.FindAsync(id);
            if (serviceOrder == null)
            {
                _logger.LogError("[WORKFLOW-UPDATE-SO] ServiceOrder #{Id} not found!", id);
                return false;
            }

            var oldStatus = serviceOrder.Status;
            _logger.LogInformation(
                "[WORKFLOW-UPDATE-SO] ServiceOrder #{Id} current status: '{OldStatus}', changing to: '{NewStatus}' (length: {Len})",
                id, oldStatus, newStatus, newStatus.Length);
            
            serviceOrder.Status = newStatus;
            serviceOrder.ModifiedDate = DateTime.UtcNow;
            serviceOrder.ModifiedBy = userId;
            
            // Set ActualStartDate when status changes to in_progress
            var lowerNewStatus = newStatus.ToLower();
            if (lowerNewStatus == "in_progress" && !serviceOrder.ActualStartDate.HasValue)
            {
                serviceOrder.ActualStartDate = DateTime.UtcNow;
                _logger.LogInformation(
                    "[WORKFLOW-UPDATE-SO] Set ActualStartDate for ServiceOrder #{Id} (status: in_progress)",
                    id);
            }
            
            // DYNAMIC: Set completion timestamps based on status name patterns
            // The status names come from workflow configuration - users can define any status
            // We detect "completion" semantics by checking if status contains certain keywords
            var lowerStatus = newStatus.ToLower();
            var isCompletionStatus = lowerStatus.Contains("completed") || 
                                     lowerStatus.Contains("finished") || 
                                     lowerStatus.Contains("done") ||
                                     lowerStatus.Contains("closed");
            var isTechnicalCompletion = lowerStatus.Contains("technical") || lowerStatus.Contains("partial");
            
            if (isCompletionStatus)
            {
                _logger.LogInformation(
                    "[WORKFLOW-UPDATE-SO] Status '{Status}' is a completion status (technical: {IsTech})",
                    newStatus, isTechnicalCompletion);
                
                serviceOrder.ActualCompletionDate = DateTime.UtcNow;
                if (isTechnicalCompletion)
                {
                    serviceOrder.TechnicallyCompletedAt = DateTime.UtcNow;
                }
                
                // Count dispatches that are in any "completed" status (dynamic matching)
                var allDispatches = await _db.Dispatches
                    .Where(d => d.ServiceOrderId == id && !d.IsDeleted)
                    .ToListAsync();
                    
                var completedCount = allDispatches.Count(d => 
                {
                    var dStatus = d.Status?.ToLower() ?? "";
                    return dStatus.Contains("completed") || dStatus.Contains("finished") || 
                           dStatus.Contains("done") || dStatus.Contains("closed");
                });
                
                serviceOrder.CompletedDispatchCount = completedCount;
                _logger.LogInformation(
                    "[WORKFLOW-UPDATE-SO] Updated CompletedDispatchCount to {Count} (out of {Total} dispatches)",
                    completedCount, allDispatches.Count);
            }
            
            try
            {
                _logger.LogInformation("[WORKFLOW-UPDATE-SO] Saving changes for ServiceOrder #{Id}...", id);
                await _db.SaveChangesAsync();
                _logger.LogInformation("[WORKFLOW-UPDATE-SO] ✅ Successfully saved ServiceOrder #{Id} with status '{NewStatus}'", id, newStatus);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex.InnerException ?? ex,
                    "[WORKFLOW-UPDATE-SO] ❌ Failed to save ServiceOrder #{Id}. " +
                    "Status: '{NewStatus}' (length: {Len}). " +
                    "DbUpdateException: {Message}. InnerException: {Inner}",
                    id, newStatus, newStatus.Length, ex.Message, ex.InnerException?.Message ?? "none");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[WORKFLOW-UPDATE-SO] ❌ Unexpected error saving ServiceOrder #{Id}: {Message}",
                    id, ex.Message);
                throw;
            }
            
            _logger.LogInformation(
                "[WORKFLOW-UPDATE-SO] Service order #{Id} status changed from '{Old}' to '{New}'",
                id, oldStatus, newStatus);
            
            return true;
        }

        private async Task<bool> UpdateDispatchStatusAsync(int id, string newStatus, string? userId)
        {
            var dispatch = await _db.Dispatches.FindAsync(id);
            if (dispatch == null) return false;

            var oldStatus = dispatch.Status;
            dispatch.Status = newStatus;
            dispatch.ModifiedDate = DateTime.UtcNow;
            dispatch.ModifiedBy = userId;
            
            // Set timestamps for status changes
            var lowerStatus = newStatus.ToLower();
            if (lowerStatus == "in_progress" && dispatch.ActualStartTime == null)
            {
                dispatch.ActualStartTime = DateTime.UtcNow;
            }
            if (lowerStatus.Contains("completed") || lowerStatus.Contains("finished") || lowerStatus.Contains("done"))
            {
                dispatch.ActualEndTime = DateTime.UtcNow;
            }
            
            await _db.SaveChangesAsync();
            
            // CRITICAL: Cascade status to parent Service Order via BusinessWorkflowService
            // This ensures ServiceOrder counters and status are updated correctly
            var isCompletionStatus = lowerStatus.Contains("completed") || lowerStatus.Contains("finished") || lowerStatus.Contains("done");
            var isInProgress = lowerStatus == "in_progress";
            
            if (isCompletionStatus)
            {
                _logger.LogInformation(
                    "UpdateDispatchStatusAsync: Dispatch #{Id} completed ({Status}), cascading to service order...",
                    id, newStatus);
                await _businessWorkflowService.HandleDispatchTechnicallyCompletedAsync(id, userId);
            }
            else if (isInProgress && oldStatus != "in_progress")
            {
                _logger.LogInformation(
                    "UpdateDispatchStatusAsync: Dispatch #{Id} in progress, cascading to service order...",
                    id);
                await _businessWorkflowService.HandleDispatchInProgressAsync(id, userId);
            }
            
            return true;
        }

        #endregion
    }
}
