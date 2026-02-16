// Real API service for Roles Management
import type {
  Role,
  CreateRoleRequest,
  UpdateRoleRequest,
} from '@/types/users';
import { getAuthHeaders } from '@/utils/apiHeaders';

import { API_URL } from '@/config/api';

export type {
  Role,
  CreateRoleRequest,
  UpdateRoleRequest,
};

export const rolesApi = {
  async getAll(): Promise<Role[]> {
    const response = await fetch(`${API_URL}/api/Roles`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to fetch roles' }));
      throw new Error(error.message || 'Failed to fetch roles');
    }

    const result = await response.json();
    return result.data || result;
  },

  async getById(id: number): Promise<Role> {
    const response = await fetch(`${API_URL}/api/Roles/${id}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch role');
    }

    const result = await response.json();
    return result.data || result;
  },

  async create(request: CreateRoleRequest): Promise<Role> {
    const response = await fetch(`${API_URL}/api/Roles`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to create role' }));
      throw new Error(error.message || 'Failed to create role');
    }

    const result = await response.json();
    return result.data || result;
  },

  async update(id: number, request: UpdateRoleRequest): Promise<Role> {
    const response = await fetch(`${API_URL}/api/Roles/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to update role' }));
      throw new Error(error.message || 'Failed to update role');
    }

    const result = await response.json();
    return result.data || result;
  },

  async delete(id: number): Promise<void> {
    const response = await fetch(`${API_URL}/api/Roles/${id}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to delete role' }));
      throw new Error(error.message || 'Failed to delete role');
    }
  },

  async assignToUser(roleId: number, userId: number): Promise<void> {
    const response = await fetch(`${API_URL}/api/Roles/${roleId}/assign/${userId}`, {
      method: 'POST',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to assign role to user');
    }
  },

  async removeFromUser(roleId: number, userId: number): Promise<void> {
    const response = await fetch(`${API_URL}/api/Roles/${roleId}/remove/${userId}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to remove role from user');
    }
  },

  async getUserRoles(userId: number): Promise<Role[]> {
    const response = await fetch(`${API_URL}/api/Roles/user/${userId}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch user roles');
    }

    const result = await response.json();
    return result.data || result;
  },
};

export default rolesApi;
