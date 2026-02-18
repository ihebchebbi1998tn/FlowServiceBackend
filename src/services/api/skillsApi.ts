// Real API service for Skills Management
import type {
  Skill,
  CreateSkillRequest,
  UpdateSkillRequest,
  UserSkill,
  AssignSkillToUserRequest,
  AssignSkillToRoleRequest,
} from '@/types/users';
import { getAuthHeaders } from '@/utils/apiHeaders';

import { API_URL } from '@/config/api';

export type {
  Skill,
  CreateSkillRequest,
  UpdateSkillRequest,
  UserSkill,
  AssignSkillToUserRequest,
  AssignSkillToRoleRequest,
};

export const skillsApi = {
  async getAll(): Promise<Skill[]> {
    const response = await fetch(`${API_URL}/api/Skills`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to fetch skills' }));
      throw new Error(error.message || 'Failed to fetch skills');
    }

    const result = await response.json();
    return result.data || result;
  },

  async getById(id: number): Promise<Skill> {
    const response = await fetch(`${API_URL}/api/Skills/${id}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch skill');
    }

    const result = await response.json();
    return result.data || result;
  },

  async getByCategory(category: string): Promise<Skill[]> {
    const response = await fetch(`${API_URL}/api/Skills/category/${encodeURIComponent(category)}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch skills by category');
    }

    const result = await response.json();
    return result.data || result;
  },

  async create(request: CreateSkillRequest): Promise<Skill> {
    const response = await fetch(`${API_URL}/api/Skills`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to create skill' }));
      throw new Error(error.message || 'Failed to create skill');
    }

    const result = await response.json();
    return result.data || result;
  },

  async update(id: number, request: UpdateSkillRequest): Promise<Skill> {
    const response = await fetch(`${API_URL}/api/Skills/${id}`, {
      method: 'PUT',
      headers: getAuthHeaders(),
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to update skill' }));
      throw new Error(error.message || 'Failed to update skill');
    }

    const result = await response.json();
    return result.data || result;
  },

  async delete(id: number): Promise<void> {
    const response = await fetch(`${API_URL}/api/Skills/${id}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Failed to delete skill' }));
      throw new Error(error.message || 'Failed to delete skill');
    }
  },

  async assignToUser(skillId: number, userId: number, request?: AssignSkillToUserRequest): Promise<void> {
    const response = await fetch(`${API_URL}/api/Skills/${skillId}/assign/${userId}`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(request || {}),
    });

    if (!response.ok) {
      throw new Error('Failed to assign skill to user');
    }
  },

  async removeFromUser(skillId: number, userId: number): Promise<void> {
    const response = await fetch(`${API_URL}/api/Skills/${skillId}/remove/${userId}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to remove skill from user');
    }
  },

  async getUserSkills(userId: number): Promise<UserSkill[]> {
    const response = await fetch(`${API_URL}/api/Skills/user/${userId}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch user skills');
    }

    const result = await response.json();
    return result.data || result;
  },

  async assignToRole(roleId: number, skillId: number, request?: AssignSkillToRoleRequest): Promise<void> {
    const response = await fetch(`${API_URL}/api/Skills/role/${roleId}/assign/${skillId}`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(request || {}),
    });

    if (!response.ok) {
      throw new Error('Failed to assign skill to role');
    }
  },

  async removeFromRole(roleId: number, skillId: number): Promise<void> {
    const response = await fetch(`${API_URL}/api/Skills/role/${roleId}/remove/${skillId}`, {
      method: 'DELETE',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to remove skill from role');
    }
  },

  async getRoleSkills(roleId: number): Promise<Skill[]> {
    const response = await fetch(`${API_URL}/api/Skills/role/${roleId}`, {
      method: 'GET',
      headers: getAuthHeaders(),
    });

    if (!response.ok) {
      throw new Error('Failed to fetch role skills');
    }

    const result = await response.json();
    return result.data || result;
  },
};

export default skillsApi;
