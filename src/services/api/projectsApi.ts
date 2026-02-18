// Projects API Service - Backend Integration
import type { Project, Column, ProjectStats } from '@/modules/tasks/types';
import { getAuthHeaders } from '@/utils/apiHeaders';

import { API_URL } from '@/config/api';

// Backend response DTOs
export interface ProjectResponseDto {
  id: number;
  name: string;
  description?: string;
  contactId?: number;
  contactName?: string;
  ownerId: number;
  ownerName: string;
  teamMembers: number[];
  teamMemberNames: string[];
  budget?: number;
  currency?: string;
  status: string;
  type: string;
  priority: string;
  progress: number;
  startDate?: string;
  endDate?: string;
  actualStartDate?: string;
  actualEndDate?: string;
  tags: string[];
  isArchived: boolean;
  createdAt: string;
  updatedAt: string;
  createdBy?: string;
  modifiedBy?: string;
  columns: ColumnResponseDto[];
  stats: ProjectStatsDto;
}

export interface ColumnResponseDto {
  id: number;
  title?: string;
  name?: string; // Backend uses 'name' instead of 'title'
  color: string;
  position?: number;
  displayOrder?: number; // Backend uses 'displayOrder' instead of 'position'
  projectId?: number;
  isDefault?: boolean;
  limit?: number;
  createdAt?: string;
  taskCount?: number;
}

export interface ProjectStatsDto {
  totalTasks: number;
  completedTasks: number;
  overdueTasks: number;
  activeMembers: number;
  completionPercentage: number;
}

