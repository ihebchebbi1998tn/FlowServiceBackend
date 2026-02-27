// Dispatch CRUD operations service
// Handles assign, unassign, reschedule, lock, resize
import { dispatchesApi, type Dispatch, type CreateDispatchFromJobRequest } from '@/services/api/dispatchesApi';
import { serviceOrdersApi } from '@/services/api/serviceOrdersApi';
import { notificationsApi } from '@/services/api/notificationsApi';
import { cacheService } from './cache.service';
import type { Job, Technician, ServiceOrder, InstallationGroup } from '../types';

// ── Helpers ──────────────────────────────────────────

function getCurrentUserName(): string {
  try {
    const userData = localStorage.getItem('user_data') || sessionStorage.getItem('user_data');
    if (userData) {
      const user = JSON.parse(userData);
      const firstName = user.firstName || user.first_name || '';
      const lastName = user.lastName || user.last_name || '';
      if (firstName || lastName) return `${firstName} ${lastName}`.trim();
      if (user.email) return user.email;
    }
  } catch { /* ignore */ }
  return 'System';
}

// Track jobs currently being assigned (prevent duplicates)
const jobsBeingAssigned = new Set<string>();
// Track installations currently being assigned (prevent duplicates)
const installationsBeingAssigned = new Set<string>();

interface ScheduleOverride {
  startIso: string;
  endIso: string;
  updatedAt: number;
}

const SCHEDULE_OVERRIDES_KEY = 'dispatcher_schedule_overrides_v1';

function loadScheduleOverrides(): Map<string, ScheduleOverride> {
  try {
    const raw = localStorage.getItem(SCHEDULE_OVERRIDES_KEY);
    if (!raw) return new Map();
    const parsed = JSON.parse(raw) as Record<string, ScheduleOverride>;
    return new Map(Object.entries(parsed));
  } catch {
    return new Map();
  }
}

const scheduleOverrides = loadScheduleOverrides();

function persistScheduleOverrides(): void {
  try {
    localStorage.setItem(SCHEDULE_OVERRIDES_KEY, JSON.stringify(Object.fromEntries(scheduleOverrides.entries())));
  } catch {
    // ignore storage errors
  }
}

function setScheduleOverride(dispatchId: string, start: Date, end: Date): void {
  scheduleOverrides.set(dispatchId, {
    startIso: start.toISOString(),
    endIso: end.toISOString(),
    updatedAt: Date.now(),
  });
  persistScheduleOverrides();
}

function getScheduleOverride(dispatchId: string): { start: Date; end: Date } | null {
  const override = scheduleOverrides.get(dispatchId);
  if (!override) return null;

  const start = new Date(override.startIso);
  const end = new Date(override.endIso);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return null;

  return { start, end };
}

// ── Undo stack ───────────────────────────────────────

export interface UndoAction {
  type: 'assign' | 'unassign' | 'reschedule';
  description: string;
  timestamp: number;
  data: {
    dispatchId?: string;
    jobId?: string;
    technicianId?: string;
    scheduledStart?: Date;
    scheduledEnd?: Date;
    originalStart?: Date;
    originalEnd?: Date;
    serviceOrderId?: number;
    dispatchNumber?: string;
  };
}

const undoStack: UndoAction[] = [];
const MAX_UNDO = 5;

function pushUndo(action: UndoAction) {
  undoStack.push(action);
  if (undoStack.length > MAX_UNDO) undoStack.shift();
}

// ── Technician extraction from dispatch ──────────────

export function extractTechnicianIdFromDispatch(dispatch: Dispatch): string | null {
  // Strategy 1: assignedTechnicians array (primary - backend returns UserLightDto objects)
  const assignedTechs = dispatch.assignedTechnicians || [];
  if (assignedTechs.length > 0) {
    const firstTech = assignedTechs[0];
    if (typeof firstTech === 'object' && firstTech.id) return String(firstTech.id);
    if (typeof firstTech === 'string' || typeof firstTech === 'number') return String(firstTech);
  }

  // Strategy 2: assignedTechnicianIds array
  const techIds = dispatch.assignedTechnicianIds || [];
  if (techIds.length > 0) return String(techIds[0]);

  // Strategy 3: dispatchedBy field
  if (dispatch.dispatchedBy) {
    const numericId = String(dispatch.dispatchedBy).match(/\d+/)?.[0];
    if (numericId) return numericId;
  }

  return null;
}

