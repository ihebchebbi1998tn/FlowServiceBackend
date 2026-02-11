# 📚 Lookups Module - Complete API Documentation

## 📋 Table of Contents
1. [Overview](#overview)
2. [Database Schema](#database-schema)
3. [Lookup Types](#lookup-types)
4. [API Endpoints](#api-endpoints)
5. [CRUD Operations](#crud-operations)
6. [Frontend Integration](#frontend-integration)
7. [TypeScript Interfaces](#typescript-interfaces)
8. [Example Service Implementation](#example-service-implementation)
9. [Usage Examples](#usage-examples)

---

## Overview

The Lookups Module provides a centralized system for managing reference data (dropdowns, statuses, categories) used throughout the application. It uses a **single polymorphic table** (`LookupItems`) with a `LookupType` discriminator to store different types of lookup data.

### Key Features

- ✅ **Single Table Design**: All lookup types stored in one table
- ✅ **Soft Delete**: Items are marked as deleted, not removed
- ✅ **Audit Trail**: Tracks who created/modified items and when
- ✅ **Type-Specific Fields**: Flexible schema with optional fields for specific use cases
- ✅ **Sorting**: Custom sort order for each item
- ✅ **Color Support**: Optional color coding for visual distinction
- ✅ **Active/Inactive**: Toggle visibility without deletion

---

## Database Schema

### LookupItems Table

```sql
CREATE TABLE "LookupItems" (
    "Id" VARCHAR(50) PRIMARY KEY,              -- GUID
    "Name" VARCHAR(100) NOT NULL,              -- Display name
    "Description" VARCHAR(500),                -- Optional description
    "Color" VARCHAR(20),                       -- Hex color or CSS color name
    "LookupType" VARCHAR(50) NOT NULL,         -- Discriminator (article-category, task-status, etc.)
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,  -- Visibility flag
    "SortOrder" INTEGER NOT NULL DEFAULT 0,    -- Display order
    "CreatedUser" VARCHAR(100) NOT NULL,       -- Who created it
    "ModifyUser" VARCHAR(100),                 -- Who last modified it
    "CreatedAt" TIMESTAMP NOT NULL,            -- When created
    "UpdatedAt" TIMESTAMP,                     -- When last updated
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,-- Soft delete flag
    
    -- Type-Specific Fields (nullable)
    "Level" INTEGER,                           -- For priorities (1=Low, 2=Medium, 3=High)
    "IsCompleted" BOOLEAN,                     -- For statuses (true=completed state)
    "DefaultDuration" INTEGER,                 -- For event types (minutes)
    "IsAvailable" BOOLEAN,                     -- For technician statuses
    "IsPaid" BOOLEAN,                          -- For leave types
    "Category" VARCHAR(100)                    -- For skills (category grouping)
);

-- Indexes
CREATE INDEX "idx_lookupitems_type" ON "LookupItems"("LookupType");
CREATE INDEX "idx_lookupitems_name" ON "LookupItems"("Name");
```

### Currencies Table

```sql
CREATE TABLE "Currencies" (
    "Id" VARCHAR(3) PRIMARY KEY,               -- ISO code (USD, EUR, GBP)
    "Name" VARCHAR(100) NOT NULL,              -- Full name (US Dollar)
    "Symbol" VARCHAR(10) NOT NULL,             -- Symbol ($, €, £)
    "Code" VARCHAR(3) NOT NULL,                -- ISO code
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "IsDefault" BOOLEAN NOT NULL DEFAULT FALSE,-- Default currency flag
    "SortOrder" INTEGER NOT NULL DEFAULT 0,
    "CreatedUser" VARCHAR(100) NOT NULL,
    "ModifyUser" VARCHAR(100),
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE
);

-- Indexes
CREATE INDEX "idx_currencies_code" ON "Currencies"("Code");
```

---

## Lookup Types

The system supports **13 lookup types**:

| Lookup Type | LookupType Value | Description | Special Fields |
|-------------|------------------|-------------|----------------|
| Article Categories | `article-category` | Categories for inventory articles | - |
| Article Statuses | `article-status` | Status of articles (In Stock, Out of Stock, etc.) | `IsCompleted` |
| Service Categories | `service-category` | Categories for services offered | - |
| Task Statuses | `task-status` | Status of tasks (To Do, In Progress, Done) | `IsCompleted` |
| Event Types | `event-type` | Types of calendar events | `DefaultDuration` |
| Priorities | `priority` | Priority levels (Low, Medium, High) | `Level` |
| Technician Statuses | `technician-status` | Availability status of technicians | `IsAvailable` |
| Leave Types | `leave-type` | Types of leave (Vacation, Sick, etc.) | `IsPaid` |
| Project Statuses | `project-status` | Status of projects | `IsCompleted` |
| Project Types | `project-type` | Types of projects | - |
| Offer Statuses | `offer-status` | Status of offers/quotes | `IsCompleted` |
| Skills | `skill` | Technician skills/competencies | `Category` |
| Countries | `country` | List of countries | - |

**Plus Currencies** (separate table):
- Currencies (USD, EUR, GBP, etc.)

---

## API Endpoints

**Base URL**: `/api/Lookups`

**Authorization**: Required - All endpoints require Bearer token authentication

### Pattern for Each Lookup Type

Each lookup type follows the same CRUD pattern:

```
GET    /api/Lookups/{type-name}           - Get all items of this type
GET    /api/Lookups/{type-name}/{id}      - Get specific item by ID
POST   /api/Lookups/{type-name}           - Create new item
PUT    /api/Lookups/{type-name}/{id}      - Update existing item
DELETE /api/Lookups/{type-name}/{id}      - Delete item (soft delete)
```

### Complete Endpoint List

#### Article Categories
```
GET    /api/Lookups/article-categories
GET    /api/Lookups/article-categories/{id}
POST   /api/Lookups/article-categories
PUT    /api/Lookups/article-categories/{id}
DELETE /api/Lookups/article-categories/{id}
```

#### Article Statuses
```
GET    /api/Lookups/article-statuses
POST   /api/Lookups/article-statuses
```

#### Service Categories
```
GET    /api/Lookups/service-categories
POST   /api/Lookups/service-categories
```

#### Task Statuses
```
GET    /api/Lookups/task-statuses
POST   /api/Lookups/task-statuses
```

#### Event Types
```
GET    /api/Lookups/event-types
POST   /api/Lookups/event-types
```

#### Priorities
```
GET    /api/Lookups/priorities
POST   /api/Lookups/priorities
```

#### Technician Statuses
```
GET    /api/Lookups/technician-statuses
POST   /api/Lookups/technician-statuses
```

#### Leave Types
```
GET    /api/Lookups/leave-types
POST   /api/Lookups/leave-types
```

#### Project Statuses
```
GET    /api/Lookups/project-statuses
POST   /api/Lookups/project-statuses
```

#### Project Types
```
GET    /api/Lookups/project-types
POST   /api/Lookups/project-types
```

#### Offer Statuses
```
GET    /api/Lookups/offer-statuses
POST   /api/Lookups/offer-statuses
```

#### Skills
```
GET    /api/Lookups/skills
POST   /api/Lookups/skills
```

#### Countries
```
GET    /api/Lookups/countries
POST   /api/Lookups/countries
```

#### Currencies (Special)
```
GET    /api/Lookups/currencies
GET    /api/Lookups/currencies/{id}
POST   /api/Lookups/currencies
PUT    /api/Lookups/currencies/{id}
DELETE /api/Lookups/currencies/{id}
```

---

## CRUD Operations

### 1. Get All Items

#### Request
```http
GET /api/Lookups/article-categories
Authorization: Bearer {token}
```

#### Response
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Electronics",
      "description": "Electronic components and devices",
      "color": "#3b82f6",
      "isActive": true,
      "sortOrder": 1,
      "createdAt": "2025-01-15T10:00:00Z",
      "updatedAt": "2025-01-15T11:30:00Z",
      "createdUser": "admin@example.com",
      "modifyUser": "admin@example.com",
      "level": null,
      "isCompleted": null,
      "defaultDuration": null,
      "isAvailable": null,
      "isPaid": null,
      "category": null
    },
    {
      "id": "660e9511-f30c-52e5-b827-557766551111",
      "name": "Hardware",
      "description": "Tools and hardware supplies",
      "color": "#ef4444",
      "isActive": true,
      "sortOrder": 2,
      "createdAt": "2025-01-15T10:05:00Z",
      "updatedAt": null,
      "createdUser": "admin@example.com",
      "modifyUser": null,
      "level": null,
      "isCompleted": null,
      "defaultDuration": null,
      "isAvailable": null,
      "isPaid": null,
      "category": null
    }
  ],
  "totalCount": 2
}
```

---

### 2. Get Single Item

#### Request
```http
GET /api/Lookups/article-categories/550e8400-e29b-41d4-a716-446655440000
Authorization: Bearer {token}
```

#### Response
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Electronics",
  "description": "Electronic components and devices",
  "color": "#3b82f6",
  "isActive": true,
  "sortOrder": 1,
  "createdAt": "2025-01-15T10:00:00Z",
  "updatedAt": "2025-01-15T11:30:00Z",
  "createdUser": "admin@example.com",
  "modifyUser": "admin@example.com",
  "level": null,
  "isCompleted": null,
  "defaultDuration": null,
  "isAvailable": null,
  "isPaid": null,
  "category": null
}
```

---

### 3. Create Item

#### Request
```http
POST /api/Lookups/article-categories
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Software",
  "description": "Software and licenses",
  "color": "#10b981",
  "isActive": true,
  "sortOrder": 3
}
```

#### Response (201 Created)
```json
{
  "id": "770fa622-g41d-63f6-c938-668877662222",
  "name": "Software",
  "description": "Software and licenses",
  "color": "#10b981",
  "isActive": true,
  "sortOrder": 3,
  "createdAt": "2025-01-15T12:00:00Z",
  "updatedAt": null,
  "createdUser": "admin@example.com",
  "modifyUser": null,
  "level": null,
  "isCompleted": null,
  "defaultDuration": null,
  "isAvailable": null,
  "isPaid": null,
  "category": null
}
```

---

### 4. Update Item

#### Request
```http
PUT /api/Lookups/article-categories/770fa622-g41d-63f6-c938-668877662222
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Software & Licenses",
  "description": "Software applications and license keys",
  "isActive": true,
  "sortOrder": 3
}
```

**Note**: All fields are optional in update. Only provided fields will be updated.

#### Response
```json
{
  "id": "770fa622-g41d-63f6-c938-668877662222",
  "name": "Software & Licenses",
  "description": "Software applications and license keys",
  "color": "#10b981",
  "isActive": true,
  "sortOrder": 3,
  "createdAt": "2025-01-15T12:00:00Z",
  "updatedAt": "2025-01-15T12:30:00Z",
  "createdUser": "admin@example.com",
  "modifyUser": "admin@example.com",
  "level": null,
  "isCompleted": null,
  "defaultDuration": null,
  "isAvailable": null,
  "isPaid": null,
  "category": null
}
```

---

### 5. Delete Item (Soft Delete)

#### Request
```http
DELETE /api/Lookups/article-categories/770fa622-g41d-63f6-c938-668877662222
Authorization: Bearer {token}
```

#### Response (204 No Content)
```
(Empty body)
```

**Note**: Items are soft-deleted (`IsDeleted = true`), not physically removed from the database.

---

## Type-Specific Examples

### Priorities (with Level field)

```json
POST /api/Lookups/priorities
{
  "name": "High",
  "description": "Urgent priority",
  "color": "#ef4444",
  "isActive": true,
  "sortOrder": 1,
  "level": 3
}
```

### Task Statuses (with IsCompleted field)

```json
POST /api/Lookups/task-statuses
{
  "name": "Completed",
  "description": "Task finished successfully",
  "color": "#10b981",
  "isActive": true,
  "sortOrder": 3,
  "isCompleted": true
}
```

### Event Types (with DefaultDuration field)

```json
POST /api/Lookups/event-types
{
  "name": "Meeting",
  "description": "Team or client meeting",
  "color": "#3b82f6",
  "isActive": true,
  "sortOrder": 1,
  "defaultDuration": 60
}
```

### Technician Statuses (with IsAvailable field)

```json
POST /api/Lookups/technician-statuses
{
  "name": "Available",
  "description": "Ready for assignments",
  "color": "#10b981",
  "isActive": true,
  "sortOrder": 1,
  "isAvailable": true
}
```

### Leave Types (with IsPaid field)

```json
POST /api/Lookups/leave-types
{
  "name": "Vacation",
  "description": "Paid vacation leave",
  "color": "#3b82f6",
  "isActive": true,
  "sortOrder": 1,
  "isPaid": true
}
```

### Skills (with Category field)

```json
POST /api/Lookups/skills
{
  "name": "React Development",
  "description": "Frontend development with React",
  "color": "#61dafb",
  "isActive": true,
  "sortOrder": 1,
  "category": "Frontend"
}
```

---

## Frontend Integration

### TypeScript Interfaces

```typescript
// src/types/lookups.ts

export interface LookupItem {
  id: string;
  name: string;
  description?: string;
  color?: string;
  isActive: boolean;
  sortOrder: number;
  createdAt: string;
  updatedAt?: string;
  createdUser: string;
  modifyUser?: string;
  
  // Type-specific fields
  level?: number;              // For priorities
  isCompleted?: boolean;       // For statuses
  defaultDuration?: number;    // For event types
  isAvailable?: boolean;       // For technician statuses
  isPaid?: boolean;            // For leave types
  category?: string;           // For skills
}

export interface LookupListResponse {
  items: LookupItem[];
  totalCount: number;
}

export interface CreateLookupRequest {
  name: string;
  description?: string;
  color?: string;
  isActive?: boolean;
  sortOrder?: number;
  
  // Type-specific fields (optional)
  level?: number;
  isCompleted?: boolean;
  defaultDuration?: number;
  isAvailable?: boolean;
  isPaid?: boolean;
  category?: string;
}

export interface UpdateLookupRequest {
  name?: string;
  description?: string;
  color?: string;
  isActive?: boolean;
  sortOrder?: number;
  
  // Type-specific fields (optional)
  level?: number;
  isCompleted?: boolean;
  defaultDuration?: number;
  isAvailable?: boolean;
  isPaid?: boolean;
  category?: string;
}

// Currency interfaces
export interface Currency {
  id: string;        // ISO code
  name: string;
  symbol: string;
  code: string;
  isActive: boolean;
  isDefault: boolean;
  sortOrder: number;
  createdAt: string;
  updatedAt?: string;
  createdUser: string;
  modifyUser?: string;
}

export interface CurrencyListResponse {
  currencies: Currency[];
  totalCount: number;
}

export interface CreateCurrencyRequest {
  name: string;
  symbol: string;
  code: string;      // ISO code
  isActive?: boolean;
  isDefault?: boolean;
  sortOrder?: number;
}

export interface UpdateCurrencyRequest {
  name?: string;
  symbol?: string;
  code?: string;
  isActive?: boolean;
  isDefault?: boolean;
  sortOrder?: number;
}

// Lookup type enum
export type LookupType =
  | 'article-categories'
  | 'article-statuses'
  | 'service-categories'
  | 'task-statuses'
  | 'event-types'
  | 'priorities'
  | 'technician-statuses'
  | 'leave-types'
  | 'project-statuses'
  | 'project-types'
  | 'offer-statuses'
  | 'skills'
  | 'countries'
  | 'currencies';
```

---

## Example Service Implementation

```typescript
// src/services/lookupsService.ts

const API_URL = import.meta.env.VITE_API_URL || import.meta.env.REACT_APP_API_URL || 'https://flowservicebackend.onrender.com';

class LookupsService {
  private baseUrl = `${API_URL}/api/Lookups`;

  private getAuthHeader(): HeadersInit {
    const token = localStorage.getItem('access_token');
    return token ? { 'Authorization': `Bearer ${token}` } : {};
  }

  // Generic methods for all lookup types
  async getAll<T = LookupItem>(type: LookupType): Promise<LookupListResponse> {
    try {
      const response = await fetch(`${this.baseUrl}/${type}`, {
        method: 'GET',
        headers: this.getAuthHeader(),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch ${type}`);
      }

      return await response.json();
    } catch (error) {
      console.error(`Error fetching ${type}:`, error);
      throw error;
    }
  }

  async getById(type: LookupType, id: string): Promise<LookupItem> {
    try {
      const response = await fetch(`${this.baseUrl}/${type}/${id}`, {
        method: 'GET',
        headers: this.getAuthHeader(),
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch ${type} with ID ${id}`);
      }

      return await response.json();
    } catch (error) {
      console.error(`Error fetching ${type} by ID:`, error);
      throw error;
    }
  }

  async create(type: LookupType, data: CreateLookupRequest): Promise<LookupItem> {
    try {
      const response = await fetch(`${this.baseUrl}/${type}`, {
        method: 'POST',
        headers: {
          ...this.getAuthHeader(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(data),
      });

      if (!response.ok) {
        throw new Error(`Failed to create ${type}`);
      }

      return await response.json();
    } catch (error) {
      console.error(`Error creating ${type}:`, error);
      throw error;
    }
  }

  async update(type: LookupType, id: string, data: UpdateLookupRequest): Promise<LookupItem> {
    try {
      const response = await fetch(`${this.baseUrl}/${type}/${id}`, {
        method: 'PUT',
        headers: {
          ...this.getAuthHeader(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(data),
      });

      if (!response.ok) {
        throw new Error(`Failed to update ${type} with ID ${id}`);
      }

      return await response.json();
    } catch (error) {
      console.error(`Error updating ${type}:`, error);
      throw error;
    }
  }

  async delete(type: LookupType, id: string): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/${type}/${id}`, {
        method: 'DELETE',
        headers: this.getAuthHeader(),
      });

      if (!response.ok) {
        throw new Error(`Failed to delete ${type} with ID ${id}`);
      }
    } catch (error) {
      console.error(`Error deleting ${type}:`, error);
      throw error;
    }
  }

  // Convenience methods for specific types
  async getArticleCategories(): Promise<LookupListResponse> {
    return this.getAll('article-categories');
  }

  async getArticleStatuses(): Promise<LookupListResponse> {
    return this.getAll('article-statuses');
  }

  async getServiceCategories(): Promise<LookupListResponse> {
    return this.getAll('service-categories');
  }

  async getTaskStatuses(): Promise<LookupListResponse> {
    return this.getAll('task-statuses');
  }

  async getEventTypes(): Promise<LookupListResponse> {
    return this.getAll('event-types');
  }

  async getPriorities(): Promise<LookupListResponse> {
    return this.getAll('priorities');
  }

  async getTechnicianStatuses(): Promise<LookupListResponse> {
    return this.getAll('technician-statuses');
  }

  async getLeaveTypes(): Promise<LookupListResponse> {
    return this.getAll('leave-types');
  }

  async getProjectStatuses(): Promise<LookupListResponse> {
    return this.getAll('project-statuses');
  }

  async getProjectTypes(): Promise<LookupListResponse> {
    return this.getAll('project-types');
  }

  async getOfferStatuses(): Promise<LookupListResponse> {
    return this.getAll('offer-statuses');
  }

  async getSkills(): Promise<LookupListResponse> {
    return this.getAll('skills');
  }

  async getCountries(): Promise<LookupListResponse> {
    return this.getAll('countries');
  }

  // Currencies (special handling)
  async getCurrencies(): Promise<CurrencyListResponse> {
    try {
      const response = await fetch(`${this.baseUrl}/currencies`, {
        method: 'GET',
        headers: this.getAuthHeader(),
      });

      if (!response.ok) {
        throw new Error('Failed to fetch currencies');
      }

      return await response.json();
    } catch (error) {
      console.error('Error fetching currencies:', error);
      throw error;
    }
  }

  async createCurrency(data: CreateCurrencyRequest): Promise<Currency> {
    try {
      const response = await fetch(`${this.baseUrl}/currencies`, {
        method: 'POST',
        headers: {
          ...this.getAuthHeader(),
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(data),
      });

      if (!response.ok) {
        throw new Error('Failed to create currency');
      }

      return await response.json();
    } catch (error) {
      console.error('Error creating currency:', error);
      throw error;
    }
  }
}

export const lookupsService = new LookupsService();
```

---

## Usage Examples

### React Component Example

```tsx
// src/components/ArticleCategorySelect.tsx
import { useEffect, useState } from 'react';
import { lookupsService } from '@/services/lookupsService';
import type { LookupItem } from '@/types/lookups';

export function ArticleCategorySelect() {
  const [categories, setCategories] = useState<LookupItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedCategory, setSelectedCategory] = useState('');

  useEffect(() => {
    loadCategories();
  }, []);

  const loadCategories = async () => {
    try {
      setLoading(true);
      const response = await lookupsService.getArticleCategories();
      // Filter only active categories
      const activeCategories = response.items.filter(item => item.isActive);
      setCategories(activeCategories);
    } catch (error) {
      console.error('Failed to load categories:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div>Loading categories...</div>;

  return (
    <select
      value={selectedCategory}
      onChange={(e) => setSelectedCategory(e.target.value)}
      className="border rounded px-3 py-2"
    >
      <option value="">Select a category</option>
      {categories.map((category) => (
        <option key={category.id} value={category.id}>
          {category.name}
        </option>
      ))}
    </select>
  );
}
```

### React Hook Example with React Query

```tsx
// src/hooks/useLookups.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { lookupsService } from '@/services/lookupsService';
import type { LookupType, CreateLookupRequest, UpdateLookupRequest } from '@/types/lookups';

export function useLookups(type: LookupType) {
  const queryClient = useQueryClient();

  // Fetch all lookups
  const { data, isLoading, error } = useQuery({
    queryKey: ['lookups', type],
    queryFn: () => lookupsService.getAll(type),
  });

  // Create lookup
  const createMutation = useMutation({
    mutationFn: (data: CreateLookupRequest) => lookupsService.create(type, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lookups', type] });
    },
  });

  // Update lookup
  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateLookupRequest }) =>
      lookupsService.update(type, id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lookups', type] });
    },
  });

  // Delete lookup
  const deleteMutation = useMutation({
    mutationFn: (id: string) => lookupsService.delete(type, id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lookups', type] });
    },
  });

  return {
    lookups: data?.items || [],
    totalCount: data?.totalCount || 0,
    isLoading,
    error,
    create: createMutation.mutate,
    update: updateMutation.mutate,
    delete: deleteMutation.mutate,
    isCreating: createMutation.isPending,
    isUpdating: updateMutation.isPending,
    isDeleting: deleteMutation.isPending,
  };
}

// Usage in component:
function ArticleCategoriesManager() {
  const {
    lookups: categories,
    isLoading,
    create,
    update,
    delete: deleteCategory,
  } = useLookups('article-categories');

  const handleCreate = () => {
    create({
      name: 'New Category',
      description: 'Description here',
      color: '#3b82f6',
      isActive: true,
      sortOrder: 0,
    });
  };

  // ... rest of component
}
```

### CRUD Management Component Example

```tsx
// src/components/LookupsManager.tsx
import { useState } from 'react';
import { useLookups } from '@/hooks/useLookups';
import type { LookupType } from '@/types/lookups';

interface LookupsManagerProps {
  type: LookupType;
  title: string;
}

export function LookupsManager({ type, title }: LookupsManagerProps) {
  const { lookups, isLoading, create, update, delete: deleteLookup } = useLookups(type);
  const [editingId, setEditingId] = useState<string | null>(null);

  const handleCreate = () => {
    const name = prompt('Enter name:');
    if (name) {
      create({
        name,
        isActive: true,
        sortOrder: lookups.length,
      });
    }
  };

  const handleUpdate = (id: string) => {
    const lookup = lookups.find(l => l.id === id);
    if (!lookup) return;

    const name = prompt('Enter new name:', lookup.name);
    if (name) {
      update({ id, data: { name } });
    }
  };

  const handleDelete = (id: string) => {
    if (confirm('Are you sure you want to delete this item?')) {
      deleteLookup(id);
    }
  };

  if (isLoading) return <div>Loading {title}...</div>;

  return (
    <div className="p-4">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-2xl font-bold">{title}</h2>
        <button
          onClick={handleCreate}
          className="px-4 py-2 bg-primary text-white rounded hover:bg-primary/90"
        >
          Add New
        </button>
      </div>

      <div className="space-y-2">
        {lookups.map((lookup) => (
          <div
            key={lookup.id}
            className="flex items-center justify-between p-3 border rounded"
          >
            <div className="flex items-center gap-3">
              {lookup.color && (
                <div
                  className="w-4 h-4 rounded"
                  style={{ backgroundColor: lookup.color }}
                />
              )}
              <div>
                <div className="font-medium">{lookup.name}</div>
                {lookup.description && (
                  <div className="text-sm text-muted-foreground">{lookup.description}</div>
                )}
              </div>
            </div>

            <div className="flex gap-2">
              <button
                onClick={() => handleUpdate(lookup.id)}
                className="px-3 py-1 text-sm bg-secondary text-secondary-foreground rounded hover:bg-secondary/80"
              >
                Edit
              </button>
              <button
                onClick={() => handleDelete(lookup.id)}
                className="px-3 py-1 text-sm bg-destructive text-destructive-foreground rounded hover:bg-destructive/80"
              >
                Delete
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

// Usage:
<LookupsManager type="article-categories" title="Article Categories" />
<LookupsManager type="task-statuses" title="Task Statuses" />
<LookupsManager type="priorities" title="Priorities" />
```

---

## Error Handling

### Backend Error Responses

```json
// 400 Bad Request
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["The Name field is required."]
  }
}

// 404 Not Found
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404
}

// 500 Internal Server Error
"An error occurred while creating the article category."
```

### Frontend Error Handling Example

```typescript
async function safeCreateLookup(type: LookupType, data: CreateLookupRequest) {
  try {
    const result = await lookupsService.create(type, data);
    toast.success(`${type} created successfully`);
    return { success: true, data: result };
  } catch (error: any) {
    console.error(`Failed to create ${type}:`, error);
    
    if (error.response?.status === 400) {
      toast.error('Validation error: Please check your input');
    } else if (error.response?.status === 401) {
      toast.error('Unauthorized: Please login again');
    } else if (error.response?.status === 404) {
      toast.error('Resource not found');
    } else {
      toast.error('An unexpected error occurred');
    }
    
    return { success: false, error };
  }
}
```

---

## Best Practices

### 1. Caching & Performance

```typescript
// Cache lookups that rarely change
const CACHE_TIME = 5 * 60 * 1000; // 5 minutes

const { data } = useQuery({
  queryKey: ['lookups', 'countries'],
  queryFn: () => lookupsService.getCountries(),
  staleTime: CACHE_TIME,
  cacheTime: CACHE_TIME,
});
```

### 2. Prefetch Common Lookups

```typescript
// Prefetch on app load
useEffect(() => {
  queryClient.prefetchQuery({
    queryKey: ['lookups', 'article-categories'],
    queryFn: () => lookupsService.getArticleCategories(),
  });
  queryClient.prefetchQuery({
    queryKey: ['lookups', 'priorities'],
    queryFn: () => lookupsService.getPriorities(),
  });
}, []);
```

### 3. Filter Active Items

```typescript
// Always filter by isActive for user-facing dropdowns
const activeCategories = categories.filter(c => c.isActive);

// Sort by sortOrder
const sortedCategories = [...activeCategories].sort((a, b) => a.sortOrder - b.sortOrder);
```

### 4. Color Display

```tsx
// Display color indicator
<div className="flex items-center gap-2">
  <div 
    className="w-3 h-3 rounded-full" 
    style={{ backgroundColor: lookup.color || '#gray' }}
  />
  <span>{lookup.name}</span>
</div>
```

### 5. Validation Before Create/Update

```typescript
import { z } from 'zod';

const createLookupSchema = z.object({
  name: z.string().min(1).max(100),
  description: z.string().max(500).optional(),
  color: z.string().regex(/^#[0-9A-F]{6}$/i).optional(),
  isActive: z.boolean().default(true),
  sortOrder: z.number().int().default(0),
});

// Validate before calling API
const validated = createLookupSchema.parse(formData);
await lookupsService.create('article-categories', validated);
```

---

## Summary

✅ **13 Lookup Types** + Currencies  
✅ **Complete CRUD Operations** for all types  
✅ **Soft Delete** - items never permanently removed  
✅ **Audit Trail** - tracks who and when  
✅ **Type-Specific Fields** - flexible schema  
✅ **Authentication Required** - Bearer token  
✅ **Frontend Service Ready** - TypeScript interfaces included  
✅ **React Hooks Compatible** - Works with React Query  

**Ready for frontend integration!** 🚀

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-15  
**API Base URL**: `https://flowservicebackend.onrender.com/api/Lookups`
