import { useState, useEffect } from "react";
import { useTranslation } from "react-i18next";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Calendar, Clock, Edit2, Trash2, Save, X, Loader2, CheckCircle, AlertTriangle, CalendarIcon, MoreHorizontal } from "lucide-react";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Calendar as CalendarComponent } from "@/components/ui/calendar";
import { format } from "date-fns";
import { toast } from "sonner";
import { TasksService } from "../services/tasks.service";
import { TaskTimeTracking } from "./TaskTimeTracking";
import { TaskChecklists } from "./TaskChecklists";
import { TaskDynamicForms } from "./TaskDynamicForms";
import { RecurringTaskSettings } from "./RecurringTaskSettings";
import { useTaskChecklists } from "../hooks/useTaskChecklists";
import { cn } from "@/lib/utils";
import type { DailyTask } from "../types";

interface DailyTaskDetailModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  task: DailyTask | null;
  onTaskUpdated?: () => void;
  onTaskDeleted?: (taskId: string) => void;
}

export function DailyTaskDetailModal({ 
  open, 
  onOpenChange, 
  task, 
  onTaskUpdated, 
  onTaskDeleted 
}: DailyTaskDetailModalProps) {
  const { t } = useTranslation('tasks');
  const [isEditing, setIsEditing] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  
  // Edit form state
  const [editTitle, setEditTitle] = useState("");
  const [editDescription, setEditDescription] = useState("");
  const [editPriority, setEditPriority] = useState<string>("medium");
  const [editStatus, setEditStatus] = useState<string>("todo");
  const [editDueDate, setEditDueDate] = useState<Date | undefined>(undefined);

  // Get numeric task ID for checklist
  const numericTaskId = task ? Number(task.id) : undefined;

  // Checklists hook
  const {
    checklists,
    isLoading: checklistsLoading,
    addChecklist,
    updateChecklist,
    deleteChecklist,
    toggleExpand,
    addItem,
    toggleItem,
    updateItem,
    deleteItem,
    reorderItems,
  } = useTaskChecklists({ dailyTaskId: numericTaskId });

  // Reset state when modal closes or task changes
  useEffect(() => {
    if (!open) {
      setIsEditing(false);
      setShowDeleteConfirm(false);
    }
  }, [open]);

  // Populate edit fields when task changes
  useEffect(() => {
    if (task) {
      setEditTitle(task.title);
      setEditDescription(task.description || "");
      setEditPriority(task.priority);
      setEditStatus(task.status);
      setEditDueDate(task.dueDate ? new Date(task.dueDate) : undefined);
    }
  }, [task]);

  if (!task) return null;

  const handleStartEdit = () => {
    setEditTitle(task.title);
    setEditDescription(task.description || "");
    setEditPriority(task.priority);
    setEditStatus(task.status);
    setEditDueDate(task.dueDate ? new Date(task.dueDate) : undefined);
    setIsEditing(true);
  };

  const handleCancelEdit = () => {
    setIsEditing(false);
    setEditTitle(task.title);
    setEditDescription(task.description || "");
    setEditPriority(task.priority);
    setEditStatus(task.status);
    setEditDueDate(task.dueDate ? new Date(task.dueDate) : undefined);
  };

  const handleSave = async () => {
    if (!editTitle.trim()) {
      toast.error(t('toast.titleRequired'));
      return;
    }

    setIsSaving(true);
    try {
      await TasksService.updateDailyTask(Number(task.id), {
        title: editTitle.trim(),
        description: editDescription.trim(),
        priority: editPriority,
        status: editStatus,
        dueDate: editDueDate ? editDueDate.toISOString() : undefined,
      });

      toast.success(t('toast.taskUpdated'));
      setIsEditing(false);
      onTaskUpdated?.();
    } catch (error) {
      console.error('Failed to update task:', error);
      toast.error(t('toast.failedUpdate'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async () => {
    setIsDeleting(true);
    try {
      await TasksService.deleteDailyTask(Number(task.id));
      toast.success(t('toast.taskDeleted'));
      setShowDeleteConfirm(false);
      onOpenChange(false);
      onTaskDeleted?.(task.id);
    } catch (error) {
      console.error('Failed to delete task:', error);
      toast.error(t('toast.failedDelete'));
    } finally {
      setIsDeleting(false);
    }
  };

  const handleStatusChange = async (newStatus: string) => {
    try {
      await TasksService.updateDailyTask(Number(task.id), { status: newStatus });
      toast.success(t('toast.taskStatusUpdated'));
      onTaskUpdated?.();
    } catch (error) {
      console.error('Failed to update status:', error);
      toast.error(t('toast.failedStatus'));
    }
  };

  const getPriorityStyles = (priority: string) => {
    switch (priority) {
      case 'urgent': return 'border-destructive/50 text-destructive bg-destructive/10';
      case 'high': return 'border-warning/50 text-warning bg-warning/10';
      case 'medium': return 'border-primary/50 text-primary bg-primary/10';
      case 'low': return 'border-muted-foreground/30 text-muted-foreground bg-muted';
      default: return 'border-muted-foreground/30 text-muted-foreground bg-muted';
    }
  };

  const getStatusStyles = (status: string) => {
    switch (status) {
      case 'done': return 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/30';
      case 'in-progress': return 'bg-primary/10 text-primary border-primary/30';
      case 'todo': return 'bg-muted text-muted-foreground border-border';
      default: return 'bg-muted text-muted-foreground border-border';
    }
  };

  const getStatusLabel = (status: string) => {
    switch (status) {
      case 'done': return t('daily.columns.done');
      case 'in-progress': return t('daily.columns.inProgress');
      case 'todo': return t('daily.columns.todo');
      default: return status;
    }
  };

  const isOverdue = task.dueDate && task.status !== 'done' && new Date(task.dueDate) < new Date();

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-[700px] md:max-w-[800px] p-0 gap-0 bg-card max-h-[95vh] md:max-h-[90vh] overflow-hidden">
          {/* Header */}
          <div className="flex items-start justify-between gap-3 p-4 md:p-6 border-b border-border bg-card">
            <div className="flex-1 min-w-0">
              {isEditing ? (
                <Input
                  value={editTitle}
                  onChange={(e) => setEditTitle(e.target.value)}
                  className="text-lg md:text-xl font-semibold border-none shadow-none px-0 h-auto focus-visible:ring-0"
                  placeholder="Task title"
                  autoFocus
                />
              ) : (
                <h2 className="text-lg md:text-xl font-semibold text-foreground leading-tight">
                  {task.title}
                </h2>
              )}
              
              {/* Status & Priority badges - inline with title area */}
              <div className="flex flex-wrap items-center gap-2 mt-3">
                {isEditing ? (
                  <>
                    <Select value={editStatus} onValueChange={setEditStatus}>
                      <SelectTrigger className="w-[130px] h-8 text-xs">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="todo">{t('daily.columns.todo')}</SelectItem>
                        <SelectItem value="in-progress">{t('daily.columns.inProgress')}</SelectItem>
                        <SelectItem value="done">{t('daily.columns.done')}</SelectItem>
                      </SelectContent>
                    </Select>
                    <Select value={editPriority} onValueChange={setEditPriority}>
                      <SelectTrigger className="w-[120px] h-8 text-xs">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="urgent">{t('daily.filters.urgent')}</SelectItem>
                        <SelectItem value="high">{t('daily.filters.high')}</SelectItem>
                        <SelectItem value="medium">{t('daily.filters.medium')}</SelectItem>
                        <SelectItem value="low">{t('daily.filters.low')}</SelectItem>
                      </SelectContent>
                    </Select>
                  </>
                ) : (
                  <>
                    <Badge variant="outline" className={cn("text-xs font-medium", getStatusStyles(task.status))}>
                      {task.status === 'done' && <CheckCircle className="h-3 w-3 mr-1" />}
                      {getStatusLabel(task.status)}
                    </Badge>
                    <Badge variant="outline" className={cn("text-xs font-medium uppercase", getPriorityStyles(task.priority))}>
                      {t(`daily.filters.${task.priority}`)}
                    </Badge>
                    {isOverdue && (
                      <Badge variant="destructive" className="text-xs font-medium">
                        <AlertTriangle className="h-3 w-3 mr-1" />
                        {t('daily.overdue')}
                      </Badge>
                    )}
                  </>
                )}
              </div>
            </div>

            {/* Action buttons */}
            <div className="flex items-center gap-1 shrink-0">
              {isEditing ? (
                <>
                  <Button size="sm" variant="ghost" onClick={handleCancelEdit} disabled={isSaving} className="h-8 w-8 p-0">
                    <X className="h-4 w-4" />
                  </Button>
                  <Button size="sm" onClick={handleSave} disabled={isSaving} className="h-8 px-3">
                    {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4 mr-1" />}
                    {t('common.save', 'Save')}
                  </Button>
                </>
              ) : (
                <>
                  {/* Mobile: dropdown menu, Desktop: inline buttons */}
                  <div className="hidden md:flex items-center gap-1">
                    <Button size="sm" variant="ghost" onClick={handleStartEdit} className="h-8 w-8 p-0">
                      <Edit2 className="h-4 w-4" />
                    </Button>
                    <Button size="sm" variant="ghost" onClick={() => setShowDeleteConfirm(true)} className="h-8 w-8 p-0 text-destructive hover:text-destructive">
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                  <div className="md:hidden">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button size="sm" variant="ghost" className="h-8 w-8 p-0">
                          <MoreHorizontal className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={handleStartEdit}>
                          <Edit2 className="h-4 w-4 mr-2" />
                          {t('common.edit', 'Edit')}
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => setShowDeleteConfirm(true)} className="text-destructive">
                          <Trash2 className="h-4 w-4 mr-2" />
                          {t('common.delete', 'Delete')}
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>
                </>
              )}
            </div>
          </div>

          {/* Content area - scrollable */}
          <ScrollArea className="flex-1 max-h-[calc(95vh-180px)] md:max-h-[calc(90vh-180px)]">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-0 md:gap-6 p-4 md:p-6">
              {/* Main content - left side on desktop */}
              <div className="md:col-span-2 space-y-6">
                {/* Description */}
                <div className="space-y-2">
                  <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                    {t('taskDetail.description')}
                  </h4>
                  {isEditing ? (
                    <Textarea
                      value={editDescription}
                      onChange={(e) => setEditDescription(e.target.value)}
                      placeholder={t('taskDetail.addDescription')}
                      rows={4}
                      className="resize-none"
                    />
                  ) : (
                    <div className="min-h-[60px] rounded-md bg-muted/30 p-3 text-sm">
                      {task.description || (
                        <span className="text-muted-foreground italic">{t('taskDetail.noDescription')}</span>
                      )}
                    </div>
                  )}
                </div>

                <Separator />

                {/* Time Tracking Section - Right after description */}
                <TaskTimeTracking
                  taskId={task.id}
                  taskType="daily"
                  taskTitle={task.title}
                  estimatedHours={task.estimatedHours}
                  onTimeUpdated={() => {
                    onTaskUpdated?.();
                  }}
                />

                <Separator />

                {/* Checklists Section */}
                <TaskChecklists
                  checklists={checklists}
                  onAddChecklist={addChecklist}
                  onUpdateChecklist={updateChecklist}
                  onDeleteChecklist={deleteChecklist}
                  onToggleExpand={toggleExpand}
                  onAddItem={addItem}
                  onToggleItem={toggleItem}
                  onUpdateItem={updateItem}
                  onDeleteItem={deleteItem}
                  onReorderItems={reorderItems}
                  isLoading={checklistsLoading}
                />

                <Separator />

                {/* Dynamic Forms Section */}
                <TaskDynamicForms
                  taskId={task.id}
                  taskType="daily"
                  taskTitle={task.title}
                  onFormsUpdated={() => onTaskUpdated?.()}
                />

                <Separator />

                {/* Recurring Task Section */}
                <RecurringTaskSettings dailyTaskId={numericTaskId} />
              </div>

              {/* Sidebar - right side on desktop, below on mobile */}
              <div className="md:border-l md:border-border md:pl-6 mt-6 md:mt-0 pt-6 md:pt-0 border-t md:border-t-0">
                <div className="space-y-5">
                  {/* Due Date */}
                  <div className="space-y-2">
                    <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-2">
                      <CalendarIcon className="h-3.5 w-3.5" />
                      {t('taskDetail.dueDate')}
                    </h4>
                    {isEditing ? (
                      <Popover>
                        <PopoverTrigger asChild>
                          <Button
                            variant="outline"
                            className={cn(
                              "w-full justify-start text-left font-normal h-9 text-sm",
                              !editDueDate && "text-muted-foreground"
                            )}
                          >
                            <CalendarIcon className="mr-2 h-4 w-4" />
                            {editDueDate ? format(editDueDate, "PPP") : <span>{t('taskDetail.pickDate', 'Pick a date')}</span>}
                          </Button>
                        </PopoverTrigger>
                        <PopoverContent className="w-auto p-0" align="start">
                          <CalendarComponent
                            mode="single"
                            selected={editDueDate}
                            onSelect={setEditDueDate}
                            initialFocus
                            className="pointer-events-auto"
                          />
                        </PopoverContent>
                      </Popover>
                    ) : (
                      <p className={cn(
                        "text-sm font-medium",
                        isOverdue ? 'text-destructive' : 'text-foreground'
                      )}>
                        {task.dueDate 
                          ? format(new Date(task.dueDate), "EEEE, MMMM d, yyyy")
                          : <span className="text-muted-foreground font-normal">-</span>
                        }
                      </p>
                    )}
                  </div>

                  <Separator />

                  {/* Created At */}
                  <div className="space-y-2">
                    <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-2">
                      <Clock className="h-3.5 w-3.5" />
                      {t('taskDetail.created')}
                    </h4>
                    <p className="text-sm text-muted-foreground">
                      {task.createdAt 
                        ? format(new Date(task.createdAt), "EEEE, MMMM d, yyyy 'at' HH:mm")
                        : '-'
                      }
                    </p>
                  </div>

                  {/* Quick Actions */}
                  {!isEditing && (
                    <>
                      <Separator />
                      <div className="space-y-3">
                        <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                          {t('quickActions', 'Quick Actions')}
                        </h4>
                        <div className="flex flex-col gap-2">
                          {task.status !== 'done' && (
                            <Button 
                              size="sm" 
                              variant="outline" 
                              onClick={() => handleStatusChange('done')}
                              className="justify-start h-9 text-emerald-600 hover:text-emerald-700 hover:bg-emerald-50 dark:hover:bg-emerald-950/30"
                            >
                              <CheckCircle className="h-4 w-4 mr-2" />
                              {t('daily.complete')}
                            </Button>
                          )}
                          {task.status === 'done' && (
                            <Button 
                              size="sm" 
                              variant="outline" 
                              onClick={() => handleStatusChange('todo')}
                              className="justify-start h-9"
                            >
                              {t('daily.reopen')}
                            </Button>
                          )}
                          {task.status === 'todo' && (
                            <Button 
                              size="sm" 
                              variant="outline" 
                              onClick={() => handleStatusChange('in-progress')}
                              className="justify-start h-9"
                            >
                              {t('startWorking', 'Start Working')}
                            </Button>
                          )}
                        </div>
                      </div>
                    </>
                  )}
                </div>
              </div>
            </div>
          </ScrollArea>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation */}
      <AlertDialog open={showDeleteConfirm} onOpenChange={setShowDeleteConfirm}>
        <AlertDialogContent className="bg-card">
          <AlertDialogHeader>
            <AlertDialogTitle>{t('taskDetail.deleteConfirm.title')}</AlertDialogTitle>
            <AlertDialogDescription>
              {t('taskDetail.deleteConfirm.description')}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isDeleting}>{t('common.cancel')}</AlertDialogCancel>
            <AlertDialogAction 
              onClick={handleDelete} 
              disabled={isDeleting}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {isDeleting ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
              {t('common.delete')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