export interface ProjectListResponseDto {
  projects: ProjectResponseDto[];
  totalCount: number;
  pageSize: number;
  pageNumber: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Request DTOs
export interface CreateProjectRequestDto {
  name: string;
  description?: string;
  contactId?: number;
  ownerId: number;
  ownerName: string;
  teamMembers?: number[];
  budget?: number;
  currency?: string;
  status?: string;
  type?: string;
  priority?: string;
  startDate?: string;
  endDate?: string;
  tags?: string[];
}

export interface UpdateProjectRequestDto {
  name?: string;
  description?: string;
  contactId?: number;
  ownerId?: number;
  ownerName?: string;
  teamMembers?: number[];
  budget?: number;
  currency?: string;
  status?: string;
  type?: string;
  priority?: string;
  progress?: number;
  startDate?: string;
  endDate?: string;
  actualStartDate?: string;
  actualEndDate?: string;
  tags?: string[];
  isArchived?: boolean;
}

export interface ProjectSearchRequestDto {
  searchTerm?: string;
  status?: string;
  type?: string;
  priority?: string;
  ownerId?: number;
  contactId?: number;
  teamMemberIds?: number[];
  tags?: string[];
  isArchived?: boolean;
  startDateFrom?: string;
  startDateTo?: string;
  endDateFrom?: string;
  endDateTo?: string;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: string;
}

export interface AssignTeamMemberDto {
  userId: number;
  userName: string;
}

export interface RemoveTeamMemberDto {
  userId: number;
}

export interface BulkUpdateProjectStatusDto {
  projectIds: number[];
  status: string;
}

// Mappers
const mapProjectResponseToFrontend = (dto: ProjectResponseDto): Project => ({
  id: String(dto.id),
  name: dto.name,
  description: dto.description,
  contactId: dto.contactId ? String(dto.contactId) : undefined,
  contactName: dto.contactName,
  ownerId: dto.ownerId ? String(dto.ownerId) : '1',
  ownerName: dto.ownerName || 'Unknown',
  teamMembers: (dto.teamMembers || []).map(String),
  budget: dto.budget,
  currency: dto.currency,
  status: (dto.status || 'active') as Project['status'],
  type: (dto.type || 'development') as Project['type'],
  priority: (dto.priority || 'medium') as Project['priority'],
  progress: dto.progress || 0,
  startDate: dto.startDate ? new Date(dto.startDate) : undefined,
  endDate: dto.endDate ? new Date(dto.endDate) : undefined,
  actualStartDate: dto.actualStartDate ? new Date(dto.actualStartDate) : undefined,
  actualEndDate: dto.actualEndDate ? new Date(dto.actualEndDate) : undefined,
  tags: dto.tags || [],
  isArchived: dto.isArchived || false,
  createdAt: dto.createdAt ? new Date(dto.createdAt) : new Date(),
  updatedAt: dto.updatedAt ? new Date(dto.updatedAt) : new Date(),
  createdBy: dto.createdBy,
  modifiedBy: dto.modifiedBy,
  columns: (dto.columns || []).map(mapColumnResponseToFrontend),
  stats: dto.stats ? {
    totalTasks: dto.stats.totalTasks,
    completedTasks: dto.stats.completedTasks,
    overdueTasks: dto.stats.overdueTasks,
    activeMembers: dto.stats.activeMembers,
    completionPercentage: dto.stats.completionPercentage,
  } : undefined,
});

const mapColumnResponseToFrontend = (dto: ColumnResponseDto): Column => ({
  id: String(dto.id),
  title: dto.title || dto.name || 'Untitled', // Backend uses 'name' instead of 'title'
  color: dto.color || '#64748b',
  position: dto.position ?? dto.displayOrder ?? 0, // Backend uses 'displayOrder' instead of 'position'
  projectId: dto.projectId ? String(dto.projectId) : undefined,
  isDefault: dto.isDefault ?? false,
  taskLimit: dto.limit,
  createdAt: dto.createdAt ? new Date(dto.createdAt) : new Date(),
  taskCount: dto.taskCount,
});

export const projectsApi = {
  // Get all projects with optional filtering and pagination
  async getAll(params?: ProjectSearchRequestDto): Promise<{ projects: Project[]; totalCount: number; pageSize: number; pageNumber: number }> {
    const queryParams = new URLSearchParams();
    
    if (params?.searchTerm) queryParams.append('searchTerm', params.searchTerm);
    if (params?.status) queryParams.append('status', params.status);
    if (params?.type) queryParams.append('type', params.type);
    if (params?.priority) queryParams.append('priority', params.priority);
    if (params?.ownerId) queryParams.append('ownerId', String(params.ownerId));
    if (params?.contactId) queryParams.append('contactId', String(params.contactId));
    if (params?.isArchived !== undefined) queryParams.append('isArchived', String(params.isArchived));
    if (params?.pageNumber) queryParams.append('pageNumber', String(params.pageNumber));
    if (params?.pageSize) queryParams.append('pageSize', String(params.pageSize));
    if (params?.sortBy) queryParams.append('sortBy', params.sortBy);
    if (params?.sortDirection) queryParams.append('sortDirection', params.sortDirection);
    if (params?.startDateFrom) queryParams.append('startDateFrom', params.startDateFrom);
    if (params?.startDateTo) queryParams.append('startDateTo', params.startDateTo);
    if (params?.endDateFrom) queryParams.append('endDateFrom', params.endDateFrom);
    if (params?.endDateTo) queryParams.append('endDateTo', params.endDateTo);
    
    if (params?.teamMemberIds) {
      params.teamMemberIds.forEach(id => queryParams.append('teamMemberIds', String(id)));
    }
    if (params?.tags) {
      params.tags.forEach(tag => queryParams.append('tags', tag));
    }

    const url = queryParams.toString() 
      ? `${API_URL}/api/Projects?${queryParams}` 
      : `${API_URL}/api/Projects`;

    const response = await fetch(url, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to fetch projects' }));
      throw new Error(error.message || 'Failed to fetch projects');
    }

    const data: ProjectListResponseDto = await response.json();
    return {
      projects: data.projects.map(mapProjectResponseToFrontend),
      totalCount: data.totalCount,
      pageSize: data.pageSize,
      pageNumber: data.pageNumber,
    };
  },

  // Get project by ID
  async getById(id: number): Promise<Project> {
    const response = await fetch(`${API_URL}/api/Projects/${id}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch project ${id}`);
    }

    const data: ProjectResponseDto = await response.json();
    return mapProjectResponseToFrontend(data);
  },

  // Create new project
  async create(request: CreateProjectRequestDto): Promise<Project> {
    const response = await fetch(`${API_URL}/api/Projects`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to create project' }));
      throw new Error(error.message || 'Failed to create project');
    }

    const data: ProjectResponseDto = await response.json();
    return mapProjectResponseToFrontend(data);
  },

