import { useState, useCallback, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Node, Edge, MarkerType } from '@xyflow/react';
import { toast } from 'sonner';
import { Badge } from '@/components/ui/badge';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Sparkles, Loader2, Send, RotateCcw, Check, AlertCircle, Zap, FileText, DollarSign, ShoppingCart, Truck, Users, GitBranch, Mail, Bot, Brain, Webhook, Calendar, Database, Bell, Clock, Globe, ArrowLeftRight } from 'lucide-react';
import ReactMarkdown from 'react-markdown';

// Icon mapping for AI-generated nodes
const AI_NODE_ICONS: Record<string, React.ComponentType<any>> = {
  'offer-status-trigger': FileText, 'sale-status-trigger': DollarSign,
  'service-order-status-trigger': ShoppingCart, 'dispatch-status-trigger': Truck,
  'webhook-trigger': Webhook, 'scheduled-trigger': Calendar,
  'offer': FileText, 'sale': DollarSign, 'service-order': ShoppingCart,
  'dispatch': Truck, 'contact': Users,
  'create-offer': FileText, 'create-sale': DollarSign,
  'create-service-order': ShoppingCart, 'create-dispatch': Truck,
  'update-offer-status': FileText, 'update-sale-status': DollarSign,
  'update-service-order-status': ShoppingCart, 'update-dispatch-status': Truck,
  'if-else': GitBranch, 'switch': GitBranch, 'loop': Zap,
  'send-notification': Bell, 'send-email': Mail, 'request-approval': Check,
  'delay': Clock, 'http-request': Globe, 'data-transfer': ArrowLeftRight,
  'ai-email-writer': Sparkles, 'ai-analyzer': Bot, 'ai-agent': Brain,
  'custom-llm': Brain, 'database': Database, 'dynamic-form': FileText,
  'human-input-form': Users, 'wait-for-event': Clock,
};

// Reuse the same OpenRouter fallback logic from the AI assistant
import { getUsableApiKeys } from '@/services/openRouterModelsService';

const OPENROUTER_CONFIG = {
  apiUrl: 'https://openrouter.ai/api/v1/chat/completions',
  models: [
    'meta-llama/llama-3.3-70b-instruct:free',
    'google/gemma-3-27b-it:free',
    'mistralai/mistral-small-3.1-24b-instruct:free',
    'meta-llama/llama-3.1-8b-instruct:free',
  ],
};

// All available node types the AI can use
const AVAILABLE_NODE_TYPES = [
  // Triggers
  'offer-status-trigger', 'sale-status-trigger', 'service-order-status-trigger',
  'dispatch-status-trigger', 'webhook-trigger', 'scheduled-trigger',
  // Entities
  'offer', 'sale', 'service-order', 'dispatch', 'contact',
  // Actions
  'create-offer', 'create-sale', 'create-service-order', 'create-dispatch',
  'update-offer-status', 'update-sale-status', 'update-service-order-status', 'update-dispatch-status',
  // Conditions
  'if-else', 'switch', 'loop',
  // Communication
  'send-notification', 'send-email', 'request-approval', 'delay',
  'human-input-form', 'wait-for-event',
  // AI
  'ai-email-writer', 'ai-analyzer', 'ai-agent', 'custom-llm',
  // Integration
  'dynamic-form', 'data-transfer', 'http-request',
];

