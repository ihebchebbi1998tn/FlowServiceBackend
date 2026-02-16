import { Handle, Position } from '@xyflow/react';
import { memo, useState, forwardRef } from 'react';
import { Loader2, Check, X, GitBranch } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useTranslation } from 'react-i18next';
import { NodeExecutionState } from './N8nStyleNode';

export interface ConditionNodeData {
  label: string;
  type: 'if-else' | 'switch';
  icon: React.ComponentType<any>;
  description?: string;
  field?: string;
  operator?: string;
  value?: string;
  config?: {
    field?: string;
    operator?: string;
    value?: string;
    checkField?: string;
    cases?: Array<{ value: string; label: string }>;
    conditionData?: {
      field?: string;
      operator?: string;
      value?: string;
      checkField?: string;
    };
  };
  executionState?: NodeExecutionState;
  activeBranch?: 'yes' | 'no' | string;
}

interface ConditionNodeProps {
  data: ConditionNodeData;
  id: string;
  selected?: boolean;
  onNodeClick?: (nodeId: string, nodeData: ConditionNodeData) => void;
  onRemove?: (nodeId: string) => void;
}

const ExecutionBadge = ({ state }: { state?: NodeExecutionState }) => {
  if (!state || state === 'idle') return null;

  const config: Record<string, { bg: string; content: React.ReactNode }> = {
    waiting: { bg: 'bg-warning', content: <div className="w-2 h-2 rounded-full bg-white animate-pulse" /> },
    executing: { bg: 'bg-primary', content: <Loader2 className="h-3 w-3 text-white animate-spin" /> },
    completed: { bg: 'bg-success', content: <Check className="h-3 w-3 text-white" strokeWidth={3} /> },
    failed: { bg: 'bg-destructive', content: <X className="h-3 w-3 text-white" strokeWidth={3} /> },
  };

  const c = config[state];
  if (!c) return null;

  return (
    <div className={cn(
      'absolute -top-2.5 -right-2.5 z-20',
      'w-6 h-6 rounded-full border-2 border-background',
      'flex items-center justify-center shadow-md',
      c.bg,
    )}>
      {c.content}
    </div>
  );
};

