import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { 
  Clock, 
  Building2, 
  User, 
  Wrench,
  GripVertical,
  ChevronDown,
  Package,
  Search,
  Loader2,
  Briefcase
} from "lucide-react";
import type { Job, ServiceOrder } from "../types";
import { DispatcherService } from "../services/dispatcher.service";
import './dispatcher-drag.css';

export type PlanningMode = 'job' | 'serviceOrder';

interface UnassignedJobsListProps {
  jobs: Job[];
  isLoading?: boolean;
  onJobUpdate: () => void;
  onJobClick?: (job: Job) => void;
  isMobile?: boolean;
  planningMode?: PlanningMode;
  onPlanningModeChange?: (mode: PlanningMode) => void;
}

export function UnassignedJobsList({ 
  jobs, 
  isLoading, 
  onJobUpdate, 
  onJobClick, 
  isMobile,
  planningMode = 'job',
  onPlanningModeChange
}: UnassignedJobsListProps) {
  const { t } = useTranslation();
  const [expandedServiceOrders, setExpandedServiceOrders] = useState<Set<string>>(new Set());
  const [searchTerm, setSearchTerm] = useState("");
  const [isDragging, setIsDragging] = useState(false);
  
  // Group jobs by service order and filter by search term
  // Only show service orders with status 'ready_for_planning'
  const serviceOrdersWithJobs = () => {
    const serviceOrders = DispatcherService.getServiceOrders();
    return serviceOrders
      .filter(order => order.status === 'ready_for_planning') // Only ready_for_planning status
      .map(order => ({
        ...order,
        unassignedJobs: jobs.filter(job => job.serviceOrderId === order.id)
      }))
      .filter(order => {
        // Filter by search term (service order ID or title)
        if (searchTerm) {
          const searchLower = searchTerm.toLowerCase();
          const matchesOrderId = order.id.toLowerCase().includes(searchLower);
          const matchesOrderTitle = order.title.toLowerCase().includes(searchLower);
          const matchesJobTitle = order.unassignedJobs.some(job => 
            job.title.toLowerCase().includes(searchLower)
          );
          return matchesOrderId || matchesOrderTitle || matchesJobTitle;
        }
        
        return true;
      });
  };

  const groupedData = serviceOrdersWithJobs();

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'urgent': return 'destructive';
      case 'high': return 'destructive';
      case 'medium': return 'secondary';
      case 'low': return 'outline';
      default: return 'outline';
    }
  };

  const toggleServiceOrder = (serviceOrderId: string) => {
    setExpandedServiceOrders(prev => {
      const newSet = new Set(prev);
      if (newSet.has(serviceOrderId)) {
        newSet.delete(serviceOrderId);
      } else {
        newSet.add(serviceOrderId);
      }
      return newSet;
    });
  };

  // Handle dragging individual jobs
  const handleJobDragStart = (e: React.DragEvent, job: Job) => {
    if (isMobile || planningMode !== 'job') {
      e.preventDefault();
      return;
    }
    
    setIsDragging(true);
    document.body.classList.add('dragging');
    
    const dragData = {
      type: 'job',
      item: job,
      timestamp: Date.now()
    };
    
    e.dataTransfer.setData('application/json', JSON.stringify(dragData));
    e.dataTransfer.effectAllowed = 'move';
    
    const dragGhost = createJobDragGhost(job);
    e.dataTransfer.setDragImage(dragGhost, dragGhost.offsetWidth / 2, dragGhost.offsetHeight / 2);
    
    const target = e.currentTarget as HTMLElement;
    target.classList.add('dragging');
    
    setTimeout(() => {
      target.style.opacity = '0.5';
    }, 50);
  };

  // Handle dragging entire service order
  const handleServiceOrderDragStart = (e: React.DragEvent, serviceOrder: ServiceOrder & { unassignedJobs: Job[] }) => {
    if (isMobile || planningMode !== 'serviceOrder') {
      e.preventDefault();
      return;
    }
    
    setIsDragging(true);
    document.body.classList.add('dragging');
    
    // Create service order drag data with all jobs
    const dragData = {
      type: 'serviceOrder',
      item: {
        ...serviceOrder,
        jobs: serviceOrder.unassignedJobs // Include all unassigned jobs
      },
      timestamp: Date.now()
    };
    
    e.dataTransfer.setData('application/json', JSON.stringify(dragData));
    e.dataTransfer.effectAllowed = 'move';
    
    const dragGhost = createServiceOrderDragGhost(serviceOrder);
    e.dataTransfer.setDragImage(dragGhost, dragGhost.offsetWidth / 2, dragGhost.offsetHeight / 2);
    
    const target = e.currentTarget as HTMLElement;
    target.classList.add('dragging');
    
    setTimeout(() => {
      target.style.opacity = '0.5';
    }, 50);
  };

  const handleDragEnd = (e: React.DragEvent) => {
    setIsDragging(false);
    document.body.classList.remove('dragging');
    
    const target = e.currentTarget as HTMLElement;
    target.classList.remove('dragging');
    target.style.opacity = '';
    
    cleanupDragGhost();
  };

  const createJobDragGhost = (job: Job) => {
    const hours = Math.floor(job.estimatedDuration / 60);
    const mins = job.estimatedDuration % 60;
    const ghost = document.createElement('div');
    ghost.className = 'drag-ghost';
    ghost.innerHTML = `
      <div class="p-2 bg-card border rounded-lg shadow-lg min-w-[200px]">
        <div class="font-medium text-sm text-foreground">${job.title}</div>
        <div class="text-xs text-muted-foreground mt-1">${job.customerName}</div>
        <div class="text-xs text-muted-foreground">${hours}${t('dispatcher.hours_short')} ${mins}${t('dispatcher.minutes_short')}</div>
      </div>
    `;
    document.body.appendChild(ghost);
    return ghost;
  };

  const createServiceOrderDragGhost = (serviceOrder: ServiceOrder & { unassignedJobs: Job[] }) => {
    const totalDuration = serviceOrder.unassignedJobs.reduce((sum, job) => sum + (job.estimatedDuration || 60), 0);
    const jobCount = serviceOrder.unassignedJobs.length;
    const totalHours = Math.round(totalDuration / 60 * 10) / 10;
    const ghost = document.createElement('div');
    ghost.className = 'drag-ghost';
    ghost.innerHTML = `
      <div class="p-3 bg-primary/10 border-2 border-primary rounded-lg shadow-lg min-w-[220px]">
        <div class="flex items-center gap-2 mb-2">
          <svg class="h-4 w-4 text-primary" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="3" y="3" width="7" height="7" /><rect x="14" y="3" width="7" height="7" /><rect x="14" y="14" width="7" height="7" /><rect x="3" y="14" width="7" height="7" />
          </svg>
          <span class="font-semibold text-sm text-foreground">${serviceOrder.title}</span>
        </div>
        <div class="text-xs text-muted-foreground">${serviceOrder.customerName}</div>
        <div class="flex items-center gap-3 mt-2 text-xs text-primary font-medium">
          <span>${jobCount} ${t('dispatcher.jobs')}</span>
          <span>${totalHours}${t('dispatcher.hours_short')} ${t('dispatcher.total')}</span>
        </div>
      </div>
    `;
    document.body.appendChild(ghost);
    return ghost;
  };

  const cleanupDragGhost = () => {
    const ghosts = document.querySelectorAll('.drag-ghost');
    ghosts.forEach(ghost => ghost.remove());
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    const target = e.currentTarget as HTMLElement;
    target.classList.add('drag-ready');
  };

  const handleDragLeave = (e: React.DragEvent) => {
    const target = e.currentTarget as HTMLElement;
    target.classList.remove('drag-ready');
  };

  return (
    <Card className="h-full rounded-none border-0">
      <CardHeader className="pb-3">
        <h3 className="text-sm font-semibold mb-2">{t('dispatcher.service_orders')}</h3>
        
        {/* Planning Mode Toggle */}
        {onPlanningModeChange && (
          <div className="flex items-center gap-2">
              <span className={`text-xs ${planningMode === 'job' ? 'text-primary font-medium' : 'text-muted-foreground'}`}>
                {t('dispatcher.planning_mode_job')}
              </span>
              <Switch
                checked={planningMode === 'serviceOrder'}
                onCheckedChange={(checked) => onPlanningModeChange(checked ? 'serviceOrder' : 'job')}
                className="data-[state=checked]:bg-primary"
              />
              <span className={`text-xs ${planningMode === 'serviceOrder' ? 'text-primary font-medium' : 'text-muted-foreground'}`}>
                {t('dispatcher.planning_mode_service_order')}
              </span>
            </div>
        )}
        
        {/* Search Input */}
        <div className="relative mt-1">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder={t('dispatcher.search_placeholder')}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-10 h-9"
          />
        </div>
      </CardHeader>
      <CardContent className="p-0">
        <ScrollArea className="h-full">
          <div className="space-y-2 p-4 pt-0">
            {isLoading ? (
              <div className="space-y-3">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="border rounded-lg p-3 space-y-2">
                    <Skeleton className="h-4 w-24" />
                    <Skeleton className="h-3 w-32" />
                    <Skeleton className="h-12 w-full" />
                  </div>
                ))}
              </div>
            ) : groupedData.length === 0 ? (
              <div className="text-center py-8 text-muted-foreground">
                <Wrench className="h-8 w-8 mx-auto mb-2 opacity-50" />
                <p className="text-sm">
                  {searchTerm ? t('common.noResults', 'No results found') : t('dispatcher.no_unassigned_jobs')}
                </p>
              </div>
            ) : (
              groupedData.map((serviceOrderData) => (
                <div 
                  key={serviceOrderData.id} 
                  className={`border rounded-lg bg-card/50 overflow-hidden ${
                    planningMode === 'serviceOrder' ? 'cursor-grab hover:border-primary/50 hover:shadow-md transition-all' : ''
                  }`}
                  draggable={planningMode === 'serviceOrder' && !isMobile}
                  onDragStart={planningMode === 'serviceOrder' ? (e) => handleServiceOrderDragStart(e, serviceOrderData) : undefined}
                  onDragEnd={planningMode === 'serviceOrder' ? handleDragEnd : undefined}
                >
                  {/* Service Order Header */}
                  <div 
                    className={`p-2.5 border-b bg-muted/30 transition-colors ${
                      planningMode === 'serviceOrder' 
                        ? 'hover:bg-primary/10' 
                        : 'cursor-pointer hover:bg-muted/40'
                    }`}
                    onClick={planningMode === 'job' ? () => toggleServiceOrder(serviceOrderData.id) : undefined}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2 flex-1 min-w-0">
                        {planningMode === 'serviceOrder' && !isMobile && (
                          <GripVertical className="h-4 w-4 text-primary flex-shrink-0" />
                        )}
                        <Package className="h-3.5 w-3.5 text-primary flex-shrink-0" />
                        <div className="min-w-0 flex-1">
                          <div className="flex items-center gap-2">
                            <span className="font-medium truncate" style={{ fontSize: '0.75rem' }}>
                              {serviceOrderData.title || `SO-${serviceOrderData.id}`}
                            </span>
                          </div>
                          <div className="text-muted-foreground" style={{ fontSize: '0.65rem' }}>
                            {serviceOrderData.customerName} • {serviceOrderData.unassignedJobs.length} {t('dispatcher.jobs')}
                            {planningMode === 'serviceOrder' && (
                              <span className="ml-1">
                                • {Math.round(serviceOrderData.unassignedJobs.reduce((sum, j) => sum + (j.estimatedDuration || 60), 0) / 60 * 10) / 10}{t('dispatcher.hours_short')}
                              </span>
                            )}
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <Badge variant={getPriorityColor(serviceOrderData.priority)} className="text-[0.6rem] px-1 py-0 h-4">
                          {t(`dispatcher.priority_${serviceOrderData.priority}`)}
                        </Badge>
                        {planningMode === 'job' && (
                          <ChevronDown 
                            className={`h-3 w-3 transition-transform text-muted-foreground ${
                              expandedServiceOrders.has(serviceOrderData.id) ? 'rotate-180' : ''
                            }`} 
                          />
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Jobs List - only show in job mode or when expanded */}
                  {planningMode === 'job' && expandedServiceOrders.has(serviceOrderData.id) && (
                    <div className="bg-background">
                      <div className="space-y-1.5 p-2">
                        {serviceOrderData.unassignedJobs.map((job) => (
                          <div
                            key={job.id}
                            draggable={!isMobile && planningMode === 'job'}
                            onDragStart={(e) => handleJobDragStart(e, job)}
                            onDragEnd={handleDragEnd}
                            onDragOver={handleDragOver}
                            onDragLeave={handleDragLeave}
                            onClick={() => isMobile && onJobClick?.(job)}
                            className={`dispatcher-job-item p-2 border rounded bg-card transition-all ${
                              isMobile 
                                ? 'mobile cursor-pointer hover:shadow-sm' 
                                : planningMode === 'job' ? 'cursor-grab hover:shadow-md' : ''
                            } hover:border-primary/50`}
                          >
                            <div className="flex items-start gap-1.5">
                              {!isMobile && (
                                <GripVertical 
                                  className="grip-icon h-3 w-3 text-muted-foreground mt-0.5 flex-shrink-0 opacity-60" 
                                />
                              )}
                              
                              <div className="flex-1 min-w-0">
                                <div className="flex items-start justify-between mb-1">
                                  <h4 className="font-medium leading-tight" style={{ fontSize: '0.7rem' }}>
                                    {job.title}
                                  </h4>
                                  <Badge variant={getPriorityColor(job.priority)} className="text-[0.6rem] px-1 py-0 h-4">
                                    {t(`dispatcher.priority_${job.priority}`)}
                                  </Badge>
                                </div>
                                
                                {job.description && (
                                  <p className="text-muted-foreground mb-1 leading-tight" style={{ fontSize: '0.65rem' }}>
                                    {job.description}
                                  </p>
                                )}
                                
                                <div className="space-y-0.5">
                                  <div className="flex items-center gap-1.5 text-muted-foreground" style={{ fontSize: '0.65rem' }}>
                                    <User className="h-2 w-2 flex-shrink-0" />
                                    <span className="truncate">{job.customerName}</span>
                                  </div>
                                  
                                  {(job.installationName || job.installationId) && (
                                    <div className="flex items-center gap-1.5 text-muted-foreground" style={{ fontSize: '0.65rem' }}>
                                      <Building2 className="h-2 w-2 flex-shrink-0" />
                                      <span className="truncate">
                                        {job.installationName || t('dispatcher.loading_installation')}
                                      </span>
                                    </div>
                                  )}
                                  
                                  <div className="flex items-center gap-1.5 text-muted-foreground" style={{ fontSize: '0.65rem' }}>
                                    <Clock className="h-2 w-2 flex-shrink-0" />
                                    <span>
                                      {Math.floor(job.estimatedDuration / 60)}{t('dispatcher.hours_short')} {job.estimatedDuration % 60}{t('dispatcher.minutes_short')}
                                    </span>
                                  </div>
                                </div>
                                
                                <div className="mt-1">
                                  <div className="flex flex-wrap gap-0.5">
                                    {job.requiredSkills.slice(0, 2).map((skill) => (
                                      <Badge key={skill} variant="outline" className="text-[0.6rem] px-0.5 py-0 h-4">
                                        {skill}
                                      </Badge>
                                    ))}
                                    {job.requiredSkills.length > 2 && (
                                      <Badge variant="outline" className="text-[0.6rem] px-0.5 py-0 h-4">
                                        +{job.requiredSkills.length - 2}
                                      </Badge>
                                    )}
                                  </div>
                                </div>
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              ))
            )}
          </div>
        </ScrollArea>
      </CardContent>
    </Card>
  );
}