import { useState, Fragment } from "react";
import { useTranslation } from "react-i18next";
import { motion, AnimatePresence } from "framer-motion";
import { ChevronRight, Check, X, Loader2, Receipt } from "lucide-react";
import { cn } from "@/lib/utils";
import { Sale } from "../types";
import { ServiceOrderConfigModal, ServiceOrderConfig } from "./ServiceOrderConfigModal";
import { useWorkflowStatus } from "@/modules/workflow/hooks/useWorkflowStatus";
import {
  saleStatusConfig,
  normalizeStatus,
  getWorkflowPosition,
  isTerminalStatus,
  getStatusById,
} from "@/config/entity-statuses";

export type SaleStatus =
  | "created"
  | "in_progress"
  | "invoiced"
  | "partially_invoiced"
  | "closed"
  | "cancelled";

interface SaleStatusFlowProps {
  currentStatus: string;
  onStatusChange: (newStatus: SaleStatus, serviceOrderConfig?: ServiceOrderConfig) => void;
  disabled?: boolean;
  sale?: Sale;
  isUpdating?: boolean;
}

const WORKFLOW_STEPS = saleStatusConfig.workflow.steps as SaleStatus[];
const TERMINAL_STATUSES = saleStatusConfig.workflow.terminalStatuses;
const BRANCH_STATUSES = saleStatusConfig.workflow.branchStatuses ?? {};

