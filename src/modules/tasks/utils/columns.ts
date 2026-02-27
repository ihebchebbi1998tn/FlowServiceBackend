import type { Column } from "../types";

// Default status columns as fallback when API data is not available
export const defaultStatusColumns: Column[] = [
  { id: 'todo', title: 'To Do', color: 'bg-muted-foreground', position: 0, isDefault: true, createdAt: new Date() },
  { id: 'in-progress', title: 'In Progress', color: 'bg-primary', position: 1, isDefault: false, createdAt: new Date() },
  { id: 'review', title: 'Review', color: 'bg-warning', position: 2, isDefault: false, createdAt: new Date() },
  { id: 'done', title: 'Done', color: 'bg-success', position: 3, isDefault: false, createdAt: new Date() },
];

export const buildStatusColumns = (taskStatuses: any[]): Column[] => {
  // Return defaults if no task statuses from API
  if (!taskStatuses || taskStatuses.length === 0) {
    return defaultStatusColumns;
  }
  
  return taskStatuses
    .filter((status: any) => status.isActive !== false) // Only include active statuses
    .sort((a: any, b: any) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)) // Sort by sortOrder
    .map((status: any, index: number) => ({
      id: status.id,
      title: status.name,
      // Use hex color directly or convert to Tailwind class
      color: status.color?.startsWith('#') ? `bg-[${status.color}]` : (status.color || 'bg-slate-500'),
      position: status.sortOrder ?? index,
      isDefault: !!status.isDefault,
      createdAt: status.createdAt ? new Date(status.createdAt) : new Date()
    }));
};

export const defaultTechnicianColumns: Column[] = [
  { id: 'sarah', title: 'Sarah Wilson', color: 'bg-primary', position: 0, isDefault: false, createdAt: new Date() },
  { id: 'mike', title: 'Mike Chen', color: 'bg-accent', position: 1, isDefault: false, createdAt: new Date() },
  { id: 'lisa', title: 'Lisa Johnson', color: 'bg-success', position: 2, isDefault: false, createdAt: new Date() },
  { id: 'david', title: 'David Park', color: 'bg-warning', position: 3, isDefault: false, createdAt: new Date() },
];
