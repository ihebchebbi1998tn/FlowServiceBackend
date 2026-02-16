import { useState } from "react";
import { useTranslation } from 'react-i18next';
import '../styles/workflow.css';
import { Button } from "@/components/ui/button";
import { Workflow, Plus } from "lucide-react";
import { QuickCreateWorkflow } from "./QuickCreateWorkflow";
import { WorkflowBuilder } from "./WorkflowBuilder";

export function WorkflowDashboard() {
  const { t } = useTranslation();
  const [showQuickCreate, setShowQuickCreate] = useState(false);

  return (
    <div className="workflow-module h-screen flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-border bg-card/50 backdrop-blur">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-lg bg-primary/10">
            <Workflow className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-xl font-semibold text-foreground">{t('builderTitle')}</h1>
            <p className="text-[11px] text-muted-foreground">{t('builderSubtitle')}</p>
          </div>
        </div>
        <div className="flex items-center gap-2" />
      
      </div>

      {/* Workflow Builder */}
      <div className="flex-1">
        <WorkflowBuilder />
      </div>

      {/* Quick Create Dialog */}
      {showQuickCreate && (
        <QuickCreateWorkflow
          open={showQuickCreate}
          onOpenChange={setShowQuickCreate}
          contactId="default"
          onComplete={() => setShowQuickCreate(false)}
        />
      )}
    </div>
  );
}