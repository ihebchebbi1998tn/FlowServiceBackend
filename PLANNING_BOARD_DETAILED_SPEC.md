# Planning Board Module - Detailed Technical Specification

## 📋 Table of Contents
1. [Overview](#overview)
2. [Current State Analysis](#current-state-analysis)
3. [Critical Gaps](#critical-gaps)
4. [Required APIs & Endpoints](#required-apis--endpoints)
5. [Database Schema](#database-schema)
6. [Backend Implementation](#backend-implementation)
7. [Frontend Integration](#frontend-integration)
8. [Implementation Roadmap](#implementation-roadmap)

---

## Overview

The **Planning Board Module** is the core scheduling and dispatch management system that enables dispatchers to:
- Assign **Service Order Jobs** to **Technicians** (Users with technician role)
- Manage technician availability, working hours, and status
- Create **Dispatches** from jobs with scheduling information
- Optimize routes and workload distribution
- Track dispatch lifecycle from planning to completion

**Key Entities:**
- **Users Table** (`Users`) - Contains all users including technicians
- **Service Order Jobs** (`service_order_jobs`) - Jobs to be dispatched
- **Dispatches** (`dispatches`) - Work assignments with schedule
- **Dispatch Technicians** (`dispatch_technicians`) - Many-to-many relationship

---

## Current State Analysis

### ✅ What We Have - Backend

#### 1. **Users Module** (`backend/Modules/Users/`)
```
Models/User.cs - Users table (technicians are users with role)
Controllers/UsersController.cs - CRUD for users
Services/IUserService.cs - User service interface
DTOs/UserResponseDto.cs - User DTOs
```

**Users Table Schema:**
```csharp
[Table("Users")]
public class User {
    int Id
    string Email
    string FirstName
    string LastName
    string? PhoneNumber
    string? Role  // "technician", "dispatcher", etc.
    bool IsActive
    DateTime? LastLoginAt
}
```

#### 2. **Dispatches Module** (`backend/Modules/Dispatches/`)
```
Models/
  - Dispatch.cs ✅
  - DispatchTechnician.cs ✅
  - TimeEntry.cs ✅
  - Expense.cs ✅
  - MaterialUsage.cs ✅
  - Attachment.cs ✅
  - Note.cs ✅

Controllers/
  - DispatchesController.cs ✅ (Basic CRUD)

Services/
  - DispatchService.cs ✅
  - IDispatchService.cs ✅

DTOs/
  - CreateDispatchFromJobDto.cs ✅
  - UpdateDispatchDto.cs ✅
  - DispatchDto.cs ✅
  - DispatchListItemDto.cs ✅

Data/
  - DispatchConfiguration.cs ✅
  - DispatchTechnicianConfiguration.cs ✅
```

**Existing Dispatch APIs:**
```
POST   /api/v1/dispatches/from-job/{jobId}  - Create dispatch from job
GET    /api/v1/dispatches                   - List dispatches (with filters)
GET    /api/v1/dispatches/{id}              - Get dispatch details
PUT    /api/v1/dispatches/{id}              - Update dispatch
PUT    /api/v1/dispatches/{id}/status       - Update status
POST   /api/v1/dispatches/{id}/start        - Start dispatch
POST   /api/v1/dispatches/{id}/complete     - Complete dispatch
DELETE /api/v1/dispatches/{id}              - Delete dispatch
```

#### 3. **Service Orders Module** (`backend/Modules/ServiceOrders/`)
```
Models/
  - ServiceOrder.cs ✅
  - ServiceOrderJob.cs ✅

Controllers/
  - ServiceOrdersController.cs ✅

Services/
  - IServiceOrderService.cs ✅
```

**ServiceOrderJob Schema:**
```csharp
[Table("service_order_jobs")]
public class ServiceOrderJob {
    string Id
    string ServiceOrderId
    string Title
    string? Description
    string Status  // "unscheduled", "scheduled", "in_progress", etc.
    string? InstallationId
    string? WorkType
    int? EstimatedDuration
    decimal? EstimatedCost
    int? CompletionPercentage
    string[]? AssignedTechnicianIds  // Array of user IDs
    DateTime CreatedAt
    DateTime UpdatedAt
}
```

### ✅ What We Have - Frontend

#### 1. **Dispatcher Module** (`src/modules/dispatcher/`)
```
pages/
  - DispatcherPage.tsx ✅ - Main dispatch list view
  
components/
  - calendar/CustomCalendar.tsx ✅ - Calendar view
  - calendar/TechnicianList.tsx ✅ - Technician sidebar
  - calendar/CalendarHeader.tsx ✅
  - DispatcherHeader.tsx ✅

services/
  - dispatcher.service.ts ✅ - MOCK DATA SERVICE

types/
  - index.ts ✅ - Job, Technician, ServiceOrder interfaces
```

**Mock Data in Frontend:**
```typescript
// dispatcher.service.ts contains hardcoded mock data:
- mockJobs: Job[]
- mockTechnicians: Technician[]
- mockServiceOrders: ServiceOrder[]

// Methods that need real API integration:
- getUnassignedJobs() -> GET /api/v1/service-orders/jobs?status=unscheduled
- getTechnicians() -> GET /api/v1/users?role=technician
- assignJob() -> POST /api/v1/planning/assign
- getAssignedJobs() -> GET /api/v1/dispatches?technicianId=X&date=Y
```

#### 2. **Scheduling Module** (`src/modules/scheduling/`)
```
pages/
  - SchedulerManager.tsx ✅ - Technician schedule management
  - ScheduleEditorPage.tsx ✅
  
components/
  - ScheduleEditor.tsx ✅ - Edit technician availability

services/
  - scheduling.service.ts ✅ - Delegates to DispatcherService
```

**Frontend Types:**
```typescript
interface Technician {
  id: string
  firstName: string
  lastName: string
  email: string
  phone: string
  skills: string[]
  status: 'available' | 'busy' | 'offline' | 'on_leave' | 'not_working' | 'over_capacity'
  location?: { lat, lng, address }
  avatar?: string
  workingHours: { start: string, end: string }
}

interface Job {
  id: string
  serviceOrderId: string
  title: string
  status: 'unassigned' | 'assigned' | 'in_progress' | 'completed' | 'cancelled'
  priority: 'low' | 'medium' | 'high' | 'urgent'
  estimatedDuration: number  // minutes
  requiredSkills: string[]
  assignedTechnicianId?: string
  scheduledStart?: Date
  scheduledEnd?: Date
  location: { address, lat?, lng? }
  customerName: string
}
```

---

## Critical Gaps

### 🔴 Missing Backend APIs

#### 1. **Planning/Scheduling APIs**
- ❌ `GET /api/v1/planning/unassigned-jobs` - Get jobs ready for assignment
- ❌ `POST /api/v1/planning/assign` - Assign job to technician with schedule
- ❌ `GET /api/v1/planning/technician-schedule/{technicianId}` - Get technician's schedule
- ❌ `POST /api/v1/planning/batch-assign` - Assign multiple jobs at once
- ❌ `POST /api/v1/planning/validate-assignment` - Validate before assigning
- ❌ `GET /api/v1/planning/available-technicians` - Get available techs for time slot

#### 2. **Technician Management APIs**
- ❌ `GET /api/v1/users/technicians` - List all technicians (filtered users)
- ❌ `GET /api/v1/users/technicians/{id}/availability` - Get availability
- ❌ `PUT /api/v1/users/technicians/{id}/status` - Update status
- ❌ `GET /api/v1/users/technicians/{id}/working-hours` - Get working hours
- ❌ `PUT /api/v1/users/technicians/{id}/working-hours` - Update working hours
- ❌ `POST /api/v1/users/technicians/{id}/leaves` - Create leave/unavailability
- ❌ `GET /api/v1/users/technicians/{id}/leaves` - List leaves

#### 3. **Service Order Job APIs**
- ❌ `GET /api/v1/service-orders/{id}/jobs` - Get jobs for a service order
- ❌ `PUT /api/v1/service-orders/jobs/{jobId}/status` - Update job status
- ❌ `POST /api/v1/service-orders/jobs/{jobId}/create-dispatch` - Create dispatch from job

#### 4. **Dispatch Enhancement APIs**
- ❌ `POST /api/v1/dispatches/{id}/reassign` - Reassign to different technician
- ❌ `PUT /api/v1/dispatches/{id}/reschedule` - Reschedule dispatch
- ❌ `GET /api/v1/dispatches/{id}/history` - Get dispatch history

### 🔴 Missing Database Tables

#### 1. **Technician Working Hours** (needed for schedule management)
```sql
CREATE TABLE technician_working_hours (
    id SERIAL PRIMARY KEY,
    technician_id INT NOT NULL REFERENCES Users(Id),
    day_of_week INT NOT NULL,  -- 0=Sunday, 6=Saturday
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    effective_from DATE,
    effective_until DATE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

#### 2. **Technician Unavailability/Leaves**
```sql
CREATE TABLE technician_leaves (
    id SERIAL PRIMARY KEY,
    technician_id INT NOT NULL REFERENCES Users(Id),
    leave_type VARCHAR(50) NOT NULL,  -- 'vacation', 'sick', 'personal', 'training'
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    start_time TIME,
    end_time TIME,
    status VARCHAR(20) DEFAULT 'pending',  -- 'pending', 'approved', 'rejected', 'cancelled'
    reason TEXT,
    approved_by INT REFERENCES Users(Id),
    approved_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

#### 3. **Technician Status History** (track status changes)
```sql
CREATE TABLE technician_status_history (
    id SERIAL PRIMARY KEY,
    technician_id INT NOT NULL REFERENCES Users(Id),
    status VARCHAR(50) NOT NULL,
    changed_from VARCHAR(50),
    changed_at TIMESTAMP DEFAULT NOW(),
    changed_by INT REFERENCES Users(Id),
    reason TEXT,
    metadata JSONB
);
```

#### 4. **Dispatch History** (track dispatch changes)
```sql
CREATE TABLE dispatch_history (
    id SERIAL PRIMARY KEY,
    dispatch_id VARCHAR(50) NOT NULL REFERENCES dispatches(id),
    action VARCHAR(50) NOT NULL,  -- 'created', 'assigned', 'rescheduled', 'reassigned', 'status_changed'
    old_value TEXT,
    new_value TEXT,
    changed_by VARCHAR(50),
    changed_at TIMESTAMP DEFAULT NOW(),
    metadata JSONB
);
```

#### 5. **Dispatch Routes** (optional - for route optimization)
```sql
CREATE TABLE dispatch_routes (
    id VARCHAR(50) PRIMARY KEY,
    route_name VARCHAR(255),
    technician_id INT NOT NULL REFERENCES Users(Id),
    route_date DATE NOT NULL,
    dispatch_ids TEXT[],  -- Array of dispatch IDs in order
    optimized_order TEXT[],
    total_estimated_time INT,
    total_estimated_distance DECIMAL(10,2),
    status VARCHAR(20) DEFAULT 'planned',
    start_location JSONB,
    end_location JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

### 🔴 Missing in `service_order_jobs` Table

Add fields to support better planning:
```sql
ALTER TABLE service_order_jobs
ADD COLUMN required_skills TEXT[],
ADD COLUMN priority VARCHAR(20) DEFAULT 'medium',
ADD COLUMN scheduled_date DATE,
ADD COLUMN scheduled_start_time TIME,
ADD COLUMN scheduled_end_time TIME,
ADD COLUMN location_json JSONB,
ADD COLUMN customer_name VARCHAR(255),
ADD COLUMN customer_phone VARCHAR(50);
```

---

## Required APIs & Endpoints

### 🎯 Priority 1: Core Planning APIs

#### **1. Get Unassigned Jobs**
```http
GET /api/v1/planning/unassigned-jobs
Query Parameters:
  - priority?: string (low, medium, high, urgent)
  - required_skills?: string[] (comma-separated)
  - service_order_id?: string
  - page?: int (default: 1)
  - page_size?: int (default: 20)

Response:
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "job-001",
        "serviceOrderId": "so-001",
        "title": "Server Maintenance",
        "description": "...",
        "status": "unscheduled",
        "priority": "high",
        "estimatedDuration": 180,
        "requiredSkills": ["Server Maintenance", "Linux"],
        "location": {
          "address": "123 Main St",
          "lat": 36.456,
          "lng": 10.737
        },
        "customerName": "Acme Corp",
        "customerPhone": "+216 72 285 123",
        "createdAt": "2024-01-20T16:45:00Z"
      }
    ],
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalItems": 45,
      "totalPages": 3
    }
  }
}
```

#### **2. Assign Job to Technician**
```http
POST /api/v1/planning/assign
Body:
{
  "jobId": "job-001",
  "technicianIds": ["1", "5"],  // Can assign multiple technicians
  "scheduledDate": "2024-02-15",
  "scheduledStartTime": "09:00:00",
  "scheduledEndTime": "12:00:00",
  "priority": "high",
  "notes": "Customer prefers morning appointment",
  "autoCreateDispatch": true  // Create dispatch automatically
}

Response:
{
  "success": true,
  "data": {
    "job": {
      "id": "job-001",
      "status": "scheduled",
      "assignedTechnicianIds": ["1", "5"],
      "scheduledDate": "2024-02-15",
      "scheduledStartTime": "09:00:00",
      "scheduledEndTime": "12:00:00"
    },
    "dispatch": {
      "id": "disp-123",
      "dispatchNumber": "DISP-20240215-001",
      "status": "assigned",
      "assignedTechnicians": [
        { "id": "1", "name": "Jean Dupont", "email": "jean@company.com" },
        { "id": "5", "name": "Mohamed Trabelsi", "email": "mohamed@company.com" }
      ]
    }
  }
}
```

#### **3. Get Technician Schedule**
```http
GET /api/v1/planning/technician-schedule/{technicianId}
Query Parameters:
  - start_date: string (ISO date, required)
  - end_date: string (ISO date, required)

Response:
{
  "success": true,
  "data": {
    "technicianId": "1",
    "technicianName": "Jean Dupont",
    "workingHours": {
      "monday": { "start": "08:00", "end": "17:00" },
      "tuesday": { "start": "08:00", "end": "17:00" },
      "wednesday": { "start": "08:00", "end": "17:00" },
      "thursday": { "start": "08:00", "end": "17:00" },
      "friday": { "start": "08:00", "end": "17:00" },
      "saturday": null,
      "sunday": null
    },
    "dispatches": [
      {
        "id": "disp-001",
        "dispatchNumber": "DISP-2024-001",
        "jobId": "job-001",
        "jobTitle": "Server Maintenance",
        "serviceOrderId": "so-001",
        "scheduledDate": "2024-02-15",
        "scheduledStartTime": "09:00:00",
        "scheduledEndTime": "12:00:00",
        "estimatedDuration": 180,
        "status": "assigned",
        "priority": "high",
        "location": {
          "address": "123 Main St",
          "lat": 36.456,
          "lng": 10.737
        }
      }
    ],
    "leaves": [
      {
        "id": 5,
        "leaveType": "vacation",
        "startDate": "2024-02-20",
        "endDate": "2024-02-23",
        "status": "approved"
      }
    ],
    "totalScheduledHours": 24.5,
    "availableHours": 15.5
  }
}
```

#### **4. Batch Assign Jobs**
```http
POST /api/v1/planning/batch-assign
Body:
{
  "assignments": [
    {
      "jobId": "job-001",
      "technicianIds": ["1"],
      "scheduledDate": "2024-02-15",
      "scheduledStartTime": "09:00:00",
      "scheduledEndTime": "12:00:00"
    },
    {
      "jobId": "job-002",
      "technicianIds": ["2"],
      "scheduledDate": "2024-02-15",
      "scheduledStartTime": "14:00:00",
      "scheduledEndTime": "16:00:00"
    }
  ],
  "autoCreateDispatches": true
}

Response:
{
  "success": true,
  "data": {
    "successful": 2,
    "failed": 0,
    "results": [
      {
        "jobId": "job-001",
        "status": "success",
        "dispatchId": "disp-123"
      },
      {
        "jobId": "job-002",
        "status": "success",
        "dispatchId": "disp-124"
      }
    ]
  }
}
```

#### **5. Validate Assignment**
```http
POST /api/v1/planning/validate-assignment
Body:
{
  "jobId": "job-001",
  "technicianIds": ["1"],
  "scheduledDate": "2024-02-15",
  "scheduledStartTime": "09:00:00",
  "scheduledEndTime": "12:00:00"
}

Response:
{
  "success": true,
  "data": {
    "isValid": true,
    "warnings": [
      "Technician has back-to-back appointments with minimal travel time"
    ],
    "conflicts": [],
    "recommendations": [
      "Consider scheduling 30 minutes earlier to allow travel time"
    ]
  }
}

// OR if conflicts:
{
  "success": true,
  "data": {
    "isValid": false,
    "warnings": [],
    "conflicts": [
      {
        "type": "time_conflict",
        "message": "Technician already has dispatch DISP-2024-001 from 08:00-11:00",
        "conflictingDispatch": {
          "id": "disp-001",
          "scheduledStartTime": "08:00:00",
          "scheduledEndTime": "11:00:00"
        }
      },
      {
        "type": "skill_mismatch",
        "message": "Technician missing required skill: 'Advanced Linux'",
        "missingSkills": ["Advanced Linux"]
      }
    ],
    "recommendations": []
  }
}
```

### 🎯 Priority 2: Technician Management APIs

#### **1. List Technicians**
```http
GET /api/v1/users/technicians
Query Parameters:
  - status?: string (available, busy, offline, on_leave, not_working, over_capacity)
  - skills?: string[] (comma-separated)
  - available_on?: string (ISO date)
  - available_between?: string (e.g., "09:00-17:00")
  - page?: int
  - page_size?: int

Response:
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "firstName": "Jean",
        "lastName": "Dupont",
        "email": "jean.dupont@company.com",
        "phoneNumber": "+216 20 123 456",
        "role": "technician",
        "skills": ["Server Maintenance", "Network Diagnostics", "Linux Administration"],
        "status": "available",
        "workingHours": {
          "monday": { "start": "08:00", "end": "17:00" },
          "tuesday": { "start": "08:00", "end": "17:00" },
          ...
        },
        "currentLocation": {
          "lat": 36.8065,
          "lng": 10.1815,
          "address": "Tunis Office",
          "lastUpdated": "2024-02-15T10:30:00Z"
        },
        "activeDispatchesCount": 0,
        "todayScheduledHours": 0,
        "isActive": true
      }
    ],
    "pagination": { ... }
  }
}
```

#### **2. Get Technician Availability**
```http
GET /api/v1/users/technicians/{id}/availability
Query Parameters:
  - date: string (ISO date, required)
  - include_leaves?: boolean (default: true)
  - include_dispatches?: boolean (default: true)

