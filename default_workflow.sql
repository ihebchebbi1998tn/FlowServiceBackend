-- =============================================
-- COMPLETE DEFAULT WORKFLOW v2
-- Aligned with frontend node IDs for proper rendering
-- Run this in Neon to update the default workflow
-- ==============de===============================

-- 1. Create workflow tables if not exist
CREATE TABLE IF NOT EXISTS "WorkflowDefinitions" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "Nodes" JSONB NOT NULL DEFAULT '[]',
    "Edges" JSONB NOT NULL DEFAULT '[]',
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "Version" INTEGER NOT NULL DEFAULT 1,
    "CreatedBy" VARCHAR(100),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP,
    "ModifiedBy" VARCHAR(100),
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false
);

CREATE TABLE IF NOT EXISTS "WorkflowTriggers" (
    "Id" SERIAL PRIMARY KEY,
    "WorkflowId" INTEGER NOT NULL REFERENCES "WorkflowDefinitions"("Id") ON DELETE CASCADE,
    "NodeId" VARCHAR(50) NOT NULL,
    "EntityType" VARCHAR(30) NOT NULL,
    "FromStatus" VARCHAR(50),
    "ToStatus" VARCHAR(50),
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS "WorkflowExecutions" (
    "Id" SERIAL PRIMARY KEY,
    "WorkflowId" INTEGER NOT NULL REFERENCES "WorkflowDefinitions"("Id"),
    "TriggerEntityType" VARCHAR(30) NOT NULL,
    "TriggerEntityId" INTEGER NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'running',
    "CurrentNodeId" VARCHAR(50),
    "Context" JSONB NOT NULL DEFAULT '{}',
    "Error" VARCHAR(1000),
    "StartedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "CompletedAt" TIMESTAMP,
    "TriggeredBy" VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS "WorkflowExecutionLogs" (
    "Id" SERIAL PRIMARY KEY,
    "ExecutionId" INTEGER NOT NULL REFERENCES "WorkflowExecutions"("Id") ON DELETE CASCADE,
    "NodeId" VARCHAR(50) NOT NULL,
    "NodeType" VARCHAR(50) NOT NULL,
    "Status" VARCHAR(20) NOT NULL,
    "Input" JSONB,
    "Output" JSONB,
    "Error" VARCHAR(500),
    "Duration" INTEGER,
    "Timestamp" TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS "WorkflowApprovals" (
    "Id" SERIAL PRIMARY KEY,
    "ExecutionId" INTEGER NOT NULL REFERENCES "WorkflowExecutions"("Id") ON DELETE CASCADE,
    "NodeId" VARCHAR(50) NOT NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Message" VARCHAR(1000),
    "ApproverRole" VARCHAR(50) NOT NULL,
    "ApprovedById" VARCHAR(100),
    "Status" VARCHAR(20) NOT NULL DEFAULT 'pending',
    "ResponseNote" VARCHAR(500),
    "TimeoutHours" INTEGER NOT NULL DEFAULT 24,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "RespondedAt" TIMESTAMP,
    "ExpiresAt" TIMESTAMP
);

-- 2. Delete old default workflow and its triggers
DELETE FROM "WorkflowTriggers" WHERE "WorkflowId" IN (
    SELECT "Id" FROM "WorkflowDefinitions" WHERE "Name" = 'Default Business Workflow'
);
DELETE FROM "WorkflowDefinitions" WHERE "Name" = 'Default Business Workflow';

-- 3. Insert the updated default workflow v2 (node IDs match frontend exactly)
INSERT INTO "WorkflowDefinitions" ("Name", "Description", "Nodes", "Edges", "IsActive", "Version", "CreatedBy", "CreatedAt", "IsDeleted")
VALUES (
    'Default Business Workflow',
    'Full pipeline: Offer → Sale → Service Order → Dispatch with automatic status cascading and completion tracking.',
    '[
        {
            "id": "trigger-offer-accepted",
            "type": "offer-status-trigger",
            "position": {"x": 100, "y": 100},
            "data": {
                "label": "Offer Accepted",
                "type": "offer-status-trigger",
                "description": "Triggers when an offer is accepted by the client",
                "entityType": "offer",
                "fromStatus": null,
                "toStatus": "accepted"
            }
        },
        {
            "id": "action-create-sale",
            "type": "sale",
            "position": {"x": 400, "y": 100},
            "data": {
                "label": "Create Sale",
                "type": "sale",
                "description": "Automatically creates a sale from the accepted offer",
                "entityType": "sale",
                "actionType": "create",
                "config": {"autoCreate": true, "copyFromOffer": true}
            }
        },
        {
            "id": "trigger-sale-in-progress",
            "type": "sale-status-trigger",
            "position": {"x": 100, "y": 280},
            "data": {
                "label": "Sale In Progress",
                "type": "sale-status-trigger",
                "description": "Triggers when sale moves to in_progress",
                "entityType": "sale",
                "fromStatus": "created",
                "toStatus": "in_progress"
            }
        },
        {
            "id": "condition-has-services",
            "type": "if-else",
            "position": {"x": 400, "y": 280},
            "data": {
                "label": "Has Services?",
                "type": "if-else",
                "description": "Checks if the sale contains service items",
                "config": {"field": "sale.hasServiceItems", "operator": "equals", "value": "true"}
            }
        },
        {
            "id": "action-create-service-order",
            "type": "service-order",
            "position": {"x": 720, "y": 220},
            "data": {
                "label": "Create Service Order",
                "type": "service-order",
                "description": "Creates a service order from sale service items",
                "entityType": "service_order",
                "actionType": "create",
                "config": {"autoCreate": true, "copyServicesFromSale": true, "initialStatus": "pending"}
            }
        },
        {
            "id": "trigger-job-planned",
            "type": "service-order-status-trigger",
            "position": {"x": 100, "y": 460},
            "data": {
                "label": "Job Planned / SO Scheduled",
                "type": "service-order-status-trigger",
                "description": "Triggers when service order is scheduled",
                "entityType": "service_order",
                "fromStatus": "pending",
                "toStatus": "scheduled"
            }
        },
        {
            "id": "action-create-dispatch",
            "type": "dispatch",
            "position": {"x": 400, "y": 460},
            "data": {
                "label": "Create Dispatches",
                "type": "dispatch",
                "description": "Creates one dispatch per service in the order",
                "entityType": "dispatch",
                "actionType": "create",
                "config": {"autoCreate": true, "createPerService": true, "initialStatus": "planned"}
            }
        },
        {
            "id": "trigger-dispatch-in-progress",
            "type": "dispatch-status-trigger",
            "position": {"x": 100, "y": 640},
            "data": {
                "label": "Dispatch Started",
                "type": "dispatch-status-trigger",
                "description": "Triggers when a technician starts work",
                "entityType": "dispatch",
                "fromStatus": null,
                "toStatus": "in_progress"
            }
        },
        {
            "id": "action-so-in-progress",
            "type": "update-service-order-status",
            "position": {"x": 400, "y": 640},
            "data": {
                "label": "SO → In Progress",
                "type": "update-service-order-status",
                "description": "Updates service order to in_progress",
                "entityType": "service_order",
                "actionType": "update-status",
                "config": {"newStatus": "in_progress", "condition": "ifNotAlreadyInProgress"}
            }
        },
        {
            "id": "trigger-dispatch-completed",
            "type": "dispatch-status-trigger",
            "position": {"x": 100, "y": 820},
            "data": {
                "label": "Dispatch Tech Complete",
                "type": "dispatch-status-trigger",
                "description": "Triggers when dispatch is technically completed",
                "entityType": "dispatch",
                "fromStatus": "in_progress",
                "toStatus": "technically_completed"
            }
        },
        {
            "id": "condition-all-done",
            "type": "if-else",
            "position": {"x": 400, "y": 820},
            "data": {
                "label": "All Dispatches Done?",
                "type": "if-else",
                "description": "Checks if all dispatches for this SO are completed",
                "config": {"field": "serviceOrder.allDispatchesCompleted", "operator": "equals", "value": "true"}
            }
        },
        {
            "id": "action-so-tech-complete",
            "type": "update-service-order-status",
            "position": {"x": 720, "y": 760},
            "data": {
                "label": "SO → Tech Complete",
                "type": "update-service-order-status",
                "description": "All dispatches done - mark SO as technically completed",
                "entityType": "service_order",
                "actionType": "update-status",
                "config": {"newStatus": "technically_completed"}
            }
        },
        {
            "id": "action-so-partial",
            "type": "update-service-order-status",
            "position": {"x": 720, "y": 900},
            "data": {
                "label": "SO → Partial",
                "type": "update-service-order-status",
                "description": "Some dispatches still pending",
                "entityType": "service_order",
                "actionType": "update-status",
                "config": {"newStatus": "partially_completed"}
            }
        }
    ]'::jsonb,
    '[
        {"id":"e1","source":"trigger-offer-accepted","target":"action-create-sale","type":"smoothstep","animated":true},
        {"id":"e2","source":"trigger-sale-in-progress","target":"condition-has-services","type":"smoothstep","animated":true},
        {"id":"e3","source":"condition-has-services","target":"action-create-service-order","sourceHandle":"yes","type":"smoothstep","animated":true,"label":"YES"},
        {"id":"e4","source":"trigger-job-planned","target":"action-create-dispatch","type":"smoothstep","animated":true},
        {"id":"e5","source":"trigger-dispatch-in-progress","target":"action-so-in-progress","type":"smoothstep","animated":true},
        {"id":"e6","source":"trigger-dispatch-completed","target":"condition-all-done","type":"smoothstep","animated":true},
        {"id":"e7","source":"condition-all-done","target":"action-so-tech-complete","sourceHandle":"yes","type":"smoothstep","animated":true,"label":"YES"},
        {"id":"e8","source":"condition-all-done","target":"action-so-partial","sourceHandle":"no","type":"smoothstep","animated":true,"label":"NO"}
    ]'::jsonb,
    true,
    2,
    'system',
    NOW(),
    false
);

-- 4. Register triggers for the new workflow
DO $$
DECLARE wf_id INTEGER;
BEGIN
    SELECT "Id" INTO wf_id FROM "WorkflowDefinitions" WHERE "Name" = 'Default Business Workflow' AND "IsActive" = true LIMIT 1;
    IF wf_id IS NOT NULL THEN
        INSERT INTO "WorkflowTriggers" ("WorkflowId","NodeId","EntityType","FromStatus","ToStatus","IsActive","CreatedAt") VALUES
            (wf_id, 'trigger-offer-accepted',    'offer',         NULL,          'accepted',              true, NOW()),
            (wf_id, 'trigger-sale-in-progress',  'sale',          'created',     'in_progress',           true, NOW()),
            (wf_id, 'trigger-job-planned',       'service_order', 'pending',     'scheduled',             true, NOW()),
            (wf_id, 'trigger-dispatch-in-progress','dispatch',    NULL,          'in_progress',           true, NOW()),
            (wf_id, 'trigger-dispatch-completed','dispatch',      'in_progress', 'technically_completed', true, NOW());
    END IF;
END $$;

-- 5. Add helper columns to ServiceOrders (idempotent)
ALTER TABLE "ServiceOrders" ADD COLUMN IF NOT EXISTS "TechnicallyCompletedAt" TIMESTAMP;
ALTER TABLE "ServiceOrders" ADD COLUMN IF NOT EXISTS "ServiceCount" INTEGER DEFAULT 0;
ALTER TABLE "ServiceOrders" ADD COLUMN IF NOT EXISTS "CompletedDispatchCount" INTEGER DEFAULT 0;