const SYSTEM_PROMPT = `You are a workflow builder AI. Given a user's description, you generate a workflow as a JSON object with "nodes" and "edges" arrays.

AVAILABLE NODE TYPES:
${AVAILABLE_NODE_TYPES.join(', ')}

RULES:
1. Return ONLY valid JSON with this structure: { "nodes": [...], "edges": [...], "name": "Workflow Name", "description": "Brief description" }
2. Each node: { "id": "node-1", "type": "<node-type>", "label": "Human Label", "config": {} }
3. Each edge: { "source": "node-1", "target": "node-2" }
4. Every workflow MUST start with a trigger node (types ending in -trigger)
5. Use meaningful labels that describe what the node does
6. For condition nodes (if-else), config should include: { "field": "...", "operator": "equals|contains|greater_than", "value": "..." }
7. For delay nodes, config: { "delayValue": 5, "delayUnit": "minutes" }
8. For email nodes, config: { "to": "{{trigger.email}}", "subject": "...", "template": "..." }
9. For http-request nodes, config: { "url": "...", "httpMethod": "GET|POST", "requestBody": "..." }
10. For data-transfer nodes, config: { "sourceModule": "contacts|sales|offers|dispatches", "operation": "read|create|update|delete" }
11. For AI nodes, config: { "prompt": "...", "model": "..." }
12. Position nodes logically — triggers at top, actions flowing downward
13. Use {{variable}} syntax for dynamic values: {{trigger.contactName}}, {{node-2.output}}, etc.
14. Do NOT wrap JSON in markdown code blocks. Return raw JSON only.
15. Do NOT include any text before or after the JSON.
16. Keep workflows practical and realistic.

ENTITY STATUS VALUES:
- Offer: draft, sent, accepted, rejected, expired
- Sale: pending, confirmed, processing, completed, cancelled
- Service Order: created, scheduled, in_progress, completed, cancelled
- Dispatch: pending, assigned, en_route, arrived, completed`;

interface AIWorkflowBuilderProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onApplyWorkflow: (nodes: Node[], edges: Edge[], name: string) => void;
  existingNodes?: Node[];
}

interface ConversationMessage {
  role: 'user' | 'assistant';
  content: string;
}

interface GeneratedWorkflow {
  nodes: Array<{ id: string; type: string; label: string; config?: Record<string, any> }>;
  edges: Array<{ source: string; target: string; sourceHandle?: string }>;
  name: string;
  description?: string;
}

