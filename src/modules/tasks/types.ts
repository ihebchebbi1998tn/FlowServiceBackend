// Enhanced Database Tables/Entities for Tasks Module
// Frontend types (compatible with backend APIs)

// Main Task interface (aligned with backend ProjectTask)
export interface Task {
  id: string; // Frontend uses string for compatibility, backend uses int
  title: string;
  description?: string;
  status: string; // Dynamic based on columns
  priority: 'low' | 'medium' | 'high' | 'urgent';
  assigneeId?: string;
  assigneeName?: string;
  assignee?: string; // For compatibility
  projectId?: string;
  projectName?: string;
  contactId?: string;
  contactName?: string;
  parentTaskId?: string; // For sub-tasks
  parentTaskTitle?: string;
  dueDate?: Date;
  startDate?: Date;
  tags: string[];
  attachments: string[];
  estimatedHours?: number;
  actualHours?: number;
  createdAt: Date;
  updatedAt: Date;
  completedAt?: Date;
  columnId: string;
  columnTitle?: string;
  columnColor?: string;
  position: number; // For ordering within columns
  subTasks?: Task[];
  commentsCount?: number;
  attachmentsCount?: number;
  createdBy?: string;
  modifiedBy?: string;
}

// Daily Task interface (aligned with backend DailyTask)
export interface DailyTask {
  id: string;
  title: string;
  description?: string;
  status: string;
  priority: 'low' | 'medium' | 'high' | 'urgent';
  userId: string;
  userName: string;
  position: number;
  dueDate?: Date;
  tags: string[];
  attachments: string[];
  estimatedHours?: number;
  actualHours?: number;
  createdAt: Date;
  updatedAt: Date;
  completedAt?: Date;
  commentsCount?: number;
  attachmentsCount?: number;
  createdBy?: string;
  modifiedBy?: string;
  // Quick completion (separate from status)
  isCompleted?: boolean;
  completedDate?: Date;
}

// Task Comment interface (aligned with backend TaskComment)
export interface TaskComment {
  id: string;
  projectTaskId?: string;
  dailyTaskId?: string;
  taskTitle: string;
  content: string;
  authorId: string;
  authorName: string;
  createdAt: Date;
  updatedAt: Date;
  isEdited: boolean;
}

// Task Attachment interface (aligned with backend TaskAttachment)
export interface TaskAttachment {
  id: string;
  projectTaskId?: string;
  dailyTaskId?: string;
  taskTitle: string;
  fileName: string;
  originalFileName: string;
  fileUrl: string;
  mimeType?: string;
  fileSize: number;
  uploadedBy: string;
  uploadedByName: string;
  uploadedAt: Date;
  caption?: string;
}

// Project interface (aligned with backend Project)
export interface Project {
  id: string;
  name: string;
  description?: string;
  contactId?: string;
  contactName?: string;
  ownerId: string;
  ownerName: string;
  teamMembers: string[]; // Array of user IDs as JSON
  budget?: number;
  currency?: string;
  status: 'active' | 'completed' | 'on-hold' | 'cancelled';
  type: 'service' | 'sales' | 'internal' | 'custom';
  priority?: 'low' | 'medium' | 'high' | 'urgent';
  progress?: number; // 0-100
  startDate?: Date;
  endDate?: Date;
  actualStartDate?: Date;
  actualEndDate?: Date;
  tags?: string[];
  isArchived?: boolean;
  createdAt: Date;
  updatedAt: Date;
  completedAt?: Date;
  createdBy?: string;
  modifiedBy?: string;
  columns: Column[]; // Custom columns for this project
  stats?: ProjectStats;
  parentProjectId?: string; // For sub-projects compatibility
}

// Project Column interface (aligned with backend ProjectColumn)
export interface Column {
  id: string;
  title: string;
  color: string;
  position: number;
  projectId?: string; // null for global columns
  isDefault: boolean;
  taskLimit?: number;
  createdAt: Date;
  taskCount?: number;
}

export interface Technician {
  id: string;
  name: string;
  email: string;
  role: string;
  isActive: boolean;
  avatar?: string;
}

export interface ProjectStats {
  totalTasks: number;
  completedTasks: number;
  overdueTasks: number;
  activeMembers: number;
  completionPercentage: number;
}
