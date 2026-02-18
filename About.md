# FlowService – Complete Application Documentation

> **Last updated:** February 2026

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Technology Stack](#technology-stack)
4. [Business Pipeline](#business-pipeline)
5. [Frontend Modules](#frontend-modules)
6. [Backend Modules](#backend-modules)
7. [Authentication & Authorization](#authentication--authorization)
8. [AI & LLM Integration](#ai--llm-integration)
9. [Workflow Engine](#workflow-engine)
10. [Dispatcher & Field Operations](#dispatcher--field-operations)
11. [Communication & Email](#communication--email)
12. [Document Management](#document-management)
13. [Dynamic Forms](#dynamic-forms)
14. [Website Builder](#website-builder)
15. [Calendar & Scheduling](#calendar--scheduling)
16. [Inventory & Stock Management](#inventory--stock-management)
17. [Analytics & Reporting](#analytics--reporting)
18. [Settings & Administration](#settings--administration)
19. [Internationalization (i18n)](#internationalization-i18n)
20. [Database & Migrations](#database--migrations)
21. [Deployment](#deployment)
22. [Integrations Catalog](#integrations-catalog)

---

## Overview

**FlowService** is a full-stack, enterprise-grade field service management (FSM) and business operations platform. It covers the entire lifecycle of a service business — from generating offers and closing sales, to dispatching technicians, managing service orders, invoicing, and beyond.

The application is built as a modular, bilingual (English/French) SPA with a .NET backend, deployed on **Render** (backend) and **Lovable** (frontend), with a **Neon PostgreSQL** database.

---

## Architecture

```
┌─────────────────────────────────────────────────┐
│                   Frontend (React SPA)           │
│  Vite + React 18 + TypeScript + Tailwind CSS     │
│  Clerk Authentication │ i18next (EN/FR)          │
│  @tanstack/react-query │ react-router-dom v6     │
│  Recharts │ React Flow │ Framer Motion           │
├─────────────────────────────────────────────────┤
│                     API Layer                     │
│         Axios → .NET 8 Web API (REST + SignalR)  │
├─────────────────────────────────────────────────┤
│                   Backend (.NET 8)                │
│  ASP.NET Core │ Entity Framework (Npgsql)         │
│  JWT Auth │ SignalR Hubs │ Modular Architecture   │
│  Hosted on Render                                 │
├─────────────────────────────────────────────────┤
│                   Database                        │
│            Neon PostgreSQL (Serverless)            │
│  Tables: MainAdminUsers, Users, Contacts, Offers, │
│  Sales, ServiceOrders, Dispatches, Articles, etc. │
└─────────────────────────────────────────────────┘
```

### Key Architectural Decisions

- **Modular frontend**: Each feature is a self-contained module under `src/modules/` with its own components, pages, locale files, types, and services.
- **Modular backend**: Each domain lives under `FlowServiceBackendOnlyFinal-main/Modules/` with Controllers, Services, Models, and DTOs.
- **Two user types**: `MainAdminUsers` (always ID = 1, the business owner/admin) and `Users` (ID ≥ 1, employees/team members). Distinguished by a `UserType` discriminator in composite keys.
- **Tunisian fiscal compliance**: Currency is TND (Tunisian Dinar), with FiscalStamp logic built into the backend.

---

## Technology Stack

### Frontend
| Technology | Purpose |
|---|---|
| **React 18** | UI framework |
| **TypeScript** | Type safety |
| **Vite** | Build tool & dev server |
| **Tailwind CSS** | Utility-first styling with semantic design tokens |
| **shadcn/ui** | UI component library (Radix primitives) |
| **@tanstack/react-query** | Server state management & caching |
| **react-router-dom v6** | Client-side routing |
| **Clerk** | Authentication (SSO, social login, email/password) |
| **i18next + react-i18next** | Internationalization (EN/FR) |
| **Framer Motion** | Animations & transitions |
| **@xyflow/react (React Flow)** | Visual workflow editor (node-based) |
| **@dnd-kit** | Drag-and-drop (Kanban boards, sortable lists) |
| **Recharts** | Charts & data visualization |
| **react-big-calendar** | Calendar views |
| **Leaflet / Mapbox GL** | Maps for dispatching & field views |
| **react-data-grid** | High-performance data grids |
| **@react-pdf/renderer + pdf-lib** | PDF generation & manipulation |
| **xlsx** | Excel import/export |
| **Zod** | Schema validation |
| **react-hook-form** | Form management |
| **Sonner** | Toast notifications |
| **cmdk** | Command palette |
| **react-joyride** | Guided onboarding tours |
| **DOMPurify** | XSS sanitization |
| **Axios** | HTTP client |
| **SignalR** | Real-time communication (workflow engine) |
| **UploadThing** | File uploads |
| **dayjs / date-fns** | Date utilities |

### Backend
| Technology | Purpose |
|---|---|
| **.NET 8 (ASP.NET Core)** | Web API framework |
| **Entity Framework Core (Npgsql)** | ORM for PostgreSQL |
| **Neon PostgreSQL** | Serverless database |
| **JWT Bearer Auth** | API authentication |
| **SignalR** | Real-time WebSocket hubs (workflow updates) |
| **Render** | Cloud hosting & deployment |

---

## Business Pipeline

The core business flow follows a **linear pipeline**:

```
Offer → Sale → Service Order → Job → Dispatch
```

1. **Offer**: A quotation/proposal created for a client with line items, pricing, and terms.
2. **Sale**: A confirmed offer becomes a sale with invoicing and payment tracking.
3. **Service Order**: The operational work order generated from a sale, containing tasks and requirements.
4. **Job**: Individual work items within a service order, assignable to technicians.
5. **Dispatch**: Scheduling and routing technicians to job sites with real-time tracking.

---

## Frontend Modules

### Core Business Modules

| Module | Path | Description |
|---|---|---|
| **Offers** | `src/modules/offers/` | Create, edit, and manage quotations. Line items, pricing, PDF generation. |
| **Sales** | `src/modules/sales/` | Confirmed sales, invoicing, payment tracking. |
| **Deals** | `src/modules/deals/` | Deal pipeline with Kanban board view, deal stages, and tracking. |
| **Contacts** | `src/modules/contacts/` | Customer/client management, contact details, history. |
| **Projects** | `src/modules/projects/` | Project management with tasks, timelines, and resource allocation. |
| **Tasks** | `src/modules/tasks/` | Task management, assignment, status tracking. |

### Field Service Modules

| Module | Path | Description |
|---|---|---|
| **Dispatcher** | `src/modules/dispatcher/` | Dispatch board with map view, technician scheduling, route optimization. |
| **Field** | `src/modules/field/` | Mobile-friendly field technician interface. |
| **Scheduling** | `src/modules/scheduling/` | Resource scheduling, availability management. |
| **Skills** | `src/modules/skills/` | Technician skill management and matching. |

### Communication & Collaboration

| Module | Path | Description |
|---|---|---|
| **Communication** | `src/modules/communication/` | Internal messaging, notifications. |
| **Email & Calendar** | `src/modules/email-calendar/` | Email account integration (Gmail, Outlook, SMTP), calendar sync. |
| **Calendar** | `src/modules/calendar/` | Calendar views (day/week/month) with react-big-calendar. |
| **Notifications** | `src/modules/notifications/` | Push notifications, in-app alerts, notification center. |
| **Support** | `src/modules/support/` | Support ticket management and customer support. |

### Intelligence & Automation

| Module | Path | Description |
|---|---|---|
| **Workflow Engine** | `src/modules/workflow/` | Visual node-based workflow builder using React Flow. Drag-and-drop nodes, conditions, triggers, actions. |
| **AI Assistant** | `src/modules/ai-assistant/` | AI-powered assistant using OpenRouter LLMs for task creation, form generation, and intelligent suggestions. |
| **Automation** | `src/modules/automation/` | Business process automation rules and triggers. |
| **Analytics** | `src/modules/analytics/` | Dashboards, charts, KPIs, and reporting using Recharts. |

### Content & Configuration

| Module | Path | Description |
|---|---|---|
| **Documents** | `src/modules/documents/` | Document management, file uploads, PDF viewer. |
| **Dynamic Forms** | `src/modules/dynamic-forms/` | Form builder with public form sharing (accessible via `/form/:formId`). |
| **Articles** | `src/modules/articles/` | Knowledge base / article management. |
| **Website Builder** | `src/modules/website-builder/` | Built-in website/landing page builder (lazy-loaded, public pages at `/site/*`). |
| **Inventory & Services** | `src/modules/inventory-services/` | Product/service catalog management. |
| **Stock Management** | `src/modules/stock-management/` | Inventory tracking, low-stock alerts. |
| **Lookups** | `src/modules/lookups/` | System lookup values (statuses, categories, types). |

### System & Administration

| Module | Path | Description |
|---|---|---|
| **Settings** | `src/modules/settings/` | Application settings, integrations, user preferences. |
| **Users** | `src/modules/users/` | User management (create, edit, roles, permissions). |
| **Auth** | `src/modules/auth/` | Login pages (MainAdmin login + User login), SSO callback. |
| **Onboarding** | `src/modules/onboarding/` | Guided tour and onboarding wizard using react-joyride. |
| **Preferences** | `src/modules/preferences/` | User preference management (theme, language, defaults). |
| **Dashboard** | `src/modules/dashboard/` | Main dashboard with widgets, stats, and quick actions. |
| **System** | `src/modules/system/` | System-level configuration. |

---

## Backend Modules

Located under `FlowServiceBackendOnlyFinal-main/Modules/`:

| Module | Description |
|---|---|
| **Auth** | JWT authentication, login, token refresh, MainAdminUser vs User login. |
| **Users** | CRUD for Users table with role assignment. |
| **Roles** | Role-based access control (RBAC), permission management. |
| **Contacts** | Customer/client data management. |
| **Offers** | Offer creation, line items, status management, PDF generation. |
| **Sales** | Sale records, invoicing, payment status, fiscal stamp calculation (TND). |
| **ServiceOrders** | Service order lifecycle, task assignment, status tracking. |
| **Dispatches** | Technician dispatch, scheduling, route data. |
| **Installations** | Installation job management and tracking. |
| **Planning** | Resource planning and capacity management. |
| **Documents** | File storage, document metadata, attachments. |
| **Signatures** | Digital signature capture and storage. |
| **DynamicForms** | Form schema storage, public form submissions. |
| **Articles** | Knowledge base articles CRUD. |
| **Calendar** | Calendar events, scheduling data. |
| **EmailAccounts** | Email account configuration (Gmail OAuth, Outlook, SMTP). |
| **Notifications** | Notification creation, delivery, read status. |
| **Lookups** | System lookup values management. |
| **Skills** | Technician skill definitions and assignments. |
| **Projects** | Project data and task hierarchies. |
| **Preferences** | User/system preference storage. |
| **AiChat** | AI conversation history and context management. |
| **UserAiSettings** | Per-user AI API key management (OpenRouter keys with priority/fallback). |
| **WorkflowEngine** | Workflow execution engine with SignalR real-time updates. |
| **WebsiteBuilder** | Website/page content storage and publishing. |
| **Shared** | Common utilities, base classes, shared DTOs. |

---

## Authentication & Authorization

### Authentication Flow
- **Provider**: [Clerk](https://clerk.com) for frontend authentication.
- **Two login paths**:
  - `/login` — MainAdminUser login (business owner, always ID = 1).
  - `/user-login` — Regular User login (employees, ID ≥ 1).
  - `/sso-callback` — SSO/OAuth callback handler.
- **Backend**: JWT Bearer token authentication. Tokens include `UserId` and `UserType` claims.
- **Session management**: `AuthProvider` context wraps the app, `SessionExpiredBanner` for expired sessions.

### Authorization
- **Role-Based Access Control (RBAC)**: Managed via the Roles backend module.
- **User types**: `MainAdminUser` and `RegularUser` — composite key `(UserId, UserType)` ensures data isolation.

---

## AI & LLM Integration

### OpenRouter Integration
- **Multi-key management**: Users can add multiple OpenRouter API keys via Settings → Administration → Integrations.
- **Priority & fallback**: Keys are ordered by priority; automatic fallback when rate-limited.
- **Per-user isolation**: Keys stored with `(UserId, UserType)` composite key.
- **Free model support**: Free OpenRouter models work without a key (lower rate limits).

### AI Features
- **AI Task Creation** (`src/services/ai/aiTaskCreationService.ts`): AI-powered task generation.
- **AI Form Creation** (`src/services/ai/aiFormCreationService.ts`): Intelligent form building.
- **AI Intent Analyzer** (`src/services/aiIntentAnalyzer.ts`): Natural language intent understanding.
- **AI Chat** (Backend: `Modules/AiChat/`): Conversational AI with context history.

### Configured Models (from `src/config/models.json`)
- DeepSeek R1 (Chimera, Qwen3-8B variants)
- Qwen3 235B
- Meta LLaMA 3.3 70B, LLaMA 3.2 3B
- Google Gemma 3 27B
- Mistral Small 3.1 24B

All routed through `https://openrouter.ai/api/v1/chat/completions`.

---

## Workflow Engine

### Frontend (`src/modules/workflow/`)
- **Visual editor**: Node-based workflow builder using `@xyflow/react` (React Flow).
- **Drag-and-drop**: Create workflows by dragging trigger, condition, and action nodes.
- **Templates**: Pre-built workflow templates for common patterns.
- **Real-time updates**: SignalR connection for live workflow execution status.

### Backend (`Modules/WorkflowEngine/`)
- **Controllers**: REST API for workflow CRUD and execution.
- **Hubs**: SignalR hubs for real-time workflow status broadcasting.
- **Services**: Workflow execution engine, condition evaluation, action processing.
- **Models & DTOs**: Workflow definition, node types, execution history.

---

## Dispatcher & Field Operations

### Dispatcher Module (`src/modules/dispatcher/`)
- **Dispatch board**: Visual scheduling board for technician assignment.
- **Map view**: Interactive map using Leaflet/Mapbox GL showing job locations.
- **Technician tracking**: Real-time location and status updates.
- **Route management**: Job routing and optimization.

### Field Module (`src/modules/field/`)
- **Mobile-first**: Designed for technicians in the field.
- **Job details**: View assigned jobs, customer info, and requirements.
- **Status updates**: Update job status on-site.
- **Digital signatures**: Capture customer signatures on completion.

---

## Communication & Email

### Email Integration (`src/modules/email-calendar/`)
- **Gmail**: OAuth 2.0 integration for sending/receiving.
- **Outlook**: Microsoft OAuth integration.
- **Custom SMTP**: Configure any SMTP server for email.
- **Calendar sync**: Sync events with Google/Outlook calendars.
- **OAuth callback**: Dedicated callback page at `/oauth/callback`.

### Notifications (`src/modules/notifications/`)
- **In-app notifications**: Real-time notification center.
- **Low-stock alerts**: Automatic notifications when inventory is low (`lowStockNotificationService.ts`).
- **Activity logging**: Track user actions (`activityLogger.ts`).

---

## Document Management

- **File uploads**: UploadThing integration for file storage.
- **PDF generation**: `@react-pdf/renderer` and `pdf-lib` for creating and manipulating PDFs.
- **PDF viewer**: `pdfjs-dist` for in-browser PDF viewing.
- **Excel support**: `xlsx` for import/export of spreadsheet data.
- **Digital signatures**: Capture and store signatures via the backend Signatures module.
- **PDF settings**: Configurable PDF templates (`pdfSettingsApi.ts`).

---

## Dynamic Forms

### Frontend (`src/modules/dynamic-forms/`)
- **Form builder**: Visual form creation with field types, validation rules.
- **Public forms**: Shareable forms accessible at `/form/:formId` without authentication.
- **AI-powered creation**: Generate forms from natural language descriptions.

### Backend (`Modules/DynamicForms/`)
- **Schema storage**: JSON-based form schema persistence.
- **Submissions**: Public form submission collection and storage.

---

## Website Builder

- **Frontend**: `src/modules/website-builder/` — Visual page builder.
- **Public pages**: Lazy-loaded at `/site/*` routes.
- **Backend**: `Modules/WebsiteBuilder/` — Page content storage and publishing.

---

## Calendar & Scheduling

- **Calendar views**: Day, week, month views using `react-big-calendar`.
- **Event management**: Create, edit, delete events.
- **Resource scheduling**: `src/modules/scheduling/` — Manage technician availability.
- **Integration**: Sync with Gmail/Outlook calendars.

---

## Inventory & Stock Management

- **Service catalog**: `src/modules/inventory-services/` — Products and services.
- **Stock tracking**: `src/modules/stock-management/` — Inventory levels, locations.
- **Low-stock alerts**: Automatic notifications via `lowStockNotificationService.ts`.
- **Articles**: `src/modules/articles/` — Product/article management with backend CRUD.

---

## Analytics & Reporting

- **Dashboard**: `src/modules/dashboard/` — Main dashboard with KPI widgets.
- **Charts**: Recharts-based visualizations (bar, line, pie, area charts).
- **Analytics module**: `src/modules/analytics/` — Detailed reporting and data analysis.
- **Activity tracking**: `activityLogger.ts` — Log and analyze user activities.

---

## Settings & Administration

### Settings Page (`src/modules/settings/`)

Organized into two sections:

#### Personal Settings
- Profile management
- Preferences (theme, language, defaults)
- Email & calendar account connections

#### Administration Settings
- **Users**: User management (CRUD, role assignment)
- **Roles**: Role-based permissions configuration
- **System**: System-level configuration
- **Integrations**: Third-party service connections
  - **Email**: Gmail, Outlook, Custom SMTP
  - **AI & Language Models**: OpenRouter API key management

---

## Internationalization (i18n)

- **Languages**: English (EN) and French (FR).
- **Framework**: `i18next` + `react-i18next`.
- **Structure**: Each module has its own `locale/` folder with `en.json` and `fr.json`.
- **Global translations**: `src/locales/` for shared/common strings.
- **Strict rule**: No hardcoded English strings in UI components — all text must use translation keys.

---

## Database & Migrations

### Database: Neon PostgreSQL (Serverless)

### Key Tables
| Table | Description |
|---|---|
| `MainAdminUsers` | Business owner/admin (always ID = 1) |
| `Users` | Employee accounts (ID ≥ 1) |
| `Contacts` | Customers and clients |
| `Offers` | Quotations/proposals |
| `Sales` | Confirmed sales and invoices |
| `ServiceOrders` | Work orders |
| `Dispatches` | Technician dispatch records |
| `Articles` | Products/articles catalog |
| `UserAiKeys` | Per-user OpenRouter API keys (composite: UserId + UserType) |
| `UserAiPreferences` | Per-user AI preferences (composite: UserId + UserType) |
| `Roles` | Role definitions |
| `Skills` | Technician skills |
| `Projects` | Project records |
| `Documents` | Document metadata |
| `Signatures` | Digital signature data |
| `DynamicForms` | Form schemas |
| `Notifications` | Notification records |
| `Lookups` | System lookup values |
| `CalendarEvents` | Calendar entries |
| `EmailAccounts` | Connected email accounts |
| `WorkflowDefinitions` | Workflow schemas |

### Migration Files
Located at `FlowServiceBackendOnlyFinal-main/Neon/`:
- Sequential SQL migration files (e.g., `14_user_ai_settings.sql`)
- Full database schema: `FullDatabaseTable.sql`

### Fiscal Compliance
- **Currency**: TND (Tunisian Dinar)
- **FiscalStamp**: Automatic fiscal stamp calculation on invoices/sales per Tunisian tax regulations.

---

## Deployment

### Frontend
- **Platform**: Lovable
- **Preview URL**: `https://id-preview--a6a27350-0327-4820-8658-b3a4b7d1217d.lovable.app`
- **Published URL**: `https://simple-glee-app.lovable.app`
- **Build**: Vite production build

### Backend
- **Platform**: Render
- **Runtime**: .NET 8
- **Build command**: `dotnet publish -c Release -o out`
- **Start command**: `dotnet out/MyApi.dll`
- **Health check**: `/health`
- **API docs**: `/api-docs`
- **Dockerfile**: Available for containerized deployment

### Environment Variables (Backend)
| Variable | Description |
|---|---|
| `DATABASE_URL` | Neon PostgreSQL connection string |
| `PORT` | Server port (auto-set by Render) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `JWT_KEY` | Secret key for JWT signing |
| `JWT_ISSUER` | JWT issuer (e.g., `MyApi`) |
| `JWT_AUDIENCE` | JWT audience (e.g., `MyApiClients`) |

---

## Integrations Catalog

### Email Providers
| Provider | Type | Description |
|---|---|---|
| Gmail | OAuth 2.0 | Google Workspace email integration |
| Outlook | OAuth 2.0 | Microsoft 365 email integration |
| Custom SMTP | Credentials | Any SMTP server (SendGrid, Mailgun, etc.) |

### AI & Language Models
| Provider | Type | Description |
|---|---|---|
| OpenRouter | API Key | Multi-model LLM gateway with key rotation and fallback |

### File Storage
| Provider | Type | Description |
|---|---|---|
| UploadThing | API | File upload and storage service |

### Authentication
| Provider | Type | Description |
|---|---|---|
| Clerk | OAuth/SSO | User authentication and session management |

### Maps
| Provider | Type | Description |
|---|---|---|
| Leaflet | Open Source | Interactive maps for dispatch view |
| Mapbox GL | API Key | Premium map tiles and geocoding |

---

## Project Structure Summary

```
FlowService/
├── src/                          # Frontend (React SPA)
│   ├── modules/                  # Feature modules (35+ modules)
│   │   ├── offers/               # Quotation management
│   │   ├── sales/                # Sales & invoicing
│   │   ├── dispatcher/           # Technician dispatch
│   │   ├── workflow/             # Visual workflow builder
│   │   ├── ai-assistant/         # AI features
│   │   ├── settings/             # App settings & integrations
│   │   └── ...                   # 30+ more modules
│   ├── services/                 # API services & utilities
│   ├── components/ui/            # shadcn/ui components
│   ├── contexts/                 # React contexts (Auth, Preferences)
│   ├── hooks/                    # Custom React hooks
│   ├── config/                   # App configuration (AI models, etc.)
│   ├── i18n/                     # i18n setup
│   └── locales/                  # Global translations (EN/FR)
│
├── FlowServiceBackendOnlyFinal-main/   # Backend (.NET 8)
│   ├── Modules/                  # Domain modules (25+ modules)
│   │   ├── Auth/                 # Authentication
│   │   ├── Offers/               # Offer management
│   │   ├── Sales/                # Sales & fiscal
│   │   ├── WorkflowEngine/       # Workflow execution + SignalR
│   │   ├── UserAiSettings/       # AI key management
│   │   └── ...                   # 20+ more modules
│   ├── Data/                     # EF Core DbContext
│   ├── Migrations/               # EF Core migrations
│   ├── Neon/                     # SQL migration scripts
│   ├── Configuration/            # App configuration
│   └── Program.cs                # Application entry point
│
├── About.md                      # This file
└── public/                       # Static assets
```

---

*FlowService — Enterprise Field Service Management Platform*