// Pre-built workflow templates
const WORKFLOW_TEMPLATES: Record<string, GeneratedWorkflow> = {
  'crm-integration': {
    name: 'Sale → CRM Integration Pipeline',
    description: 'Triggers on sale status change, waits 5 minutes, syncs to external CRM via HTTP, updates contacts, and sends notification.',
    nodes: [
      {
        id: 'node-1',
        type: 'sale-status-trigger',
        label: 'Sale Status Changed',
        config: { 
          entityType: 'sale',
          statusFilter: 'confirmed',
          description: 'Triggers when a sale is confirmed'
        },
      },
      {
        id: 'node-2',
        type: 'delay',
        label: 'Wait 5 Minutes',
        config: { 
          delayValue: 5, 
          delayUnit: 'minutes',
          delayMode: 'relative',
        },
      },
      {
        id: 'node-3',
        type: 'http-request',
        label: 'Sync to CRM API',
        config: {
          url: 'https://api.example-crm.com/v1/deals',
          httpMethod: 'POST',
          contentType: 'json',
          requestBody: '{\n  "dealName": "{{node-1.entityId}}",\n  "status": "won",\n  "amount": "{{node-1.amount}}",\n  "contact": "{{node-1.contactName}}",\n  "source": "FlowService"\n}',
          customHeaders: 'Accept: application/json',
          authType: 'bearer',
          bearerToken: '{{secrets.CRM_API_KEY}}',
          timeout: 30,
          followRedirects: true,
          retryOnFailure: true,
          retryCount: 3,
          retryDelay: 2000,
          responseFormat: 'json',
          responseMapping: 'crmDealId: $.data.id\ncrmStatus: $.data.status',
          successCondition: 'status_2xx',
          storeFullResponse: false,
        },
      },
      {
        id: 'node-4',
        type: 'data-transfer',
        label: 'Update Contact Record',
        config: {
          sourceModule: 'contacts',
          operation: 'update',
          filter: 'contactId: {{node-1.contactId}}',
          dataMapping: 'crmSynced: true\ncrmDealId: {{node-3.crmDealId}}\nlastSyncDate: {{now}}',
        },
      },
      {
        id: 'node-5',
        type: 'send-notification',
        label: 'Notify Sales Team',
        config: {
          title: 'CRM Sync Complete',
          message: 'Sale #{{node-1.entityId}} has been synced to CRM. Deal ID: {{node-3.crmDealId}}',
          recipients: 'sales_team',
          notificationType: 'info',
        },
      },
    ],
    edges: [
      { source: 'node-1', target: 'node-2' },
      { source: 'node-2', target: 'node-3' },
      { source: 'node-3', target: 'node-4' },
      { source: 'node-4', target: 'node-5' },
    ],
  },
  'approval-chain': {
    name: 'Offer Approval Chain',
    description: 'When an offer is created, check amount — if > 1000 require approval, otherwise auto-approve and notify.',
    nodes: [
      { id: 'node-1', type: 'offer-status-trigger', label: 'Offer Created', config: { statusFilter: 'draft' } },
      { id: 'node-2', type: 'if-else', label: 'Amount > 1000?', config: { field: 'amount', operator: 'greater_than', value: '1000' } },
      { id: 'node-3', type: 'request-approval', label: 'Request Manager Approval', config: { approverRole: 'manager' } },
      { id: 'node-4', type: 'update-offer-status', label: 'Auto-Approve Offer', config: { targetStatus: 'sent' } },
      { id: 'node-5', type: 'send-email', label: 'Send Offer to Client', config: { to: '{{node-1.clientEmail}}', subject: 'Your offer is ready', template: 'offer-confirmation' } },
    ],
    edges: [
      { source: 'node-1', target: 'node-2' },
      { source: 'node-2', target: 'node-3', sourceHandle: 'yes' },
      { source: 'node-2', target: 'node-4', sourceHandle: 'no' },
      { source: 'node-3', target: 'node-5' },
      { source: 'node-4', target: 'node-5' },
    ],
  },
  'dispatch-automation': {
    name: 'Dispatch Scheduling Pipeline',
    description: 'On new service order, auto-create dispatch, wait for assignment, then notify technician.',
    nodes: [
      { id: 'node-1', type: 'service-order-status-trigger', label: 'Service Order Created', config: { statusFilter: 'created' } },
      { id: 'node-2', type: 'create-dispatch', label: 'Create Dispatch', config: { createPerService: true } },
      { id: 'node-3', type: 'delay', label: 'Wait 10 Minutes', config: { delayValue: 10, delayUnit: 'minutes' } },
      { id: 'node-4', type: 'send-notification', label: 'Alert Dispatcher', config: { title: 'New dispatch pending', message: 'Service order {{node-1.entityId}} needs assignment', recipients: 'dispatchers' } },
    ],
    edges: [
      { source: 'node-1', target: 'node-2' },
      { source: 'node-2', target: 'node-3' },
      { source: 'node-3', target: 'node-4' },
    ],
  },
  'lead-nurturing': {
    name: 'Lead Nurturing',
    description: 'Webhook receives a new lead, AI analyzes their profile, then conditionally sends a personalized or generic welcome email.',
    nodes: [
      { id: 'node-1', type: 'webhook-trigger', label: 'New Lead Webhook', config: { method: 'POST', description: 'Receives lead data from landing page or ad platform' } },
      { id: 'node-2', type: 'ai-analyzer', label: 'Analyze Lead Profile', config: { prompt: 'Analyze this lead and classify as hot, warm, or cold based on their data: {{node-1.body}}', model: 'default', outputField: 'leadScore' } },
      { id: 'node-3', type: 'if-else', label: 'Is Hot Lead?', config: { field: 'leadScore', operator: 'equals', value: 'hot' } },
      { id: 'node-4', type: 'send-email', label: 'Send Personalized Email', config: { to: '{{node-1.email}}', subject: 'Welcome — let\'s schedule a call!', template: 'hot-lead-welcome' } },
      { id: 'node-5', type: 'send-email', label: 'Send Generic Welcome', config: { to: '{{node-1.email}}', subject: 'Welcome to our platform!', template: 'generic-welcome' } },
      { id: 'node-6', type: 'data-transfer', label: 'Save Lead to Contacts', config: { sourceModule: 'contacts', operation: 'create', dataMapping: 'name: {{node-1.name}}\nemail: {{node-1.email}}\nscore: {{node-2.leadScore}}\nsource: webhook' } },
    ],
    edges: [
      { source: 'node-1', target: 'node-2' },
      { source: 'node-2', target: 'node-3' },
      { source: 'node-3', target: 'node-4', sourceHandle: 'yes' },
      { source: 'node-3', target: 'node-5', sourceHandle: 'no' },
      { source: 'node-4', target: 'node-6' },
      { source: 'node-5', target: 'node-6' },
    ],
  },
  'inventory-alert': {
    name: 'Inventory Alert',
    description: 'Scheduled daily check reads stock levels, checks if any item is below threshold, and sends an alert notification.',
    nodes: [
      { id: 'node-1', type: 'scheduled-trigger', label: 'Daily at 8:00 AM', config: { cronExpression: '0 8 * * *', timezone: 'UTC', description: 'Runs every day at 8 AM' } },
      { id: 'node-2', type: 'data-transfer', label: 'Read Stock Levels', config: { sourceModule: 'stock', operation: 'read', filter: 'status: active', dataMapping: 'items: {{all}}' } },
      { id: 'node-3', type: 'if-else', label: 'Any Item Below Threshold?', config: { field: 'lowStockCount', operator: 'greater_than', value: '0' } },
      { id: 'node-4', type: 'send-notification', label: 'Send Low Stock Alert', config: { title: 'Low Stock Alert ⚠️', message: '{{node-2.lowStockCount}} items are below minimum stock level. Please review and reorder.', recipients: 'inventory_team', notificationType: 'warning' } },
      { id: 'node-5', type: 'send-email', label: 'Email Stock Report', config: { to: 'inventory@company.com', subject: 'Daily Stock Report — {{node-2.lowStockCount}} items low', template: 'stock-report' } },
    ],
    edges: [
      { source: 'node-1', target: 'node-2' },
      { source: 'node-2', target: 'node-3' },
      { source: 'node-3', target: 'node-4', sourceHandle: 'yes' },
      { source: 'node-4', target: 'node-5' },
    ],
  },
};