export function technicianMatchesDispatch(dispatch: Dispatch, technicianId: string): boolean {
  const dispatchTechId = extractTechnicianIdFromDispatch(dispatch);
  if (!dispatchTechId) return false;

  const dispatchNumeric = dispatchTechId.match(/\d+/)?.[0] || dispatchTechId;
  const techNumeric = technicianId.match(/\d+/)?.[0] || technicianId;

  return dispatchTechId === technicianId || dispatchNumeric === techNumeric;
}

// ── Main service ─────────────────────────────────────

export class DispatchOperationsService {
  // ── Fetch dispatches ───────────────────────────────

  static async fetchDispatches(): Promise<Dispatch[]> {
    try {
      const response = await dispatchesApi.getAll({ pageSize: 500 });
      cacheService.dispatches = response.data;
      return response.data;
    } catch (error) {
      console.error('Failed to fetch dispatches:', error);
      return cacheService.dispatches;
    }
  }

  static async fetchDispatchesCached(): Promise<Dispatch[]> {
    if (cacheService.hasFreshDispatches()) return cacheService.dispatches;
    if (cacheService.pendingDispatches) return cacheService.pendingDispatches;

    cacheService.pendingDispatches = (async () => {
      try {
        const response = await dispatchesApi.getAll({ pageSize: 500 });
        cacheService.dispatches = response.data;
        return response.data;
      } catch (error) {
        console.error('Failed to fetch dispatches:', error);
        return cacheService.dispatches;
      } finally {
        cacheService.pendingDispatches = null;
      }
    })();

    return cacheService.pendingDispatches;
  }

  // ── Create dispatch from job ───────────────────────

  static async createDispatchFromJob(
    jobId: number,
    technicianId: string,
    scheduledDate: Date,
    priority = 'medium',
    notes?: string,
    siteAddress?: string,
    scheduledEndDate?: Date
  ): Promise<Dispatch> {
    const backendTechnicianId = technicianId.match(/\d+/)?.[0] || technicianId;

    // Format start/end times as TimeSpan "HH:mm:ss"
    const pad = (n: number) => String(n).padStart(2, '0');
    const startTime = `${pad(scheduledDate.getHours())}:${pad(scheduledDate.getMinutes())}:00`;
    const endDate = scheduledEndDate || new Date(scheduledDate.getTime() + 60 * 60 * 1000); // default 1h
    const endTime = `${pad(endDate.getHours())}:${pad(endDate.getMinutes())}:00`;

    const request: CreateDispatchFromJobRequest = {
      assignedTechnicianIds: [backendTechnicianId],
      scheduledDate: scheduledDate.toISOString(),
      scheduledStartTime: startTime,
      scheduledEndTime: endTime,
      priority,
      notes: notes || 'Dispatch created from planning interface',
      siteAddress,
    };

    return await dispatchesApi.createFromJob(jobId, request);
  }

  // ── Assign job (create dispatch) ───────────────────