Response:
{
  "success": true,
  "data": {
    "technicianId": 1,
    "date": "2024-02-15",
    "workingHours": { "start": "08:00", "end": "17:00" },
    "isWorkingDay": true,
    "onLeave": false,
    "status": "available",
    "scheduledSlots": [
      {
        "start": "09:00",
        "end": "12:00",
        "dispatchId": "disp-001",
        "jobTitle": "Server Maintenance"
      }
    ],
    "availableSlots": [
      { "start": "08:00", "end": "09:00" },
      { "start": "12:00", "end": "17:00" }
    ],
    "totalAvailableMinutes": 300,
    "totalScheduledMinutes": 180,
    "utilizationPercentage": 37.5
  }
}
```

#### **3. Update Technician Status**
```http
PUT /api/v1/users/technicians/{id}/status
Body:
{
  "status": "on_leave",
  "reason": "Vacation",
  "effectiveFrom": "2024-02-20T00:00:00Z",
  "effectiveUntil": "2024-02-23T23:59:59Z"
}

Response:
{
  "success": true,
  "data": {
    "id": 1,
    "status": "on_leave",
    "statusChangedAt": "2024-02-15T10:45:00Z",
    "statusHistory": [
      {
        "status": "on_leave",
        "changedFrom": "available",
        "changedAt": "2024-02-15T10:45:00Z",
        "reason": "Vacation"
      }
    ]
  }
}
```

#### **4. Manage Working Hours**
```http
GET /api/v1/users/technicians/{id}/working-hours

