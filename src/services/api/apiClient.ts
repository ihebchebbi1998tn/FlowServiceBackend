// API Client with automatic logging for all CRUD operations
import { authService } from '@/services/authService';
import { logger } from '@/hooks/useLogger';
import { getCurrentTenant } from '@/utils/tenant';
import { API_URL } from '@/config/api';
import { dedupFetch } from '@/utils/requestDedup';

// Helper to get auth token
const getAuthToken = (): string | null => {
  return localStorage.getItem('access_token');
};

// Helper to create auth headers (includes X-Tenant when applicable)
const getAuthHeaders = (): HeadersInit => {
  const token = getAuthToken();
  const tenant = getCurrentTenant();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json; charset=utf-8',
  };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (tenant) {
    headers['X-Tenant'] = tenant;
    if (import.meta.env.DEV) {
      console.info(`🏢 [API] X-Tenant: "${tenant}"`);
    }
  }
  return headers;
};

// Track if we're currently refreshing to prevent multiple refresh calls
let isRefreshing = false;
let refreshPromise: Promise<boolean> | null = null;

// Attempt to refresh the token
const attemptTokenRefresh = async (): Promise<boolean> => {
  if (isRefreshing && refreshPromise) {
    return refreshPromise;
  }
  
  isRefreshing = true;
  refreshPromise = (async () => {
    try {
      const result = await authService.refreshToken();
      return result !== null;
    } catch (error) {
      console.error('Token refresh failed:', error);
      return false;
    } finally {
      isRefreshing = false;
      refreshPromise = null;
    }
  })();
  
  return refreshPromise;
};

// Extract module name and action from endpoint
const parseEndpointForLogging = (endpoint: string, method: string): { module: string; action: 'create' | 'read' | 'update' | 'delete' | 'other'; entityId?: string } => {
  // Remove query params and clean up
  const cleanPath = endpoint.split('?')[0].replace(/^\/api\//, '').replace(/^\//, '');
  const parts = cleanPath.split('/');
  
  // Get module name (first part, capitalize)
  const module = parts[0] ? parts[0].charAt(0).toUpperCase() + parts[0].slice(1) : 'Unknown';
  
  // Get entity ID if present (usually second part if it's a number or GUID-like)
  const entityId = parts[1] && /^[\d]+$|^[a-f0-9-]{36}$/i.test(parts[1]) ? parts[1] : undefined;
  
  // Determine action from HTTP method
  let action: 'create' | 'read' | 'update' | 'delete' | 'other' = 'other';
  switch (method.toUpperCase()) {
    case 'GET':
      action = 'read';
      break;
    case 'POST':
      action = 'create';
      break;
    case 'PUT':
    case 'PATCH':
      action = 'update';
      break;
    case 'DELETE':
      action = 'delete';
      break;
  }
  
  return { module, action, entityId };
};

// List of endpoints to skip logging (to avoid infinite loops)
const SKIP_LOGGING_ENDPOINTS = [
  '/api/SystemLogs',
  '/api/logs',
  '/api/Auth/refresh',
  '/api/workflows/default', // Skip logging for default workflow to avoid cascades when backend is down
];

// API fetch wrapper with automatic token refresh on 401 and logging
export const apiFetch = async <T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<{ data: T | null; status: number; error?: string }> => {
  const url = endpoint.startsWith('http') ? endpoint : `${API_URL}${endpoint}`;
  const method = options.method || 'GET';
  
  // Check if we should skip logging for this endpoint (via list or header)
  const skipHeader = (options.headers as Record<string, string>)?.['X-Skip-Logging'] === 'true';
  const shouldSkipLogging = skipHeader || SKIP_LOGGING_ENDPOINTS.some(skip => endpoint.includes(skip));
  
  const makeRequest = async (): Promise<Response> => {
    const fetchFn = method === 'GET' ? dedupFetch : fetch;
    return fetchFn(url, {
      ...options,
      headers: {
        ...getAuthHeaders(),
        ...options.headers,
      },
    });
  };

  try {
    let response = await makeRequest();
    
    // If 401 Unauthorized, try to refresh token and retry once
    if (response.status === 401) {
      console.log('Received 401, attempting token refresh...');
      const refreshed = await attemptTokenRefresh();
      
      if (refreshed) {
        console.log('Token refreshed successfully, retrying request...');
        response = await makeRequest();
      } else {
        console.log('Token refresh failed, redirecting to login...');
        // Clear session and redirect to login
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        localStorage.removeItem('token_expires_at');
        localStorage.removeItem('user_data');
        
        // Log the session expiry
        if (!shouldSkipLogging) {
          logger.warning('Session expired - user logged out', 'Auth', 'logout');
        }
        
        // Dispatch event to notify auth context
        window.dispatchEvent(new CustomEvent('auth:session-expired'));
        
        return { 
          data: null, 
          status: 401, 
          error: 'Session expired. Please log in again.' 
        };
      }
    }

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Request failed' }));
      const errorMessage = errorData.message || `HTTP ${response.status}`;
      
      // Log errors (except for endpoints we skip)
      if (!shouldSkipLogging && method !== 'GET') {
        const { module, action, entityId } = parseEndpointForLogging(endpoint, method);
        logger.error(
          `Failed to ${action} ${module}${entityId ? ` (ID: ${entityId})` : ''}: ${errorMessage}`,
          module,
          action,
          { entityId, details: errorMessage }
        );
      }
      
      return { 
        data: null, 
        status: response.status, 
        error: errorMessage 
      };
    }

    // Handle empty responses (204 No Content)
    if (response.status === 204) {
      // Log successful delete operations
      if (!shouldSkipLogging && method === 'DELETE') {
        const { module, action, entityId } = parseEndpointForLogging(endpoint, method);
        logger.success(
          `${module} deleted successfully${entityId ? ` (ID: ${entityId})` : ''}`,
          module,
          action,
          { entityId }
        );
      }
      return { data: null, status: 204 };
    }

    const data = await response.json();
    
    // Log successful mutations (create, update, delete)
    if (!shouldSkipLogging && method !== 'GET') {
      const { module, action, entityId } = parseEndpointForLogging(endpoint, method);
      const successEntityId = entityId || (data as any)?.id || (data as any)?.Id;
      
      const actionWord = action === 'create' ? 'created' : action === 'update' ? 'updated' : action === 'delete' ? 'deleted' : 'processed';
      logger.success(
        `${module} ${actionWord} successfully${successEntityId ? ` (ID: ${successEntityId})` : ''}`,
        module,
        action,
        { entityId: successEntityId?.toString() }
      );
    }
    
    return { data, status: response.status };
  } catch (error: any) {
    console.error('API request error:', error);
    
    // Log network errors
    if (!shouldSkipLogging) {
      const { module, action, entityId } = parseEndpointForLogging(endpoint, method);
      logger.error(
        `Network error in ${module}: ${error.message || 'Unknown error'}`,
        module,
        action,
        { entityId, details: error.message }
      );
    }
    
    return { 
      data: null, 
      status: 0, 
      error: error.message || 'Network error' 
    };
  }
};

export { API_URL, getAuthToken, getAuthHeaders };