  // Update existing project
  async update(id: number, request: UpdateProjectRequestDto): Promise<Project> {
    const response = await fetch(`${API_URL}/api/Projects/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to update project' }));
      throw new Error(error.message || 'Failed to update project');
    }

    const data: ProjectResponseDto = await response.json();
    return mapProjectResponseToFrontend(data);
  },

  // Delete project (soft delete)
  async delete(id: number): Promise<void> {
    const response = await fetch(`${API_URL}/api/Projects/${id}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to delete project');
    }
  },

  // Search projects
  async search(searchTerm: string, pageNumber = 1, pageSize = 20): Promise<{ projects: Project[]; totalCount: number }> {
    const queryParams = new URLSearchParams({
      searchTerm,
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    });

    const response = await fetch(`${API_URL}/api/Projects/search?${queryParams}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to search projects');
    }

    const data: ProjectListResponseDto = await response.json();
    return {
      projects: data.projects.map(mapProjectResponseToFrontend),
      totalCount: data.totalCount,
    };
  },

  // Get projects by owner
  async getByOwner(ownerId: number, pageNumber = 1, pageSize = 20): Promise<{ projects: Project[]; totalCount: number }> {
    const queryParams = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    });

    const response = await fetch(`${API_URL}/api/Projects/owner/${ownerId}?${queryParams}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch projects by owner');
    }

    const data: ProjectListResponseDto = await response.json();
    return {
      projects: data.projects.map(mapProjectResponseToFrontend),
      totalCount: data.totalCount,
    };
  },

  // Get projects by contact
  async getByContact(contactId: number, pageNumber = 1, pageSize = 20): Promise<{ projects: Project[]; totalCount: number }> {
    const queryParams = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    });

    const response = await fetch(`${API_URL}/api/Projects/contact/${contactId}?${queryParams}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch projects by contact');
    }

    const data: ProjectListResponseDto = await response.json();
    return {
      projects: data.projects.map(mapProjectResponseToFrontend),
      totalCount: data.totalCount,
    };
  },

  // Get projects by team member
  async getByTeamMember(userId: number, pageNumber = 1, pageSize = 20): Promise<{ projects: Project[]; totalCount: number }> {
    const queryParams = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    });

    const response = await fetch(`${API_URL}/api/Projects/team-member/${userId}?${queryParams}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch projects by team member');
    }

    const data: ProjectListResponseDto = await response.json();
    return {
      projects: data.projects.map(mapProjectResponseToFrontend),
      totalCount: data.totalCount,
    };
  },

  // Assign team member to project
  async assignTeamMember(projectId: number, dto: AssignTeamMemberDto): Promise<void> {
    const response = await fetch(`${API_URL}/api/Projects/${projectId}/team-members`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(dto),
    });

    if (!response.ok) {
      throw new Error('Failed to assign team member');
    }
  },

  // Remove team member from project
  async removeTeamMember(projectId: number, dto: RemoveTeamMemberDto): Promise<void> {
    const response = await fetch(`${API_URL}/api/Projects/${projectId}/team-members`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
      body: JSON.stringify(dto),
    });

    if (!response.ok) {
      throw new Error('Failed to remove team member');
    }
  },

  // Get project team members
  async getTeamMembers(projectId: number): Promise<number[]> {
    const response = await fetch(`${API_URL}/api/Projects/${projectId}/team-members`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch team members');
    }

    return await response.json();
  },

  // Get project statistics
  async getStats(projectId: number): Promise<ProjectStats> {
    const response = await fetch(`${API_URL}/api/Projects/${projectId}/stats`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch project stats');
    }

    return await response.json();
  },

  // Bulk update project status
  async bulkUpdateStatus(dto: BulkUpdateProjectStatusDto): Promise<void> {
    const response = await fetch(`${API_URL}/api/Projects/bulk/status`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(dto),
    });

    if (!response.ok) {
      throw new Error('Failed to bulk update project status');
    }
  },

  // Bulk archive/unarchive projects
  async bulkArchive(projectIds: number[], archive = true): Promise<void> {
    const queryParams = new URLSearchParams({ archive: String(archive) });

    const response = await fetch(`${API_URL}/api/Projects/bulk/archive?${queryParams}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(projectIds),
    });

    if (!response.ok) {
      throw new Error(`Failed to ${archive ? 'archive' : 'unarchive'} projects`);
    }
  },
};