  static async assignJob(
    jobId: string,
    technicianId: string,
    scheduledStart: Date,
    scheduledEnd: Date,
    technicianName?: string,
    priority: 'low' | 'medium' | 'high' | 'urgent' = 'medium'
  ): Promise<Dispatch | null> {
    if (jobsBeingAssigned.has(jobId)) {
      throw new Error('This job is already being processed. Please wait.');
    }

    try {
      const jobIdNum = parseInt(jobId, 10);
      if (isNaN(jobIdNum)) return null;

      jobsBeingAssigned.add(jobId);

      const dispatch = await DispatchOperationsService.createDispatchFromJob(
        jobIdNum, technicianId, scheduledStart, priority,
        `Scheduled: ${scheduledStart.toLocaleTimeString()} - ${scheduledEnd.toLocaleTimeString()}`,
        undefined, scheduledEnd
      );

      // Push undo action
      pushUndo({
        type: 'assign',
        description: `Assigned job to ${technicianName || 'technician'}`,
        timestamp: Date.now(),
        data: {
          dispatchId: String(dispatch.id),
          jobId,
          technicianId,
          scheduledStart,
          scheduledEnd,
          serviceOrderId: dispatch.serviceOrderId,
          dispatchNumber: dispatch.dispatchNumber,
        }
      });

      // Add audit note to service order
      if (dispatch.serviceOrderId) {
        try {
          const dispatcherName = getCurrentUserName();
          const assignedTechName = technicianName || 'Unknown Technician';
          await serviceOrdersApi.addNote(dispatch.serviceOrderId, {
            content: `📋 Job dispatched - Dispatch #${dispatch.dispatchNumber || dispatch.id}\n` +
              `• Dispatched by: ${dispatcherName}\n` +
              `• Assigned to: ${assignedTechName}\n` +
              `• Priority: ${priority.charAt(0).toUpperCase() + priority.slice(1)}\n` +
              `• Scheduled: ${scheduledStart.toLocaleDateString()} at ${scheduledStart.toLocaleTimeString()}`,
            type: 'dispatch_created'
          });
        } catch { /* ignore */ }
      }

      // Create notification for assigned technician
      try {
        const technicianIdNum = parseInt(technicianId, 10);
        if (!isNaN(technicianIdNum)) {
          await notificationsApi.create({
            userId: technicianIdNum,
            title: 'New Job Assigned',
            description: `You have been assigned a new job (Dispatch #${dispatch.dispatchNumber || dispatch.id}) scheduled for ${scheduledStart.toLocaleDateString()} at ${scheduledStart.toLocaleTimeString()}.`,
            type: 'info',
            category: 'service_order',
            link: `/dashboard/field/dispatches/${dispatch.id}`,
            relatedEntityId: dispatch.id,
            relatedEntityType: 'dispatch'
          });
        }
      } catch { /* ignore */ }

      cacheService.invalidateDispatchData();
      return dispatch;
    } catch (error) {
      console.error('Failed to assign job:', error);
      throw error;
    } finally {
      jobsBeingAssigned.delete(jobId);
    }
  }

  // ── Batch assign service order jobs ────────────────

  static async assignServiceOrderJobs(
    serviceOrderId: string,
    jobs: Array<{ id: string; estimatedDuration: number; priority?: 'low' | 'medium' | 'high' | 'urgent' }>,
    technicianId: string,
    startTime: Date,
    technicianName?: string
  ): Promise<{ success: Dispatch[]; failed: string[] }> {
    const success: Dispatch[] = [];
    const failed: string[] = [];
    let currentStartTime = new Date(startTime);

    for (const job of jobs) {
      const duration = job.estimatedDuration || 60;
      const jobEndTime = new Date(currentStartTime.getTime() + duration * 60 * 1000);
      try {
        const dispatch = await DispatchOperationsService.assignJob(
          job.id, technicianId, currentStartTime, jobEndTime, technicianName, job.priority || 'medium'
        );
        if (dispatch) success.push(dispatch);
        else failed.push(job.id);
      } catch {
        failed.push(job.id);
      }
      currentStartTime = jobEndTime;
    }

    // Add batch summary note
    if (success.length > 0) {
      try {
        const dispatcherName = getCurrentUserName();
        await serviceOrdersApi.addNote(parseInt(serviceOrderId, 10), {
          content: `📦 Batch dispatch - ${success.length} jobs assigned\n` +
            `• Dispatched by: ${dispatcherName}\n` +
            `• Assigned to: ${technicianName || 'Unknown Technician'}\n` +
            `• Start: ${startTime.toLocaleDateString()} at ${startTime.toLocaleTimeString()}\n` +
            `• Jobs: ${success.map(d => `#${d.dispatchNumber || d.id}`).join(', ')}` +
            (failed.length > 0 ? `\n• Failed: ${failed.length} jobs` : ''),
          type: 'batch_dispatch_created'
        });
      } catch { /* ignore */ }
    }

    return { success, failed };
  }

  // ── Assign installation group (single dispatch for all jobs) ──

