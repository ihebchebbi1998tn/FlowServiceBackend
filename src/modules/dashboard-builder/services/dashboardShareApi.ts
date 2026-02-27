import { apiFetch, API_URL } from '@/services/api/apiClient';
import type { Dashboard } from '../types';

const BASE = '/api/Dashboards';

export interface SharedDashboardInfo {
  shareToken: string;
  publicUrl: string;
  isPublic: boolean;
  expiresAt?: string;
}

export const dashboardShareApi = {
  /** Generate or retrieve a share token for a dashboard, optionally with a data snapshot */
  async generateShareLink(dashboardId: number, dataSnapshot?: Record<string, any>): Promise<SharedDashboardInfo> {
    const { data, error } = await apiFetch<{ success: boolean; data: SharedDashboardInfo }>(
      `${BASE}/${dashboardId}/share`,
      {
        method: 'POST',
        body: dataSnapshot ? JSON.stringify({ dataSnapshot }) : undefined,
        headers: dataSnapshot ? { 'Content-Type': 'application/json' } : undefined,
      }
    );
    if (error) throw new Error(error);
    if (!data?.data) throw new Error('Invalid response from server');
    return data.data;
  },

  /** Revoke (disable) sharing for a dashboard */
  async revokeShareLink(dashboardId: number): Promise<void> {
    const { error } = await apiFetch<void>(
      `${BASE}/${dashboardId}/share`,
      { method: 'DELETE' }
    );
    if (error) throw new Error(error);
  },

  /** Fetch a dashboard by its public share token (NO auth required) */
  async getByShareToken(token: string): Promise<Dashboard> {
    const url = `${API_URL}${BASE}/public/${token}`;
    const res = await fetch(url, {
      headers: { 'Content-Type': 'application/json' },
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: 'Not found' }));
      throw new Error(err.message || 'Dashboard not found or link expired');
    }
    const data = await res.json();
    return data;
  },
};
