import { useState, useEffect } from "react";
import { ContentSkeleton } from "@/components/ui/page-skeleton";
import { useNavigate } from "react-router-dom";
import { getStatusColorClass } from "@/config/entity-statuses";
import { useTranslation } from "react-i18next";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { CollapsibleSearch } from "@/components/ui/collapsible-search";
import { 
  Wrench, Clock, AlertTriangle, CheckCircle, 
  Building2, Loader2, MoreVertical, Eye, Edit, 
  User, Calendar, Trash2, Filter, ChevronDown, X
} from "lucide-react";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { serviceOrdersApi, type ServiceOrderJob } from "@/services/api/serviceOrdersApi";
import { installationsApi } from "@/services/api/installationsApi";

interface DispatchJobsTabProps {
  dispatchId: string;
  jobId: string;
  serviceOrderId: string;
  dispatchStatus?: string;
  dispatchPriority?: string;
}

export function DispatchJobsTab({ 
  dispatchId, 
  jobId, 
  serviceOrderId,
  dispatchStatus,
  dispatchPriority
}: DispatchJobsTabProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [job, setJob] = useState<ServiceOrderJob | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [showFilterBar, setShowFilterBar] = useState(false);
  const [filterStatus, setFilterStatus] = useState('all');
  const [filterWorkType, setFilterWorkType] = useState('all');
  const [installationName, setInstallationName] = useState<string | null>(null);

  useEffect(() => {
    const fetchJob = async () => {
      if (!serviceOrderId || !jobId) {
        setLoading(false);
        setError("Missing job or service order reference");
        return;
      }

      try {
        setLoading(true);
        setError(null);
        
        const serviceOrderIdNum = parseInt(serviceOrderId);
        const jobIdNum = parseInt(jobId);
        
        if (isNaN(serviceOrderIdNum) || isNaN(jobIdNum)) {
          setError("Invalid job or service order ID");
          setLoading(false);
          return;
        }

        const fetchedJob = await serviceOrdersApi.getJobById(serviceOrderIdNum, jobIdNum);
        setJob(fetchedJob);

        // Fetch installation name if we have installationId but no installationName
        if (fetchedJob?.installationId && !fetchedJob?.installationName) {
          try {
            const installation = await installationsApi.getById(String(fetchedJob.installationId));
            if (installation?.name) {
              setInstallationName(installation.name);
            }
          } catch (err) {
            console.warn('Failed to fetch installation name:', err);
          }
        } else if (fetchedJob?.installationName) {
          setInstallationName(fetchedJob.installationName);
        }
      } catch (err) {
        console.error('Failed to fetch job:', err);
        setError("Failed to load job details");
      } finally {
        setLoading(false);
      }
    };

    fetchJob();
  }, [serviceOrderId, jobId]);

  // Status color derived from centralized job config
  const getStatusColor = (status: string) => {
    return getStatusColorClass('job', status) + " hover:opacity-80 transition-colors";
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'dispatched':
        return <CheckCircle className="h-3 w-3" />;
      case 'ready':
        return <Clock className="h-3 w-3" />;
      case 'cancelled':
        return <AlertTriangle className="h-3 w-3" />;
      case 'unscheduled':
        return <Wrench className="h-3 w-3" />;
      default:
        return <Wrench className="h-3 w-3" />;
    }
  };

  // Count active filters
  const activeFiltersCount = [
    filterStatus !== 'all' ? 1 : 0,
    filterWorkType !== 'all' ? 1 : 0
  ].reduce((a, b) => a + b, 0);

  if (loading) {
    return (
      <Card className="bg-background border">
        <CardContent className="py-4">
          <ContentSkeleton rows={4} />
        </CardContent>
      </Card>
    );
  }

  // Build job data from fetched data or fallback from dispatch props
  const jobData = job || (jobId ? {
    id: parseInt(jobId),
    serviceOrderId: parseInt(serviceOrderId),
    title: `Job #${jobId}`,
    jobNumber: `JOB-${jobId}`,
    description: '',
    status: (dispatchStatus === 'completed' ? 'dispatched' : 
             dispatchStatus === 'in_progress' ? 'ready' : 'unscheduled') as ServiceOrderJob['status'],
    workType: 'service',
    priority: dispatchPriority,
  } : null);

  if (!jobData) {
    return (
      <Card className="bg-background border">
        <CardContent>
          <p className="text-muted-foreground text-center py-8">
            No job associated with this dispatch
          </p>
        </CardContent>
      </Card>
    );
  }

  // Get display values
  const displayTitle = job?.title || `Job #${jobId}`;
  const displayJobNumber = `JOB-${jobData.id}`;
  const displayStatus = job?.status || jobData.status || 'unscheduled';
  const displayWorkType = job?.workType || jobData.workType || 'service';
  const displayInstallationName = installationName || job?.installationName;
  const displayInstallationId = job?.installationId;

  // Filter logic (for single job, mainly for UI consistency)
  const matchesSearch = !searchTerm || 
    displayTitle.toLowerCase().includes(searchTerm.toLowerCase()) ||
    displayJobNumber.toLowerCase().includes(searchTerm.toLowerCase()) ||
    displayWorkType.toLowerCase().includes(searchTerm.toLowerCase());
  
  const matchesStatus = filterStatus === 'all' || displayStatus === filterStatus;
  const matchesWorkType = filterWorkType === 'all' || displayWorkType === filterWorkType;
  
  const showJob = matchesSearch && matchesStatus && matchesWorkType;

  return (
    <Card>
      <CardContent className="pt-6">
        {/* Search and Filter Bar */}
        <div className="flex flex-col gap-3 sm:flex-row sm:gap-4 sm:items-center sm:justify-between mb-4">
          <div className="flex-1">
            <CollapsibleSearch 
              placeholder="Search jobs..."
              value={searchTerm}
              onChange={setSearchTerm}
              className="w-full"
            />
          </div>
          <div className="flex items-center gap-2">
            <Button 
              variant="outline" 
              size="sm" 
              className="gap-2 px-3 z-20" 
              onClick={() => setShowFilterBar(s => !s)}
            >
              <Filter className="h-4 w-4" />
              Filters
              {activeFiltersCount > 0 && (
                <Badge variant="secondary" className="ml-1 h-4 px-1 text-xs">
                  {activeFiltersCount}
                </Badge>
              )}
            </Button>
          </div>
        </div>

        {/* Filter Bar */}
        {showFilterBar && (
          <div className="mt-3 pt-3 border-t border-border">
            <div className="flex flex-wrap items-end gap-3">
              <div className="flex flex-col gap-1.5 min-w-[140px]">
                <label className="text-xs font-medium text-muted-foreground">Status</label>
                <Select value={filterStatus} onValueChange={setFilterStatus}>
                  <SelectTrigger className="h-9 bg-background">
                    <SelectValue placeholder="All Statuses" />
                  </SelectTrigger>
                  <SelectContent className="bg-popover border shadow-md z-50">
                    <SelectItem value="all">All Statuses</SelectItem>
                    <SelectItem value="unscheduled">Unscheduled</SelectItem>
                    <SelectItem value="ready">Ready</SelectItem>
                    <SelectItem value="dispatched">Dispatched</SelectItem>
                    <SelectItem value="cancelled">Cancelled</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex flex-col gap-1.5 min-w-[140px]">
                <label className="text-xs font-medium text-muted-foreground">Work Type</label>
                <Select value={filterWorkType} onValueChange={setFilterWorkType}>
                  <SelectTrigger className="h-9 bg-background">
                    <SelectValue placeholder="All Work Types" />
                  </SelectTrigger>
                  <SelectContent className="bg-popover border shadow-md z-50">
                    <SelectItem value="all">All Work Types</SelectItem>
                    <SelectItem value="installation">Installation</SelectItem>
                    <SelectItem value="repair">Repair</SelectItem>
                    <SelectItem value="maintenance">Maintenance</SelectItem>
                    <SelectItem value="inspection">Inspection</SelectItem>
                    <SelectItem value="service">Service</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              {(filterStatus !== 'all' || filterWorkType !== 'all') && (
                <Button 
                  variant="ghost" 
                  size="sm" 
                  onClick={() => { 
                    setSearchTerm('');
                    setFilterStatus('all'); 
                    setFilterWorkType('all'); 
                    setShowFilterBar(false); 
                  }}
                  className="h-9 text-muted-foreground hover:text-foreground"
                >
                  <X className="h-4 w-4 mr-1" />
                  Clear Filters
                </Button>
              )}
            </div>
          </div>
        )}

        {/* Jobs Table */}
        {!showJob ? (
          <p className="text-muted-foreground text-center py-8">
            No jobs match the current filters
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b bg-muted/30">
                  <th className="text-left p-4 text-sm font-medium text-muted-foreground">Job</th>
                  <th className="text-left p-4 text-sm font-medium text-muted-foreground">Status</th>
                  <th className="text-left p-4 text-sm font-medium text-muted-foreground">Work Type</th>
                  <th className="text-left p-4 text-sm font-medium text-muted-foreground">Installation</th>
                  
                </tr>
              </thead>
              <tbody>
                <tr className="border-b hover:bg-muted/50 transition-colors">
                  <td className="px-4 py-3">
                    <div className="space-y-1">
                      <div className="font-medium text-sm">{displayTitle}</div>
                      <div className="text-sm text-muted-foreground">
                        <span>{displayJobNumber}</span>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <Badge variant="secondary" className={getStatusColor(displayStatus)}>
                      <div className="flex items-center gap-1.5">
                        {getStatusIcon(displayStatus)}
                        <span className="capitalize font-medium">{displayStatus.replace('_', ' ')}</span>
                      </div>
                    </Badge>
                  </td>
                  <td className="px-4 py-3">
                    <span className="capitalize text-sm">{displayWorkType.replace('_', ' ')}</span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      {displayInstallationName ? (
                        <>
                          <Building2 className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                          <span 
                            className="text-sm cursor-pointer hover:text-primary hover:underline transition-colors font-medium"
                            onClick={() => navigate(`/dashboard/installations/${displayInstallationId}`)}
                          >
                            {displayInstallationName}
                          </span>
                        </>
                      ) : displayInstallationId ? (
                        <>
                          <Building2 className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                          <span 
                            className="text-sm text-muted-foreground cursor-pointer hover:text-primary hover:underline transition-colors"
                            onClick={() => navigate(`/dashboard/installations/${displayInstallationId}`)}
                          >
                            Installation #{displayInstallationId}
                          </span>
                        </>
                      ) : (
                        <span className="text-sm text-muted-foreground">No installation</span>
                      )}
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