  static async assignInstallationGroup(
    installationGroup: InstallationGroup,
    technicianId: string,
    scheduledStart: Date,
    scheduledEnd: Date,
    technicianName?: string,
    priority: 'low' | 'medium' | 'high' | 'urgent' = 'medium'
  ): Promise<Dispatch | null> {
    const groupKey = `inst-${installationGroup.installationId}`;
    if (installationsBeingAssigned.has(groupKey)) {
      throw new Error('This installation is already being processed. Please wait.');
    }

    try {
      installationsBeingAssigned.add(groupKey);
      const backendTechId = technicianId.match(/\d+/)?.[0] || technicianId;
      const jobIds = installationGroup.jobs.map(j => parseInt(j.id, 10)).filter(id => !isNaN(id));

      if (jobIds.length === 0) throw new Error('No valid job IDs found');

      const firstJob = installationGroup.jobs[0];
      const dispatch = await dispatchesApi.createFromInstallation({
        installationId: parseInt(installationGroup.installationId, 10),
        installationName: installationGroup.installationName,
        jobIds,
        assignedTechnicianIds: [backendTechId],
        scheduledDate: scheduledStart.toISOString(),
        priority,
        notes: `Installation: ${installationGroup.installationName}\n${jobIds.length} jobs\nScheduled: ${scheduledStart.toLocaleTimeString()} - ${scheduledEnd.toLocaleTimeString()}`,
        siteAddress: firstJob?.location?.address,
        contactId: firstJob?.contactId,
        serviceOrderId: firstJob?.serviceOrderId ? parseInt(firstJob.serviceOrderId, 10) : undefined,
      });

      // Push undo action
      pushUndo({
        type: 'assign',
        description: `Assigned installation "${installationGroup.installationName}" to ${technicianName || 'technician'}`,
        timestamp: Date.now(),
        data: {
          dispatchId: String(dispatch.id),
          technicianId,
          scheduledStart,
          scheduledEnd,
          serviceOrderId: dispatch.serviceOrderId,
          dispatchNumber: dispatch.dispatchNumber,
        }
      });

      // Audit note on service order
      if (dispatch.serviceOrderId) {
        try {
          const dispatcherName = getCurrentUserName();
          await serviceOrdersApi.addNote(dispatch.serviceOrderId, {
            content: `🏗️ Installation dispatched - ${installationGroup.installationName}\n` +
              `• Dispatch #${dispatch.dispatchNumber || dispatch.id}\n` +
              `• ${jobIds.length} jobs included\n` +
              `• Dispatched by: ${dispatcherName}\n` +
              `• Assigned to: ${technicianName || 'Unknown'}\n` +
              `• Priority: ${priority}\n` +
              `• Scheduled: ${scheduledStart.toLocaleDateString()} at ${scheduledStart.toLocaleTimeString()}`,
            type: 'dispatch_created'
          });
        } catch { /* ignore */ }
      }

      // Notification to technician
      try {
        const techIdNum = parseInt(technicianId, 10);
        if (!isNaN(techIdNum)) {
          await notificationsApi.create({
            userId: techIdNum,
            title: 'New Installation Assigned',
            description: `Installation "${installationGroup.installationName}" (${jobIds.length} jobs) assigned. Dispatch #${dispatch.dispatchNumber || dispatch.id}, scheduled ${scheduledStart.toLocaleDateString()} at ${scheduledStart.toLocaleTimeString()}.`,
            type: 'info',
            category: 'service_order',
            link: `/dashboard/field/dispatches/${dispatch.id}`,
            relatedEntityId: dispatch.id,
            relatedEntityType: 'dispatch'
          });
        }
      } catch { /* ignore */ }

      cacheService.invalidateDispatchData();
      return dispatch;
    } catch (error) {
      console.error('Failed to assign installation group:', error);
      throw error;
    } finally {
      installationsBeingAssigned.delete(groupKey);
    }
  }

  // ── Unassign / delete dispatch ─────────────────────

  static async unassignJob(dispatchId: string, dispatchInfo?: { serviceOrderId?: number; dispatchNumber?: string }): Promise<void> {
    // Check lock via dispatch status
    try {
      const dispatchIdNum = parseInt(dispatchId, 10);
      if (isNaN(dispatchIdNum)) throw new Error('Invalid dispatch ID');

      // Fetch dispatch to check status-based lock
      let dispatch: Dispatch | null = null;
      try {
        dispatch = await dispatchesApi.getById(dispatchIdNum);
      } catch { /* proceed */ }

      if (dispatch?.status === 'confirmed' || dispatch?.status === 'in_progress' || dispatch?.status === 'completed') {
        throw new Error(`This dispatch is ${dispatch.status} and cannot be deleted. Change status first.`);
      }

      const serviceOrderId = dispatchInfo?.serviceOrderId || dispatch?.serviceOrderId;
      const dispatchNumber = dispatchInfo?.dispatchNumber || dispatch?.dispatchNumber;

      // Push undo data before deleting
      pushUndo({
        type: 'unassign',
        description: `Deleted dispatch #${dispatchNumber || dispatchId}`,
        timestamp: Date.now(),
        data: {
          dispatchId,
          serviceOrderId,
          dispatchNumber,
        }
      });

      // Delete the dispatch - backend will reset job status to unassigned/pending
      await dispatchesApi.delete(dispatchIdNum);

      // Add audit note
      if (serviceOrderId) {
        try {
          const userName = getCurrentUserName();
          await serviceOrdersApi.addNote(serviceOrderId, {
            content: `🗑️ Dispatch deleted - #${dispatchNumber || dispatchIdNum}\n` +
              `• Deleted by: ${userName}\n` +
              `• Date: ${new Date().toLocaleDateString()} at ${new Date().toLocaleTimeString()}\n` +
              `• Job is now available for reassignment`,
            type: 'dispatch_deleted'
          });
        } catch { /* ignore */ }
      }

      cacheService.invalidateDispatchData();
    } catch (error) {
      console.error('Failed to delete dispatch:', error);
      throw error;
    }
  }