Response:
{
  "success": true,
  "data": {
    "technicianId": 1,
    "defaultHours": {
      "monday": { "start": "08:00", "end": "17:00" },
      "tuesday": { "start": "08:00", "end": "17:00" },
      "wednesday": { "start": "08:00", "end": "17:00" },
      "thursday": { "start": "08:00", "end": "17:00" },
      "friday": { "start": "08:00", "end": "17:00" },
      "saturday": null,
      "sunday": null
    },
    "overrides": [
      {
        "date": "2024-02-15",
        "start": "10:00",
        "end": "19:00",
        "reason": "Special project"
      }
    ]
  }
}

PUT /api/v1/users/technicians/{id}/working-hours
Body:
{
  "monday": { "start": "08:00", "end": "17:00" },
  "tuesday": { "start": "08:00", "end": "17:00" },
  ...
}
```

#### **5. Manage Leaves/Unavailability**
```http
POST /api/v1/users/technicians/{id}/leaves
Body:
{
  "leaveType": "vacation",
  "startDate": "2024-02-20",
  "endDate": "2024-02-23",
  "reason": "Annual vacation"
}

GET /api/v1/users/technicians/{id}/leaves
Query: ?start_date=2024-02-01&end_date=2024-02-29

Response:
{
  "success": true,
  "data": [
    {
      "id": 5,
      "technicianId": 1,
      "leaveType": "vacation",
      "startDate": "2024-02-20",
      "endDate": "2024-02-23",
      "status": "approved",
      "approvedBy": 2,
      "approvedAt": "2024-02-15T09:00:00Z"
    }
  ]
}
```

### 🎯 Priority 3: Service Order Job APIs

#### **1. Get Jobs for Service Order**
```http
GET /api/v1/service-orders/{serviceOrderId}/jobs
Query Parameters:
  - status?: string (unscheduled, scheduled, in_progress, completed)
  - include_dispatches?: boolean (default: false)

Response:
{
  "success": true,
  "data": [
    {
      "id": "job-001",
      "serviceOrderId": "so-001",
      "title": "Server Maintenance",
      "description": "Replace faulty components",
      "status": "scheduled",
      "priority": "high",
      "estimatedDuration": 180,
      "requiredSkills": ["Server Maintenance", "Linux"],
      "assignedTechnicianIds": ["1", "5"],
      "scheduledDate": "2024-02-15",
      "scheduledStartTime": "09:00:00",
      "scheduledEndTime": "12:00:00",
      "location": { ... },
      "customerName": "Acme Corp",
      "dispatches": [
        {
          "id": "disp-001",
          "dispatchNumber": "DISP-2024-001",
          "status": "assigned"
        }
      ]
    }
  ]
}
```

#### **2. Update Job Status**
```http
PUT /api/v1/service-orders/jobs/{jobId}/status
Body:
{
  "status": "scheduled",
  "notes": "Scheduled with technician approval"
}

Response:
{
  "success": true,
  "data": {
    "id": "job-001",
    "status": "scheduled",
    "updatedAt": "2024-02-15T10:30:00Z"
  }
}
```

#### **3. Create Dispatch from Job**
```http
POST /api/v1/service-orders/jobs/{jobId}/create-dispatch
Body:
{
  "technicianIds": ["1"],
  "scheduledDate": "2024-02-15",
  "scheduledStartTime": "09:00:00",
  "scheduledEndTime": "12:00:00",
  "priority": "high",
  "notes": "Customer prefers morning"
}

Response:
{
  "success": true,
  "data": {
    "dispatch": { ... },
    "job": {
      "id": "job-001",
      "status": "scheduled",
      "assignedTechnicianIds": ["1"]
    }
  }
}
```

### 🎯 Priority 4: Dispatch Enhancement APIs

#### **1. Reassign Dispatch**
```http
POST /api/v1/dispatches/{id}/reassign
Body:
{
  "newTechnicianIds": ["2", "3"],
  "reason": "Original technician unavailable",
  "reschedule": true,
  "newScheduledDate": "2024-02-16",
  "newScheduledStartTime": "10:00:00",
  "newScheduledEndTime": "13:00:00"
}

Response:
{
  "success": true,
  "data": {
    "dispatch": {
      "id": "disp-001",
      "assignedTechnicians": [
        { "id": "2", "name": "Ahmed Ben Ali" },
        { "id": "3", "name": "Sarah Wilson" }
      ],
      "scheduledDate": "2024-02-16",
      "scheduledStartTime": "10:00:00",
      "scheduledEndTime": "13:00:00"
    },
    "history": {
      "action": "reassigned",
      "previousTechnicians": ["1"],
      "reason": "Original technician unavailable",
      "changedAt": "2024-02-15T11:00:00Z"
    }
  }
}
```

#### **2. Reschedule Dispatch**
```http
PUT /api/v1/dispatches/{id}/reschedule
Body:
{
  "scheduledDate": "2024-02-16",
  "scheduledStartTime": "14:00:00",
  "scheduledEndTime": "17:00:00",
  "reason": "Customer requested different time"
}

