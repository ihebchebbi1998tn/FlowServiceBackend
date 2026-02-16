import { useState, useEffect, useCallback } from "react";
import { PageSkeleton, ContentSkeleton } from "@/components/ui/page-skeleton";
import { useParams, useNavigate } from "react-router-dom";
import { KanbanBoard } from "../components/KanbanBoard";
import TaskListView from "../components/TaskListView";
import { Button } from "@/components/ui/button";
import { CollapsibleSearch } from "@/components/ui/collapsible-search";
import { Badge } from "@/components/ui/badge";
import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { 
  ArrowLeft, 
  LayoutGrid, 
  List, 
  PlusCircle, 
  Settings, 
  FolderOpen,
  Clock,
  CheckCircle,
  TrendingUp,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  ChevronUp,
  Search,
  Filter,
  Loader2,
  CalendarIcon,
  Check,
  Trash2
} from "lucide-react";
import { Task, Project } from '../types';
import { useTranslation } from 'react-i18next';
import { TasksService } from '../services/tasks.service';
import { ProjectsService } from '../services/projects.service';
import { usersApi } from '@/services/api/usersApi';
import { useLookups } from '@/shared/contexts/LookupsContext';
import { useActionLogger } from "@/hooks/useActionLogger";
import { format, isToday, isSameDay } from "date-fns";
import { cn } from "@/lib/utils";

// Interface for technician/assignable users
interface Technician {
  id: string;
  name: string;
  email?: string;
}