  static async deleteDispatch(dispatchId: string, dispatchInfo?: { serviceOrderId?: number; dispatchNumber?: string }): Promise<void> {
    return DispatchOperationsService.unassignJob(dispatchId, dispatchInfo);
  }

  // ── Reschedule dispatch ────────────────────────────

  static async rescheduleDispatch(dispatchId: string, newScheduledDate: Date): Promise<void> {
    try {
      const dispatchIdNum = parseInt(dispatchId, 10);
      if (isNaN(dispatchIdNum)) throw new Error('Invalid dispatch ID');

      // Fetch current for undo
      let originalDate: Date | undefined;
      try {
        const current = await dispatchesApi.getById(dispatchIdNum);
        originalDate = current.scheduledDate ? new Date(current.scheduledDate) : undefined;
      } catch { /* proceed */ }

      const pad = (n: number) => String(n).padStart(2, '0');
      const startTime = `${pad(newScheduledDate.getHours())}:${pad(newScheduledDate.getMinutes())}:00`;

      await dispatchesApi.update(dispatchIdNum, {
        scheduledDate: newScheduledDate.toISOString(),
        scheduledStartTime: startTime,
      });

      // Invalidate cache immediately so subsequent calls get fresh data
      cacheService.invalidateDispatchData();

      pushUndo({
        type: 'reschedule',
        description: `Rescheduled dispatch #${dispatchId}`,
        timestamp: Date.now(),
        data: {
          dispatchId,
          originalStart: originalDate,
          scheduledStart: newScheduledDate,
        }
      });
    } catch (error) {
      console.error('Failed to reschedule dispatch:', error);
      throw error;
    }
  }

  // ── Update schedule (date + time + duration in one call) ───

  static async updateSchedule(dispatchId: string, newStart: Date, newEnd: Date): Promise<void> {
    try {
      const dispatchIdNum = parseInt(dispatchId, 10);
      if (isNaN(dispatchIdNum)) throw new Error('Invalid dispatch ID');

      const newDurationMinutes = Math.round((newEnd.getTime() - newStart.getTime()) / 60000);
      if (newDurationMinutes < 15) throw new Error('Duration must be at least 15 minutes');

      // Fetch current for undo
      let originalDate: Date | undefined;
      try {
        const current = await dispatchesApi.getById(dispatchIdNum);
        originalDate = current.scheduledDate ? new Date(current.scheduledDate) : undefined;
      } catch { /* proceed */ }

      // Backend now persists ScheduledStartTime and ScheduledEndTime columns
      const pad = (n: number) => String(n).padStart(2, '0');
      const startTime = `${pad(newStart.getHours())}:${pad(newStart.getMinutes())}:00`;
      const endTime = `${pad(newEnd.getHours())}:${pad(newEnd.getMinutes())}:00`;

      await dispatchesApi.update(dispatchIdNum, {
        scheduledDate: newStart.toISOString(),
        scheduledStartTime: startTime,
        scheduledEndTime: endTime,
      });

      // Local override no longer strictly needed but kept for instant UI feedback
      setScheduleOverride(dispatchId, newStart, newEnd);

      // Invalidate cache so calendar refreshes with fresh data
      cacheService.invalidateDispatchData();

      pushUndo({
        type: 'reschedule',
        description: `Updated schedule for dispatch #${dispatchId}`,
        timestamp: Date.now(),
        data: {
          dispatchId,
          originalStart: originalDate,
          scheduledStart: newStart,
        }
      });
    } catch (error) {
      console.error('Failed to update schedule:', error);
      throw error;
    }
  }

