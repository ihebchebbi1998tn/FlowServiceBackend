-- =============================================
-- COMPLETE DEFAULT WORKFLOW SETUP - N8N STYLE
-- Updated to wordek with new frontend node typesde
-- Run this in Neon to update the default workflowe
-- =============================================de

-- First, delete existing default workflow if it exists
DELETE FROM "WorkflowTriggers" WHERE "WorkflowId" IN (
    SELECT "Id" FROM "WorkflowDefinitions" WHERE "Name" = 'Default Business Workflow'
);
DELETE FROM "WorkflowDefinitions" WHERE "Name" = 'Default Business Workflow';

-- Insert the updated default workflow with n8n-style node types
INSERT INTO "WorkflowDefinitions" ("Name", "Description", "Nodes", "Edges", "IsActive", "Version", "CreatedBy", "CreatedAt", "IsDeleted")
VALUES (
    'Default Business Workflow',
    'Standard workflow: Offer → Sale → Service Order → Dispatch with automatic status cascading. Always active.',
    '[
        {
            "id": "trigger-offer-accepted",
            "type": "entityTrigger",
            "position": {"x": 100, "y": 100},
            "data": {
                "label": "Offer Accepted",
                "type": "offer-status-trigger",
                "entityType": "offer",
                "fromStatus": null,
                "toStatus": "accepted",
                "description": "Triggers when an offer is accepted by the client"
            }
        },
        {
            "id": "action-create-sale",
            "type": "entityAction",
            "position": {"x": 400, "y": 100},
            "data": {
                "label": "Create Sale",
                "type": "sale",
                "entityType": "sale",
                "actionType": "create",
                "autoCreate": true,
                "description": "Automatically creates a sale from the accepted offer",
                "config": {"autoCreate": true}
            }
        },
        {
            "id": "trigger-sale-in-progress",
            "type": "entityTrigger",
            "position": {"x": 100, "y": 280},
            "data": {
                "label": "Sale In Progress",
                "type": "sale-status-trigger",
                "entityType": "sale",
                "fromStatus": "created",
                "toStatus": "in_progress",
                "description": "Triggers when sale status changes to in_progress"
            }
        },
        {
            "id": "condition-has-services",
            "type": "conditionNode",
            "position": {"x": 400, "y": 280},
            "data": {
                "label": "Has Services?",
                "type": "if-else",
                "description": "Check if sale contains at least one service item",
                "config": {
                    "field": "sale.hasServiceItems",
                    "operator": "equals",
                    "value": "true"
                }
            }
        },
        {
            "id": "action-create-service-order",
            "type": "entityAction",
            "position": {"x": 720, "y": 220},
            "data": {
                "label": "Create Service Order",
                "type": "service-order",
                "entityType": "service_order",
                "actionType": "create",
                "autoCreate": true,
                "description": "Creates service order from sale service items",
                "config": {"autoCreate": true}
            }
        },
        {
            "id": "trigger-job-planned",
            "type": "entityTrigger",
            "position": {"x": 100, "y": 460},
            "data": {
                "label": "Job Planned",
                "type": "service-order-status-trigger",
                "entityType": "service_order",
                "fromStatus": "pending",
                "toStatus": "scheduled",
                "description": "Triggers when a job is planned/scheduled"
            }
        },
        {
            "id": "action-create-dispatch",
            "type": "entityAction",
            "position": {"x": 400, "y": 460},
            "data": {
                "label": "Create Dispatch",
                "type": "dispatch",
                "entityType": "dispatch",
                "actionType": "create",
                "autoCreate": true,
                "description": "Creates dispatch entries for each scheduled job",
                "config": {"createPerService": true, "autoCreate": true}
            }
        },
        {
            "id": "trigger-dispatch-in-progress",
            "type": "entityTrigger",
            "position": {"x": 100, "y": 640},
            "data": {
                "label": "Dispatch Started",
                "type": "dispatch-status-trigger",
                "entityType": "dispatch",
                "fromStatus": null,
                "toStatus": "in_progress",
                "description": "Triggers when user starts working on dispatch"
            }
        },
        {
            "id": "action-so-in-progress",
            "type": "entityAction",
            "position": {"x": 400, "y": 640},
            "data": {
                "label": "SO → In Progress",
                "type": "update-service-order-status",
                "entityType": "service_order",
                "actionType": "update-status",
                "newStatus": "in_progress",
                "description": "Updates parent service order to in_progress",
                "config": {"newStatus": "in_progress"}
            }
        },
        {
            "id": "trigger-dispatch-completed",
            "type": "entityTrigger",
            "position": {"x": 100, "y": 820},
            "data": {
                "label": "Dispatch Tech Complete",
                "type": "dispatch-status-trigger",
                "entityType": "dispatch",
                "fromStatus": "in_progress",
                "toStatus": "technically_completed",
                "description": "Triggers when dispatch is technically completed"
            }
        },
        {
            "id": "condition-all-done",
            "type": "conditionNode",
            "position": {"x": 400, "y": 820},
            "data": {
                "label": "All Dispatches Done?",
                "type": "if-else",
                "description": "Check if all sibling dispatches are completed",
                "config": {
                    "field": "serviceOrder.allDispatchesCompleted",
                    "operator": "equals",
                    "value": "true"
                }
            }
        },
        {
            "id": "action-so-tech-complete",
            "type": "entityAction",
            "position": {"x": 720, "y": 760},
            "data": {
                "label": "SO → Tech Complete",
                "type": "update-service-order-status",
                "entityType": "service_order",
                "actionType": "update-status",
                "newStatus": "technically_completed",
                "description": "All dispatches done - mark SO as technically completed",
                "config": {"newStatus": "technically_completed"}
            }
        },
        {
            "id": "action-so-partial",
            "type": "entityAction",
            "position": {"x": 720, "y": 900},
            "data": {
                "label": "SO → Partial",
                "type": "update-service-order-status",
                "entityType": "service_order",
                "actionType": "update-status",
                "newStatus": "partially_completed",
                "description": "Some dispatches still pending - mark SO as partially completed",
                "config": {"newStatus": "partially_completed"}
            }
        }
    ]'::jsonb,
    '[
        {"id": "e1", "source": "trigger-offer-accepted", "target": "action-create-sale", "type": "smoothstep", "animated": true},
        {"id": "e2", "source": "trigger-sale-in-progress", "target": "condition-has-services", "type": "smoothstep", "animated": true},
        {"id": "e3", "source": "condition-has-services", "target": "action-create-service-order", "sourceHandle": "yes", "type": "smoothstep", "animated": true, "label": "YES"},
        {"id": "e4", "source": "trigger-job-planned", "target": "action-create-dispatch", "type": "smoothstep", "animated": true},
        {"id": "e5", "source": "trigger-dispatch-in-progress", "target": "action-so-in-progress", "type": "smoothstep", "animated": true},
        {"id": "e6", "source": "trigger-dispatch-completed", "target": "condition-all-done", "type": "smoothstep", "animated": true},
        {"id": "e7", "source": "condition-all-done", "target": "action-so-tech-complete", "sourceHandle": "yes", "type": "smoothstep", "animated": true, "label": "YES"},
        {"id": "e8", "source": "condition-all-done", "target": "action-so-partial", "sourceHandle": "no", "type": "smoothstep", "animated": true, "label": "NO"}
    ]'::jsonb,
    true,
    1,
    'system',
    NOW(),
    false
);