export function SaleStatusFlow({
  currentStatus,
  onStatusChange,
  disabled = false,
  sale,
  isUpdating = false,
}: SaleStatusFlowProps) {
  const { t } = useTranslation("sales");
  const workflowStatus = useWorkflowStatus();
  const [showServiceOrderConfig, setShowServiceOrderConfig] = useState(false);

  // Check if sale has service items
  const hasServiceItems = sale?.items?.some((item) => item.type === "service") || false;
  const isAlreadyConverted = !!sale?.convertedToServiceOrderId;

  // Normalize legacy/alias statuses to canonical config IDs
  const currentNormalized = normalizeStatus(saleStatusConfig, currentStatus) as SaleStatus;
  const currentIndex = getWorkflowPosition(saleStatusConfig, currentNormalized);
  const isTerminal = isTerminalStatus(saleStatusConfig, currentNormalized);

  // Branch options at current workflow step
  const currentStepId = WORKFLOW_STEPS[currentIndex];
  // If the current status IS a branch status (e.g. partially_invoiced), check its own branches
  const branches = BRANCH_STATUSES[currentNormalized] ?? BRANCH_STATUSES[currentStepId] ?? [];
  // Don't show branches if current status is already one of the parent's branch options
  // and has its own branch definition (e.g. partially_invoiced → invoiced)
  const hasBranches = branches.length > 0 && !isTerminal;
  const canGoForward = currentIndex < WORKFLOW_STEPS.length - 1 && !isTerminal && !hasBranches;

  const handleNext = () => {
    if (!canGoForward || disabled || isUpdating) return;
    const nextStep = WORKFLOW_STEPS[currentIndex + 1];
    
    // Special: created → in_progress may need service order config
    if (currentNormalized === 'created') {
      if (workflowStatus.isActive && hasServiceItems && !isAlreadyConverted) {
        setShowServiceOrderConfig(true);
        return;
      }
    }
    
    if (nextStep) onStatusChange(nextStep as SaleStatus);
  };

  const handleBranchClick = (statusId: SaleStatus) => {
    if (disabled || isTerminal || isUpdating) return;
    onStatusChange(statusId);
  };

  const handleCancelClick = () => {
    if (disabled || isTerminal || isUpdating) return;
    onStatusChange('cancelled');
  };

  const handleServiceOrderConfigConfirm = (config: ServiceOrderConfig) => {
    setShowServiceOrderConfig(false);
    onStatusChange('in_progress', config);
  };

  const handleServiceOrderConfigCancel = () => {
    setShowServiceOrderConfig(false);
  };

  const getStatusDef = (statusId: string) => getStatusById(saleStatusConfig, statusId);

  // Visible steps: current + next (or branches)
  const getVisibleSteps = () => {
    if (isTerminal) return { current: currentNormalized, next: null };
    const nextStep = !hasBranches && currentIndex < WORKFLOW_STEPS.length - 1
      ? WORKFLOW_STEPS[currentIndex + 1]
      : null;
    return { current: currentNormalized, next: nextStep };
  };
  const visibleSteps = getVisibleSteps();

  const getTranslationKey = (statusId: string): string => {
    const def = getStatusDef(statusId);
    return def?.translationKey || `statusFlow.${statusId}`;
  };

  // ── Step Badge ──────────────────────────────────────────────────────
  const StepBadge = ({ step, type }: { step: SaleStatus; type: "current" | "next" }) => {
    const isCurrent = type === "current";
    const statusDef = getStatusDef(step);

    // Terminal state
    if (isCurrent && isTerminal) {
      const isNeg = statusDef?.isNegative;
      return (
        <motion.div
          className={cn(
            "flex items-center gap-1.5 px-2.5 py-1 rounded-md border",
            isNeg
              ? "bg-destructive/15 border-destructive/30"
              : "bg-success/15 border-success/30"
          )}
          initial={{ scale: 0.8, opacity: 0 }}
          animate={{ scale: 1, opacity: 1 }}
          transition={{ type: "spring", stiffness: 400, damping: 20 }}
        >
          <motion.div
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            transition={{ delay: 0.1, type: "spring", stiffness: 500 }}
          >
            {isNeg ? (
              <X className="h-3 w-3 text-destructive" />
            ) : (
              <Check className="h-3 w-3 text-success" />
            )}
          </motion.div>
          <span
            className={cn(
              "text-xs font-medium",
              isNeg ? "text-destructive" : "text-success"
            )}
          >
            {t(getTranslationKey(step))}
          </span>
        </motion.div>
      );
    }

    // Current (active)
    if (isCurrent) {
      return (
        <motion.div
          className="flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-primary/15 border border-primary/30 shadow-sm"
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
          ) : (
            <motion.div
              className="w-1.5 h-1.5 rounded-full bg-primary"
              layoutId="sale-status-dot"
            />
          )}
          <span className="text-xs font-medium text-primary">
            {t(getTranslationKey(step))}
          </span>
        </motion.div>
      );
    }

    // Next (upcoming) — clickable to advance
    return (
      <motion.button
        onClick={handleNext}
        disabled={disabled || !canGoForward || isUpdating}
        className="flex items-center gap-1.5 px-2 py-1 rounded-md text-xs font-medium text-muted-foreground bg-muted/50 hover:bg-muted border border-transparent hover:border-border transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        whileHover={{ scale: 1.02 }}
        whileTap={{ scale: 0.98 }}
        initial={{ opacity: 0, x: -10 }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ delay: 0.1 }}
      >
        <div className="w-1.5 h-1.5 rounded-full bg-muted-foreground/30" />
        <span>{t(getTranslationKey(step))}</span>
      </motion.button>
    );
  };

  // ── Branch Buttons (inline, like offer accepted/declined) ───────────
  const BranchButtons = () => {
    if (!hasBranches) return null;
    return (
      <motion.div
        className="flex items-center gap-1.5"
        initial={{ opacity: 0, x: -10 }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ delay: 0.1 }}
      >
        {branches.map((branchId, idx) => {
          const def = getStatusDef(branchId);
          const isNeg = def?.isNegative;
          // Use warning style for partially_invoiced, info/success for invoiced
          const isPartial = branchId === 'partially_invoiced';
          return (
            <Fragment key={branchId}>
              {idx > 0 && (
                <span className="text-[10px] text-muted-foreground">/</span>
              )}
              <motion.button
                onClick={() => handleBranchClick(branchId as SaleStatus)}
                disabled={disabled || isUpdating}
                className={cn(
                  "flex items-center gap-1 px-2 py-1 rounded-md text-xs font-medium border transition-colors disabled:opacity-50",
                  isNeg
                    ? "text-destructive bg-destructive/10 hover:bg-destructive/20 border-destructive/20 hover:border-destructive/40"
                    : isPartial
                    ? "text-warning bg-warning/10 hover:bg-warning/20 border-warning/20 hover:border-warning/40"
                    : "text-success bg-success/10 hover:bg-success/20 border-success/20 hover:border-success/40"
                )}
                whileHover={{ scale: 1.02 }}
                whileTap={{ scale: 0.98 }}
              >
                {isNeg ? (
                  <X className="h-3 w-3" />
                ) : isPartial ? (
                  <Receipt className="h-3 w-3" />
                ) : (
                  <Check className="h-3 w-3" />
                )}
                {t(getTranslationKey(branchId))}
              </motion.button>
            </Fragment>
          );
        })}
      </motion.div>
    );
  };




  return (
    <>
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
                {t("statusFlow.updating")}
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
                      delay: i * 0.15,
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
              {visibleSteps.next && !isTerminal && (
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
              {hasBranches && (
                <>
                  <motion.div
                    initial={{ opacity: 0, scale: 0.5 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ delay: 0.05 }}
                  >
                    <ChevronRight className="h-3 w-3 text-muted-foreground/40" />
                  </motion.div>
                  <BranchButtons />
                </>
              )}
            </motion.div>
          )}
        </AnimatePresence>
      </div>



      {/* Service Order Configuration Modal */}
      {sale && (
        <ServiceOrderConfigModal
          open={showServiceOrderConfig}
          onOpenChange={setShowServiceOrderConfig}
          sale={sale}
          onConfirm={handleServiceOrderConfigConfirm}
          onCancel={handleServiceOrderConfigCancel}
        />
      )}
    </>
  );
}
