import { useState, Fragment } from "react";
import { useTranslation } from "react-i18next";
import { motion, AnimatePresence } from "framer-motion";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from "@/components/ui/dialog";
import { ChevronRight, Check, X, AlertTriangle, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import {
  offerStatusConfig,
  normalizeStatus,
  getWorkflowPosition,
  isTerminalStatus,
  getStatusById,
} from "@/config/entity-statuses";

// Status type — accepts any valid config status ID
export type OfferStatus = string;

interface OfferStatusFlowProps {
  currentStatus: string;
  onStatusChange: (newStatus: string) => void;
  disabled?: boolean;
  isUpdating?: boolean;
}

// Workflow data sourced from centralized config — edit offer.config.ts to add/remove/reorder
const WORKFLOW_STEPS = offerStatusConfig.workflow.steps;
const TERMINAL_STATUSES = offerStatusConfig.workflow.terminalStatuses;
const BRANCH_STATUSES = offerStatusConfig.workflow.branchStatuses ?? {};

export function OfferStatusFlow({
  currentStatus,
  onStatusChange,
  disabled = false,
  isUpdating = false,
}: OfferStatusFlowProps) {
  const { t } = useTranslation("offers");
  const [confirmDialog, setConfirmDialog] = useState<{
    open: boolean;
    action: string | null;
  }>({ open: false, action: null });

  // Normalize raw backend status to canonical config ID (uses aliases)
  const currentNormalized = normalizeStatus(offerStatusConfig, currentStatus);
  const currentIndex = getWorkflowPosition(offerStatusConfig, currentNormalized);
  const isTerminal = isTerminalStatus(offerStatusConfig, currentNormalized);

  // Branch options at current workflow step (e.g. accepted/declined after 'sent')
  const currentStepId = WORKFLOW_STEPS[currentIndex];
  const branches = BRANCH_STATUSES[currentStepId] ?? [];
  const hasBranches = branches.length > 0;

  const canGoForward =
    currentIndex < WORKFLOW_STEPS.length - 1 && !isTerminal && !hasBranches;

  const handleNext = () => {
    if (!canGoForward || disabled || isUpdating) return;
    const nextStep = WORKFLOW_STEPS[currentIndex + 1];
    if (nextStep) onStatusChange(nextStep);
  };

  const handleBranchClick = (statusId: string) => {
    if (disabled || isTerminal || isUpdating) return;
    setConfirmDialog({ open: true, action: statusId });
  };

  const handleConfirm = () => {
    if (confirmDialog.action) onStatusChange(confirmDialog.action);
    setConfirmDialog({ open: false, action: null });
  };

  const handleCancel = () => setConfirmDialog({ open: false, action: null });

  // Visible steps: current + next (or branches)
  const getVisibleSteps = () => {
    if (isTerminal) return { current: currentNormalized, next: null };
    const nextStep =
      !hasBranches && currentIndex < WORKFLOW_STEPS.length - 1
        ? WORKFLOW_STEPS[currentIndex + 1]
        : null;
    return { current: currentNormalized, next: nextStep };
  };
  const visibleSteps = getVisibleSteps();

  const getStatusDef = (statusId: string) =>
    getStatusById(offerStatusConfig, statusId);

  // ── Step Badge ──────────────────────────────────────────────────────
  const StepBadge = ({
    step,
    type,
  }: {
    step: string;
    type: "current" | "next";
  }) => {
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
            {t(`status.${step}`)}
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
              layoutId="offer-status-dot"
            />
          )}
          <span className="text-xs font-medium text-primary">
            {t(`status.${step}`)}
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
        <span>{t(`status.${step}`)}</span>
      </motion.button>
    );
  };

  // ── Branch Buttons ──────────────────────────────────────────────────
  const BranchButtons = () => {
    if (!hasBranches || isTerminal) return null;
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
          return (
            <Fragment key={branchId}>
              {idx > 0 && (
                <span className="text-[10px] text-muted-foreground">/</span>
              )}
              <motion.button
                onClick={() => handleBranchClick(branchId)}
                disabled={disabled || isUpdating}
                className={cn(
                  "flex items-center gap-1 px-2 py-1 rounded-md text-xs font-medium border transition-colors disabled:opacity-50",
                  isNeg
                    ? "text-destructive bg-destructive/10 hover:bg-destructive/20 border-destructive/20 hover:border-destructive/40"
                    : "text-success bg-success/10 hover:bg-success/20 border-success/20 hover:border-success/40"
                )}
                whileHover={{ scale: 1.02 }}
                whileTap={{ scale: 0.98 }}
              >
                {isNeg ? (
                  <X className="h-3 w-3" />
                ) : (
                  <Check className="h-3 w-3" />
                )}
                {t(`status.${branchId}`)}
              </motion.button>
            </Fragment>
          );
        })}
      </motion.div>
    );
  };

  // Confirmation dialog — styling derived from config
  const actionDef = confirmDialog.action
    ? getStatusDef(confirmDialog.action)
    : null;
  const isNegativeAction = actionDef?.isNegative;

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
                {t("updating", { defaultValue: "Updating..." })}
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
              {hasBranches && !isTerminal && (
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

      {/* Confirmation Dialog — styling derived from config isNegative */}
      <Dialog
        open={confirmDialog.open}
        onOpenChange={(open) => !open && handleCancel()}
      >
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-base">
              <motion.div
                className={cn(
                  "flex items-center justify-center w-7 h-7 rounded-full",
                  isNegativeAction ? "bg-destructive/10" : "bg-success/10"
                )}
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                transition={{ type: "spring", stiffness: 500 }}
              >
                {isNegativeAction ? (
                  <AlertTriangle className="h-3.5 w-3.5 text-destructive" />
                ) : (
                  <Check className="h-3.5 w-3.5 text-success" />
                )}
              </motion.div>
              {isNegativeAction
                ? t("statusFlow.confirmDeclineTitle")
                : t("statusFlow.confirmAcceptTitle")}
            </DialogTitle>
            <DialogDescription className="text-sm">
              {isNegativeAction
                ? t("statusFlow.confirmDeclineDescription")
                : t("statusFlow.confirmAcceptDescription")}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="gap-2 pt-2">
            <Button variant="outline" size="sm" onClick={handleCancel}>
              {t("cancel")}
            </Button>
            <Button
              size="sm"
              onClick={handleConfirm}
              className={cn(
                isNegativeAction
                  ? "bg-destructive hover:bg-destructive/90 text-destructive-foreground"
                  : "bg-success hover:bg-success/90 text-success-foreground"
              )}
            >
              {isNegativeAction
                ? t("statusFlow.confirmDecline")
                : t("statusFlow.confirmAccept")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