  // ── Resize job (update duration) ───────────────────

  static async resizeJob(jobId: string, newEnd: Date): Promise<void> {
    try {
      const dispatchIdNum = parseInt(jobId, 10);
      if (isNaN(dispatchIdNum)) throw new Error('Invalid dispatch ID');

      const dispatch = await dispatchesApi.getById(dispatchIdNum);
      const scheduling = dispatch.scheduling || ({} as any);
      const scheduledDateStr = dispatch.scheduledDate || scheduling.scheduledDate;
      const scheduledStart = scheduledDateStr ? new Date(scheduledDateStr) : new Date();

      const newDurationMinutes = Math.round((newEnd.getTime() - scheduledStart.getTime()) / 60000);
      if (newDurationMinutes < 15) throw new Error('Duration must be at least 15 minutes');

      // Persist both start and end times
      const pad = (n: number) => String(n).padStart(2, '0');
      const startTime = `${pad(scheduledStart.getHours())}:${pad(scheduledStart.getMinutes())}:00`;
      const endTime = `${pad(newEnd.getHours())}:${pad(newEnd.getMinutes())}:00`;

      await dispatchesApi.update(dispatchIdNum, {
        scheduledDate: scheduledStart.toISOString(),
        scheduledStartTime: startTime,
        scheduledEndTime: endTime,
      });

      // Keep a local schedule override so UI duration/time stays accurate.
      setScheduleOverride(jobId, scheduledStart, newEnd);

      // Invalidate assigned jobs cache so calendar refreshes
      cacheService.invalidateDispatchData();
    } catch (error) {
      console.error('Failed to resize job:', error);
      throw error;
    }
  }

  // ── Unlock dispatch (change status back to assigned) ───

  static async unlockJob(dispatchId: string): Promise<void> {
    try {
      const dispatchIdNum = parseInt(dispatchId, 10);
      if (isNaN(dispatchIdNum)) return;

      // Update dispatch status back to 'assigned' to unlock it
      await dispatchesApi.updateStatus(dispatchIdNum, 'assigned', 'confirmed');

      // Add audit note
      const userName = getCurrentUserName();
      try {
        await dispatchesApi.addNote(
          dispatchIdNum,
          `🔓 Dispatch unlocked by ${userName} on ${new Date().toLocaleString()}`,
          'system'
        );
      } catch { /* ignore */ }

      cacheService.invalidateDispatchData();
    } catch (error) {
      console.error('Failed to unlock dispatch:', error);
      throw error;
    }
  }

  // ── Lock / Unlock (status-based) ───────────────────

  static async lockJob(dispatchId: string): Promise<void> {
    try {
      const dispatchIdNum = parseInt(dispatchId, 10);
      if (isNaN(dispatchIdNum)) return;

      // Update dispatch status to 'confirmed' which acts as lock
      await dispatchesApi.updateStatus(dispatchIdNum, 'confirmed');

      // Add audit note
      const userName = getCurrentUserName();
      try {
        await dispatchesApi.addNote(
          dispatchIdNum,
          `🔒 Dispatch confirmed and locked by ${userName} on ${new Date().toLocaleString()}`,
          'system'
        );
      } catch { /* ignore */ }
    } catch (error) {
      console.error('Failed to lock dispatch:', error);
      throw error;
    }
  }

  static async isDispatchLocked(dispatchId: string): Promise<boolean> {
    try {
      const dispatchIdNum = parseInt(dispatchId, 10);
      if (isNaN(dispatchIdNum)) return false;
      const dispatch = await dispatchesApi.getById(dispatchIdNum);
      return dispatch.status === 'confirmed' || dispatch.status === 'in_progress' || dispatch.status === 'completed';
    } catch {
      return false;
    }
  }

  // Sync check from cached data (for UI rendering - fast)
  static isDispatchLockedSync(dispatchId: string, dispatches?: Dispatch[]): boolean {
    const allDispatches = dispatches || cacheService.dispatches;
    const dispatch = allDispatches.find(d => String(d.id) === dispatchId);
    if (!dispatch) return false;
    return dispatch.status === 'confirmed' || dispatch.status === 'in_progress' || dispatch.status === 'completed';
  }

