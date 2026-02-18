/**
 * Shared Axios instance with automatic X-Tenant header injection.
 * ALL API services should import this instead of raw `axios`.
 */
import axios from 'axios';
import { getCurrentTenant } from '@/utils/tenant';
import { API_CONFIG } from '@/config/api.config';

const axiosInstance = axios.create({
  baseURL: API_CONFIG.baseURL,
  timeout: API_CONFIG.timeout,
  headers: API_CONFIG.headers,
});

// Interceptor: attach JWT + X-Tenant on every request
axiosInstance.interceptors.request.use((config) => {
  const token = localStorage.getItem('access_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  const tenant = getCurrentTenant();
  if (tenant) {
    config.headers['X-Tenant'] = tenant;
  }
  return config;
});

export default axiosInstance;
