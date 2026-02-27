import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { 
  MoreHorizontal, 
  CheckSquare,
  Play,
  Pause,
  Edit3,
  Trash2,
  Eye
} from "lucide-react";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Project, ProjectStats, Technician } from "../types";
import { format } from "date-fns";

interface ProjectsTableProps {
  projects: Project[];
  projectStats: Record<string, ProjectStats>;
  technicians: Technician[];
  onOpenProject: (project: Project) => void;
  onEditProject: (project: Project) => void;
  onDeleteProject: (projectId: string) => void;
  onToggleStatus: (projectId: string, status: Project['status']) => void;
  enablePagination?: boolean;
  itemsPerPage?: number;
  currentPage?: number;
  onPageChange?: (page: number) => void;
  totalItems?: number;
}

const getStatusColor = (status: Project['status']) => {
  switch (status) {
    case 'active': return 'bg-success text-success-foreground';
    case 'completed': return 'bg-primary text-primary-foreground';
    case 'on-hold': return 'bg-warning text-warning-foreground';
    case 'cancelled': return 'bg-destructive text-destructive-foreground';
    default: return 'bg-secondary text-secondary-foreground';
  }
};

const getTypeColor = (type: Project['type']) => {
  switch (type) {
    case 'service': return 'bg-primary/10 text-primary';
    case 'sales': return 'bg-success/10 text-success';
    case 'internal': return 'bg-secondary text-secondary-foreground';
    case 'custom': return 'bg-warning/10 text-warning';
    default: return 'bg-secondary text-secondary-foreground';
  }
};