  static getDispatchLockInfo(dispatchId: string): { isLocked: boolean; lockedAt?: string; lockedBy?: string } {
    const dispatch = cacheService.dispatches.find(d => String(d.id) === dispatchId);
    if (!dispatch) return { isLocked: false };
    const isLocked = dispatch.status === 'confirmed' || dispatch.status === 'in_progress' || dispatch.status === 'completed';
    return {
      isLocked,
      lockedAt: dispatch.modifiedDate || dispatch.createdDate,
      lockedBy: dispatch.modifiedBy || dispatch.createdBy,
    };
  }

  // ── Undo ───────────────────────────────────────────

  static getLastUndoAction(): UndoAction | null {
    return undoStack.length > 0 ? undoStack[undoStack.length - 1] : null;
  }

  static async undoLastAction(): Promise<boolean> {
    const action = undoStack.pop();
    if (!action) return false;

    try {
      switch (action.type) {
        case 'assign':
          // Undo assign = delete the dispatch
          if (action.data.dispatchId) {
            await dispatchesApi.delete(parseInt(action.data.dispatchId, 10));
            cacheService.invalidateDispatchData();
          }
          break;

        case 'reschedule':
          // Undo reschedule = reschedule back to original time
          if (action.data.dispatchId && action.data.originalStart) {
            await dispatchesApi.update(parseInt(action.data.dispatchId, 10), {
              scheduledDate: action.data.originalStart.toISOString(),
            });
            cacheService.invalidateDispatchData();
          }
          break;

        case 'unassign':
          // Cannot fully undo a deletion without re-creating
          // Return false to indicate partial undo
          return false;
      }
      return true;
    } catch (error) {
      console.error('Failed to undo action:', error);
      return false;
    }
  }

  // ── Assigned jobs for calendar ─────────────────────

  static async getAssignedJobsForTechnician(technicianId: string, date: Date): Promise<Job[]> {
    const cacheKey = `${technicianId}-${date.toISOString().split('T')[0]}`;
    const cached = cacheService.getAssignedJobs(cacheKey);
    if (cached) return cached;

    try {
      const startOfDay = new Date(date);
      startOfDay.setHours(0, 0, 0, 0);
      const endOfDay = new Date(date);
      endOfDay.setHours(23, 59, 59, 999);

      const numericTechId = technicianId.match(/\d+/)?.[0] || technicianId;

      const response = await dispatchesApi.getAll({
        pageSize: 500,
        dateFrom: startOfDay.toISOString(),
        dateTo: endOfDay.toISOString(),
      });

      const dispatchesForTech = response.data.filter((dispatch: Dispatch) =>
        technicianMatchesDispatch(dispatch, technicianId)
      );

      const jobs = dispatchesForTech.map((dispatch: Dispatch) =>
        DispatchOperationsService.mapDispatchToJob(dispatch, numericTechId)
      );

      cacheService.setAssignedJobs(cacheKey, jobs);
      return jobs;
    } catch (error) {
      console.error('Failed to fetch assigned jobs:', error);
      return [];
    }
  }

  static async getAssignedJobsForDateRange(
    technicianIds: string[],
    startDate: Date,
    endDate: Date
  ): Promise<Record<string, Job[]>> {
    try {
      const start = new Date(startDate);
      start.setHours(0, 0, 0, 0);
      const end = new Date(endDate);
      end.setHours(23, 59, 59, 999);

      const response = await dispatchesApi.getAll({
        pageSize: 500,
        dateFrom: start.toISOString(),
        dateTo: end.toISOString(),
      });

      const result: Record<string, Job[]> = {};

      for (const dispatch of response.data) {
        const techId = extractTechnicianIdFromDispatch(dispatch);
        if (!techId) continue;

        const matchingTechId = technicianIds.find(id => {
          const numericId = id.match(/\d+/)?.[0] || id;
          const dispatchNumericId = techId.match(/\d+/)?.[0] || techId;
          return numericId === dispatchNumericId;
        });
        if (!matchingTechId) continue;

        const scheduledDateStr = dispatch.scheduledDate || dispatch.scheduling?.scheduledDate;
        const scheduledDate = scheduledDateStr ? new Date(scheduledDateStr) : new Date();
        const dateKey = scheduledDate.toISOString().split('T')[0];
        const key = `${matchingTechId}-${dateKey}`;

        if (!result[key]) result[key] = [];

        const numericTechId = matchingTechId.match(/\d+/)?.[0] || matchingTechId;
        result[key].push(DispatchOperationsService.mapDispatchToJob(dispatch, numericTechId));
      }

      // Update cache
      for (const [key, jobs] of Object.entries(result)) {
        cacheService.setAssignedJobs(key, jobs);
      }

      return result;
    } catch (error) {
      console.error('Failed to fetch assigned jobs for date range:', error);
      return {};
    }
  }