-- Register triggers for the new workflow
DO $$
DECLARE wf_id INTEGER;
BEGIN
    SELECT "Id" INTO wf_id FROM "WorkflowDefinitions" WHERE "Name" = 'Default Business Workflow' AND "IsActive" = true LIMIT 1;
    IF wf_id IS NOT NULL THEN
        INSERT INTO "WorkflowTriggers" ("WorkflowId", "NodeId", "EntityType", "FromStatus", "ToStatus", "IsActive", "CreatedAt") VALUES
            (wf_id, 'trigger-offer-accepted', 'offer', NULL, 'accepted', true, NOW()),
            (wf_id, 'trigger-sale-in-progress', 'sale', 'created', 'in_progress', true, NOW()),
            (wf_id, 'trigger-job-planned', 'service_order', 'pending', 'scheduled', true, NOW()),
            (wf_id, 'trigger-dispatch-in-progress', 'dispatch', NULL, 'in_progress', true, NOW()),
            (wf_id, 'trigger-dispatch-completed', 'dispatch', 'in_progress', 'technically_completed', true, NOW())
        ON CONFLICT DO NOTHING;
    END IF;
END $$;

-- Ensure helper columns exist on ServiceOrders
ALTER TABLE "ServiceOrders" ADD COLUMN IF NOT EXISTS "TechnicallyCompletedAt" TIMESTAMP;
ALTER TABLE "ServiceOrders" ADD COLUMN IF NOT EXISTS "ServiceCount" INTEGER DEFAULT 0;
ALTER TABLE "ServiceOrders" ADD COLUMN IF NOT EXISTS "CompletedDispatchCount" INTEGER DEFAULT 0;

-- Add comments for documentation
COMMENT ON TABLE "WorkflowDefinitions" IS 'Stores visual workflow definitions with n8n-style node types (entityTrigger, entityAction, conditionNode)';
