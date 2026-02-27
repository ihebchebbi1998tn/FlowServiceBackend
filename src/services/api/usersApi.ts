// Real API service for Users Management
import type {
  User,
  CreateUserRequest,
  UpdateUserRequest,
  UserListResponse,
  ChangePasswordRequest,
} from '@/types/users';
import { getAuthHeaders } from '@/utils/apiHeaders';

import { API_URL } from '@/config/api';

export type {
  User,
  CreateUserRequest,
  UpdateUserRequest,
  UserListResponse,
  ChangePasswordRequest,
};

export const usersApi = {
  async getAll(): Promise<UserListResponse> {
    // Fetch users and all roles in parallel to avoid N+1 queries
    const [usersResponse, allRolesResponse] = await Promise.all([
      fetch(`${API_URL}/api/Users`, {
        method: 'GET',
        headers: getAuthHeaders(),
      }),
      fetch(`${API_URL}/api/Roles/all-user-roles`, {
        method: 'GET',
        headers: getAuthHeaders(),
      }).catch(() => null), // Graceful fallback if endpoint doesn't exist
    ]);

    if (!usersResponse.ok) {
      const error = await usersResponse.json().catch(() => ({ message: 'Failed to fetch users' }));
      throw new Error(error.message || 'Failed to fetch users');
    }

    const result = await usersResponse.json();
    const usersData = result.data || result;
    const regularUsers: User[] = usersData.users || usersData || [];

    // Try batch roles first (single request), fall back to per-user if not available
    if (allRolesResponse?.ok) {
      const rolesData = await allRolesResponse.json();
      const rolesByUser: Record<string, any[]> = rolesData.data || rolesData || {};

      const usersWithRoles = regularUsers.map(user => ({
        ...user,
        roles: rolesByUser[String(user.id)] || [],
      }));

      return {
        users: usersWithRoles,
        totalCount: usersData.totalCount || usersWithRoles.length,
      };
    }

    // Fallback: fetch roles per user with concurrency limit to avoid hammering the server
    const BATCH_SIZE = 5;
    const usersWithRoles: User[] = [];
    
    for (let i = 0; i < regularUsers.length; i += BATCH_SIZE) {
      const batch = regularUsers.slice(i, i + BATCH_SIZE);
      const batchResults = await Promise.all(
        batch.map(async (user: User) => {
          try {
            const rolesResponse = await fetch(`${API_URL}/api/Roles/user/${user.id}`, {
              method: 'GET',
              headers: getAuthHeaders(),
            });
            if (rolesResponse.ok) {
              const rolesResult = await rolesResponse.json();
              return { ...user, roles: rolesResult.data || rolesResult || [] };
            }
          } catch (e) {
            // Silent fallback — don't spam console per user
          }
          return { ...user, roles: [] };
        })
      );
      usersWithRoles.push(...batchResults);
    }

    return {
      users: usersWithRoles,
      totalCount: usersData.totalCount || usersWithRoles.length,
    };
  },

  async getById(id: number): Promise<User> {
    const response = await fetch(`${API_URL}/api/Users/${id}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch user');
    }

    const result = await response.json();
    return result.data || result;
  },

  async getByEmail(email: string): Promise<User> {
    const response = await fetch(`${API_URL}/api/Users/email/${encodeURIComponent(email)}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch user');
    }

    const result = await response.json();
    return result.data || result;
  },

  async create(request: CreateUserRequest): Promise<User> {
    const response = await fetch(`${API_URL}/api/Users`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to create user' }));
      throw new Error(error.message || 'Failed to create user');
    }

    const result = await response.json();
    return result.data || result;
  },

  async update(id: number, request: UpdateUserRequest): Promise<User> {
    const response = await fetch(`${API_URL}/api/Users/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to update user' }));
      throw new Error(error.message || 'Failed to update user');
    }

    const result = await response.json();
    return result.data || result;
  },

  async delete(id: number): Promise<void> {
    const response = await fetch(`${API_URL}/api/Users/${id}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to delete user' }));
      throw new Error(error.message || 'Failed to delete user');
    }
  },

  async changePassword(id: number, request: ChangePasswordRequest): Promise<void> {
    const response = await fetch(`${API_URL}/api/Users/${id}/change-password`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to change password' }));
      throw new Error(error.message || 'Failed to change password');
    }
  },

  async checkEmailExists(email: string, excludeUserId?: number): Promise<{ exists: boolean; source?: string }> {
    const response = await fetch(`${API_URL}/api/Users/check-email?email=${encodeURIComponent(email)}${excludeUserId ? `&excludeUserId=${excludeUserId}` : ''}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to check email' }));
      throw new Error(error.message || 'Failed to check email');
    }

    const result = await response.json();
    return result.data || result;
  },
};

export default usersApi;