export function ProjectsTable({
  projects,
  projectStats,
  technicians,
  onOpenProject: _onOpenProject,
  onEditProject,
  onDeleteProject,
  onToggleStatus,
  enablePagination = false,
  itemsPerPage = 5,
  currentPage = 1,
  onPageChange,
  totalItems
}: ProjectsTableProps) {
  const navigate = useNavigate();

  const handleProjectClick = (project: Project) => {
    // Navigate to the new project detail route
    navigate(`/dashboard/tasks/projects/${project.id}`);
  };

  const totalPages = enablePagination && totalItems ? Math.ceil(totalItems / itemsPerPage) : 1;
  const hasNextPage = enablePagination ? currentPage < totalPages : false;
  const hasPreviousPage = enablePagination ? currentPage > 1 : false;

  const handlePageChange = (page: number) => {
    if (onPageChange) {
      onPageChange(page);
    }
  };

  const handleNextPage = () => {
    if (hasNextPage && onPageChange) {
      onPageChange(currentPage + 1);
    }
  };

  const handlePreviousPage = () => {
    if (hasPreviousPage && onPageChange) {
      onPageChange(currentPage - 1);
    }
  };

  return (
    <div className="w-full">
      <div className="overflow-x-auto">
        <Table className="w-full min-w-full">
          <TableHeader>
            <TableRow>
              <TableHead className="w-[250px]">Project</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Progress</TableHead>
                  <TableHead>Team</TableHead>
                  <TableHead>Start Date</TableHead>
                  <TableHead className="w-[50px]"></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {projects.map(project => {
                  const stats = projectStats[project.id] || { totalTasks: 0, completedTasks: 0, completionPercentage: 0 };
                  return (
                    <TableRow key={project.id} className="cursor-pointer hover:bg-muted/50 group" onClick={() => handleProjectClick(project)}>
                      <TableCell className="p-4">
                        <div className="flex items-center gap-3">
                          <Avatar className="h-8 w-8">
                            <AvatarFallback className="text-xs bg-primary/10 text-primary">PR</AvatarFallback>
                          </Avatar>
                          <div className="min-w-0">
                            <div className="text-foreground truncate">{project.name}</div>
                            {project.description && <div className="text-sm text-muted-foreground line-clamp-1">{project.description}</div>}
                          </div>
                        </div>
                      </TableCell>
                      <TableCell className="p-4">
                        <Badge className={getTypeColor(project.type)} variant="outline">{project.type}</Badge>
                      </TableCell>
                      <TableCell className="p-4">
                        <Badge className={getStatusColor(project.status)} variant="secondary">{project.status}</Badge>
                      </TableCell>
                      <TableCell className="p-4">
                        <div className="space-y-1">
                          <div className="text-sm text-muted-foreground">{stats.completedTasks}/{stats.totalTasks} tasks</div>
                          {stats.completionPercentage > 0 && (
                            <div className="flex items-center gap-2">
                              <div className="w-16 h-2 bg-secondary rounded-full overflow-hidden">
                                <div className="h-full bg-primary transition-all" style={{ width: `${stats.completionPercentage}%` }} />
                              </div>
                              <span className="text-xs text-muted-foreground">{stats.completionPercentage}%</span>
                            </div>
                          )}
                        </div>
                      </TableCell>
                      <TableCell className="p-4">
                        <div className="flex items-center gap-1">
                          {project.teamMembers.slice(0,3).map((memberId: any) => {
                            const member = technicians.find(t => t.id === memberId);
                            return member ? (
                              <Avatar key={member.id} className="h-6 w-6"><AvatarFallback className="text-xs">{member.name.split(' ').map((n:any)=>n[0]).join('')}</AvatarFallback></Avatar>
                            ) : null;
                          })}
                          {project.teamMembers.length > 3 && <div className="h-6 w-6 rounded-full bg-secondary flex items-center justify-center text-xs text-muted-foreground">+{project.teamMembers.length - 3}</div>}
                          <span className="text-sm text-muted-foreground ml-1">{project.teamMembers.length} members</span>
                        </div>
                      </TableCell>
                      <TableCell className="p-4">
                        <div className="text-sm text-muted-foreground">{project.startDate ? format(project.startDate, 'MMM dd, yyyy') : '-'}</div>
                      </TableCell>
                      <TableCell className="p-4 text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="sm" className="h-8 w-8 p-0" onClick={(e:any) => e.stopPropagation()}>
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={(e:any)=>{e.stopPropagation(); handleProjectClick(project);}}><Eye className="h-4 w-4 mr-2"/>Open</DropdownMenuItem>
                            <DropdownMenuItem onClick={(e:any)=>{e.stopPropagation(); onEditProject(project);}}><Edit3 className="h-4 w-4 mr-2"/>Edit Project</DropdownMenuItem>
                            <DropdownMenuItem onClick={(e:any)=>{e.stopPropagation(); onToggleStatus(project.id, project.status === 'active' ? 'on-hold' : 'active');}}>
                              {project.status === 'active' ? (<><Pause className="h-4 w-4 mr-2"/>Pause Project</>) : (<><Play className="h-4 w-4 mr-2"/>Resume Project</>)}
                            </DropdownMenuItem>
                            <DropdownMenuItem onClick={()=>onToggleStatus(project.id,'completed')} disabled={project.status === 'completed'}><CheckSquare className="h-4 w-4 mr-2"/>Mark Complete</DropdownMenuItem>
                            <DropdownMenuItem onClick={()=>onDeleteProject(project.id)} className="text-destructive"><Trash2 className="h-4 w-4 mr-2"/>Delete Project</DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
      </Table>
      </div>
      
      {enablePagination && totalItems && totalItems > itemsPerPage && (
        <div className="border-t border-border p-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <p className="text-sm text-muted-foreground">
                Showing {(currentPage - 1) * itemsPerPage + 1} to {Math.min(currentPage * itemsPerPage, totalItems)} of {totalItems} results
              </p>
            </div>
            <div className="flex items-center gap-1">
              <button
                onClick={handlePreviousPage}
                disabled={!hasPreviousPage}
                className="px-3 py-1 text-sm border rounded disabled:opacity-50 disabled:cursor-not-allowed hover:bg-muted"
              >
                Previous
              </button>
              <span className="px-3 py-1 text-sm">
                {currentPage} of {totalPages}
              </span>
              <button
                onClick={handleNextPage}
                disabled={!hasNextPage}
                className="px-3 py-1 text-sm border rounded disabled:opacity-50 disabled:cursor-not-allowed hover:bg-muted"
              >
                Next
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}