Response:
{
  "success": true,
  "data": {
    "dispatch": { ... },
    "history": { ... }
  }
}
```

#### **3. Get Dispatch History**
```http
GET /api/v1/dispatches/{id}/history

Response:
{
  "success": true,
  "data": [
    {
      "id": 1,
      "dispatchId": "disp-001",
      "action": "created",
      "changedBy": "user-001",
      "changedAt": "2024-02-15T09:00:00Z",
      "metadata": { ... }
    },
    {
      "id": 2,
      "dispatchId": "disp-001",
      "action": "assigned",
      "oldValue": null,
      "newValue": "technician-001",
      "changedBy": "dispatcher-001",
      "changedAt": "2024-02-15T09:15:00Z"
    },
    {
      "id": 3,
      "dispatchId": "disp-001",
      "action": "rescheduled",
      "oldValue": "2024-02-15 09:00-12:00",
      "newValue": "2024-02-16 14:00-17:00",
      "changedBy": "dispatcher-001",
      "changedAt": "2024-02-15T11:00:00Z",
      "metadata": {
        "reason": "Customer requested different time"
      }
    }
  ]
}
```

---

## Database Schema

### 📊 New Tables to Create

#### **1. Technician Working Hours**
```sql
CREATE TABLE technician_working_hours (
    id SERIAL PRIMARY KEY,
    technician_id INT NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    day_of_week INT NOT NULL CHECK (day_of_week >= 0 AND day_of_week <= 6),
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    effective_from DATE,
    effective_until DATE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    
    CONSTRAINT unique_technician_day UNIQUE (technician_id, day_of_week, effective_from)
);

CREATE INDEX idx_tech_working_hours_technician ON technician_working_hours(technician_id);
CREATE INDEX idx_tech_working_hours_dates ON technician_working_hours(effective_from, effective_until);
```

#### **2. Technician Leaves**
```sql
CREATE TABLE technician_leaves (
    id SERIAL PRIMARY KEY,
    technician_id INT NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    leave_type VARCHAR(50) NOT NULL CHECK (leave_type IN ('vacation', 'sick', 'personal', 'training', 'other')),
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    start_time TIME,
    end_time TIME,
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'approved', 'rejected', 'cancelled')),
    reason TEXT,
    approved_by INT REFERENCES "Users"("Id"),
    approved_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    
    CONSTRAINT valid_date_range CHECK (end_date >= start_date)
);

CREATE INDEX idx_tech_leaves_technician ON technician_leaves(technician_id);
CREATE INDEX idx_tech_leaves_dates ON technician_leaves(start_date, end_date);
CREATE INDEX idx_tech_leaves_status ON technician_leaves(status);
```

#### **3. Technician Status History**
```sql
CREATE TABLE technician_status_history (
    id SERIAL PRIMARY KEY,
    technician_id INT NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    status VARCHAR(50) NOT NULL,
    changed_from VARCHAR(50),
    changed_at TIMESTAMP DEFAULT NOW(),
    changed_by INT REFERENCES "Users"("Id"),
    reason TEXT,
    metadata JSONB,
    
    CONSTRAINT valid_status CHECK (status IN ('available', 'busy', 'offline', 'on_leave', 'not_working', 'over_capacity'))
);

CREATE INDEX idx_tech_status_history_technician ON technician_status_history(technician_id);
CREATE INDEX idx_tech_status_history_date ON technician_status_history(changed_at DESC);
```

#### **4. Dispatch History**
```sql
CREATE TABLE dispatch_history (
    id SERIAL PRIMARY KEY,
    dispatch_id VARCHAR(50) NOT NULL REFERENCES dispatches(id) ON DELETE CASCADE,
    action VARCHAR(50) NOT NULL,
    old_value TEXT,
    new_value TEXT,
    changed_by VARCHAR(50),
    changed_at TIMESTAMP DEFAULT NOW(),
    metadata JSONB,
    
    CONSTRAINT valid_action CHECK (action IN ('created', 'assigned', 'rescheduled', 'reassigned', 'status_changed', 'updated', 'cancelled', 'deleted'))
);

