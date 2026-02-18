import { useTranslation } from "react-i18next";
import { motion, AnimatePresence } from "framer-motion";
import { ChevronRight, Check, X, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import { dispatchStatusConfig } from "@/config/entity-statuses";

export type DispatchStatus = 
  | "pending"
  | "planned"
  | "confirmed"
  | "rejected"
  | "in_progress" 
  | "completed"
  | "cancelled";

// Workflow steps sourced from centralized config - edit dispatch.config.ts to modify
const WORKFLOW_STEPS = dispatchStatusConfig.workflow.steps as DispatchStatus[];
const TERMINAL_STATUSES = dispatchStatusConfig.workflow.terminalStatuses;

interface DispatchStatusFlowProps {
  currentStatus: DispatchStatus;
  onStatusChange: (newStatus: DispatchStatus) => void;
  disabled?: boolean;
  isUpdating?: boolean;
}

export function DispatchStatusFlow({ 
  currentStatus, 
  onStatusChange, 
  disabled = false,
  isUpdating = false
}: DispatchStatusFlowProps) {
  const { t } = useTranslation('dispatches');

  if (TERMINAL_STATUSES.includes(currentStatus) && currentStatus !== 'completed') {
    return (
      <motion.div 
        className="flex items-center gap-1.5"
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
      >
        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-destructive/15 border border-destructive/30">
          <X className="h-3 w-3 text-destructive" />
          <span className="text-xs font-medium text-destructive">
            {t('dispatches.statuses.cancelled')}
          </span>
        </div>
      </motion.div>
    );
  }

  const currentIndex = WORKFLOW_STEPS.findIndex(step => step === currentStatus);
  const validCurrentIndex = currentIndex >= 0 ? currentIndex : 0;
  const isLastStep = validCurrentIndex === WORKFLOW_STEPS.length - 1;

  const getVisibleSteps = () => {
    const currentStep = WORKFLOW_STEPS[validCurrentIndex];
    const nextStep = !isLastStep ? WORKFLOW_STEPS[validCurrentIndex + 1] : null;
    return { current: currentStep, next: nextStep };
  };

  const visibleSteps = getVisibleSteps();

  const handleNextClick = () => {
    if (disabled || isLastStep || !visibleSteps.next || isUpdating) return;
    onStatusChange(visibleSteps.next);
  };

  const StepBadge = ({ step, type }: { step: DispatchStatus; type: 'current' | 'next' }) => {
    const isCurrent = type === 'current';
    
    if (isCurrent) {
      const isTerminal = TERMINAL_STATUSES.includes(step);
      return (
        <motion.div 
          className={cn(
            "flex items-center gap-1.5 px-2.5 py-1 rounded-md border",
            isTerminal 
              ? "bg-success/15 border-success/30" 
              : "bg-primary/15 border-primary/30"
          )}
          layout
          transition={{ type: "spring", stiffness: 400, damping: 30 }}
        >
          {isUpdating ? (
            <motion.div
              animate={{ rotate: 360 }}
              transition={{ duration: 1, repeat: Infinity, ease: "linear" }}
            >
              <Loader2 className="h-3 w-3 text-primary" />
            </motion.div>
          ) : isTerminal ? (
            <motion.div
              initial={{ scale: 0 }}
              animate={{ scale: 1 }}
              transition={{ type: "spring", stiffness: 500 }}
            >
              <Check className="h-3 w-3 text-success" />
            </motion.div>
          ) : (
            <motion.div 
              className="w-1.5 h-1.5 rounded-full bg-primary"
              layoutId="dispatch-status-dot"
            />
          )}
          <span className={cn(
            "text-xs font-medium",
            isTerminal ? "text-success" : "text-primary"
          )}>
            {t(`dispatches.statuses.${step}`)}
          </span>
        </motion.div>
      );
    }

    return (
      <motion.button
        onClick={handleNextClick}
        disabled={disabled || isUpdating}
        className="flex items-center gap-1.5 px-2 py-1 rounded-md text-xs font-medium text-muted-foreground bg-muted/50 hover:bg-muted border border-transparent hover:border-border transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        whileHover={{ scale: 1.02 }}
        whileTap={{ scale: 0.98 }}
        initial={{ opacity: 0, x: -10 }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ delay: 0.1 }}
      >
        <div className="w-1.5 h-1.5 rounded-full bg-muted-foreground/30" />
        <span>{t(`dispatches.statuses.${step}`)}</span>
      </motion.button>
    );
  };

  return (
    <div className="flex items-center gap-1.5">
      <AnimatePresence mode="wait">
        {isUpdating ? (
          <motion.div
            key="updating"
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.9 }}
            className="flex items-center gap-2 px-3 py-1.5 rounded-md bg-primary/10 border border-primary/20"
          >
            <motion.div
              animate={{ rotate: 360 }}
              transition={{ duration: 1, repeat: Infinity, ease: "linear" }}
            >
              <Loader2 className="h-3.5 w-3.5 text-primary" />
            </motion.div>
            <span className="text-xs font-medium text-primary">
              {t('updating', { defaultValue: 'Updating...' })}
            </span>
            <motion.div className="flex gap-0.5">
              {[0, 1, 2].map((i) => (
                <motion.span
                  key={i}
                  className="w-1 h-1 rounded-full bg-primary"
                  animate={{ opacity: [0.3, 1, 0.3] }}
                  transition={{ 
                    duration: 0.8, 
                    repeat: Infinity, 
                    delay: i * 0.15 
                  }}
                />
              ))}
            </motion.div>
          </motion.div>
        ) : (
          <motion.div
            key="status"
            initial={{ opacity: 0, y: 5 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -5 }}
            className="flex items-center gap-1.5"
          >
            <StepBadge step={visibleSteps.current} type="current" />
            {visibleSteps.next && (
              <>
                <motion.div
                  initial={{ opacity: 0, scale: 0.5 }}
                  animate={{ opacity: 1, scale: 1 }}
                  transition={{ delay: 0.05 }}
                >
                  <ChevronRight className="h-3 w-3 text-muted-foreground/40" />
                </motion.div>
                <StepBadge step={visibleSteps.next} type="next" />
              </>
            )}
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
