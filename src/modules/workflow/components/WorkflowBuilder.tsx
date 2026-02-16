
import { useState, useCallback, useEffect, useRef, useMemo } from 'react';
import '../styles/workflow.css';
import '../styles/workflow-execution.css';
import { entityStatusConfigs } from '@/config/entity-statuses';
import {
  ReactFlow,
  Controls,
  Background,
  MiniMap,
  useNodesState,
  useEdgesState,
  addEdge,
  Node,
  Edge,
  Connection,
  MarkerType,
  BackgroundVariant,
  type ReactFlowInstance,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { Button } from "@/components/ui/button";
import { 
  Plus, Play, Zap, Bot, Settings, GitBranch, Users, DollarSign, 
  ShoppingCart, Truck, Mail, Send, Sparkles, Brain, Webhook, 
  Calendar, Database, FileText, Clipboard,
  Menu, RotateCcw, Split, Shield, Download, Upload, Square, Wifi, WifiOff, Bug, RefreshCw,
  Edit3, Save, Loader2, X, Copy, Check, ClipboardList, PauseCircle, ArrowLeftRight, Globe, Wand2
} from "lucide-react";
import { WorkflowNode, NodeExecutionState } from '../small-components/WorkflowNode';
import { WorkflowToolbar } from '../small-components/WorkflowToolbar';
import EdgeToolbar from '../small-components/EdgeToolbar';
import { WorkflowManager } from './WorkflowManager';
import { NodeConfigurationModal } from './NodeConfigurationModal';
import { ConditionalConfigModal } from './conditional/ConditionalConfigModal';
import { SavedWorkflow } from '../hooks/useWorkflowStorage';
import { IfElseNode } from './conditional/IfElseNode';
import { SwitchNode } from './conditional/SwitchNode';
import { LoopNode } from './conditional/LoopNode';
import { ParallelNode } from './conditional/ParallelNode';
import { TryCatchNode } from './conditional/TryCatchNode';
import { ExportDialog } from './export-import/ExportDialog';
import { ImportDialog } from './export-import/ImportDialog';
import { WorkflowLoadingSkeleton } from '../small-components/WorkflowLoadingSkeleton';
import { useWorkflowSignalR } from '../hooks/useWorkflowSignalR';
import { toast } from "sonner";
import { useTranslation } from 'react-i18next';
import { workflowApi, workflowReconciliationApi } from '@/services/api/workflowApi';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { N8nStyleNode, EntityTriggerNode, EntityActionNode, ConditionNode } from './nodes';
import { NodePalette, NodeTemplate } from './panels/NodePalette';
import { transformWorkflowFromBackend } from '../utils/nodeTransformer';
import { NodeConfigPanel, NodeConfig } from './panels/NodeConfigPanel';
import { ExecutionDebugPanel } from './panels/ExecutionDebugPanel';
import { AddButtonEdge } from './edges/AddButtonEdge';
import { WorkflowVersionBadge, type WorkflowVersionStatus } from './panels/WorkflowVersionBadge';
import { WorkflowDebugConsole } from './WorkflowDebugConsole';
import { AIWorkflowBuilder } from './AIWorkflowBuilder';

const createNodeTypes = (
  onNodeClick: (nodeId: string, nodeData: any) => void,
  onRemove: (nodeId: string) => void
) => ({
  // n8n-style nodes
  n8nNode: (props: any) => <N8nStyleNode {...props} onNodeClick={onNodeClick} onRemove={onRemove} />,
  entityTrigger: (props: any) => <EntityTriggerNode {...props} onNodeClick={onNodeClick} onRemove={onRemove} />,
  entityAction: (props: any) => <EntityActionNode {...props} onNodeClick={onNodeClick} onRemove={onRemove} />,
  conditionNode: (props: any) => <ConditionNode {...props} onNodeClick={onNodeClick} onRemove={onRemove} />,
  // Legacy nodes for backward compatibility
  workflowNode: (props: any) => <WorkflowNode {...props} onNodeClick={onNodeClick} onRemove={onRemove} />,
  ifElseNode: (props: any) => <IfElseNode {...props} onNodeClick={onNodeClick} onRemove={onRemove} />,
  switchNode: (props: any) => <SwitchNode {...props} onNodeClick={onNodeClick} onRemove={onRemove} />,
  loopNode: (props: any) => <LoopNode {...props} onNodeClick={onNodeClick} onRemove={onRemove} />,
  parallelNode: (props: any) => <ParallelNode {...props} onNodeClick={onNodeClick} onRemove={onRemove} />,
  tryCatchNode: (props: any) => <TryCatchNode {...props} onNodeClick={onNodeClick} onRemove={onRemove} />,
});

export function WorkflowBuilder() {
  const { t } = useTranslation('workflow');
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  const [isRunning, setIsRunning] = useState(false);
  const [executionProgress, setExecutionProgress] = useState(0);
  
  const [selectedEdgeId, setSelectedEdgeId] = useState<string | null>(null);
  const [selectedEdgePos, setSelectedEdgePos] = useState<{ x: number; y: number } | null>(null);
  const [configModal, setConfigModal] = useState<{
    isOpen: boolean;
    nodeData?: any;
  }>({ isOpen: false });
  const [configPanel, setConfigPanel] = useState<{
    isOpen: boolean;
    nodeId: string;
    nodeData: any;
  }>({ isOpen: false, nodeId: '', nodeData: null });
  const [exportModal, setExportModal] = useState(false);
  const [importModal, setImportModal] = useState(false);
  const [initialized, setInitialized] = useState(false);
  const [workflowLoading, setWorkflowLoading] = useState(true);
  const [liveExecutionId, setLiveExecutionId] = useState<number | null>(null);
  const [currentWorkflowId, setCurrentWorkflowId] = useState<number | null>(null);
  const [isWorkflowActive, setIsWorkflowActive] = useState(true); // Workflows are active by default
  const [stopConfirmModal, setStopConfirmModal] = useState(false);
  const [debugPanelOpen, setDebugPanelOpen] = useState(false);
  const [debugConsoleOpen, setDebugConsoleOpen] = useState(false); // Debug console modal
  const [isReconciling, setIsReconciling] = useState(false);
  const [nextRunCountdown, setNextRunCountdown] = useState(300); // 5 minutes in seconds
  const [isEditMode, setIsEditMode] = useState(false); // Edit mode toggle
  const [isSaving, setIsSaving] = useState(false); // Saving state
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false); // Track changes
  const [isCopied, setIsCopied] = useState(false); // Copy feedback
  const [aiBuilderOpen, setAiBuilderOpen] = useState(false); // AI workflow builder
  const [workflowVersion, setWorkflowVersion] = useState(1);
  const [versionStatus, setVersionStatus] = useState<WorkflowVersionStatus>('active');
  const reactFlowInstance = useRef<ReactFlowInstance<any, any> | null>(null);

  // Track if auto-reconciliation should run
  const shouldAutoReconcileRef = useRef(false);
  // Ref for reconciliation handler to avoid circular deps
  const handleReconciliationRef = useRef<(() => Promise<void>) | null>(null);

  // Format countdown as MM:SS
  const formatCountdown = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  // SignalR for real-time workflow execution updates from backend
  const { 
    connectionState, 
    isConnected 
  } = useWorkflowSignalR({
    autoConnect: true,
    onExecutionStarted: (event) => {
      console.log('[Workflow] Real execution started:', event);
      setLiveExecutionId(event.executionId);
      setIsRunning(true);
      setExecutionProgress(0);
      
      // Set all nodes to waiting state
      setNodes(nds => nds.map(n => ({
        ...n,
        data: { ...n.data, executionState: 'waiting' as NodeExecutionState }
      })));
    },
    onNodeExecuting: (event) => {
      console.log('[Workflow] Node executing:', event);
      
      // Find matching node by type or ID and set to executing
      setNodes(nds => nds.map(n => {
        const nodeType = (n.data as any)?.type;
        const matches = n.id === event.nodeId || 
                       nodeType === event.nodeType ||
                       nodeType === event.nodeType.replace('-trigger', '');
        
        return {
          ...n,
          data: { 
            ...n.data, 
            executionState: matches ? 'executing' as NodeExecutionState : n.data.executionState 
          }
        };
      }));
    },
    onNodeCompleted: (event) => {
      console.log('[Workflow] Node completed:', event);
      
      // Mark the node as completed or failed
      setNodes(nds => nds.map(n => {
        const nodeType = (n.data as any)?.type;
        const matches = n.id === event.nodeId || 
                       nodeType === event.nodeType ||
                       nodeType === event.nodeType.replace('-trigger', '');
        
        return {
          ...n,
          data: { 
            ...n.data, 
            executionState: matches 
              ? (event.success ? 'completed' : 'failed') as NodeExecutionState 
              : n.data.executionState 
          }
        };
      }));
    },
    onExecutionCompleted: (event) => {
      console.log('[Workflow] Execution completed:', event);
      setExecutionProgress(100);
      
      setTimeout(() => {
        setIsRunning(false);
        setLiveExecutionId(null);
        
        // Reset after showing completion
        setTimeout(() => {
          setNodes(nds => nds.map(n => ({
            ...n,
            data: { ...n.data, executionState: 'idle' as NodeExecutionState }
          })));
          setExecutionProgress(0);
        }, 3000);
      }, 500);
    },
    onExecutionError: (event) => {
      console.log('[Workflow] Execution error:', event);
      setIsRunning(false);
      setLiveExecutionId(null);
    },
  });

  // Countdown timer for next reconciliation run - triggers reconciliation when it reaches 0
  useEffect(() => {
    const interval = setInterval(() => {
      setNextRunCountdown(prev => {
        if (prev <= 1) {
          // Set flag to trigger reconciliation
          shouldAutoReconcileRef.current = true;
          return 300; // Reset to 5 minutes
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(interval);
  }, []);

  // Effect to actually trigger reconciliation when countdown hits 0
  useEffect(() => {
    if (shouldAutoReconcileRef.current && isConnected && !isReconciling && !isRunning && nodes.length > 0) {
      shouldAutoReconcileRef.current = false;
      // Trigger reconciliation with animations
      handleReconciliationRef.current?.();
    }
  }, [nextRunCountdown, isConnected, isReconciling, isRunning, nodes.length]);

  // Default business template for fallback (when API is unavailable) - uses n8n-style nodes
  // Status values are pulled from centralized config so they auto-update with config changes
  const getDefaultBusinessTemplate = useCallback(() => {
    // Pull status IDs from centralized config — template stays in sync automatically
    const offerCfg = entityStatusConfigs.offer;
    const saleCfg = entityStatusConfigs.sale;
    const soCfg = entityStatusConfigs.service_order;
    const dispatchCfg = entityStatusConfigs.dispatch;

    // Resolve key statuses from workflow steps & terminal statuses
    const offerAccepted = offerCfg.workflow.steps[offerCfg.workflow.steps.length - 1]; // last happy step = 'accepted'
    const saleDefault = saleCfg.defaultStatus; // 'created'
    const saleInProgress = saleCfg.workflow.steps[1]; // second step = 'in_progress'
    const soDefault = soCfg.defaultStatus; // 'draft' or 'pending'
    const soScheduled = soCfg.workflow.steps.find(s => s === 'scheduled') ?? soCfg.workflow.steps[2];
    const soInProgress = soCfg.workflow.steps.find(s => s === 'in_progress') ?? soCfg.workflow.steps[3];
    const soTechComplete = soCfg.statuses.find(s => s.id === 'technically_completed')?.id ?? 'technically_completed';
    const soPartial = soCfg.statuses.find(s => s.id === 'partially_completed')?.id ?? 'partially_completed';
    const dispatchInProgress = dispatchCfg.workflow.steps.find(s => s === 'in_progress') ?? dispatchCfg.workflow.steps[2];
    const dispatchCompleted = dispatchCfg.statuses.find(s => s.id === 'completed')?.id ?? 'completed';

    const templateNodes: Node[] = [
      // Trigger: Offer Accepted
      {
        id: 'trigger-offer-accepted',
        type: 'entityTrigger',
        position: { x: 100, y: 100 },
        data: {
          label: t('offerAccepted'),
          type: 'offer-status-trigger',
          entityType: 'offer',
          icon: FileText,
          fromStatus: null,
          toStatus: offerAccepted,
          description: t('offerAcceptedDesc'),
        }
      },
      // Action: Create Sale
      {
        id: 'action-create-sale',
        type: 'entityAction',
        position: { x: 400, y: 100 },
        data: {
          label: t('createSale'),
          type: 'sale',
          entityType: 'sale',
          actionType: 'create',
          icon: DollarSign,
          autoCreate: true,
          description: t('createSaleDesc'),
          config: { autoCreate: true },
        }
      },
      // Trigger: Sale In Progress
      {
        id: 'trigger-sale-in-progress',
        type: 'entityTrigger',
        position: { x: 100, y: 280 },
        data: {
          label: t('saleInProgress'),
          type: 'sale-status-trigger',
          entityType: 'sale',
          icon: DollarSign,
          fromStatus: saleDefault,
          toStatus: saleInProgress,
          description: t('saleInProgressDesc'),
        }
      },
      // Condition: Has Services?
      {
        id: 'condition-has-services',
        type: 'conditionNode',
        position: { x: 400, y: 280 },
        data: {
          label: t('hasServices'),
          type: 'if-else',
          icon: GitBranch,
          description: t('hasServicesDesc'),
          config: { field: 'sale.hasServiceItems', operator: 'equals', value: 'true' },
        }
      },
      // Action: Create Service Order
      {
        id: 'action-create-service-order',
        type: 'entityAction',
        position: { x: 720, y: 220 },
        data: {
          label: t('createServiceOrder'),
          type: 'service-order',
          entityType: 'service_order',
          actionType: 'create',
          icon: ShoppingCart,
          autoCreate: true,
          description: t('createServiceOrderDesc'),
          config: { autoCreate: true },
        }
      },
      // Trigger: Service Order Scheduled
      {
        id: 'trigger-job-planned',
        type: 'entityTrigger',
        position: { x: 100, y: 460 },
        data: {
          label: t('jobPlanned'),
          type: 'service-order-status-trigger',
          entityType: 'service_order',
          icon: ShoppingCart,
          fromStatus: soDefault === 'draft' ? 'pending' : soDefault,
          toStatus: soScheduled,
          description: t('jobPlannedDesc'),
        }
      },
      // Action: Create Dispatch
      {
        id: 'action-create-dispatch',
        type: 'entityAction',
        position: { x: 400, y: 460 },
        data: {
          label: t('createDispatch'),
          type: 'dispatch',
          entityType: 'dispatch',
          actionType: 'create',
          icon: Truck,
          autoCreate: true,
          description: t('createDispatchDesc'),
          config: { createPerService: true, autoCreate: true },
        }
      },
      // Trigger: Dispatch Started
      {
        id: 'trigger-dispatch-in-progress',
        type: 'entityTrigger',
        position: { x: 100, y: 640 },
        data: {
          label: t('dispatchStarted'),
          type: 'dispatch-status-trigger',
          entityType: 'dispatch',
          icon: Truck,
          fromStatus: null,
          toStatus: dispatchInProgress,
          description: t('dispatchStartedDesc'),
        }
      },
      // Action: SO → In Progress
      {
        id: 'action-so-in-progress',
        type: 'entityAction',
        position: { x: 400, y: 640 },
        data: {
          label: t('soInProgress'),
          type: 'update-service-order-status',
          entityType: 'service_order',
          actionType: 'update-status',
          icon: ShoppingCart,
          newStatus: soInProgress,
          description: t('soInProgressDesc'),
          config: { newStatus: soInProgress },
        }
      },
      // Trigger: Dispatch Completed
      {
        id: 'trigger-dispatch-completed',
        type: 'entityTrigger',
        position: { x: 100, y: 820 },
        data: {
          label: t('dispatchComplete'),
          type: 'dispatch-status-trigger',
          entityType: 'dispatch',
          icon: Truck,
          fromStatus: dispatchInProgress,
          toStatus: dispatchCompleted,
          description: t('dispatchCompleteDesc'),
        }
      },
      // Condition: All Done?
      {
        id: 'condition-all-done',
        type: 'conditionNode',
        position: { x: 400, y: 820 },
        data: {
          label: t('allDone'),
          type: 'if-else',
          icon: GitBranch,
          description: t('allDoneDesc'),
          field: 'serviceOrder.dispatches',
          operator: 'all_match',
          value: dispatchCompleted,
          config: { 
            field: 'serviceOrder.dispatches', 
            operator: 'all_match', 
            value: dispatchCompleted,
            checkField: 'status',
            conditionData: {
              field: 'serviceOrder.dispatches',
              operator: 'all_match',
              value: dispatchCompleted,
              checkField: 'status'
            }
          },
        }
      },
      // Action: SO → Tech Complete
      {
        id: 'action-so-tech-complete',
        type: 'entityAction',
        position: { x: 720, y: 760 },
        data: {
          label: t('soTechComplete'),
          type: 'update-service-order-status',
          entityType: 'service_order',
          actionType: 'update-status',
          icon: ShoppingCart,
          newStatus: soTechComplete,
          description: t('soTechCompleteDesc'),
          config: { newStatus: soTechComplete },
        }
      },
      // Action: SO → Partial
      {
        id: 'action-so-partial',
        type: 'entityAction',
        position: { x: 720, y: 900 },
        data: {
          label: t('soPartial'),
          type: 'update-service-order-status',
          entityType: 'service_order',
          actionType: 'update-status',
          icon: ShoppingCart,
          newStatus: soPartial,
          description: t('soPartialDesc'),
          config: { newStatus: soPartial },
        }
      },
    ];

    const templateEdges: Edge[] = [
      { id: 'e1', source: 'trigger-offer-accepted', target: 'action-create-sale', type: 'smoothstep', animated: true },
      { id: 'e2', source: 'trigger-sale-in-progress', target: 'condition-has-services', type: 'smoothstep', animated: true },
      { id: 'e3', source: 'condition-has-services', target: 'action-create-service-order', sourceHandle: 'yes', type: 'smoothstep', animated: true, label: t('nodeUi.yes') },
      { id: 'e4', source: 'trigger-job-planned', target: 'action-create-dispatch', type: 'smoothstep', animated: true },
      { id: 'e5', source: 'trigger-dispatch-in-progress', target: 'action-so-in-progress', type: 'smoothstep', animated: true },
      { id: 'e6', source: 'trigger-dispatch-completed', target: 'condition-all-done', type: 'smoothstep', animated: true },
      { id: 'e7', source: 'condition-all-done', target: 'action-so-tech-complete', sourceHandle: 'yes', type: 'smoothstep', animated: true, label: t('nodeUi.yes') },
      { id: 'e8', source: 'condition-all-done', target: 'action-so-partial', sourceHandle: 'no', type: 'smoothstep', animated: true, label: t('nodeUi.no') },
    ];

    return { nodes: templateNodes, edges: templateEdges };
  }, [t]);

  // Auto-load default workflow from API on mount - always try backend first
  useEffect(() => {
    const loadDefaultWorkflow = async () => {
      if (initialized) return;
      
      setWorkflowLoading(true);
      
      // Always try to load from API - show explicit error if backend is down
      try {
        const defaultWorkflow = await workflowApi.getDefault();
        if (defaultWorkflow) {
          // Transform backend nodes to frontend n8n-style format
          const { nodes: transformedNodes, edges: transformedEdges } = transformWorkflowFromBackend({
            nodes: defaultWorkflow.nodes,
            edges: defaultWorkflow.edges,
          });
          
          if (transformedNodes.length > 0) {
            console.log('[Workflow] Loaded from backend:', transformedNodes.length, 'nodes');
            setNodes(transformedNodes);
            setEdges(transformedEdges);
            setCurrentWorkflowId(defaultWorkflow.id);
            setIsWorkflowActive(defaultWorkflow.isActive);
            setWorkflowVersion(defaultWorkflow.version || 1);
            setVersionStatus(defaultWorkflow.isActive ? 'active' : 'draft');
            setWorkflowLoading(false);
            setInitialized(true);
            return;
          } else {
            console.warn('Backend workflow has no nodes after transformation');
            toast.warning(t('workflowParseError'));
          }
        }
        
        // Backend returned null or empty - backend might be starting up
        console.warn('Backend returned empty workflow - server may be starting up');
        toast.warning(t('backendStarting'));
        
        // Still load local template but inform user
        const { nodes: fallbackNodes, edges: fallbackEdges } = getDefaultBusinessTemplate();
        setNodes(fallbackNodes);
        setEdges(fallbackEdges);
        setWorkflowLoading(false);
        setInitialized(true);
      } catch (err) {
        // Network error - backend truly unreachable
        console.error('Backend unreachable:', err);
        toast.error(t('backendUnreachable'));
        
        // Load local template as last resort
        const { nodes: fallbackNodes, edges: fallbackEdges } = getDefaultBusinessTemplate();
        setNodes(fallbackNodes);
        setEdges(fallbackEdges);
        setWorkflowLoading(false);
        setInitialized(true);
      }
    };

    loadDefaultWorkflow();
  }, [initialized, setNodes, setEdges, getDefaultBusinessTemplate, t]);

  // Center the view when workflow is loaded
  useEffect(() => {
    if (!workflowLoading && nodes.length > 0 && reactFlowInstance.current) {
      // Use a short delay to ensure nodes are rendered
      const timer = setTimeout(() => {
        reactFlowInstance.current?.fitView({
          padding: 0.3,
          includeHiddenNodes: false,
          minZoom: 0.5,
          maxZoom: 1,
          duration: 400,
        });
      }, 150);
      return () => clearTimeout(timer);
    }
  }, [workflowLoading, nodes.length]);

  const handleNodeClick = useCallback((nodeId: string, nodeData: any) => {
    // Prevent node editing when not in edit mode - users can still pan/zoom
    if (!isEditMode) return;
    
    // Use the new n8n-style config panel for entity/trigger/action/notification/email nodes
    const isN8nNode = nodeData.type?.includes('trigger') || 
                      nodeData.type?.includes('update') || 
                      nodeData.type?.includes('create') ||
                      nodeData.type?.includes('email') ||
                      nodeData.type?.includes('notification') ||
                      nodeData.type?.includes('approval') ||
                      nodeData.type?.includes('webhook') ||
                      nodeData.type?.includes('api') ||
                      nodeData.type?.includes('delay') ||
                      nodeData.type?.includes('database') ||
                      nodeData.type?.includes('ai-') ||
                      nodeData.type?.includes('loop') ||
                      nodeData.type?.includes('if-') ||
                      nodeData.type === 'switch' ||
                      ['offer', 'sale', 'service-order', 'service_order', 'dispatch', 'contact'].includes(nodeData.type);
    
    if (isN8nNode) {
      setConfigPanel({
        isOpen: true,
        nodeId,
        nodeData
      });
    } else {
      // Fall back to legacy modal for conditional nodes
      setConfigModal({
        isOpen: true,
        nodeData: { ...nodeData, id: nodeId }
      });
    }
  }, [isEditMode]);

  const handleRemoveNode = useCallback((nodeId: string) => {
    if (!isEditMode) return; // Prevent removal when not in edit mode
    setNodes((nds) => nds.filter(n => n.id !== nodeId));
    setEdges((eds) => eds.filter(e => e.source !== nodeId && e.target !== nodeId));
    setHasUnsavedChanges(true);
    toast(t('nodeRemoved'), { duration: 2000 });
  }, [setNodes, setEdges, t, isEditMode]);

  // CRITICAL: Memoize nodeTypes to prevent React Flow from remounting all nodes on every render
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const nodeTypes = useMemo(() => createNodeTypes(handleNodeClick, handleRemoveNode), [handleNodeClick, handleRemoveNode]);

  const onConnect = useCallback(
    (params: Connection) => {
      if (!isEditMode) return; // Prevent connections when not in edit mode
      setEdges((eds) =>
        addEdge(
          { ...params, type: 'smoothstep', animated: true },
          eds
        )
      );
      setHasUnsavedChanges(true);
    },
    [setEdges, isEditMode]
  );

  // Track changes when nodes or edges change
  const handleNodesChangeWithTracking = useCallback((changes: any) => {
    if (isEditMode && changes.length > 0) {
      // Only track actual changes, not selection changes
      const hasRealChange = changes.some((c: any) => 
        c.type === 'position' || c.type === 'remove' || c.type === 'add'
      );
      if (hasRealChange) {
        setHasUnsavedChanges(true);
      }
    }
    onNodesChange(changes);
  }, [onNodesChange, isEditMode]);

  const handleEdgesChangeWithTracking = useCallback((changes: any) => {
    if (isEditMode && changes.length > 0) {
      const hasRealChange = changes.some((c: any) => 
        c.type === 'remove' || c.type === 'add'
      );
      if (hasRealChange) {
        setHasUnsavedChanges(true);
      }
    }
    onEdgesChange(changes);
  }, [onEdgesChange, isEditMode]);

  // Save workflow to backend
  const handleSaveWorkflow = useCallback(async () => {
    if (!currentWorkflowId) {
      toast.error(t('noWorkflowSelected'));
      return;
    }
    
    setIsSaving(true);
    try {
      // Clean nodes - remove non-serializable icons
      const cleanNodes = nodes.map(n => ({
        ...n,
        data: { ...n.data, icon: undefined }
      }));
      
      await workflowApi.update(currentWorkflowId, {
        nodes: cleanNodes,
        edges: edges
      });
      
      setHasUnsavedChanges(false);
      setIsEditMode(false);
      toast.success(t('saved'), { duration: 2500 });
    } catch (err) {
      console.error('Failed to save workflow:', err);
      toast.error(t('saveFailed'));
    } finally {
      setIsSaving(false);
    }
  }, [currentWorkflowId, nodes, edges, t]);

  // Cancel edit mode
  const handleCancelEdit = useCallback(() => {
    if (hasUnsavedChanges) {
      // Could show a confirmation dialog here
      toast.warning(t('changesDiscarded'));
    }
    setIsEditMode(false);
    setHasUnsavedChanges(false);
  }, [hasUnsavedChanges, t]);

  const handleEdgeClick = useCallback((edgeId: string) => {
    setSelectedEdgeId(edgeId);
  }, []);

  const changeEdgeType = useCallback((edgeId: string, type: string) => {
    setEdges(eds => eds.map(e => e.id === edgeId ? { ...e, type } : e));
  }, [setEdges]);

  const toggleEdgeAnimated = useCallback((edgeId: string) => {
    setEdges(eds => eds.map(e => e.id === edgeId ? { ...e, animated: !e.animated } : e));
  }, [setEdges]);

  const reverseEdge = useCallback((edgeId: string) => {
    setEdges(eds => {
      const existing = eds.find(e => e.id === edgeId);
      if (!existing) return eds;

      // create a new edge object with swapped source/target and a fresh id
      const newId = `${existing.id}-rev-${Date.now()}`;
      const newEdge = {
        ...existing,
        id: newId,
        source: existing.target,
        target: existing.source,
      } as Edge;

      // replace the old edge with the new one
      const updated = eds.map(e => e.id === edgeId ? newEdge : e);
      // set the selected edge to the new id
      setSelectedEdgeId(newId);
      return updated;
    });
  }, [setEdges]);

  const createBusinessTemplate = useCallback(() => {
    const templateNodes: Node[] = [
      {
        id: 'contact-1',
        type: 'workflowNode',
        position: { x: 150, y: 200 },
  data: { label: t('node.contact.label'), type: 'contact', icon: Users, description: t('node.contact.desc') }
      },
      {
        id: 'offer-1',
        type: 'workflowNode',
        position: { x: 400, y: 200 },
  data: { label: t('node.offer.label'), type: 'offer', icon: FileText, description: t('node.offer.desc') }
      },
      {
        id: 'email-offer-1',
        type: 'workflowNode',
        position: { x: 400, y: 350 },
  data: { label: t('node.email-llm.label'), type: 'email-llm', icon: Sparkles, description: t('node.email-llm.desc') }
      },
      {
        id: 'sale-1',
        type: 'workflowNode',
        position: { x: 650, y: 200 },
  data: { label: t('node.sale.label'), type: 'sale', icon: DollarSign, description: t('node.sale.desc') }
      },
      {
        id: 'service-1',
        type: 'workflowNode',
        position: { x: 900, y: 200 },
  data: { label: t('node.service-order.label'), type: 'service-order', icon: ShoppingCart, description: t('node.service-order.desc') }
      },
      {
        id: 'dispatch-1',
        type: 'workflowNode',
        position: { x: 1150, y: 200 },
  data: { label: t('node.dispatch.label'), type: 'dispatch', icon: Truck, description: t('node.dispatch.desc') }
      }
    ];

    const templateEdges: Edge[] = [
      { id: 'e1-2', source: 'contact-1', target: 'offer-1', type: 'smoothstep', animated: true },
      { id: 'e2-3', source: 'offer-1', target: 'email-offer-1', type: 'smoothstep' },
      { id: 'e2-4', source: 'offer-1', target: 'sale-1', type: 'smoothstep', animated: true },
      { id: 'e4-5', source: 'sale-1', target: 'service-1', type: 'smoothstep', animated: true },
      { id: 'e5-6', source: 'service-1', target: 'dispatch-1', type: 'smoothstep', animated: true }
    ];

  // Ensure labels/descriptions respect current locale and user's custom name if present
  setNodes(templateNodes.map((n) => ({
    ...n,
    data: {
      ...n.data,
      label: (n.data as any)?.config?.name || (n.data as any)?.label || getNodeLabel((n.data as any)?.type),
      description: (n.data as any)?.description || getNodeDescription((n.data as any)?.type),
    }
  })));
  setEdges(templateEdges);
  toast(t('templateCreated'), { duration: 2000 });
  }, [setNodes, setEdges]);

  const addNode = useCallback((type: string) => {
    if (type === 'template-business') {
      createBusinessTemplate();
      return;
    }

    const nodeType = getNodeType(type);
    const newNode: Node = {
      id: `node-${Date.now()}`,
      type: nodeType,
      position: { x: Math.random() * 500 + 200, y: Math.random() * 400 + 150 },
      data: {
        label: getNodeLabel(type),
        type,
        icon: getNodeIcon(type),
        description: getNodeDescription(type)
      },
    };
    setNodes((nds) => [...nds, newNode]);
  }, [setNodes, createBusinessTemplate]);

  // Add node from palette template (n8n-style)
  const addNodeFromTemplate = useCallback((template: NodeTemplate) => {
    const getN8nNodeType = (tmpl: NodeTemplate) => {
      if (tmpl.type === 'loop') return 'loopNode';
      if (tmpl.type === 'parallel') return 'parallelNode';
      if (tmpl.type === 'try-catch') return 'tryCatchNode';
      if (tmpl.isTrigger && tmpl.entityType) return 'entityTrigger';
      if (tmpl.isAction && tmpl.entityType) return 'entityAction';
      if (tmpl.type === 'if-else' || tmpl.type === 'switch') return 'conditionNode';
      if (tmpl.entityType) return 'n8nNode';
      return 'n8nNode';
    };

    // Find empty space below existing nodes
    const currentNodes = reactFlowInstance.current?.getNodes() || [];
    let posX = 400;
    let posY = 150;

    if (currentNodes.length > 0) {
      let maxY = -Infinity;
      let maxYNodeX = 400;
      currentNodes.forEach(n => {
        const ny = (n.position?.y ?? 0) + 120;
        if (ny > maxY) {
          maxY = ny;
          maxYNodeX = n.position?.x ?? 400;
        }
      });
      posX = maxYNodeX;
      posY = maxY + 60;
    }

    const nodeType = getN8nNodeType(template);
    const nodeId = `node-${Date.now()}`;
    const newNode: Node = {
      id: nodeId,
      type: nodeType,
      position: { x: posX, y: posY },
      data: {
        label: t(template.label),
        type: template.type,
        category: template.category,
        entityType: template.entityType,
        icon: template.icon,
        description: t(template.description),
        isTrigger: template.isTrigger,
        isAction: template.isAction,
        config: template.defaultConfig || {},
        actionType: template.type.includes('create') ? 'create' : 
                    template.type.includes('update') ? 'update-status' : undefined,
      },
    };
    setNodes((nds) => [...nds, newNode]);

    // Auto-pan to the new node
    setTimeout(() => {
      reactFlowInstance.current?.fitView({
        nodes: [{ id: nodeId }],
        padding: 1.5,
        duration: 300,
      });
    }, 50);

    toast(t(template.label), {
      description: t('nodeAdded', { name: t(template.label) }) || 'Added to canvas',
      duration: 2000,
    });
  }, [setNodes, t]);

  // Apply AI-generated workflow as a separate group (doesn't replace existing)
  const handleApplyAIWorkflow = useCallback((newNodes: Node[], newEdges: Edge[], name: string) => {
    // Offset AI nodes to not overlap with existing ones
    const existingMaxX = nodes.reduce((max, n) => Math.max(max, n.position.x), 0);
    const offsetX = existingMaxX > 0 ? existingMaxX + 400 : 0;
    
    const offsetNodes = newNodes.map(n => ({
      ...n,
      position: { x: n.position.x + offsetX, y: n.position.y },
    }));

    setNodes(prev => [...prev, ...offsetNodes]);
    setEdges(prev => [...prev, ...newEdges]);
    setHasUnsavedChanges(true);
    if (!isEditMode) setIsEditMode(true);
  }, [nodes, setNodes, setEdges, isEditMode]);

  const getNodeType = (type: string) => {
    switch (type) {
      case 'if-else': return 'ifElseNode';
      case 'switch': return 'switchNode';
      case 'loop': return 'loopNode';
      case 'parallel': return 'parallelNode';
      case 'try-catch': return 'tryCatchNode';
      // n8n-style entity nodes
      case 'offer-status-trigger':
      case 'sale-status-trigger':
      case 'service-order-status-trigger':
      case 'dispatch-status-trigger':
        return 'entityTrigger';
      case 'create-offer':
      case 'create-sale':
      case 'create-service-order':
      case 'create-dispatch':
      case 'update-offer-status':
      case 'update-sale-status':
      case 'update-service-order-status':
      case 'update-dispatch-status':
        return 'entityAction';
      default: return 'workflowNode';
    }
  };

  const getNodeLabel = (type: string) => {
    switch (type) {
      // Business Process
  case 'contact': return t('node.contact.label');
  case 'offer': return t('node.offer.label');
  case 'sale': return t('node.sale.label');
  case 'service-order': return t('node.service-order.label');
  case 'dispatch': return t('node.dispatch.label');
      
      // Communication
  case 'email': return t('node.email.label');
  case 'email-template': return t('node.email-template.label');
  case 'email-llm': return t('node.email-llm.label');
      
      // AI/LLM
  case 'llm-writer': return t('node.llm-writer.label');
  case 'llm-analyzer': return t('node.llm-analyzer.label');
  case 'llm-personalizer': return t('node.llm-personalizer.label');
      
      // Triggers
  case 'trigger': return t('node.trigger.label');
  case 'webhook': return t('node.webhook.label');
  case 'scheduled': return t('node.scheduled.label');
      
      // Logic
  case 'condition': return t('node.condition.label');
  case 'filter': return t('node.filter.label');
      
      // Advanced Flow Control
  case 'if-else': return t('node.if-else.label');
  case 'switch': return t('node.switch.label');
  case 'loop': return t('node.loop.label');
  case 'parallel': return t('node.parallel.label');
  case 'try-catch': return t('node.try-catch.label');
      
      // Generic Actions
  case 'action': return t('node.action.label');
  case 'database': return t('node.database.label');
  case 'api': return t('node.api.label');
      
  default: return t('node.default.desc') || 'Node';
    }
  };

  const getNodeIcon = (type: string) => {
    switch (type) {
      // Business Process
      case 'contact': return Users;
      case 'offer': return FileText;
      case 'sale': return DollarSign;
      case 'service-order': return ShoppingCart;
      case 'dispatch': return Truck;
      
      // Communication
      case 'email': return Mail;
      case 'email-template': return Send;
      case 'email-llm': return Sparkles;
      
      // AI/LLM
      case 'llm-writer': return Brain;
      case 'llm-analyzer': return Bot;
      case 'llm-personalizer': return Sparkles;
      case 'ai-agent': return Bot;
      
      // Human-in-the-loop
      case 'human-input-form': return ClipboardList;
      case 'wait-for-event': return PauseCircle;
      
      // Integration
      case 'dynamic-form': return ClipboardList;
      case 'data-transfer': return ArrowLeftRight;
      case 'http-request': return Globe;
      case 'custom-llm': return Settings;
      
      // Triggers
      case 'trigger': return Play;
      case 'webhook': return Webhook;
      case 'scheduled': return Calendar;
      
      // Logic
      case 'condition': return GitBranch;
      case 'filter': return Settings;
      
      // Advanced Flow Control
      case 'if-else': return GitBranch;
      case 'switch': return Menu;
      case 'loop': return RotateCcw;
      case 'parallel': return Split;
      case 'try-catch': return Shield;
      
      // Generic Actions
      case 'action': return Zap;
      case 'database': return Database;
      case 'api': return Settings;
      
      default: return Plus;
    }
  };

  const getNodeDescription = (type: string) => {
    switch (type) {
      // Business Process
  case 'contact': return t('node.contact.desc');
  case 'offer': return t('node.offer.desc');
  case 'sale': return t('node.sale.desc');
  case 'service-order': return t('node.service-order.desc');
  case 'dispatch': return t('node.dispatch.desc');
      
      // Communication
  case 'email': return t('node.email.desc');
  case 'email-template': return t('node.email-template.desc');
  case 'email-llm': return t('node.email-llm.desc');
      
      // AI/LLM
  case 'llm-writer': return t('node.llm-writer.desc');
  case 'llm-analyzer': return t('node.llm-analyzer.desc');
  case 'llm-personalizer': return t('node.llm-personalizer.desc');
      
      // Triggers
  case 'trigger': return t('node.trigger.desc');
  case 'webhook': return t('node.webhook.desc');
  case 'scheduled': return t('node.scheduled.desc');
      
      // Logic
  case 'condition': return t('node.condition.desc');
  case 'filter': return t('node.filter.desc');
      
      // Advanced Flow Control
  case 'if-else': return t('node.if-else.desc');
  case 'switch': return t('node.switch.desc');
  case 'loop': return t('node.loop.desc');
  case 'parallel': return t('node.parallel.desc');
  case 'try-catch': return t('node.try-catch.desc');
      
      // Generic Actions
  case 'action': return t('node.action.desc');
  case 'database': return t('node.database.desc');
  case 'api': return t('node.api.desc');
      
  default: return t('node.default.desc') || 'Custom node';
    }
  };

  const executionTimeoutRef = useRef<NodeJS.Timeout[]>([]);

  // Helper function to determine execution order from edges
  const getExecutionOrder = useCallback((nodes: Node[], edges: Edge[]): string[] => {
    const order: string[] = [];
    const visited = new Set<string>();
    
    // Find start nodes (nodes with no incoming edges)
    const targetNodes = new Set(edges.map(e => e.target));
    const startNodes = nodes.filter(n => !targetNodes.has(n.id));
    
    // BFS to get order
    const queue = startNodes.map(n => n.id);
    
    while (queue.length > 0) {
      const nodeId = queue.shift()!;
      if (visited.has(nodeId)) continue;
      
      visited.add(nodeId);
      order.push(nodeId);
      
      // Find all outgoing edges and add targets
      edges
        .filter(e => e.source === nodeId)
        .forEach(e => {
          if (!visited.has(e.target)) {
            queue.push(e.target);
          }
        });
    }
    
    // Add any nodes not connected
    nodes.forEach(n => {
      if (!visited.has(n.id)) {
        order.push(n.id);
      }
    });
    
    return order;
  }, []);

  const stopExecution = useCallback(() => {
    // Clear all pending timeouts
    executionTimeoutRef.current.forEach(timeout => clearTimeout(timeout));
    executionTimeoutRef.current = [];
    
    // Reset all node states and progress
    setNodes(nds => nds.map(n => ({
      ...n,
      data: { ...n.data, executionState: 'idle' as NodeExecutionState }
    })));
    setIsRunning(false);
    setExecutionProgress(0);
  }, [setNodes]);

  // Show confirmation modal before stopping workflow
  const handleStopWorkflowClick = useCallback(() => {
    if (isWorkflowActive) {
      setStopConfirmModal(true);
    } else {
      // If not active, just start it (no confirmation needed)
      confirmToggleWorkflow();
    }
  }, [isWorkflowActive]);

  // Actually toggle workflow active state (called after confirmation)
  const confirmToggleWorkflow = useCallback(async () => {
    if (!currentWorkflowId) {
      toast.error(t('noWorkflowToToggle'));
      return;
    }
    
    setStopConfirmModal(false);
    
    try {
      if (isWorkflowActive) {
        // Deactivate workflow
        const success = await workflowApi.deactivate(currentWorkflowId);
        if (success) {
          setIsWorkflowActive(false);
          toast.success(t('workflowStoppedMessage'));
        } else {
          toast.error(t('failedToStopWorkflow'));
        }
      } else {
        // Activate workflow
        const success = await workflowApi.activate(currentWorkflowId);
        if (success) {
          setIsWorkflowActive(true);
          toast.success(t('workflowStarted'));
        } else {
          toast.error(t('failedToStartWorkflow'));
        }
      }
    } catch (err) {
      console.error('Failed to toggle workflow:', err);
      toast.error(t('failedToToggleWorkflow'));
    }
  }, [currentWorkflowId, isWorkflowActive, t]);

  const handleRunWorkflow = useCallback(() => {
    if (nodes.length === 0) {
      return;
    }
    
    // Import and use validator
    import('../utils/workflowValidator').then(({ WorkflowValidator }) => {
      const validation = WorkflowValidator.validateWorkflow(nodes, edges);
      
      if (!validation.isValid) {
        return;
      }
      
      setIsRunning(true);
      setExecutionProgress(0);

      // Build execution order by traversing edges
      const nodeOrder = getExecutionOrder(nodes, edges);
      const totalNodes = nodeOrder.length;
      
      // Set all nodes to waiting state first
      setNodes(nds => nds.map(n => ({
        ...n,
        data: { ...n.data, executionState: 'waiting' as NodeExecutionState }
      })));

      // Animate through each node sequentially with smoother timing
      nodeOrder.forEach((nodeId, index) => {
        const timeout = setTimeout(() => {
          // Update progress when starting a node
          setExecutionProgress(Math.round(((index + 0.5) / totalNodes) * 100));
          
          // Set current node to executing, keep previous as completed
          setNodes(nds => nds.map(n => ({
            ...n,
            data: {
              ...n.data,
              executionState: n.id === nodeId 
                ? 'executing' as NodeExecutionState 
                : n.data.executionState === 'executing'
                ? 'completed' as NodeExecutionState
                : n.data.executionState
            }
          })));

          // After a delay, mark as completed
          const completeTimeout = setTimeout(() => {
            // Update progress when completing a node
            setExecutionProgress(Math.round(((index + 1) / totalNodes) * 100));
            
            setNodes(nds => nds.map(n => ({
              ...n,
              data: {
                ...n.data,
                executionState: n.id === nodeId 
                  ? 'completed' as NodeExecutionState 
                  : n.data.executionState
              }
            })));

            // If this is the last node, complete the workflow
            if (index === nodeOrder.length - 1) {
              setTimeout(() => {
                setIsRunning(false);
                setExecutionProgress(100);
                
                // Reset after showing completion
                setTimeout(() => {
                  setNodes(nds => nds.map(n => ({
                    ...n,
                    data: { ...n.data, executionState: 'idle' as NodeExecutionState }
                  })));
                  setExecutionProgress(0);
                }, 3000);
              }, 600);
            }
          }, 600);
          
          executionTimeoutRef.current.push(completeTimeout);
        }, index * 1000);
        
        executionTimeoutRef.current.push(timeout);
      });
    });
  }, [nodes, edges, setNodes, getExecutionOrder]);

  const handleLoadWorkflow = (workflow: SavedWorkflow) => {
  const normalize = (node: Node) => ({
    ...node,
    data: {
      ...node.data,
      label: (node.data as any)?.config?.name || (node.data as any)?.label || getNodeLabel((node.data as any)?.type),
      description: (node.data as any)?.description || getNodeDescription((node.data as any)?.type),
    }
  });

  setNodes(workflow.nodes.map(normalize));
  setEdges(workflow.edges);
  toast.success(t('workflowLoaded', { name: workflow.name }));
  };

  const handleNewWorkflow = async () => {
    // Reload default workflow from API with proper transformation
    try {
      const defaultWorkflow = await workflowApi.getDefault();
      if (defaultWorkflow) {
        const { nodes: transformedNodes, edges: transformedEdges } = transformWorkflowFromBackend({
          nodes: defaultWorkflow.nodes,
          edges: defaultWorkflow.edges,
        });
        if (transformedNodes.length > 0) {
          setNodes(transformedNodes);
          setEdges(transformedEdges);
        } else {
          setNodes([]);
          setEdges([]);
        }
      } else {
        setNodes([]);
        setEdges([]);
      }
    } catch (err) {
      setNodes([]);
      setEdges([]);
    }
    toast.success(t('newWorkflowCreated'));
  };


  // Legacy config save handler - also spread all fields to top-level for backend
  const handleConfigSave = useCallback((config: any) => {
    setNodes((nds) =>
      nds.map((node) =>
        node.id === configModal.nodeData?.id
          ? {
              ...node,
              data: {
                ...node.data,
                config,
                label: config.name || node.data.label,
                description: config.description || node.data.description,
                // Spread condition fields for if-else/switch/condition nodes
                // ConditionalConfigModal stores if-else as { condition: { field, operator, value } }
                // NodeConfigPanel stores flat as { field, operator, value }
                field: config.field || config.conditionField || config.condition?.field,
                operator: config.operator || config.condition?.operator,
                value: config.value || config.expectedValue || config.condition?.value,
                // Spread trigger fields
                fromStatus: config.fromStatus,
                toStatus: config.toStatus,
                triggerType: config.triggerType,
                // Spread action fields
                newStatus: config.newStatus,
                autoCreate: config.autoCreate,
                // Spread email fields
                subject: config.subject,
                template: config.template,
                to: config.recipientType || config.to,
                // Spread approval fields
                approverRole: config.approverRole,
                timeoutHours: config.timeoutHours,
                // Spread notification fields
                title: config.notificationTitle || config.approvalTitle,
                message: config.notificationMessage || config.approvalMessage,
              }
            }
          : node
      )
    );
    toast.success(t('configSaved'));
  }, [configModal.nodeData?.id, setNodes, t]);

  // Handler for n8n-style config panel
  // CRITICAL: Spread ALL config fields to top-level node.data so backend WorkflowNodeExecutor
  // can read them via GetNodeDataString (which checks both top-level and nested config)
  const handlePanelConfigSave = useCallback((nodeId: string, config: NodeConfig) => {
    setNodes((nds) =>
      nds.map((node) =>
        node.id === nodeId
          ? {
              ...node,
              data: {
                ...node.data,
                config,
                // Spread all config fields to top-level for backend compatibility
                label: config.name || node.data.label,
                description: config.description || node.data.description,
                // Status trigger/action fields
                fromStatus: config.fromStatus,
                toStatus: config.toStatus,
                newStatus: config.newStatus,
                // Action fields
                autoCreate: config.autoCreate,
                createPerService: config.createPerService,
                // Condition fields (critical for if-else/switch nodes)
                field: config.field,
                operator: config.operator,
                value: config.value,
                // Approval fields
                requiresApproval: config.requiresApproval,
                approverRole: config.approverRole,
                timeoutHours: config.timeoutHours,
                // Notification fields
                title: config.notificationTitle,
                message: config.notificationMessage,
                notificationType: config.notificationType,
                recipientType: config.recipientType,
                // Email fields
                subject: config.emailSubject,
                template: config.emailTemplate,
                // AI fields
                model: config.model,
                prompt: config.prompt,
                maxTokens: config.maxTokens,
                // Loop fields
                loopType: config.loopType,
                iterations: config.iterations,
                // Delay fields
                delayMs: config.delayMs,
                // Webhook/API fields
                url: config.url,
                method: config.method,
                headers: config.headers,
                body: config.body,
                // Scheduled fields
                cronExpression: config.cronExpression,
                timezone: config.timezone,
                // Database fields
                operation: config.operation,
                table: config.table,
                // Email to field
                to: config.to,
              }
            }
          : node
      )
    );
    setConfigPanel({ isOpen: false, nodeId: '', nodeData: null });
    toast.success(t('configSaved'));
  }, [setNodes, t]);

  const handleImportWorkflow = useCallback((importedNodes: Node[], importedEdges: Edge[], name?: string) => {
    // Restore icons and normalize node data for imported workflows
    const normalizedNodes = importedNodes.map((node) => {
      const nodeType = (node.data as any)?.type || '';
      return {
        ...node,
        data: {
          ...node.data,
          // Restore icon based on node type (icons can't be serialized to JSON)
          icon: getNodeIcon(nodeType),
          // Ensure label and description are set
          label: (node.data as any)?.config?.name || (node.data as any)?.label || getNodeLabel(nodeType),
          description: (node.data as any)?.description || getNodeDescription(nodeType),
          // Reset execution state
          executionState: 'idle' as NodeExecutionState,
        }
      };
    });

    // Normalize edges to ensure proper rendering
    const normalizedEdges = importedEdges.map((edge) => ({
      ...edge,
      type: edge.type || 'smoothstep',
      animated: edge.animated ?? false,
      markerEnd: edge.markerEnd || {
        type: MarkerType.ArrowClosed,
        width: 15,
        height: 15,
      },
    }));

    setNodes(normalizedNodes);
    setEdges(normalizedEdges);
    setImportModal(false);
    toast.success(t('importedWorkflowLoaded', { name: name || t('importedDefaultName') }));
  }, [setNodes, setEdges, t, getNodeIcon, getNodeLabel, getNodeDescription]);

  // Copy workflow configuration to clipboard for debugging/sharing - fetches from API for accuracy
  const handleCopyConfiguration = useCallback(async () => {
    try {
      // Fetch the actual configuration from the backend API for accuracy
      let apiWorkflow = null;
      if (currentWorkflowId) {
        try {
          apiWorkflow = await workflowApi.getById(currentWorkflowId);
        } catch (e) {
          console.warn('Failed to fetch workflow from API, using local state:', e);
        }
      }

      // Parse nodes/edges from API response (handles both string and object formats)
      const parseJsonField = (field: any) => {
        if (!field) return [];
        if (typeof field === 'string') {
          try {
            return JSON.parse(field);
          } catch {
            return [];
          }
        }
        return field;
      };

      // Use API data if available, otherwise fall back to local state
      const sourceNodes = apiWorkflow ? parseJsonField(apiWorkflow.nodes) : nodes;
      const sourceEdges = apiWorkflow ? parseJsonField(apiWorkflow.edges) : edges;
      const triggers = apiWorkflow?.triggers || [];

      // Build a clean export of the workflow configuration
      const workflowConfig = {
        _info: {
          source: apiWorkflow ? 'backend_api' : 'local_state',
          exportedAt: new Date().toISOString(),
          workflowId: currentWorkflowId,
          workflowName: apiWorkflow?.name || 'Unknown',
          isActive: apiWorkflow?.isActive ?? isWorkflowActive,
          version: apiWorkflow?.version || 1,
          nodeCount: sourceNodes.length,
          edgeCount: sourceEdges.length,
          triggerCount: triggers.length,
        },
        nodes: sourceNodes.map((node: any) => ({
          id: node.id,
          type: node.type,
          position: node.position,
          data: {
            label: node.data?.label,
            type: node.data?.type,
            entityType: node.data?.entityType,
            actionType: node.data?.actionType,
            fromStatus: node.data?.fromStatus,
            toStatus: node.data?.toStatus,
            newStatus: node.data?.newStatus,
            targetEntity: node.data?.targetEntity,
            targetStatus: node.data?.targetStatus,
            field: node.data?.field,
            operator: node.data?.operator,
            value: node.data?.value,
            expectedValue: node.data?.expectedValue,
            description: node.data?.description,
            config: node.data?.config,
            autoCreate: node.data?.autoCreate,
            createPerService: node.data?.createPerService,
          }
        })),
        edges: sourceEdges.map((edge: any) => ({
          id: edge.id,
          source: edge.source,
          target: edge.target,
          sourceHandle: edge.sourceHandle,
          targetHandle: edge.targetHandle,
          type: edge.type,
          label: edge.label,
        })),
        triggers: triggers.map((trigger: any) => ({
          id: trigger.id,
          nodeId: trigger.nodeId,
          entityType: trigger.entityType,
          fromStatus: trigger.fromStatus || null,
          toStatus: trigger.toStatus,
          isActive: trigger.isActive,
        })),
      };

      const configText = JSON.stringify(workflowConfig, null, 2);
      await navigator.clipboard.writeText(configText);
      
      setIsCopied(true);
      toast.success(
        apiWorkflow 
          ? t('configCopiedFromApi')
          : t('configCopied')
      );
      
      setTimeout(() => setIsCopied(false), 2000);
    } catch (error) {
      console.error('Failed to copy configuration:', error);
      toast.error(t('configCopyFailed'));
    }
  }, [nodes, edges, currentWorkflowId, isWorkflowActive, t]);

  // Workflow Reconciliation - manually trigger the background job with live visualization
  const handleReconciliation = useCallback(async () => {
    setIsReconciling(true);
    setIsRunning(true);
    setExecutionProgress(0);
    
    // Set all nodes to waiting state for visual feedback
    setNodes(nds => nds.map(n => ({
      ...n,
      data: { ...n.data, executionState: 'waiting' as NodeExecutionState }
    })));
    
    try {
      // Get execution order for sequential animation
      const nodeOrder = getExecutionOrder(nodes, edges);
      const totalNodes = nodeOrder.length;
      
      // Animate through each node in execution order
      for (let i = 0; i < totalNodes; i++) {
        const currentNodeId = nodeOrder[i];
        setExecutionProgress(Math.round(((i + 0.5) / totalNodes) * 100));
        
        // Set current node to executing
        setNodes(nds => nds.map(n => ({
          ...n,
          data: { 
            ...n.data, 
            executionState: n.id === currentNodeId 
              ? 'executing' as NodeExecutionState 
              : n.data.executionState === 'executing'
              ? 'completed' as NodeExecutionState
              : n.data.executionState 
          }
        })));
        
        // Delay for visual effect
        await new Promise(resolve => setTimeout(resolve, 500));
        
        // Mark as completed
        setNodes(nds => nds.map(n => ({
          ...n,
          data: { 
            ...n.data, 
            executionState: n.id === currentNodeId 
              ? 'completed' as NodeExecutionState 
              : n.data.executionState 
          }
        })));
        
        setExecutionProgress(Math.round(((i + 1) / totalNodes) * 100));
      }
      
      // Now actually call the backend
      await workflowReconciliationApi.run();
      
      setExecutionProgress(100);
      
      // Reset nodes after a delay
      setTimeout(() => {
        setNodes(nds => nds.map(n => ({
          ...n,
          data: { ...n.data, executionState: 'idle' as NodeExecutionState }
        })));
        setExecutionProgress(0);
        setIsRunning(false);
      }, 3000);
      
    } catch (error) {
      console.error('Reconciliation failed:', error);
      
      // Mark all nodes as failed
      setNodes(nds => nds.map(n => ({
        ...n,
        data: { ...n.data, executionState: 'failed' as NodeExecutionState }
      })));
      
      // Reset after showing error
      setTimeout(() => {
        setNodes(nds => nds.map(n => ({
          ...n,
          data: { ...n.data, executionState: 'idle' as NodeExecutionState }
        })));
        setExecutionProgress(0);
        setIsRunning(false);
      }, 3000);
    } finally {
      setIsReconciling(false);
    }
  }, [setNodes, nodes, edges, getExecutionOrder]);

  // Keep the ref updated with the latest reconciliation handler
  useEffect(() => {
    handleReconciliationRef.current = handleReconciliation;
  }, [handleReconciliation]);

  return (
    <div className="workflow-module h-full flex flex-col bg-background">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-border bg-card/80 backdrop-blur-sm">
        <div className="flex items-center gap-3">
          {/* Workflow Version Badge */}
          <WorkflowVersionBadge
            currentVersion={workflowVersion}
            versionStatus={versionStatus}
            isActive={isWorkflowActive}
            onCreateDraft={() => {
              setVersionStatus('draft');
              setIsEditMode(true);
              toast.success(t('version.draftCreated'));
            }}
            onPromoteToActive={() => {
              setVersionStatus('active');
              handleSaveWorkflow();
            }}
            onArchive={() => {
              setVersionStatus('archived');
              toast.success(t('version.archived'));
            }}
          />
          {liveExecutionId && (
            <span className="text-xs text-muted-foreground animate-pulse">
              {t('executionId')} #{liveExecutionId}
            </span>
          )}
        </div>
        
        <div className="flex items-center gap-2">
          {/* Edit Mode Indicator */}
          {isEditMode && (
            <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-warning/20 text-xs text-warning-foreground">
              <Edit3 className="h-3 w-3" />
              <span>{t('editMode')}</span>
              {hasUnsavedChanges && (
                <span className="ml-1 font-bold">•</span>
              )}
            </div>
          )}
          
          {/* Next Run Countdown - only show when not editing */}
          {!isEditMode && (
            <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-muted/50 text-xs text-muted-foreground">
              <span>{t('nextRunIn')}</span>
              <span className="font-medium text-foreground">{formatCountdown(nextRunCountdown)}</span>
            </div>
          )}
          
          {/* Build with AI Button */}
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="outline"
                  onClick={() => setAiBuilderOpen(true)}
                  size="sm"
                  className="h-8 gap-1.5 border-primary/30 text-primary hover:bg-primary/10"
                >
                  <Wand2 className="h-4 w-4" />
                  <span className="hidden sm:inline text-xs">{t('ai.buildWithAI', 'Build with AI')}</span>
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>{t('ai.buildWithAITooltip', 'Describe a workflow and let AI build it')}</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>

          {/* Debug Console Button */}
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="outline"
                  onClick={() => setDebugConsoleOpen(true)}
                  disabled={!currentWorkflowId}
                  size="sm"
                  className="h-8 w-8 p-0"
                >
                  <Bug className="h-4 w-4" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>{t('debug.openConsole')}</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
          
          {/* Copy Configuration Button */}
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="outline"
                  onClick={handleCopyConfiguration}
                  disabled={nodes.length === 0}
                  size="sm"
                  className="h-8 w-8 p-0"
                >
                  {isCopied ? <Check className="h-4 w-4 text-success" /> : <Copy className="h-4 w-4" />}
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>{t('copyConfig')}</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
          
          <Button
            variant="outline"
            onClick={() => setImportModal(true)}
            size="sm"
            className="h-8 w-8 p-0"
            disabled={!isEditMode}
          >
            <Upload className="h-4 w-4" />
          </Button>
          
          <Button
            variant="outline"
            onClick={() => setExportModal(true)}
            disabled={nodes.length === 0}
            size="sm"
            className="h-8 w-8 p-0"
          >
            <Download className="h-4 w-4" />
          </Button>
          
          <WorkflowManager
            nodes={nodes}
            edges={edges}
            onLoadWorkflow={handleLoadWorkflow}
            onNewWorkflow={handleNewWorkflow}
          />
          
          {/* Edit/Save Toggle */}
          {isEditMode ? (
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                onClick={handleCancelEdit}
                disabled={isSaving}
                size="sm"
              >
                <X className="h-4 w-4 mr-2" />
                {t('cancel')}
              </Button>
              <Button
                onClick={handleSaveWorkflow}
                disabled={isSaving || !hasUnsavedChanges}
                className="bg-primary text-primary-foreground hover:bg-primary/90 shadow-sm"
                size="sm"
              >
                {isSaving ? (
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                ) : (
                  <Save className="h-4 w-4 mr-2" />
                )}
                {t('save')}
              </Button>
            </div>
          ) : (
            <Button
              variant="outline"
              onClick={() => setIsEditMode(true)}
              disabled={workflowLoading || nodes.length === 0}
              size="sm"
            >
              <Edit3 className="h-4 w-4 mr-2" />
              {t('common.edit')}
            </Button>
          )}
          
          {/* Workflow Active/Inactive Toggle - only show when not editing */}
          {!isEditMode && (
            <>
              {isRunning ? (
                <Button 
                  onClick={stopExecution}
                  variant="destructive"
                  className="shadow-sm flex items-center gap-2"
                >
                  <Square className="h-4 w-4 mr-2" />
                  {t('stopExecution')}
                </Button>
              ) : isWorkflowActive ? (
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button 
                        onClick={handleStopWorkflowClick}
                        disabled={nodes.length === 0 || workflowLoading}
                        variant="destructive"
                        className="shadow-sm flex items-center gap-2"
                      >
                        <Square className="h-4 w-4 mr-2" />
                        {t('stop')}
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>
                      <p>{t('stopTooltip')}</p>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              ) : (
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button 
                        onClick={confirmToggleWorkflow}
                        disabled={nodes.length === 0 || workflowLoading}
                        className="bg-primary text-primary-foreground hover:bg-primary/90 shadow-sm flex items-center gap-2"
                      >
                        <Play className="h-4 w-4 mr-2" />
                        {t('start')}
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>
                      <p>{t('startTooltip')}</p>
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              )}
            </>
          )}
          
        </div>
      </div>

      <div className="flex-1 flex overflow-hidden">
        {/* n8n-style Node Palette */}
        <NodePalette 
          onAddNode={addNodeFromTemplate}
          nextRunCountdown={nextRunCountdown}
          onReconcile={handleReconciliation}
          isReconciling={isReconciling}
          isConnected={isConnected}
          isEditMode={isEditMode}
        />
        

        {/* Canvas */}
        <div className="flex-1 relative bg-gradient-to-br from-background to-muted/20">
          {/* Execution Progress Bar - Top */}
          {isRunning && (
            <div className="absolute top-0 left-0 right-0 z-50">
              <div className="h-1 bg-muted/50">
                <div 
                  className="h-full bg-gradient-to-r from-primary via-primary/80 to-success transition-all duration-300 ease-out relative overflow-hidden" 
                  style={{ width: `${executionProgress}%` }}
                >
                  <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/30 to-transparent" style={{ animation: 'progressShimmer 1.5s ease-in-out infinite' }} />
                </div>
              </div>
            </div>
          )}
          
          {/* Loading State */}
          {workflowLoading ? (
            <WorkflowLoadingSkeleton />
          ) : nodes.length === 0 ? (
            <div className="absolute inset-0 flex items-center justify-center">
              <div className="text-center">
                <div className="mb-4 p-4 rounded-full bg-primary/10 w-fit mx-auto">
                  <Plus className="h-8 w-8 text-primary" />
                </div>
                <h3 className="text-lg font-medium mb-2 flex items-center gap-2 justify-center">
                  {t('gettingStartedTitle')}
                </h3>
                <p className="text-muted-foreground mb-4 max-w-sm">
                  {t('gettingStartedDescription')}
                </p>
                <Button variant="outline" onClick={() => addNode('trigger')}>
                  <Plus className="h-4 w-4 mr-2" />
                  {t('emptyAddTrigger')}
                </Button>
              </div>
            </div>
          ) : null}
          
          <ReactFlow
            nodes={nodes}
            edges={edges}
            onNodesChange={handleNodesChangeWithTracking}
            onEdgesChange={handleEdgesChangeWithTracking}
            onConnect={onConnect}
            onEdgeClick={(evt, edge) => { 
              if (!isEditMode) return; // Only allow edge editing in edit mode
              evt?.preventDefault(); 
              if (edge?.id) { 
                handleEdgeClick(edge.id); 
                setSelectedEdgePos({ x: evt.clientX, y: evt.clientY }); 
              } 
            }}
            nodeTypes={nodeTypes}
            edgeTypes={{ addButton: AddButtonEdge, addButtonEdge: AddButtonEdge }}
            onInit={(instance) => {
              reactFlowInstance.current = instance;
              // Center the workflow after initialization with a slight delay for nodes to render
              setTimeout(() => {
                instance.fitView({
                  padding: 0.3,
                  includeHiddenNodes: false,
                  minZoom: 0.5,
                  maxZoom: 1,
                  duration: 300,
                });
              }, 100);
            }}
            fitView
            fitViewOptions={{
              padding: 0.3,
              includeHiddenNodes: false,
              minZoom: 0.5,
              maxZoom: 1,
              duration: 300,
            }}
            minZoom={0.2}
            maxZoom={2}
            nodesDraggable={true}
            nodesConnectable={isEditMode}
            elementsSelectable={true}
            panOnDrag={true}
            zoomOnScroll={true}
            panOnScroll={true}
            zoomOnPinch={true}
            deleteKeyCode={isEditMode ? 'Backspace' : null}
            className="bg-transparent"
            attributionPosition="bottom-left"
            defaultEdgeOptions={{
              type: 'addButton',
              animated: false,
              markerEnd: {
                type: MarkerType.ArrowClosed,
                width: 16,
                height: 16,
              },
              style: {
                strokeWidth: 1.5,
              }
            }}
          >
            <Controls 
              className="bg-background/90 backdrop-blur border border-border shadow-lg rounded-lg [&>button]:bg-card [&>button]:text-foreground [&>button]:border-border hover:[&>button]:bg-accent hover:[&>button]:text-accent-foreground"
              showZoom={true}
              showFitView={true}
              showInteractive={true}
            />
            <Background 
              variant={BackgroundVariant.Dots} 
              gap={16} 
              size={1.5}
              color="hsl(var(--muted-foreground) / 0.2)"
            />
            <MiniMap
              className="!bg-card/80 !border-border !rounded-lg !shadow-lg backdrop-blur-sm"
              nodeColor={(node) => {
                const type = (node.data as any)?.type || '';
                if (type.includes('offer')) return '#f97316';
                if (type.includes('sale')) return '#10b981';
                if (type.includes('service') || type.includes('dispatch')) return '#3b82f6';
                if (type.includes('condition') || type.includes('if-') || type === 'switch') return '#f59e0b';
                if (type.includes('email') || type.includes('notification')) return '#06b6d4';
                if (type.includes('ai') || type.includes('llm')) return '#8b5cf6';
                return '#64748b';
              }}
              maskColor="hsl(var(--background) / 0.7)"
              pannable
              zoomable
              style={{ width: 160, height: 100 }}
            />
          </ReactFlow>
          {/* Edge toolbar */}
          <EdgeToolbar
            edge={edges.find(e => e.id === selectedEdgeId) || null}
            nodes={nodes}
            onChangeType={changeEdgeType}
            onToggleAnimated={toggleEdgeAnimated}
            onReverse={reverseEdge}
            // @ts-ignore
            position={selectedEdgePos}
          />
        </div>

        {/* Execution Debug Panel */}
        <ExecutionDebugPanel
          workflowId={currentWorkflowId}
          nodes={nodes}
          isOpen={debugPanelOpen}
          onClose={() => setDebugPanelOpen(false)}
          liveExecutionId={liveExecutionId}
          onHighlightNode={(nodeId) => {
            if (nodeId && reactFlowInstance.current) {
              const node = nodes.find(n => n.id === nodeId);
              if (node) {
                reactFlowInstance.current.fitView({
                  nodes: [{ id: nodeId }],
                  padding: 1.5,
                  duration: 400,
                });
                // Briefly highlight the node
                setNodes(nds => nds.map(n => ({
                  ...n,
                  data: {
                    ...n.data,
                    highlighted: n.id === nodeId,
                  }
                })));
                setTimeout(() => {
                  setNodes(nds => nds.map(n => ({
                    ...n,
                    data: { ...n.data, highlighted: false }
                  })));
                }, 2000);
              }
            }
          }}
        />
      </div>

      {/* n8n-style Configuration Panel */}
      <NodeConfigPanel
        isOpen={configPanel.isOpen}
        onClose={() => setConfigPanel({ isOpen: false, nodeId: '', nodeData: null })}
        nodeId={configPanel.nodeId}
        nodeData={configPanel.nodeData || { label: '', type: '' }}
        onSave={handlePanelConfigSave}
        nodes={nodes}
        edges={edges}
      />

      {/* Legacy Configuration Modal for conditional nodes */}
      {configModal.nodeData?.type && ['if-else', 'switch', 'loop', 'parallel', 'try-catch'].includes(configModal.nodeData.type) ? (
        <ConditionalConfigModal
          isOpen={configModal.isOpen}
          onClose={() => setConfigModal({ isOpen: false })}
          nodeData={configModal.nodeData}
          onSave={handleConfigSave}
        />
      ) : (
        <NodeConfigurationModal
          isOpen={configModal.isOpen}
          onClose={() => setConfigModal({ isOpen: false })}
          nodeData={configModal.nodeData}
          onSave={handleConfigSave}
        />
      )}

      {/* Export Modal */}
      <ExportDialog
        isOpen={exportModal}
        onClose={() => setExportModal(false)}
        nodes={nodes}
        edges={edges}
        workflowName={t('templateTitle')}
      />

      {/* Import Modal */}
      <ImportDialog
        isOpen={importModal}
        onClose={() => setImportModal(false)}
        onImport={handleImportWorkflow}
      />

      {/* Stop Workflow Confirmation Modal */}
      <AlertDialog open={stopConfirmModal} onOpenChange={setStopConfirmModal}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t('stopConfirmTitle')}</AlertDialogTitle>
            <AlertDialogDescription className="space-y-3">
              <p>{t('stopConfirmDescription')}</p>
              <div className="bg-muted p-3 rounded-lg text-sm">
                <p className="font-medium mb-2">{t('stopConfirmManualRequired')}</p>
                <ul className="list-disc list-inside space-y-1 text-muted-foreground">
                  <li>{t('stopConfirmOffers')}</li>
                  <li>{t('stopConfirmSales')}</li>
                  <li>{t('stopConfirmServiceOrders')}</li>
                  <li>{t('stopConfirmDispatches')}</li>
                </ul>
              </div>
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t('cancel')}</AlertDialogCancel>
            <AlertDialogAction 
              onClick={confirmToggleWorkflow}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {t('stopConfirmButton')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Debug Console Modal */}
      <WorkflowDebugConsole
        isOpen={debugConsoleOpen}
        onClose={() => setDebugConsoleOpen(false)}
        workflowId={currentWorkflowId}
        lastExecutionId={liveExecutionId}
      />

      {/* AI Workflow Builder Dialog */}
      <AIWorkflowBuilder
        open={aiBuilderOpen}
        onOpenChange={setAiBuilderOpen}
        onApplyWorkflow={handleApplyAIWorkflow}
        existingNodes={nodes}
      />
    </div>
  );
}