CREATE INDEX idx_dispatch_history_dispatch ON dispatch_history(dispatch_id);
CREATE INDEX idx_dispatch_history_date ON dispatch_history(changed_at DESC);
CREATE INDEX idx_dispatch_history_action ON dispatch_history(action);
```

#### **5. Dispatch Routes (Optional)**
```sql
CREATE TABLE dispatch_routes (
    id VARCHAR(50) PRIMARY KEY,
    route_name VARCHAR(255),
    technician_id INT NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    route_date DATE NOT NULL,
    dispatch_ids TEXT[],
    optimized_order TEXT[],
    total_estimated_time INT,
    total_estimated_distance DECIMAL(10,2),
    actual_time INT,
    actual_distance DECIMAL(10,2),
    status VARCHAR(20) DEFAULT 'planned' CHECK (status IN ('planned', 'in_progress', 'completed', 'cancelled')),
    start_location JSONB,
    end_location JSONB,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_routes_technician ON dispatch_routes(technician_id);
CREATE INDEX idx_routes_date ON dispatch_routes(route_date);
CREATE INDEX idx_routes_status ON dispatch_routes(status);
```

### 📊 Modify Existing Tables

#### **Modify `service_order_jobs` table**
```sql
ALTER TABLE service_order_jobs
ADD COLUMN IF NOT EXISTS required_skills TEXT[],
ADD COLUMN IF NOT EXISTS priority VARCHAR(20) DEFAULT 'medium' CHECK (priority IN ('low', 'medium', 'high', 'urgent')),
ADD COLUMN IF NOT EXISTS scheduled_date DATE,
ADD COLUMN IF NOT EXISTS scheduled_start_time TIME,
ADD COLUMN IF NOT EXISTS scheduled_end_time TIME,
ADD COLUMN IF NOT EXISTS location_json JSONB,
ADD COLUMN IF NOT EXISTS customer_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS customer_phone VARCHAR(50);

CREATE INDEX IF NOT EXISTS idx_jobs_status ON service_order_jobs(status);
CREATE INDEX IF NOT EXISTS idx_jobs_scheduled_date ON service_order_jobs(scheduled_date);
CREATE INDEX IF NOT EXISTS idx_jobs_priority ON service_order_jobs(priority);
```

#### **Add to `Users` table (if not exists)**
```sql
ALTER TABLE "Users"
ADD COLUMN IF NOT EXISTS skills TEXT[],
ADD COLUMN IF NOT EXISTS current_status VARCHAR(50) DEFAULT 'offline',
ADD COLUMN IF NOT EXISTS location_json JSONB;

CREATE INDEX IF NOT EXISTS idx_users_role ON "Users"("Role");
CREATE INDEX IF NOT EXISTS idx_users_status ON "Users"(current_status);
```

---

## Backend Implementation

### 🏗️ Detailed C# Implementation

#### **1. Create Planning Module Structure**

```
backend/Modules/Planning/
├── Controllers/
│   └── PlanningController.cs
├── Services/
│   ├── IPlanningService.cs
│   └── PlanningService.cs
├── DTOs/
│   ├── AssignJobDto.cs
│   ├── BatchAssignDto.cs
│   ├── ValidateAssignmentDto.cs
│   ├── TechnicianScheduleDto.cs
│   └── AssignmentValidationResult.cs
└── Models/
    ├── TechnicianWorkingHours.cs
    ├── TechnicianLeave.cs
    ├── TechnicianStatusHistory.cs
    └── DispatchHistory.cs
```

#### **2. DTOs - Planning Module**

**AssignJobDto.cs:**
```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Planning.DTOs
{
    public class AssignJobDto
    {
        [Required]
        public string JobId { get; set; } = null!;

        [Required]
        [MinLength(1, ErrorMessage = "At least one technician must be assigned")]
        public List<string> TechnicianIds { get; set; } = new();

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        public TimeSpan ScheduledStartTime { get; set; }

        [Required]
        public TimeSpan ScheduledEndTime { get; set; }

        public string Priority { get; set; } = "medium";

        public string? Notes { get; set; }

        public bool AutoCreateDispatch { get; set; } = true;
    }

    public class AssignJobResponseDto
    {
        public ServiceOrderJobDto Job { get; set; } = null!;
        public DispatchDto? Dispatch { get; set; }
    }

    public class ServiceOrderJobDto
    {
        public string Id { get; set; } = null!;
        public string ServiceOrderId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Status { get; set; } = null!;
        public List<string> AssignedTechnicianIds { get; set; } = new();
        public DateTime? ScheduledDate { get; set; }
        public TimeSpan? ScheduledStartTime { get; set; }
        public TimeSpan? ScheduledEndTime { get; set; }
    }
}
```

**BatchAssignDto.cs:**
```csharp
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyApi.Modules.Planning.DTOs
{
    public class BatchAssignDto
    {
        [Required]
        [MinLength(1)]
        public List<AssignJobDto> Assignments { get; set; } = new();

        public bool AutoCreateDispatches { get; set; } = true;
    }

    public class BatchAssignResponseDto
    {
        public int Successful { get; set; }
        public int Failed { get; set; }
        public List<BatchAssignResult> Results { get; set; } = new();
    }

    public class BatchAssignResult
    {
        public string JobId { get; set; } = null!;
        public string Status { get; set; } = null!; // "success" or "failed"
        public string? DispatchId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
```

**ValidateAssignmentDto.cs:**
```csharp
using System;
using System.Collections.Generic;

namespace MyApi.Modules.Planning.DTOs
{
    public class ValidateAssignmentDto
    {
        public string JobId { get; set; } = null!;
        public List<string> TechnicianIds { get; set; } = new();
        public DateTime ScheduledDate { get; set; }
        public TimeSpan ScheduledStartTime { get; set; }
        public TimeSpan ScheduledEndTime { get; set; }
    }

    public class AssignmentValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<AssignmentConflict> Conflicts { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class AssignmentConflict
    {
        public string Type { get; set; } = null!; // "time_conflict", "skill_mismatch", "on_leave", etc.
        public string Message { get; set; } = null!;
        public object? ConflictingData { get; set; }
    }
}
```

**TechnicianScheduleDto.cs:**
```csharp
using System;
using System.Collections.Generic;

namespace MyApi.Modules.Planning.DTOs
{
    public class TechnicianScheduleDto
    {
        public string TechnicianId { get; set; } = null!;
        public string TechnicianName { get; set; } = null!;
        public Dictionary<string, WorkingHoursDto?> WorkingHours { get; set; } = new();
        public List<DispatchScheduleDto> Dispatches { get; set; } = new();
        public List<TechnicianLeaveDto> Leaves { get; set; } = new();
        public decimal TotalScheduledHours { get; set; }
        public decimal AvailableHours { get; set; }
    }

    public class WorkingHoursDto
    {
        public string Start { get; set; } = null!;
        public string End { get; set; } = null!;
    }

    public class DispatchScheduleDto
    {
        public string Id { get; set; } = null!;
        public string DispatchNumber { get; set; } = null!;
        public string JobId { get; set; } = null!;
        public string JobTitle { get; set; } = null!;
        public string ServiceOrderId { get; set; } = null!;
        public DateTime ScheduledDate { get; set; }
        public TimeSpan ScheduledStartTime { get; set; }
        public TimeSpan ScheduledEndTime { get; set; }
        public int EstimatedDuration { get; set; }
        public string Status { get; set; } = null!;
        public string Priority { get; set; } = null!;
        public LocationDto? Location { get; set; }
    }

    public class LocationDto
    {
        public string Address { get; set; } = null!;
        public double? Lat { get; set; }
        public double? Lng { get; set; }
    }

    public class TechnicianLeaveDto
    {
        public int Id { get; set; }
        public string LeaveType { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = null!;
    }
}
```

#### **3. Models - Planning Module**

**TechnicianWorkingHours.cs:**
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Planning.Models
{
    [Table("technician_working_hours")]
    public class TechnicianWorkingHours
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("technician_id")]
        public int TechnicianId { get; set; }

        [Required]
        [Column("day_of_week")]
        public int DayOfWeek { get; set; } // 0 = Sunday, 6 = Saturday

        [Required]
        [Column("start_time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Column("end_time")]
        public TimeSpan EndTime { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("effective_from")]
        public DateTime? EffectiveFrom { get; set; }

        [Column("effective_until")]
        public DateTime? EffectiveUntil { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

**TechnicianLeave.cs:**
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Planning.Models
{
    [Table("technician_leaves")]
    public class TechnicianLeave
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("technician_id")]
        public int TechnicianId { get; set; }

        [Required]
        [Column("leave_type")]
        [MaxLength(50)]
        public string LeaveType { get; set; } = null!;

        [Required]
        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("start_time")]
        public TimeSpan? StartTime { get; set; }

        [Column("end_time")]
        public TimeSpan? EndTime { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("approved_by")]
        public int? ApprovedBy { get; set; }

        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

**TechnicianStatusHistory.cs:**
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Planning.Models
{
    [Table("technician_status_history")]
    public class TechnicianStatusHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("technician_id")]
        public int TechnicianId { get; set; }

        [Required]
        [Column("status")]
        [MaxLength(50)]
        public string Status { get; set; } = null!;

        [Column("changed_from")]
        [MaxLength(50)]
        public string? ChangedFrom { get; set; }

        [Column("changed_at")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [Column("changed_by")]
        public int? ChangedBy { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("metadata", TypeName = "jsonb")]
        public string? MetadataJson { get; set; }
    }
}
```

**DispatchHistory.cs:**
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Modules.Planning.Models
{
    [Table("dispatch_history")]
    public class DispatchHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("dispatch_id")]
        [MaxLength(50)]
        public string DispatchId { get; set; } = null!;

        [Required]
        [Column("action")]
        [MaxLength(50)]
        public string Action { get; set; } = null!;

        [Column("old_value")]
        public string? OldValue { get; set; }

        [Column("new_value")]
        public string? NewValue { get; set; }

        [Column("changed_by")]
        [MaxLength(50)]
        public string? ChangedBy { get; set; }

        [Column("changed_at")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [Column("metadata", TypeName = "jsonb")]
        public string? MetadataJson { get; set; }
    }
}
```

#### **4. Service Interface - IPlanningService.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApi.Modules.Planning.DTOs;
using MyApi.Modules.ServiceOrders.Models;

namespace MyApi.Modules.Planning.Services
{
    public interface IPlanningService
    {
        // Job Assignment
        Task<AssignJobResponseDto> AssignJobAsync(AssignJobDto dto, string userId);
        Task<BatchAssignResponseDto> BatchAssignAsync(BatchAssignDto dto, string userId);
        Task<AssignmentValidationResult> ValidateAssignmentAsync(ValidateAssignmentDto dto);

        // Unassigned Jobs
        Task<PagedResult<ServiceOrderJobDto>> GetUnassignedJobsAsync(
            string? priority,
            List<string>? requiredSkills,
            string? serviceOrderId,
            int page,
            int pageSize
        );

        // Technician Schedule
        Task<TechnicianScheduleDto> GetTechnicianScheduleAsync(
            string technicianId,
            DateTime startDate,
            DateTime endDate
        );

        // Available Technicians
        Task<List<TechnicianAvailabilityDto>> GetAvailableTechniciansAsync(
            DateTime date,
            TimeSpan startTime,
            TimeSpan endTime,
            List<string>? requiredSkills
        );
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }

    public class PaginationDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class TechnicianAvailabilityDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public List<string> Skills { get; set; } = new();
        public string Status { get; set; } = null!;
        public bool IsAvailable { get; set; }
        public int AvailableMinutes { get; set; }
        public int ScheduledMinutes { get; set; }
        public decimal UtilizationPercentage { get; set; }
    }
}
```

#### **5. Service Implementation - PlanningService.cs (Partial)**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyApi.Data;
using MyApi.Modules.Planning.DTOs;
using MyApi.Modules.Planning.Models;
using MyApi.Modules.ServiceOrders.Models;
using MyApi.Modules.Dispatches.Models;
using MyApi.Modules.Dispatches.Services;

namespace MyApi.Modules.Planning.Services
{
    public class PlanningService : IPlanningService
    {
        private readonly ApplicationDbContext _db;
        private readonly IDispatchService _dispatchService;
        private readonly ILogger<PlanningService> _logger;

        public PlanningService(
            ApplicationDbContext db,
            IDispatchService dispatchService,
            ILogger<PlanningService> logger)
        {
            _db = db;
            _dispatchService = dispatchService;
            _logger = logger;
        }

        public async Task<AssignJobResponseDto> AssignJobAsync(AssignJobDto dto, string userId)
        {
            // 1. Validate assignment
            var validation = await ValidateAssignmentAsync(new ValidateAssignmentDto
            {
                JobId = dto.JobId,
                TechnicianIds = dto.TechnicianIds,
                ScheduledDate = dto.ScheduledDate,
                ScheduledStartTime = dto.ScheduledStartTime,
                ScheduledEndTime = dto.ScheduledEndTime
            });

            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Assignment validation failed: {string.Join(", ", validation.Conflicts.Select(c => c.Message))}");
            }

            // 2. Update job
            var job = await _db.ServiceOrderJobs.FirstOrDefaultAsync(j => j.Id == dto.JobId);
            if (job == null)
                throw new KeyNotFoundException($"Job {dto.JobId} not found");

            job.AssignedTechnicianIds = dto.TechnicianIds.ToArray();
            job.ScheduledDate = dto.ScheduledDate;
            job.ScheduledStartTime = dto.ScheduledStartTime;
            job.ScheduledEndTime = dto.ScheduledEndTime;
            job.Status = "scheduled";
            job.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // 3. Create dispatch if requested
            DispatchDto? dispatch = null;
            if (dto.AutoCreateDispatch)
            {
                var createDispatchDto = new CreateDispatchFromJobDto
                {
                    AssignedTechnicianIds = dto.TechnicianIds,
                    ScheduledDate = dto.ScheduledDate,
                    ScheduledStartTime = dto.ScheduledStartTime,
                    ScheduledEndTime = dto.ScheduledEndTime,
                    Priority = dto.Priority
                };

                dispatch = await _dispatchService.CreateFromJobAsync(dto.JobId, createDispatchDto, userId);
            }

            // 4. Return response
            return new AssignJobResponseDto
            {
                Job = MapJobToDto(job),
                Dispatch = dispatch
            };
        }

        public async Task<AssignmentValidationResult> ValidateAssignmentAsync(ValidateAssignmentDto dto)
        {
            var result = new AssignmentValidationResult { IsValid = true };

            // 1. Check job exists and is unscheduled
            var job = await _db.ServiceOrderJobs
                .Include(j => j.ServiceOrder)
                .FirstOrDefaultAsync(j => j.Id == dto.JobId);

            if (job == null)
            {
                result.IsValid = false;
                result.Conflicts.Add(new AssignmentConflict
                {
                    Type = "job_not_found",
                    Message = $"Job {dto.JobId} not found"
                });
                return result;
            }

            // 2. Check each technician
            foreach (var techId in dto.TechnicianIds)
            {
                if (!int.TryParse(techId, out int technicianId))
                {
                    result.IsValid = false;
                    result.Conflicts.Add(new AssignmentConflict
                    {
                        Type = "invalid_technician_id",
                        Message = $"Invalid technician ID: {techId}"
                    });
                    continue;
                }

                var technician = await _db.Users.FirstOrDefaultAsync(u => u.Id == technicianId && u.Role == "technician");
                if (technician == null)
                {
                    result.IsValid = false;
                    result.Conflicts.Add(new AssignmentConflict
                    {
                        Type = "technician_not_found",
                        Message = $"Technician {techId} not found"
                    });
                    continue;
                }

                // 3. Check technician on leave
                var onLeave = await _db.Set<TechnicianLeave>()
                    .AnyAsync(l =>
                        l.TechnicianId == technicianId &&
                        l.Status == "approved" &&
                        l.StartDate <= dto.ScheduledDate &&
                        l.EndDate >= dto.ScheduledDate);

                if (onLeave)
                {
                    result.IsValid = false;
                    result.Conflicts.Add(new AssignmentConflict
                    {
                        Type = "on_leave",
                        Message = $"Technician {technician.FirstName} {technician.LastName} is on leave on {dto.ScheduledDate:yyyy-MM-dd}"
                    });
                    continue;
                }

                // 4. Check time conflicts with existing dispatches
                var conflictingDispatches = await _db.Dispatches
                    .Include(d => d.AssignedTechnicians)
                    .Where(d =>
                        d.AssignedTechnicians.Any(at => at.TechnicianId == techId) &&
                        d.ScheduledDate == dto.ScheduledDate &&
                        !d.IsDeleted &&
                        d.Status != "cancelled" &&
                        d.Status != "completed")
                    .ToListAsync();

                foreach (var cd in conflictingDispatches)
                {
                    if (cd.ScheduledStartTime.HasValue && cd.ScheduledEndTime.HasValue)
                    {
                        bool overlaps =
                            (dto.ScheduledStartTime >= cd.ScheduledStartTime.Value && dto.ScheduledStartTime < cd.ScheduledEndTime.Value) ||
                            (dto.ScheduledEndTime > cd.ScheduledStartTime.Value && dto.ScheduledEndTime <= cd.ScheduledEndTime.Value) ||
                            (dto.ScheduledStartTime <= cd.ScheduledStartTime.Value && dto.ScheduledEndTime >= cd.ScheduledEndTime.Value);

                        if (overlaps)
                        {
                            result.IsValid = false;
                            result.Conflicts.Add(new AssignmentConflict
                            {
                                Type = "time_conflict",
                                Message = $"Technician {technician.FirstName} {technician.LastName} already has dispatch {cd.DispatchNumber} from {cd.ScheduledStartTime:hh\\:mm}-{cd.ScheduledEndTime:hh\\:mm}",
                                ConflictingData = new
                                {
                                    cd.Id,
                                    cd.DispatchNumber,
                                    ScheduledStartTime = cd.ScheduledStartTime.ToString(),
                                    ScheduledEndTime = cd.ScheduledEndTime.ToString()
                                }
                            });
                        }
                    }
                }

                // 5. Check skill match (warning only)
                // TODO: Implement skill matching when skills are added to job
            }

            return result;
        }

        public async Task<PagedResult<ServiceOrderJobDto>> GetUnassignedJobsAsync(
            string? priority,
            List<string>? requiredSkills,
            string? serviceOrderId,
            int page,
            int pageSize)
        {
            var query = _db.ServiceOrderJobs
                .Where(j => j.Status == "unscheduled" || j.Status == "unassigned");

            if (!string.IsNullOrEmpty(priority))
                query = query.Where(j => j.Priority == priority);

            if (!string.IsNullOrEmpty(serviceOrderId))
                query = query.Where(j => j.ServiceOrderId == serviceOrderId);

            // TODO: Filter by required skills when implemented

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(j => j.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ServiceOrderJobDto>
            {
                Items = items.Select(MapJobToDto).ToList(),
                Pagination = new PaginationDto
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                }
            };
        }

        public async Task<TechnicianScheduleDto> GetTechnicianScheduleAsync(
            string technicianId,
            DateTime startDate,
            DateTime endDate)
        {
            if (!int.TryParse(technicianId, out int techId))
                throw new ArgumentException("Invalid technician ID");

            var technician = await _db.Users.FirstOrDefaultAsync(u => u.Id == techId);
            if (technician == null)
                throw new KeyNotFoundException($"Technician {technicianId} not found");

            // Get working hours
            var workingHours = await _db.Set<TechnicianWorkingHours>()
                .Where(wh => wh.TechnicianId == techId && wh.IsActive)
                .ToListAsync();

            // Get dispatches
            var dispatches = await _db.Dispatches
                .Include(d => d.AssignedTechnicians)
                .Where(d =>
                    d.AssignedTechnicians.Any(at => at.TechnicianId == technicianId) &&
                    d.ScheduledDate >= startDate &&
                    d.ScheduledDate <= endDate &&
                    !d.IsDeleted)
                .ToListAsync();

            // Get leaves
            var leaves = await _db.Set<TechnicianLeave>()
                .Where(l =>
                    l.TechnicianId == techId &&
                    l.Status == "approved" &&
                    l.StartDate <= endDate &&
                    l.EndDate >= startDate)
                .ToListAsync();

            // Build response
            return new TechnicianScheduleDto
            {
                TechnicianId = technicianId,
                TechnicianName = $"{technician.FirstName} {technician.LastName}",
                WorkingHours = BuildWorkingHoursDict(workingHours),
                Dispatches = dispatches.Select(MapDispatchToScheduleDto).ToList(),
                Leaves = leaves.Select(MapLeaveToDto).ToList(),
                TotalScheduledHours = CalculateTotalScheduledHours(dispatches),
                AvailableHours = CalculateAvailableHours(workingHours, dispatches, startDate, endDate)
            };
        }

        // Helper methods
        private ServiceOrderJobDto MapJobToDto(ServiceOrderJob job)
        {
            return new ServiceOrderJobDto
            {
                Id = job.Id,
                ServiceOrderId = job.ServiceOrderId,
                Title = job.Title,
                Status = job.Status,
                AssignedTechnicianIds = job.AssignedTechnicianIds?.ToList() ?? new List<string>(),
                ScheduledDate = job.ScheduledDate,
                ScheduledStartTime = job.ScheduledStartTime,
                ScheduledEndTime = job.ScheduledEndTime
            };
        }

        private Dictionary<string, WorkingHoursDto?> BuildWorkingHoursDict(List<TechnicianWorkingHours> hours)
        {
            var days = new[] { "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday" };
            var dict = new Dictionary<string, WorkingHoursDto?>();

            foreach (var day in days.Select((name, index) => (name, index)))
            {
                var wh = hours.FirstOrDefault(h => h.DayOfWeek == day.index);
                dict[day.name] = wh == null ? null : new WorkingHoursDto
                {
                    Start = wh.StartTime.ToString(@"hh\:mm"),
                    End = wh.EndTime.ToString(@"hh\:mm")
                };
            }

            return dict;
        }

        private DispatchScheduleDto MapDispatchToScheduleDto(Dispatch dispatch)
        {
            // TODO: Get job details from service order jobs
            return new DispatchScheduleDto
            {
                Id = dispatch.Id,
                DispatchNumber = dispatch.DispatchNumber,
                JobId = dispatch.JobId ?? "",
                JobTitle = "Job Title", // TODO: Fetch from job
                ServiceOrderId = dispatch.ServiceOrderId ?? "",
                ScheduledDate = dispatch.ScheduledDate ?? DateTime.MinValue,
                ScheduledStartTime = dispatch.ScheduledStartTime ?? TimeSpan.Zero,
                ScheduledEndTime = dispatch.ScheduledEndTime ?? TimeSpan.Zero,
                EstimatedDuration = dispatch.EstimatedDuration ?? 0,
                Status = dispatch.Status,
                Priority = dispatch.Priority
            };
        }

        private TechnicianLeaveDto MapLeaveToDto(TechnicianLeave leave)
        {
            return new TechnicianLeaveDto
            {
                Id = leave.Id,
                LeaveType = leave.LeaveType,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                Status = leave.Status
            };
        }

        private decimal CalculateTotalScheduledHours(List<Dispatch> dispatches)
        {
            decimal total = 0;
            foreach (var d in dispatches)
            {
                if (d.ScheduledStartTime.HasValue && d.ScheduledEndTime.HasValue)
                {
                    total += (decimal)(d.ScheduledEndTime.Value - d.ScheduledStartTime.Value).TotalHours;
                }
            }
            return total;
        }

        private decimal CalculateAvailableHours(
            List<TechnicianWorkingHours> workingHours,
            List<Dispatch> dispatches,
            DateTime startDate,
            DateTime endDate)
        {
            // Simplified calculation - would need to be more sophisticated in reality
            decimal totalWorkingHours = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dayOfWeek = (int)date.DayOfWeek;
                var wh = workingHours.FirstOrDefault(w => w.DayOfWeek == dayOfWeek);
                if (wh != null)
                {
                    totalWorkingHours += (decimal)(wh.EndTime - wh.StartTime).TotalHours;
                }
            }

            var scheduledHours = CalculateTotalScheduledHours(dispatches);
            return Math.Max(0, totalWorkingHours - scheduledHours);
        }

        // Implement remaining methods...
        public Task<BatchAssignResponseDto> BatchAssignAsync(BatchAssignDto dto, string userId)
        {
            throw new NotImplementedException();
        }

        public Task<List<TechnicianAvailabilityDto>> GetAvailableTechniciansAsync(DateTime date, TimeSpan startTime, TimeSpan endTime, List<string>? requiredSkills)
        {
            throw new NotImplementedException();
        }
    }
}
```

#### **6. Controller - PlanningController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Modules.Planning.DTOs;
using MyApi.Modules.Planning.Services;
using System.Security.Claims;

namespace MyApi.Modules.Planning.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/planning")]
    public class PlanningController : ControllerBase
    {
        private readonly IPlanningService _planningService;
        private readonly ILogger<PlanningController> _logger;

        public PlanningController(IPlanningService planningService, ILogger<PlanningController> logger)
        {
            _planningService = planningService;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        }

        [HttpGet("unassigned-jobs")]
        public async Task<IActionResult> GetUnassignedJobs(
            [FromQuery] string? priority = null,
            [FromQuery] string? required_skills = null,
            [FromQuery] string? service_order_id = null,
            [FromQuery] int page = 1,
            [FromQuery] int page_size = 20)
        {
            try
            {
                var skills = string.IsNullOrEmpty(required_skills)
                    ? null
                    : required_skills.Split(',').ToList();

                var result = await _planningService.GetUnassignedJobsAsync(
                    priority, skills, service_order_id, page, page_size);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unassigned jobs");
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = ex.Message } });
            }
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignJob([FromBody] AssignJobDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _planningService.AssignJobAsync(dto, userId);
                return Ok(new { success = true, data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = ex.Message } });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, error = new { code = "VALIDATION_ERROR", message = ex.Message } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning job");
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = ex.Message } });
            }
        }

        [HttpPost("batch-assign")]
        public async Task<IActionResult> BatchAssign([FromBody] BatchAssignDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _planningService.BatchAssignAsync(dto, userId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch assigning jobs");
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = ex.Message } });
            }
        }

        [HttpPost("validate-assignment")]
        public async Task<IActionResult> ValidateAssignment([FromBody] ValidateAssignmentDto dto)
        {
            try
            {
                var result = await _planningService.ValidateAssignmentAsync(dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating assignment");
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = ex.Message } });
            }
        }

        [HttpGet("technician-schedule/{technicianId}")]
        public async Task<IActionResult> GetTechnicianSchedule(
            string technicianId,
            [FromQuery] string start_date,
            [FromQuery] string end_date)
        {
            try
            {
                if (!DateTime.TryParse(start_date, out var startDate) ||
                    !DateTime.TryParse(end_date, out var endDate))
                {
                    return BadRequest(new { success = false, error = new { code = "INVALID_DATES", message = "Invalid date format" } });
                }

                var result = await _planningService.GetTechnicianScheduleAsync(technicianId, startDate, endDate);
                return Ok(new { success = true, data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = ex.Message } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching technician schedule");
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = ex.Message } });
            }
        }

        [HttpGet("available-technicians")]
        public async Task<IActionResult> GetAvailableTechnicians(
            [FromQuery] string date,
            [FromQuery] string start_time,
            [FromQuery] string end_time,
            [FromQuery] string? required_skills = null)
        {
            try
            {
                if (!DateTime.TryParse(date, out var parsedDate) ||
                    !TimeSpan.TryParse(start_time, out var startTimeSpan) ||
                    !TimeSpan.TryParse(end_time, out var endTimeSpan))
                {
                    return BadRequest(new { success = false, error = new { code = "INVALID_PARAMETERS", message = "Invalid date or time format" } });
                }

                var skills = string.IsNullOrEmpty(required_skills)
                    ? null
                    : required_skills.Split(',').ToList();

                var result = await _planningService.GetAvailableTechniciansAsync(
                    parsedDate, startTimeSpan, endTimeSpan, skills);

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available technicians");
                return StatusCode(500, new { success = false, error = new { code = "INTERNAL_ERROR", message = ex.Message } });
            }
        }
    }
}
```

---

## Frontend Integration

### 🎨 Update Frontend Service

Replace mock data in `src/modules/dispatcher/services/dispatcher.service.ts`:

```typescript
import axios from 'axios';
import type { Job, Technician, ServiceOrder } from '../types';

const API_BASE = '/api/v1';

export class DispatcherService {
  // Get unassigned jobs
  static async getUnassignedJobs(filters?: {
    priority?: string;
    requiredSkills?: string[];
    serviceOrderId?: string;
  }): Promise<Job[]> {
    const params = new URLSearchParams();
    if (filters?.priority) params.append('priority', filters.priority);
    if (filters?.requiredSkills) params.append('required_skills', filters.requiredSkills.join(','));
    if (filters?.serviceOrderId) params.append('service_order_id', filters.serviceOrderId);

    const response = await axios.get(`${API_BASE}/planning/unassigned-jobs?${params}`);
    return response.data.data.items;
  }

  // Get technicians (users with role=technician)
  static async getTechnicians(filters?: {
    status?: string;
    skills?: string[];
    availableOn?: Date;
  }): Promise<Technician[]> {
    const params = new URLSearchParams();
    if (filters?.status) params.append('status', filters.status);
    if (filters?.skills) params.append('skills', filters.skills.join(','));
    if (filters?.availableOn) params.append('available_on', filters.availableOn.toISOString());

    const response = await axios.get(`${API_BASE}/users/technicians?${params}`);
    return response.data.data.items;
  }

  // Assign job to technician
  static async assignJob(
    jobId: string,
    technicianId: string,
    scheduledStart: Date,
    scheduledEnd: Date
  ): Promise<void> {
    const scheduledDate = scheduledStart.toISOString().split('T')[0];
    const startTime = scheduledStart.toTimeString().split(' ')[0];
    const endTime = scheduledEnd.toTimeString().split(' ')[0];

    await axios.post(`${API_BASE}/planning/assign`, {
      jobId,
      technicianIds: [technicianId],
      scheduledDate,
      scheduledStartTime: startTime,
      scheduledEndTime: endTime,
      autoCreateDispatch: true
    });
  }

  // Get assigned jobs for technician on a specific date
  static async getAssignedJobs(technicianId: string, date: Date): Promise<Job[]> {
    const startDate = new Date(date);
    startDate.setHours(0, 0, 0, 0);
    const endDate = new Date(date);
    endDate.setHours(23, 59, 59, 999);

    const response = await axios.get(
      `${API_BASE}/planning/technician-schedule/${technicianId}`,
      {
        params: {
          start_date: startDate.toISOString().split('T')[0],
          end_date: endDate.toISOString().split('T')[0]
        }
      }
    );

    // Map dispatches to Job format
    return response.data.data.dispatches.map((d: any) => ({
      id: d.jobId,
      serviceOrderId: d.serviceOrderId,
      title: d.jobTitle,
      status: 'assigned',
      priority: d.priority,
      estimatedDuration: d.estimatedDuration,
      assignedTechnicianId: technicianId,
      scheduledStart: new Date(`${d.scheduledDate}T${d.scheduledStartTime}`),
      scheduledEnd: new Date(`${d.scheduledDate}T${d.scheduledEndTime}`),
      location: d.location,
      customerName: '',
      createdAt: new Date(),
      updatedAt: new Date()
    }));
  }

  // Validate assignment before committing
  static async validateAssignment(
    jobId: string,
    technicianIds: string[],
    scheduledDate: Date,
    scheduledStart: string,
    scheduledEnd: string
  ): Promise<{ isValid: boolean; warnings: string[]; conflicts: any[] }> {
    const response = await axios.post(`${API_BASE}/planning/validate-assignment`, {
      jobId,
      technicianIds,
      scheduledDate: scheduledDate.toISOString().split('T')[0],
      scheduledStartTime: scheduledStart,
      scheduledEndTime: scheduledEnd
    });

    return response.data.data;
  }

  // Unassign job
  static async unassignJob(jobId: string): Promise<void> {
    // Update job status back to unscheduled
    await axios.put(`${API_BASE}/service-orders/jobs/${jobId}/status`, {
      status: 'unscheduled'
    });
  }

  // Get service orders
  static getServiceOrders(): ServiceOrder[] {
    // TODO: Implement when service orders API is integrated
    return [];
  }

  // Legacy methods for compatibility (will be removed)
  static lockJob(jobId: string): Promise<void> {
    return Promise.resolve();
  }

  static resizeJob(jobId: string, newEnd: Date): Promise<void> {
    return Promise.resolve();
  }

  static setTechnicianMeta(technicianId: string, meta: Record<string, any>): void {
    // Store in localStorage temporarily
    localStorage.setItem(`tech_meta_${technicianId}`, JSON.stringify(meta));
  }

  static getTechnicianMeta(technicianId: string): Record<string, any> | null {
    const stored = localStorage.getItem(`tech_meta_${technicianId}`);
    return stored ? JSON.parse(stored) : null;
  }
}
```

---

## Implementation Roadmap

### Phase 1: Database & Models (Week 1)
1. ✅ Create migration for new tables:
   - `technician_working_hours`
   - `technician_leaves`
   - `technician_status_history`
   - `dispatch_history`
2. ✅ Alter `service_order_jobs` table with new columns
3. ✅ Create C# models for new entities
4. ✅ Update `ApplicationDbContext` with new DbSets
5. ✅ Test migration locally

### Phase 2: Core Planning Module (Week 2)
1. ✅ Create Planning module folder structure
2. ✅ Implement DTOs
3. ✅ Implement `IPlanningService` interface
4. ✅ Implement `PlanningService` (assign, validate)
5. ✅ Create `PlanningController`
6. ✅ Register services in `Program.cs`
7. ✅ Test endpoints with Postman/Swagger

### Phase 3: Technician Module Enhancement (Week 2-3)
1. ✅ Extend `UsersController` with technician-specific endpoints
2. ✅ Implement working hours management
3. ✅ Implement leave/unavailability management
4. ✅ Implement status tracking
5. ✅ Add technician availability calculation
6. ✅ Test all technician endpoints

### Phase 4: Schedule & Planning (Week 3)
1. ✅ Implement get unassigned jobs
2. ✅ Implement technician schedule retrieval
3. ✅ Implement available technicians lookup
4. ✅ Implement batch assignment
5. ✅ Add conflict detection
6. ✅ Test assignment workflow end-to-end

### Phase 5: Dispatch Enhancements (Week 4)
1. ✅ Implement reassign dispatch
2. ✅ Implement reschedule dispatch
3. ✅ Implement dispatch history tracking
4. ✅ Add dispatch status workflow validation
5. ✅ Test dispatch lifecycle

### Phase 6: Frontend Integration (Week 5)
1. ✅ Update `dispatcher.service.ts` to use real APIs
2. ✅ Remove all mock data
3. ✅ Implement error handling
4. ✅ Add loading states
5. ✅ Update components to handle API responses
6. ✅ Test drag-and-drop assignment with backend
7. ✅ Test calendar view with real data

### Phase 7: Advanced Features (Week 6)
1. ⭕ Route optimization (optional)
2. ⭕ Skills matching algorithm
3. ⭕ Workload balancing
4. ⭕ Multi-day scheduling
5. ⭕ Recurring dispatches

### Phase 8: Testing & Polish (Week 7)
1. ⭕ Unit tests for services
2. ⭕ Integration tests for APIs
3. ⭕ Frontend E2E tests
4. ⭕ Performance optimization
5. ⭕ Documentation
6. ⭕ User acceptance testing

---

## Success Criteria

### ✅ Functionality
- [ ] Dispatchers can view all unassigned jobs
- [ ] Dispatchers can assign jobs to technicians with schedule
- [ ] System validates assignments (conflicts, skills, availability)
- [ ] Technicians' schedules are accurately displayed
- [ ] Dispatches are created automatically from job assignments
- [ ] Dispatch status can be updated through lifecycle
- [ ] Technician working hours can be managed
- [ ] Leaves/unavailability can be tracked
- [ ] Assignment history is recorded

### ✅ Performance
- [ ] Unassigned jobs load in < 500ms
- [ ] Technician schedule loads in < 1s
- [ ] Assignment validation completes in < 300ms
- [ ] Supports 100+ concurrent dispatchers

### ✅ User Experience
- [ ] Drag-and-drop assignment is smooth
- [ ] Real-time conflict warnings
- [ ] Clear error messages
- [ ] Responsive on mobile devices
- [ ] Intuitive calendar interface

---

## Notes

**Technicians = Users with role="technician"**
- All technician data lives in the `Users` table
- Filter by `Role = "technician"` to get technicians
- Store additional technician-specific data in related tables (working hours, leaves, etc.)

**Key Integration Points:**
- Service Order Jobs → Assignments → Dispatches
- Users (Technicians) ← Working Hours, Leaves, Status
- Dispatches ← History tracking

**Critical for Success:**
- Proper conflict detection
- Accurate availability calculation
- Clean separation between job assignment and dispatch creation
- Comprehensive validation before committing assignments