export const ConditionNode = memo(forwardRef<HTMLDivElement, ConditionNodeProps>(({ data, id, selected, onNodeClick, onRemove }, ref) => {
  const { t } = useTranslation('workflow');
  const [isHovered, setIsHovered] = useState(false);
  
  const IconComponent = data.icon || GitBranch;
  
  const translateField = (field: string) => {
    const translationKey = `conditionFields.${field.replace(/\./g, '_')}`;
    const translated = t(translationKey, '');
    return translated || field;
  };
  
  const translateOperator = (operator: string) => t(`operators.${operator}`, operator);
  
  const translateStatusValue = (value: string | undefined) => {
    if (!value) return '';
    return value.split(',').map(status => {
      const trimmed = status.trim();
      const entityTypes = ['dispatch', 'service_order', 'sale', 'offer'];
      for (const entityType of entityTypes) {
        const translated = t(`status.${entityType}.${trimmed}`, '');
        if (translated) return translated;
      }
      return trimmed.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
    }).join(', ');
  };
  
  const conditionField = data.field ?? data.config?.field ?? data.config?.conditionData?.field;
  const conditionOperator = data.operator ?? data.config?.operator ?? data.config?.conditionData?.operator ?? 'equals';
  const conditionValue = data.value ?? data.config?.value ?? data.config?.conditionData?.value;
  
  const displayLabel = (data.config as any)?.name || data.label;

  const getExecutionRing = () => {
    switch (data.executionState) {
      case 'executing': return 'ring-2 ring-blue-400 ring-offset-2 ring-offset-background';
      case 'completed': return 'ring-2 ring-emerald-500 ring-offset-2 ring-offset-background';
      case 'failed': return 'ring-2 ring-red-500 ring-offset-2 ring-offset-background';
      default: return '';
    }
  };

  return (
    <div
      ref={ref}
      className={cn(
        'relative group transition-all duration-200',
        'w-[230px]',
        selected && 'scale-[1.02]',
        data.executionState === 'waiting' && 'workflow-node-waiting',
        data.executionState === 'executing' && 'workflow-node-executing',
      )}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {/* Input Handle */}
      <Handle
        type="target"
        position={Position.Left}
        className={cn(
          '!w-[10px] !h-[10px] !rounded-full',
          '!bg-background !border-[2.5px]',
          isHovered ? '!border-primary !shadow-[0_0_6px_hsl(var(--primary)/0.5)]' : '!border-muted-foreground/50',
          'transition-all duration-200',
        )}
        isConnectable={true}
      />

      {/* Main Card */}
      <div
        onClick={() => onNodeClick?.(id, data)}
        className={cn(
          'cursor-pointer rounded-xl overflow-hidden',
          'bg-card border shadow-sm',
          selected ? 'border-primary shadow-lg' : 'border-amber-300/60 dark:border-amber-700/60',
          'hover:shadow-lg hover:border-amber-400',
          'transition-all duration-200',
          getExecutionRing(),
        )}
      >
        {/* Amber accent */}
        <div className="h-[3px] bg-warning" />

        {/* Header */}
        <div className="px-3.5 py-2.5 flex items-center gap-3">
          <div className="w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0 shadow-sm bg-warning/10 border border-warning/30">
            <IconComponent className="h-[18px] w-[18px] text-warning" />
          </div>

          <div className="flex-1 min-w-0">
            <div className="font-semibold text-[13px] text-foreground truncate leading-tight">
              {displayLabel}
            </div>
            <div className="text-[11px] text-warning font-medium mt-0.5">
              {data.type === 'if-else' ? t('conditionLabel') : t('switch')}
            </div>
          </div>
        </div>

        {/* Condition expression */}
        {conditionField && (
          <div className="px-3.5 pb-3">
            <div className="text-[11px] text-muted-foreground bg-muted/50 rounded-lg px-2.5 py-1.5 font-mono">
              <span className="text-warning">{translateField(conditionField)}</span>
              {' '}
              <span className="text-muted-foreground">{translateOperator(conditionOperator)}</span>
              {' '}
              <span className="text-foreground font-medium">{translateStatusValue(conditionValue)}</span>
            </div>
          </div>
        )}

        {/* Branch labels */}
        <div className="flex border-t border-border/30">
          <div className={cn(
            'flex-1 py-1.5 text-center text-[10px] font-semibold border-r border-border/30',
            data.activeBranch === 'yes' ? 'bg-success text-white' : 'text-success'
          )}>
            ✓ {t('yes')}
          </div>
          <div className={cn(
            'flex-1 py-1.5 text-center text-[10px] font-semibold',
            data.activeBranch === 'no' ? 'bg-destructive text-white' : 'text-destructive'
          )}>
            ✗ {t('no')}
          </div>
        </div>
      </div>

      <ExecutionBadge state={data.executionState} />

      {/* Remove */}
      {isHovered && (
        <button
          onClick={(e) => { e.stopPropagation(); onRemove?.(id); }}
          className="absolute -top-2 -right-2 z-20 w-5 h-5 rounded-full bg-destructive/90 text-destructive-foreground flex items-center justify-center shadow-md hover:scale-110 hover:bg-destructive transition-transform text-xs"
        >
          ×
        </button>
      )}

      {/* Output Handles - Yes/No */}
      <Handle
        type="source"
        position={Position.Right}
        id="yes"
        className={cn(
          '!w-[10px] !h-[10px] !rounded-full',
          '!bg-success !border-[2.5px] !border-success',
          'transition-all duration-200',
          '!top-[40%]'
        )}
        isConnectable={true}
      />
      <Handle
        type="source"
        position={Position.Right}
        id="no"
        className={cn(
          '!w-[10px] !h-[10px] !rounded-full',
          '!bg-destructive !border-[2.5px] !border-destructive',
          'transition-all duration-200',
          '!top-[70%]'
        )}
        isConnectable={true}
      />
    </div>
  );
}));

ConditionNode.displayName = 'ConditionNode';
