import { memo, useState } from 'react';
import {
  BaseEdge,
  EdgeLabelRenderer,
  getBezierPath,
  getSmoothStepPath,
  type EdgeProps,
} from '@xyflow/react';
import { Plus } from 'lucide-react';
import { cn } from '@/lib/utils';

interface AddButtonEdgeProps extends EdgeProps {
  data?: {
    onAddNode?: (position: { x: number; y: number }, sourceId: string, targetId: string) => void;
  };
}

export const AddButtonEdge = memo(({
  id,
  sourceX,
  sourceY,
  targetX,
  targetY,
  sourcePosition,
  targetPosition,
  style = {},
  markerEnd,
  animated,
  selected,
  data,
  label,
}: AddButtonEdgeProps) => {
  const [isHovered, setIsHovered] = useState(false);

  const [edgePath, labelX, labelY] = getSmoothStepPath({
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
    borderRadius: 16,
  });

  return (
    <>
      {/* Invisible wider path for easier hovering */}
      <path
        d={edgePath}
        fill="none"
        stroke="transparent"
        strokeWidth={20}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      />

      <BaseEdge
        path={edgePath}
        markerEnd={markerEnd}
        style={{
          ...style,
          strokeWidth: selected ? 2.5 : isHovered ? 2 : 1.5,
          stroke: selected
            ? 'hsl(var(--primary))'
            : isHovered
            ? 'hsl(var(--primary) / 0.7)'
            : 'hsl(var(--muted-foreground) / 0.4)',
          transition: 'stroke 0.2s, stroke-width 0.2s',
        }}
      />

      <EdgeLabelRenderer>
        {/* Edge label */}
        {label && (
          <div
            style={{
              position: 'absolute',
              transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY - 16}px)`,
              pointerEvents: 'none',
            }}
            className="nodrag nopan"
          >
            <span className="text-[10px] font-medium px-1.5 py-0.5 rounded bg-card border border-border text-muted-foreground shadow-sm">
              {label}
            </span>
          </div>
        )}

        {/* Add button in the middle of the edge */}
        <div
          style={{
            position: 'absolute',
            transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)`,
            pointerEvents: 'all',
          }}
          className="nodrag nopan"
          onMouseEnter={() => setIsHovered(true)}
          onMouseLeave={() => setIsHovered(false)}
        >
          <button
            onClick={(e) => {
              e.stopPropagation();
              data?.onAddNode?.(
                { x: labelX, y: labelY },
                String(sourceX),
                String(targetX)
              );
            }}
            className={cn(
              'flex items-center justify-center',
              'w-6 h-6 rounded-full',
              'bg-card border-2 border-muted-foreground/30',
              'text-muted-foreground',
              'hover:border-primary hover:text-primary hover:bg-primary/10',
              'hover:scale-125',
              'transition-all duration-200',
              'shadow-sm hover:shadow-md',
              isHovered ? 'opacity-100 scale-100' : 'opacity-0 scale-75',
            )}
          >
            <Plus className="h-3 w-3" strokeWidth={2.5} />
          </button>
        </div>
      </EdgeLabelRenderer>
    </>
  );
});

AddButtonEdge.displayName = 'AddButtonEdge';
