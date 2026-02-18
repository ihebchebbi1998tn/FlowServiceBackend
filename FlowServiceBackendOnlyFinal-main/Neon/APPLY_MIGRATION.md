# 🚀 APPLY MIGRATION NOW

## Required Migration: `13_complete_schema_setup.sql`

This migration must be run to fix all 500 errors in the API tests.

---

## Quick Steps

### Step 1: Open Neon Console
Go to: https://console.neon.tech/

### Step 2: Open SQL Editor
Navigate to your database → SQL Editor

### Step 3: Run Migration
Copy and paste the contents of `13_complete_schema_setup.sql` and execute.

### Step 4: Verify
The migration will output:
- List of all tables created
- Lookup item counts by type

### Step 5: Redeploy Backend
Trigger a new deployment on Render for your backend.

---

## What Gets Fixed

| Module | Before | After |
|--------|--------|-------|
| Contacts | 500 Error | ✅ Working |
| Contact Tags | 500 Error | ✅ Working |
| Articles | 500 Error | ✅ Working |
| Lookups | 500 Error | ✅ Working |
| Projects | 500 Error | ✅ Working |
| Installations | 500 Error | ✅ Working |
| Auth | Token too long | ✅ Fixed |

---

## Tables Created

- `Contacts`
- `ContactNotes`
- `ContactTags`
- `ContactTagAssignments`
- `Articles`
- `ArticleCategories`
- `Locations`
- `InventoryTransactions`
- `LookupItems` (with 40+ default items)
- `Currencies` (USD, EUR, GBP, CAD, AUD)
- `Installations`
- `MaintenanceHistory`

---

## Default Data Inserted

### Lookup Items (40+ items)
- Priorities: Low, Medium, High, Urgent
- Task Statuses: To Do, In Progress, Completed
- Project Statuses: Planning, Active, On Hold, Completed
- Project Types: Service, Sales, Internal
- Event Types: Meeting, Call, Task
- Article Categories: Electronics, Plumbing, HVAC, Tools
- Article Statuses: Active, Inactive, Discontinued
- Service Categories: Installation, Maintenance, Repair
- Countries: US, Canada, UK, Germany
- Technician Statuses: Available, Busy, Offline
- Leave Types: Annual, Sick, Personal
- Offer Statuses: Draft, Sent, Accepted, Rejected

### Currencies
- USD (default)
- EUR
- GBP
- CAD
- AUD