// Call OpenRouter with fallback
async function callAI(messages: Array<{ role: string; content: string }>): Promise<string> {
  const apiKeys = getUsableApiKeys();
  if (apiKeys.length === 0) throw new Error('No API keys configured');
  for (const model of OPENROUTER_CONFIG.models) {
    for (const apiKey of apiKeys) {
      try {
        const response = await fetch(OPENROUTER_CONFIG.apiUrl, {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${apiKey}`,
            'Content-Type': 'application/json',
            'HTTP-Referer': window.location.origin,
            'X-Title': 'FlowService Workflow AI',
          },
          body: JSON.stringify({
            model,
            messages,
            max_tokens: 4096,
            temperature: 0.3, // Lower temp for structured output
          }),
        });

        if (!response.ok) continue;

        const data = await response.json();
        const content = data.choices?.[0]?.message?.content;
        if (content) return content;
      } catch {
        continue;
      }
    }
  }
  throw new Error('All AI models failed. Please try again.');
}

// Map generated node type to ReactFlow node type
function getReactFlowNodeType(type: string): string {
  if (type === 'loop') return 'loopNode';
  if (type === 'if-else' || type === 'switch') return 'conditionNode';
  if (type.endsWith('-trigger') && ['offer', 'sale', 'service-order', 'dispatch'].some(e => type.startsWith(e))) return 'entityTrigger';
  if (type.startsWith('create-') || type.startsWith('update-')) return 'entityAction';
  return 'n8nNode';
}

// Get icon name for display
function getNodeCategory(type: string): string {
  if (type.includes('trigger')) return 'triggers';
  if (['offer', 'sale', 'service-order', 'dispatch', 'contact'].includes(type)) return 'entities';
  if (type.startsWith('create-') || type.startsWith('update-')) return 'actions';
  if (['if-else', 'switch', 'loop'].includes(type)) return 'conditions';
  if (['send-notification', 'send-email', 'request-approval', 'delay', 'human-input-form', 'wait-for-event'].includes(type)) return 'communication';
  if (type.startsWith('ai-') || type === 'custom-llm') return 'ai';
  return 'integration';
}

function getEntityType(type: string): string | undefined {
  if (type.includes('offer')) return 'offer';
  if (type.includes('sale')) return 'sale';
  if (type.includes('service-order')) return 'service_order';
  if (type.includes('dispatch')) return 'dispatch';
  return undefined;
}

export function AIWorkflowBuilder({ open, onOpenChange, onApplyWorkflow, existingNodes }: AIWorkflowBuilderProps) {
  const { t } = useTranslation('workflow');
  const [prompt, setPrompt] = useState('');
  const [conversation, setConversation] = useState<ConversationMessage[]>([]);
  const [isGenerating, setIsGenerating] = useState(false);
  const [generatedWorkflow, setGeneratedWorkflow] = useState<GeneratedWorkflow | null>(null);
  const [error, setError] = useState<string | null>(null);
  const scrollRef = useRef<HTMLDivElement>(null);

  const parseWorkflowJSON = (text: string): GeneratedWorkflow | null => {
    // Try to extract JSON from the response
    let jsonStr = text.trim();
    
    // Remove markdown code blocks if present
    const codeBlockMatch = jsonStr.match(/```(?:json)?\s*([\s\S]*?)```/);
    if (codeBlockMatch) {
      jsonStr = codeBlockMatch[1].trim();
    }
    
    // Try to find JSON object
    const jsonMatch = jsonStr.match(/\{[\s\S]*\}/);
    if (jsonMatch) {
      jsonStr = jsonMatch[0];
    }

    try {
      const parsed = JSON.parse(jsonStr);
      if (parsed.nodes && Array.isArray(parsed.nodes) && parsed.edges && Array.isArray(parsed.edges)) {
        return parsed as GeneratedWorkflow;
      }
    } catch {
      // Failed
    }
    return null;
  };

  const handleGenerate = useCallback(async () => {
    if (!prompt.trim()) return;

    const userMessage = prompt.trim();
    setPrompt('');
    setError(null);
    setIsGenerating(true);

    const newConversation: ConversationMessage[] = [
      ...conversation,
      { role: 'user', content: userMessage },
    ];
    setConversation(newConversation);

    try {
      const messages = [
        { role: 'system', content: SYSTEM_PROMPT },
        ...newConversation.map(m => ({ role: m.role, content: m.content })),
      ];

      const response = await callAI(messages);
      
      const parsed = parseWorkflowJSON(response);
      
      if (parsed) {
        setGeneratedWorkflow(parsed);
        setConversation(prev => [
          ...prev,
          { role: 'assistant', content: `✅ Generated workflow **"${parsed.name}"** with ${parsed.nodes.length} nodes and ${parsed.edges.length} connections.${parsed.description ? `\n\n${parsed.description}` : ''}\n\n**Nodes:**\n${parsed.nodes.map((n, i) => `${i + 1}. **${n.label}** (\`${n.type}\`)`).join('\n')}\n\nYou can refine this by telling me what to change, or click **Apply** to add it to your canvas.` },
        ]);
      } else {
        // AI returned text instead of JSON — show it and ask to retry
        setConversation(prev => [
          ...prev,
          { role: 'assistant', content: response },
        ]);
        setError('Could not parse workflow. Please refine your description.');
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Unknown error';
      setError(msg);
      setConversation(prev => [
        ...prev,
        { role: 'assistant', content: `❌ Error: ${msg}` },
      ]);
    } finally {
      setIsGenerating(false);
    }
  }, [prompt, conversation]);

  const handleApply = useCallback(() => {
    if (!generatedWorkflow) return;

    const SPACING_X = 300;
    const SPACING_Y = 160;
    const START_X = 400;
    const START_Y = 100;

    // Build node positions - arrange in a flowing layout
    const reactFlowNodes: Node[] = generatedWorkflow.nodes.map((node, index) => {
      const col = index % 3;
      const row = Math.floor(index / 3);
      
      return {
        id: node.id,
        type: getReactFlowNodeType(node.type),
        position: {
          x: START_X + col * SPACING_X,
          y: START_Y + row * SPACING_Y,
        },
        data: {
          label: node.label,
          type: node.type,
          category: getNodeCategory(node.type),
          entityType: getEntityType(node.type),
          icon: AI_NODE_ICONS[node.type] || Zap,
          isTrigger: node.type.includes('trigger'),
          isAction: node.type.startsWith('create-') || node.type.startsWith('update-'),
          config: node.config || {},
          description: node.label,
          isAIGenerated: true,
        },
      };
    });

    const reactFlowEdges: Edge[] = generatedWorkflow.edges.map((edge, index) => ({
      id: `ai-edge-${Date.now()}-${index}`,
      source: edge.source,
      target: edge.target,
      sourceHandle: edge.sourceHandle,
      type: 'addButtonEdge',
      animated: true,
      markerEnd: { type: MarkerType.ArrowClosed },
      style: { stroke: 'hsl(var(--primary))' },
    }));

    onApplyWorkflow(reactFlowNodes, reactFlowEdges, generatedWorkflow.name);
    toast.success(`AI workflow "${generatedWorkflow.name}" applied with ${reactFlowNodes.length} nodes!`);
    handleReset();
    onOpenChange(false);
  }, [generatedWorkflow, onApplyWorkflow, onOpenChange]);

  const handleReset = () => {
    setConversation([]);
    setGeneratedWorkflow(null);
    setError(null);
    setPrompt('');
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleGenerate();
    }
  };

  // Get a simple label for node type
  const nodeTypeLabel = (type: string) => type.replace(/-/g, ' ').replace(/\b\w/g, c => c.toUpperCase());

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[85vh] flex flex-col gap-0 p-0 overflow-hidden">
        {/* Header */}
        <div className="px-5 pt-5 pb-4 border-b border-border">
          <DialogHeader className="space-y-1">
            <DialogTitle className="text-base font-semibold text-foreground">
              {t('ai.buildWithAI', 'Build with AI')}
            </DialogTitle>
            <DialogDescription className="text-xs text-muted-foreground">
              {t('ai.buildDescription', 'Describe your workflow in plain language. The AI will generate it for you. You can refine your prompt until it\'s perfect.')}
            </DialogDescription>
          </DialogHeader>
        </div>

        {/* Main content area */}
        <div className="flex-1 min-h-0 overflow-y-auto px-5 py-4">
          {conversation.length === 0 ? (
            <div className="space-y-5">
              {/* Quick prompts */}
              <div>
                <p className="text-[11px] font-medium text-muted-foreground uppercase tracking-wider mb-2">
                  {t('ai.quickAsks', 'Quick prompts')}
                </p>
                <div className="flex flex-wrap gap-1.5">
                  {(['saleNotify', 'offerFollowup', 'dispatchComplete', 'webhookProcess', 'dailyReport', 'aiAnalyze'] as const).map((key) => (
                    <button
                      key={key}
                      onClick={() => setPrompt(t(`ai.ask.${key}`))}
                      className="text-[11px] px-2.5 py-1.5 rounded-md border border-border bg-background hover:bg-muted transition-colors text-foreground/80 text-left leading-tight"
                    >
                      {t(`ai.ask.${key}`)}
                    </button>
                  ))}
                </div>
              </div>

              {/* Templates */}
              <div>
                <p className="text-[11px] font-medium text-muted-foreground uppercase tracking-wider mb-2">
                  {t('ai.templates', 'Templates')}
                </p>
                <div className="space-y-1.5">
                  {Object.entries(WORKFLOW_TEMPLATES).map(([key, tmpl]) => {
                    const templateKeyMap: Record<string, string> = { 'crm-integration': 'crm', 'approval-chain': 'approval', 'dispatch-automation': 'dispatch', 'lead-nurturing': 'leadNurturing', 'inventory-alert': 'inventoryAlert' };
                    const templateKey = templateKeyMap[key] || key;
                    return (
                      <button
                        key={key}
                        onClick={() => {
                          const name = t(`ai.template.${templateKey}.name`);
                          const desc = t(`ai.template.${templateKey}.description`);
                          setGeneratedWorkflow(tmpl);
                          setConversation([
                            { role: 'user', content: name },
                            { role: 'assistant', content: `✅ ${t('ai.template.loaded', { name, count: tmpl.nodes.length })}\n\n${desc}\n\n**${t('ai.template.nodeList')}**\n${tmpl.nodes.map((n, i) => `${i + 1}. **${n.label}** (\`${n.type}\`)`).join('\n')}\n\n${t('ai.template.loadedAction')}` },
                          ]);
                        }}
                        className="w-full flex items-start gap-3 p-3 rounded-lg border border-border bg-background hover:bg-muted/50 hover:border-foreground/10 transition-all text-left group"
                      >
                        <div className="flex items-center justify-center w-8 h-8 rounded-md bg-muted border border-border shrink-0 mt-0.5">
                          <Zap className="h-4 w-4 text-foreground/60" />
                        </div>
                        <div className="min-w-0 flex-1">
                          <div className="text-sm font-medium text-foreground leading-tight">{t(`ai.template.${templateKey}.name`)}</div>
                          <div className="text-[11px] text-muted-foreground mt-0.5 line-clamp-1">{t(`ai.template.${templateKey}.description`)}</div>
                          <div className="flex gap-1 mt-1.5 flex-wrap">
                            {tmpl.nodes.slice(0, 4).map((n, i) => (
                              <span key={i} className="text-[10px] px-1.5 py-0.5 rounded bg-muted text-muted-foreground font-medium">
                                {nodeTypeLabel(n.type)}
                              </span>
                            ))}
                            {tmpl.nodes.length > 4 && (
                              <span className="text-[10px] px-1.5 py-0.5 rounded bg-muted text-muted-foreground">
                                +{tmpl.nodes.length - 4}
                              </span>
                            )}
                          </div>
                        </div>
                      </button>
                    );
                  })}
                </div>
              </div>

              <p className="text-[10px] text-muted-foreground text-center">
                {t('ai.orDescribe', 'Or describe your own workflow below')}
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              {conversation.map((msg, i) => (
                <div
                  key={i}
                  className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}
                >
                  <div
                    className={`max-w-[85%] rounded-lg px-3 py-2 text-sm ${
                      msg.role === 'user'
                        ? 'bg-primary text-primary-foreground'
                        : 'bg-muted/50 border border-border'
                    }`}
                  >
                    {msg.role === 'assistant' ? (
                      <div className="prose prose-sm dark:prose-invert max-w-none [&_p]:my-1 [&_li]:my-0">
                        <ReactMarkdown>{msg.content}</ReactMarkdown>
                      </div>
                    ) : (
                      msg.content
                    )}
                  </div>
                </div>
              ))}
              {isGenerating && (
                <div className="flex justify-start">
                  <div className="bg-muted/50 border border-border rounded-lg px-3 py-2 text-sm flex items-center gap-2">
                    <Loader2 className="h-3.5 w-3.5 animate-spin text-muted-foreground" />
                    <span className="text-muted-foreground text-xs">{t('ai.generating', 'Generating workflow...')}</span>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Error */}
        {error && (
          <div className="mx-5 flex items-center gap-2 text-xs text-destructive bg-destructive/10 rounded-md px-3 py-2">
            <AlertCircle className="h-3.5 w-3.5 shrink-0" />
            {error}
          </div>
        )}

        {/* Input + Footer */}
        <div className="px-5 pb-4 pt-3 border-t border-border space-y-3">
          <div className="flex gap-2 items-end">
            <Textarea
              value={prompt}
              onChange={(e) => setPrompt(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={
                generatedWorkflow
                  ? t('ai.refinePlaceholder', 'Describe changes... e.g. "Add an approval step before the email"')
                  : t('ai.promptPlaceholder', 'Describe your workflow...')
              }
              rows={2}
              className="resize-none flex-1 text-sm"
              disabled={isGenerating}
            />
            <Button
              onClick={handleGenerate}
              disabled={!prompt.trim() || isGenerating}
              size="sm"
              className="h-10 px-3"
            >
              {isGenerating ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Send className="h-4 w-4" />
              )}
            </Button>
          </div>

          <div className="flex items-center justify-between">
            <Button
              variant="ghost"
              size="sm"
              onClick={handleReset}
              disabled={conversation.length === 0 || isGenerating}
              className="text-xs text-muted-foreground"
            >
              <RotateCcw className="h-3.5 w-3.5 mr-1.5" />
              {t('ai.reset', 'Reset')}
            </Button>
            <Button
              onClick={handleApply}
              disabled={!generatedWorkflow || isGenerating}
              size="sm"
            >
              {t('ai.applyWorkflow', 'Apply to Canvas')}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