  // ── Map dispatch to Job for calendar display ───────

  private static mapDispatchToJob(dispatch: Dispatch, numericTechId: string): Job {
    const scheduling = dispatch.scheduling || {} as any;
    const scheduledDateStr = dispatch.scheduledDate || scheduling.scheduledDate;
    const scheduledDate = scheduledDateStr ? new Date(scheduledDateStr) : new Date();
    const override = getScheduleOverride(String(dispatch.id));

    let scheduledStart = new Date(scheduledDate);
    let scheduledEnd: Date;

    if (override) {
      scheduledStart = new Date(override.start);
      scheduledEnd = new Date(override.end);
    } else {
      // Prefer scheduledStartTime/scheduledEndTime from API when available
      const startTimeStr: string | null = scheduling.scheduledStartTime || dispatch.scheduledStartTime || null;
      const endTimeStr: string | null = scheduling.scheduledEndTime || dispatch.scheduledEndTime || null;

      if (startTimeStr) {
        const [h, m] = startTimeStr.split(':').map(Number);
        scheduledStart = new Date(scheduledDate);
        scheduledStart.setHours(h, m, 0, 0);
      }

      if (endTimeStr) {
        const [h, m] = endTimeStr.split(':').map(Number);
        scheduledEnd = new Date(scheduledDate);
        scheduledEnd.setHours(h, m, 0, 0);
        if (scheduledEnd <= scheduledStart) scheduledEnd.setDate(scheduledEnd.getDate() + 1);
      } else {
        const fallbackDuration = scheduling.estimatedDuration || 60;
        scheduledEnd = new Date(scheduledStart.getTime() + fallbackDuration * 60 * 1000);
      }
    }

    const estimatedDuration = Math.max(15, Math.round((scheduledEnd.getTime() - scheduledStart.getTime()) / 60000));
    const rawNotes = dispatch.notes || '';
    const cleanNotes = typeof rawNotes === 'string' ? rawNotes.trim() : '';

    // Try to get service order info from cache
    let serviceOrderNumber = `SO-${dispatch.serviceOrderId}`;
    let serviceOrderTitle = `SO-${dispatch.serviceOrderId}`;
    const cachedSOs = cacheService.serviceOrders;
    if (cachedSOs.length > 0) {
      const so = cachedSOs.find(s => String(s.id) === String(dispatch.serviceOrderId)) as any;
      if (so) {
        serviceOrderNumber = so.orderNumber || serviceOrderNumber;
        serviceOrderTitle = so.title || so.orderNumber || serviceOrderTitle;
      }
    }

    // Determine lock status from dispatch status
    const isLocked = dispatch.status === 'confirmed' || dispatch.status === 'in_progress' || dispatch.status === 'completed';

    // Multi-job dispatch display logic
    const isMultiJobDispatch = (dispatch.jobIds?.length || 0) > 1;
    const displayTitle = isMultiJobDispatch && dispatch.installationName
      ? dispatch.installationName
      : cleanNotes.split('\n')[0] || `Dispatch #${dispatch.dispatchNumber}`;
    const displayDescription = isMultiJobDispatch
      ? `${dispatch.jobIds!.length} jobs`
      : cleanNotes;

    return {
      id: String(dispatch.id),
      serviceOrderId: String(dispatch.serviceOrderId || dispatch.jobId),
      serviceOrderNumber,
      serviceOrderTitle,
      title: displayTitle,
      description: displayDescription,
      status: (dispatch.status === 'completed' ? 'completed' :
        dispatch.status === 'in_progress' ? 'in_progress' : 'assigned') as any,
      priority: dispatch.priority as any,
      estimatedDuration,
      requiredSkills: [],
      assignedTechnicianId: numericTechId,
      scheduledStart,
      scheduledEnd,
      isLocked,
      installationId: dispatch.installationId,
      installationName: dispatch.installationName,
      location: { address: dispatch.siteAddress || 'No address' },
      customerName: dispatch.contactName || 'Unknown',
      createdAt: new Date(dispatch.createdDate || Date.now()),
      updatedAt: new Date(dispatch.modifiedDate || Date.now()),
    };
  }
}