export default function ProjectTasksPage() {
  const { t } = useTranslation('tasks');
  const { projectId } = useParams();
  const navigate = useNavigate();
  const { logAction, logSearch, logFilter } = useActionLogger('Projects');
  const [taskViewMode, setTaskViewMode] = useState<'board' | 'list'>('board');
  const [isColumnEditorOpen, setIsColumnEditorOpen] = useState(false);
  const [isQuickTaskModalOpen, setIsQuickTaskModalOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [filterStatus, setFilterStatus] = useState<'all' | 'todo' | 'in-progress' | 'review' | 'done'>('all');
  const [filterPriority, setFilterPriority] = useState<'all' | 'urgent' | 'high' | 'medium' | 'low'>('all');
  const [showFilterBar, setShowFilterBar] = useState(false);
  const [filterAssignee, setFilterAssignee] = useState<'all' | string>('all');
  
  // Date filter state
  const [selectedDate, setSelectedDate] = useState<Date>(new Date());
  const [showAllDates, setShowAllDates] = useState(true); // Default to all dates for projects
  
  // Completed tasks section
  const [showCompletedSection, setShowCompletedSection] = useState(false);
  
  // API state
  const [tasksState, setTasksState] = useState<any[]>([]);
  const [isLoadingTasks, setIsLoadingTasks] = useState(false);
  const [tasksError, setTasksError] = useState<string | null>(null);
  
  // Project state from API
  const [project, setProject] = useState<Project | null>(null);
  const [isLoadingProject, setIsLoadingProject] = useState(true);
  const [projectError, setProjectError] = useState<string | null>(null);
  
  // Technicians/Users state - real users from API
  const [technicians, setTechnicians] = useState<Technician[]>([]);

  const { priorities: lookupPriorities } = useLookups();

  // Log project view
  useEffect(() => {
    if (projectId && project) {
      logAction('view_project', `Viewed project: ${project.name}`, { 
        entityType: 'Project', 
        entityId: projectId 
      });
    }
  }, [projectId, project?.name]);

  // Log search
  useEffect(() => {
    if (searchTerm.length > 2) {
      const timer = setTimeout(() => {
        logSearch(searchTerm, tasksState?.length || 0, { entityType: 'ProjectTask' });
      }, 500);
      return () => clearTimeout(timer);
    }
  }, [searchTerm]);

  // Log filter changes
  useEffect(() => {
    if (filterStatus !== 'all') {
      logFilter('Status', filterStatus, { entityType: 'ProjectTask' });
    }
  }, [filterStatus]);

  useEffect(() => {
    if (filterPriority !== 'all') {
      logFilter('Priority', filterPriority, { entityType: 'ProjectTask' });
    }
  }, [filterPriority]);

  // Fetch users for task assignment (MainAdminUser + Users table)
  const fetchTechnicians = useCallback(async () => {
    try {
      const techList: Technician[] = [];
      
      // Add MainAdminUser from localStorage (id = 1)
      const userData = localStorage.getItem('user_data');
      if (userData) {
        try {
          const mainAdmin = JSON.parse(userData);
          if (mainAdmin && mainAdmin.id) {
            techList.push({
              id: String(mainAdmin.id),
              name: `${mainAdmin.firstName || ''} ${mainAdmin.lastName || ''}`.trim() || mainAdmin.email || 'Admin',
              email: mainAdmin.email,
            });
          }
        } catch (e) {
          console.warn('Failed to parse user_data for MainAdminUser');
        }
      }
      
      // Fetch users from Users table (id >= 2)
      try {
        const usersResult = await usersApi.getAll();
        if (usersResult.users) {
          usersResult.users.forEach(user => {
            // Avoid duplicates if MainAdminUser is also in Users table
            if (!techList.some(t => t.id === String(user.id))) {
              techList.push({
                id: String(user.id),
                name: `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.email,
                email: user.email,
              });
            }
          });
        }
      } catch (e) {
        console.warn('Failed to fetch users from API:', e);
      }
      
      setTechnicians(techList);
    } catch (error) {
      console.error('Failed to fetch technicians:', error);
    }
  }, []);

  useEffect(() => {
    fetchTechnicians();
  }, [fetchTechnicians]);
  
  // Parse numeric project ID
  const numericProjectId = projectId ? parseInt(projectId, 10) : null;

  // Map API tasks to local task format
  const mapToLocal = useCallback((apiTasks: Task[]) => apiTasks.map(pt => ({
    id: pt.id,
    title: pt.title,
    description: pt.description || '',
    priority: (pt.priority as 'high' | 'medium' | 'low') || 'medium',
    assignee: (pt.assignee as string) || pt.assigneeName || '',
    assigneeId: pt.assigneeId || '',
    dueDate: pt.dueDate instanceof Date ? pt.dueDate.toLocaleDateString() : String(pt.dueDate || ''),
    columnId: String(pt.columnId) || (pt.status as string) || 'todo',
    createdAt: pt.createdAt || new Date(),
    projectId: pt.projectId || projectId,
  })), [projectId]);

  // Fetch project from API
  const fetchProject = useCallback(async () => {
    if (!numericProjectId || isNaN(numericProjectId)) {
      setProjectError('Invalid project ID');
      setIsLoadingProject(false);
      return;
    }

    setIsLoadingProject(true);
    setProjectError(null);

    try {
      const fetchedProject = await ProjectsService.getProjectById(numericProjectId);
      setProject(fetchedProject);
    } catch (error) {
      console.error('Failed to fetch project from API:', error);
      setProjectError('Failed to load project');
    } finally {
      setIsLoadingProject(false);
    }
  }, [numericProjectId]);

  // Fetch tasks from API
  const fetchTasks = useCallback(async () => {
    if (!numericProjectId || isNaN(numericProjectId)) {
      return;
    }

    setIsLoadingTasks(true);
    setTasksError(null);

    try {
      const apiTasks = await TasksService.getProjectTasks(numericProjectId);
      setTasksState(mapToLocal(apiTasks));
    } catch (error) {
      console.error('Failed to fetch tasks from API:', error);
      setTasksError('Failed to load tasks');
      // Keep existing tasks if API fails
    } finally {
      setIsLoadingTasks(false);
    }
  }, [numericProjectId, mapToLocal]);

  // Fetch project and tasks on mount
  useEffect(() => {
    fetchProject();
  }, [fetchProject]);

  useEffect(() => {
    if (project) {
      fetchTasks();
    }
  }, [project, fetchTasks]);

  // Calculate project statistics from live tasksState
  const projectStats = {
    totalTasks: tasksState.length,
    completedTasks: tasksState.filter((task: any) => (task.columnId === 'done') || task.completedAt).length,
    inProgressTasks: tasksState.filter((task: any) => task.columnId === 'in-progress').length,
    overdueTasks: tasksState.filter((task: any) => {
      try {
        const d = new Date(task.dueDate);
        return d < new Date() && !task.completedAt;
      } catch {
        return false;
      }
    }).length,
    totalEstimatedHours: tasksState.reduce((sum: number, task: any) => sum + (task.estimatedHours || 0), 0),
    totalActualHours: tasksState.reduce((sum: number, task: any) => sum + (task.actualHours || 0), 0),
    completionPercentage: tasksState.length > 0
      ? Math.round((tasksState.filter((task: any) => (task.columnId === 'done') || task.completedAt).length / tasksState.length) * 100)
      : 0
  };

  // Date navigation helpers
  const goToPreviousDay = () => {
    const newDate = new Date(selectedDate);
    newDate.setDate(newDate.getDate() - 1);
    setSelectedDate(newDate);
  };

  const goToNextDay = () => {
    const newDate = new Date(selectedDate);
    newDate.setDate(newDate.getDate() + 1);
    setSelectedDate(newDate);
  };

  const goToToday = () => {
    setSelectedDate(new Date());
  };

  // Filter tasks by date (if not showing all dates)
  const getFilteredTasks = () => {
    let filtered = tasksState
      .filter(t => t.title.toLowerCase().includes(searchTerm.toLowerCase()) || (t.description||'').toLowerCase().includes(searchTerm.toLowerCase()))
      .filter(t => filterStatus === 'all' ? true : (t.columnId === filterStatus))
      .filter(t => filterPriority === 'all' ? true : t.priority === filterPriority)
      .filter(t => filterAssignee === 'all' ? true : t.assignee === filterAssignee);
    
    if (!showAllDates) {
      filtered = filtered.filter(t => {
        const dueDate = t.dueDate ? new Date(t.dueDate) : null;
        const createdDate = t.createdAt ? new Date(t.createdAt) : null;
        return (dueDate && isSameDay(dueDate, selectedDate)) || (createdDate && isSameDay(createdDate, selectedDate));
      });
    }
    
    return filtered;
  };

  // Get open vs completed tasks
  const filteredTasks = getFilteredTasks();
  const openTasks = filteredTasks.filter(t => t.columnId !== 'done' && !t.completedAt);
  const completedTasks = filteredTasks.filter(t => t.columnId === 'done' || t.completedAt);

  const handleTaskClick = (task: Task) => {
    console.log('Task clicked:', task);
  };

  const handleAddTask = () => {
    setIsQuickTaskModalOpen(true);
  };

  const handleTaskComplete = (taskId: string) => {
    console.log('Complete task:', taskId);
  };

  const handleBackToProjects = () => {
    navigate("/dashboard/tasks/projects");
  };

  // Loading state for project
  if (isLoadingProject) {
    return <PageSkeleton />;
  }

  if (!project || projectError) {
    return (
      <div className="h-full bg-background flex items-center justify-center">
        <div className="text-center">
          <FolderOpen className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-foreground mb-2">{t('projects.notFoundTitle')}</h2>
          <p className="text-muted-foreground mb-4">{projectError || t('projects.notFoundDesc')}</p>
          <Button onClick={handleBackToProjects} className="gap-2">
            <ArrowLeft className="h-4 w-4" />
            {t('projects.header.back')}
          </Button>
        </div>
      </div>
    );
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active': return 'bg-success text-success-foreground';
      case 'completed': return 'bg-primary text-primary-foreground';
      case 'on-hold': return 'bg-warning text-warning-foreground';
      case 'cancelled': return 'bg-destructive text-destructive-foreground';
      default: return 'bg-secondary text-secondary-foreground';
    }
  };

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'service': return 'bg-primary/10 text-primary';
      case 'sales': return 'bg-success/10 text-success';
      case 'internal': return 'bg-secondary text-secondary-foreground';
      case 'custom': return 'bg-warning/10 text-warning';
      default: return 'bg-muted text-muted-foreground';
    }
  };

  const Header = () => (
    <>
      {/* Desktop Header - matches articles/contacts pattern */}
      <div className="hidden md:flex items-center justify-between p-4 border-b border-border bg-card/50 backdrop-blur">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="sm" onClick={handleBackToProjects} className="gap-2 hover:bg-background/80 -ml-2">
            <ArrowLeft className="h-4 w-4" />
            {t('projects.header.back')}
          </Button>
          <div className="h-6 w-px bg-border" />
          <div className="p-2 rounded-lg bg-primary/10">
            <FolderOpen className="h-6 w-6 text-primary" />
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-3">
              <h1 className="text-xl font-semibold text-foreground truncate">{project.name}</h1>
              <Badge className={getStatusColor(project.status)}>{project.status}</Badge>
              <Badge className={getTypeColor(project.type)} variant="outline">{project.type}</Badge>
            </div>
            <p className="text-[11px] text-muted-foreground truncate max-w-md">
              {project.description || t('projects.header.noDescription', { defaultValue: 'No description' })}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => setIsColumnEditorOpen(true)} className="gap-2">
            <Settings className="h-4 w-4" />
            {t('projects.header.manage')}
          </Button>
          <Button 
            className="gradient-primary text-primary-foreground shadow-medium hover-lift gap-2"
            onClick={handleAddTask}
          >
            <PlusCircle className="h-4 w-4" />
            {t('projects.header.addTask')}
          </Button>
        </div>
      </div>

      {/* Mobile Header */}
      <div className="md:hidden border-b border-border bg-card/50 backdrop-blur">
        <div className="flex items-center justify-between p-3 border-b border-border/50">
          <Button variant="ghost" size="sm" onClick={handleBackToProjects} className="gap-2 -ml-2">
            <ArrowLeft className="h-4 w-4" />
            {t('projects.header.backShort')}
          </Button>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={() => setIsColumnEditorOpen(true)} className="h-8 w-8 p-0">
              <Settings className="h-4 w-4" />
            </Button>
            <Button 
              size="sm"
              className="gradient-primary text-primary-foreground shadow-medium hover-lift gap-2"
              onClick={handleAddTask}
            >
              <PlusCircle className="h-4 w-4" />
              {t('projects.header.add')}
            </Button>
          </div>
        </div>
        <div className="p-3">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10 shrink-0">
              <FolderOpen className="h-5 w-5 text-primary" />
            </div>
            <div className="flex-1 min-w-0">
              <h1 className="text-base font-semibold text-foreground truncate">{project.name}</h1>
              <div className="flex items-center gap-2 mt-1">
                <Badge className={`${getStatusColor(project.status)} text-[10px] px-1.5 py-0`}>{project.status}</Badge>
                <Badge className={`${getTypeColor(project.type)} text-[10px] px-1.5 py-0`} variant="outline">{project.type}</Badge>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );

  return (
    <div className="h-full bg-background">
      <Header />

      {/* Date Navigation */}
      <div className="bg-card border-b border-border/50 px-4 py-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="icon"
              className="h-8 w-8"
              onClick={goToPreviousDay}
              disabled={showAllDates}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            
            <Popover>
              <PopoverTrigger asChild>
                <Button
                  variant="outline"
                  className={cn(
                    "min-w-[180px] justify-center text-left font-medium gap-2 bg-background",
                    !showAllDates && isToday(selectedDate) && "border-primary/50"
                  )}
                  disabled={showAllDates}
                >
                  <CalendarIcon className="h-4 w-4" />
                  {showAllDates ? (
                    <span>{t('projects.dateFilter.allDates', 'All Dates')}</span>
                  ) : isToday(selectedDate) ? (
                    <span>{t('daily.today')} - {format(selectedDate, "MMM d, yyyy")}</span>
                  ) : (
                    <span>{format(selectedDate, "EEE, MMM d, yyyy")}</span>
                  )}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0" align="center">
                <Calendar
                  mode="single"
                  selected={selectedDate}
                  onSelect={(date) => date && setSelectedDate(date)}
                  initialFocus
                  className={cn("p-3 pointer-events-auto")}
                />
              </PopoverContent>
            </Popover>
            
            <Button
              variant="outline"
              size="icon"
              className="h-8 w-8"
              onClick={goToNextDay}
              disabled={showAllDates}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
            
            {!showAllDates && !isToday(selectedDate) && (
              <Button
                variant="ghost"
                size="sm"
                onClick={goToToday}
                className="text-primary"
              >
                {t('daily.today')}
              </Button>
            )}
          </div>
          
          <Button
            variant={showAllDates ? "default" : "outline"}
            size="sm"
            onClick={() => setShowAllDates(!showAllDates)}
            className={cn("gap-2", !showAllDates && "bg-background hover:bg-muted")}
          >
            {showAllDates ? t('projects.dateFilter.showingAll', 'Showing All') : t('projects.dateFilter.filterByDate', 'Filter by Date')}
          </Button>
        </div>
      </div>

      {/* Search and Controls */}
      <div className="p-3 sm:p-4 border-b border-border bg-card">
        <div className="flex flex-col gap-3 sm:flex-row sm:gap-4 sm:items-center sm:justify-between">
          <div className="flex gap-2 sm:gap-3 flex-1 w-full">
            <CollapsibleSearch 
              placeholder={t('projects.header.searchPlaceholder')} 
              value={searchTerm} 
              onChange={setSearchTerm}
              className="flex-1"
            />
            <div className="relative">
              <Button variant="outline" size="sm" className="gap-1 sm:gap-2 px-2 sm:px-3" onClick={() => setShowFilterBar(s => !s)}>
                <Filter className="h-4 w-4" />
                <span className="hidden sm:inline">{t('projects.filters.filters')}</span>
                {((filterStatus !== 'all') || (filterPriority !== 'all') || (filterAssignee !== 'all')) && (
                  <Badge variant="secondary" className="ml-2 h-4 px-1 text-xs">
                    {[(filterStatus !== 'all' ? 1 : 0), (filterPriority !== 'all' ? 1 : 0), (filterAssignee !== 'all' ? 1 : 0)].reduce((a,b)=>a+b,0)}
                  </Badge>
                )}
              </Button>
            </div>
          </div>

          <div className="flex items-center gap-2 w-full sm:w-auto">
            <Button 
              variant={taskViewMode === 'board' ? 'default' : 'outline'} 
              size="sm" 
              onClick={() => setTaskViewMode('board')} 
              className={`flex-1 sm:flex-none ${taskViewMode === 'board' ? 'bg-primary text-white hover:bg-primary/90' : ''}`}
            >
              <LayoutGrid className={`h-4 w-4 ${taskViewMode === 'board' ? 'text-white' : ''}`} />
            </Button>
            <Button 
              variant={taskViewMode === 'list' ? 'default' : 'outline'} 
              size="sm" 
              onClick={() => setTaskViewMode('list')} 
              className={`flex-1 sm:flex-none ${taskViewMode === 'list' ? 'bg-primary text-white hover:bg-primary/90' : ''}`}
            >
              <List className={`h-4 w-4 ${taskViewMode === 'list' ? 'text-white' : ''}`} />
            </Button>
          </div>
        </div>

        {showFilterBar && (
          <div className="p-3 sm:p-4 border-b border-border bg-background/50">
            <div className="flex flex-col sm:flex-row sm:items-center gap-3 sm:gap-4">
              <div className="flex-1 grid grid-cols-1 sm:grid-cols-4 gap-2">
                <div className="relative">
                  <select className="border rounded px-3 py-2 pr-10 appearance-none bg-background text-foreground w-full" value={filterStatus} onChange={e => setFilterStatus(e.target.value as any)}>
                    <option value="all">{t('projects.filters.all')}</option>
                    <option value="todo">{t('projects.filters.todo')}</option>
                    <option value="in-progress">{t('projects.filters.inProgress')}</option>
                    <option value="review">{t('projects.filters.review')}</option>
                    <option value="done">{t('projects.filters.done')}</option>
                  </select>
                  <ChevronDown className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                </div>
                <div className="relative">
                  <select className="border rounded px-3 py-2 pr-10 appearance-none bg-background text-foreground w-full" value={filterPriority} onChange={e => setFilterPriority(e.target.value as any)}>
                    <option value="all">{t('projects.filters.all')}</option>
                    {lookupPriorities.map((p:any) => (
                      <option key={p.id} value={p.id}>{p.name}</option>
                    ))}
                  </select>
                  <ChevronDown className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                </div>
                <div className="relative">
                  <select className="border rounded px-3 py-2 pr-10 appearance-none bg-background text-foreground w-full" value={filterAssignee} onChange={e => setFilterAssignee(e.target.value)}>
                    <option value="all">{t('projects.filters.allAssignees')}</option>
                    {technicians.map((tech)=> <option key={tech.id} value={tech.name}>{tech.name}</option>)}
                  </select>
                  <ChevronDown className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                </div>
                <div className="relative">
                  <select className="border rounded px-3 py-2 pr-10 appearance-none bg-background text-foreground w-full">
                    <option value="any">{t('projects.filters.anyTime')}</option>
                    <option value="7">{t('projects.filters.last7')}</option>
                    <option value="30">{t('projects.filters.last30')}</option>
                    <option value="365">{t('projects.filters.last365')}</option>
                  </select>
                  <ChevronDown className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                </div>
              </div>
                <div className="flex items-center gap-2">
                <button className="px-3 py-1 rounded-full border border-border text-sm" onClick={() => { setFilterStatus('all'); setFilterPriority('all'); setFilterAssignee('all'); setShowFilterBar(false); }}>{t('projects.filters.clear')}</button>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Task Content */}
      <div className="p-4 sm:p-6">
        {isLoadingTasks ? (
          <ContentSkeleton rows={6} />
        ) : tasksError ? (
          <div className="flex flex-col items-center justify-center py-12">
            <FolderOpen className="h-12 w-12 text-muted-foreground mb-4" />
            <p className="text-muted-foreground mb-4">{t('projects.noTasks.none')}</p>
            <Button onClick={fetchTasks} variant="outline" className="gap-2">
              <Loader2 className="h-4 w-4" />
              {t('projects.noTasks.refresh')}
            </Button>
          </div>
        ) : (
          <>
            {/* Completed Tasks Banner */}
            {completedTasks.length > 0 && (
              <div className="mb-4 bg-success/5 border border-success/20 rounded-lg overflow-hidden">
                <button
                  onClick={() => setShowCompletedSection(!showCompletedSection)}
                  className="w-full flex items-center justify-between px-4 py-2.5 hover:bg-success/10 transition-colors"
                >
                  <div className="flex items-center gap-3">
                    <div className="flex items-center justify-center w-6 h-6 rounded-full bg-success/20">
                      <Check className="h-3.5 w-3.5 text-success" />
                    </div>
                    <span className="text-sm font-medium text-foreground">
                      {t('projects.completedSection.completed', { count: completedTasks.length })}
                    </span>
                    <Badge variant="secondary" className="bg-success/10 text-success border-0">
                      {completedTasks.length}
                    </Badge>
                  </div>
                  <div className="flex items-center gap-2">
                    {showCompletedSection ? (
                      <ChevronUp className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      <ChevronDown className="h-4 w-4 text-muted-foreground" />
                    )}
                  </div>
                </button>
                
                {showCompletedSection && (
                  <div className="px-4 pb-3 pt-1">
                    <div className="flex flex-wrap gap-2">
                      {completedTasks.map(task => (
                        <div 
                          key={task.id}
                          className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-card border border-border text-sm group hover:border-success/30 transition-colors cursor-pointer"
                          onClick={() => handleTaskClick(task)}
                        >
                          <div className="flex-shrink-0 w-4 h-4 rounded-full bg-success flex items-center justify-center text-white">
                            <Check className="h-2.5 w-2.5" />
                          </div>
                          <span className="text-muted-foreground line-through max-w-[200px] truncate">
                            {task.title}
                          </span>
                          <Badge 
                            variant="outline" 
                            className={cn(
                              "text-[9px] px-1.5 py-0 h-4 opacity-60",
                              task.priority === 'urgent' && "border-destructive/50 text-destructive",
                              task.priority === 'high' && "border-warning/50 text-warning",
                              task.priority === 'medium' && "border-warning/50 text-warning",
                              task.priority === 'low' && "border-success/50 text-success"
                            )}
                          >
                            {task.priority}
                          </Badge>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* Main Task View */}
            {taskViewMode === 'list' ? (
              <TaskListView
                tasks={openTasks.map(t => ({
                  ...t,
                  status: t.columnId,
                  dueDate: new Date(t.dueDate || Date.now()),
                  createdAt: new Date(t.createdAt || Date.now()),
                  updatedAt: new Date(),
                  tags: [],
                  position: 0,
                })) as Task[]}
                columns={project?.columns}
                onTaskClick={handleTaskClick}
                onAddTask={() => handleAddTask()}
                onTaskComplete={(id) => handleTaskComplete(id)}
              />
            ) : (
              <KanbanBoard 
                onSwitchToProjects={handleBackToProjects}
                isDailyTasks={false}
                hideHeader={true}
                project={project}
                technicians={technicians}
                columnEditorOpen={isColumnEditorOpen}
                onColumnEditorOpenChange={(open) => setIsColumnEditorOpen(open)}
                quickTaskModalOpen={isQuickTaskModalOpen}
                onQuickTaskModalOpenChange={(open) => setIsQuickTaskModalOpen(open)}
                initialTasks={openTasks}
                onTasksChange={(next) => setTasksState(next)}
              />
            )}
          </>
        )}
      </div>
    </div>
  );
}