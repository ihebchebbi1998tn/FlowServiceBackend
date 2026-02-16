/**
 * Centralized auth + tenant headers for ALL API calls (fetch-based).
 * Import this instead of defining your own getAuthHeaders().
 */
import { getCurrentTenant } from '@/utils/tenant';

export const getAuthToken = (): string | null => {
  return localStorage.getItem('access_token');
};

export const getAuthHeaders = (): Record<string, string> => {
  const token = getAuthToken();
  const tenant = getCurrentTenant();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (tenant) headers['X-Tenant'] = tenant;
  return headers;
};

/**
 * Same as getAuthHeaders but without Content-Type (for file uploads).
 */
export const getAuthHeadersNoContentType = (): Record<string, string> => {
  const token = getAuthToken();
  const tenant = getCurrentTenant();
  const headers: Record<string, string> = {};
  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (tenant) headers['X-Tenant'] = tenant;
  return headers;
};
