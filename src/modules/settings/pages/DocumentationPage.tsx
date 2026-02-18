import { useState, lazy, Suspense } from "react";

// Lazy load the developer onboarding guide
const DeveloperOnboardingGuide = lazy(() => import("../components/DeveloperOnboardingGuide"));
const AiDocumentation = lazy(() => import("../components/AiDocumentation"));

// API Configuration
const SWAGGER_URL = import.meta.env.VITE_API_URL 
  ? `${import.meta.env.VITE_API_URL}/swagger`
  : 'https://api.flowentra.app/swagger';
import { useNavigate } from "react-router-dom";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import {
  ArrowLeft,
  Book,
  Code,
  Server,
  Globe,
  FileCode,
  Layers,
  Shield,
  Workflow,
  ExternalLink,
  Copy,
  Check,
  Terminal,
  Folder,
  GitBranch,
  Network,
  Lock,
  Users,
  Settings,
  Package,
  Languages,
  Zap,
  Database,
  RefreshCw,
  Key,
  Search,
  Filter,
  Upload,
  Download,
  Activity,
  AlertCircle,
  CheckCircle,
  Clock,
  Layers2,
  FileJson,
  Cog,
  AlertTriangle,
  Bug,
  XCircle,
  HelpCircle,
  Wrench,
  LifeBuoy,
  Info,
  Bot
} from "lucide-react";
import { toast } from "sonner";

type Language = 'en' | 'fr';

// Comprehensive documentation content
const documentationContent = {
  en: {
    title: "Technical Documentation",
    subtitle: "Complete System Architecture & API Reference",
    
    overview: {
      title: "System Overview",
      description: "FlowService is an enterprise-grade CRM and Field Service Management platform designed for managing customers, sales pipelines, service orders, technician dispatching, installations, and time/expense tracking.",
      
      techStack: {
        title: "Technology Stack",
        frontend: [
          { name: "React 18", version: "^18.3.1", description: "UI library with concurrent features and Suspense" },
          { name: "TypeScript", version: "Latest", description: "Type-safe JavaScript for better developer experience" },
          { name: "Vite", version: "Latest", description: "Next-generation build tool with HMR" },
          { name: "TailwindCSS", version: "Latest", description: "Utility-first CSS framework with custom design tokens" },
          { name: "Shadcn/ui", version: "Latest", description: "Accessible component library based on Radix UI" },
          { name: "TanStack Query", version: "^5.87.1", description: "Powerful async state management for API calls" },
          { name: "React Router", version: "^6.26.2", description: "Client-side routing with nested routes" },
          { name: "React Hook Form", version: "^7.53.0", description: "Performant form handling with Zod validation" },
          { name: "i18next", version: "^25.3.2", description: "Internationalization framework (FR/EN support)" },
          { name: "Recharts", version: "^2.12.7", description: "Data visualization library" },
          { name: "Lucide React", version: "^0.541.0", description: "Beautiful icon library" }
        ],
        backend: [
          { name: ".NET 8", version: "8.0", description: "High-performance cross-platform runtime" },
          { name: "ASP.NET Core", version: "8.0", description: "Web API framework with minimal APIs support" },
          { name: "Entity Framework Core", version: "8.0", description: "Modern ORM with LINQ support" },
          { name: "SQL Server", version: "2019+", description: "Enterprise relational database" },
          { name: "JWT Authentication", version: "Latest", description: "Secure token-based authentication" },
          { name: "BCrypt.Net", version: "Latest", description: "Secure password hashing algorithm" },
          { name: "Swagger/OpenAPI", version: "Latest", description: "API documentation and testing" },
          { name: "Serilog", version: "Latest", description: "Structured logging framework" }
        ]
      },
      
      architecture: {
        title: "System Architecture",
        content: `┌─────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   React     │  │  TanStack   │  │    React Router         │  │
│  │   18 + TS   │  │   Query     │  │    Protected Routes     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  Shadcn/ui  │  │ TailwindCSS │  │   i18next (FR/EN)       │  │
│  │  Components │  │   Styling   │  │   Internationalization  │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │ HTTP/REST
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        API LAYER                                 │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  ASP.NET    │  │    JWT      │  │      Swagger/OpenAPI    │  │
│  │  Core 8.0   │  │    Auth     │  │      Documentation      │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Controllers │  │  Services   │  │        DTOs             │  │
│  │  (REST)     │  │  (Logic)    │  │   (Data Transfer)       │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │ Entity Framework Core
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        DATA LAYER                                │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                    SQL Server Database                       │ │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐   │ │
│  │  │  Users   │ │ Contacts │ │  Sales   │ │ServiceOrders │   │ │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────┘   │ │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐   │ │
│  │  │  Roles   │ │  Offers  │ │Dispatches│ │ Installations│   │ │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────┘   │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘`
      }
    },
    
    frontend: {
      title: "Frontend Architecture",
      
      structure: {
        title: "Project Directory Structure",
        content: `src/
├── App.tsx                    # Root component with providers & routing
├── main.tsx                   # Application entry point
├── index.css                  # Global styles & Tailwind config
│
├── assets/                    # Static assets (images, fonts)
│
├── components/                # Shared UI components
│   ├── ui/                   # Shadcn/ui base components (40+ components)
│   │   ├── button.tsx        # Button variants (default, outline, ghost, etc.)
│   │   ├── card.tsx          # Card container component
│   │   ├── dialog.tsx        # Modal dialog component
│   │   ├── dropdown-menu.tsx # Dropdown menus
│   │   ├── form.tsx          # Form components with react-hook-form
│   │   ├── input.tsx         # Input field component
│   │   ├── select.tsx        # Select dropdown component
│   │   ├── table.tsx         # Table component
│   │   ├── tabs.tsx          # Tab navigation component
│   │   ├── toast.tsx         # Toast notifications
│   │   └── ...               # 30+ more UI primitives
│   │
│   ├── shared/               # Shared business components
│   │   ├── AppLoader.tsx     # Application loading screen
│   │   ├── TopProgressBar.tsx# Navigation progress indicator
│   │   └── ...
│   │
│   ├── navigation/           # Navigation components
│   │   ├── Sidebar.tsx       # Main navigation sidebar
│   │   └── Header.tsx        # Top header bar
│   │
│   └── permissions/          # Permission-based components
│       ├── PermissionRoute.tsx   # Route protection wrapper
│       └── PermissionButton.tsx  # Conditional action buttons
│
├── contexts/                  # React Context providers
│   ├── AuthContext.tsx       # Authentication state & methods
│   ├── PreferencesContext.tsx# User preferences state
│   └── LoadingContext.tsx    # Global loading state
│
├── hooks/                     # Custom React hooks
│   ├── usePermissions.tsx    # Permission checking hook (RBAC)
│   ├── useUserType.ts        # MainAdmin vs User detection
│   ├── useNotifications.ts   # Notification management
│   ├── usePreferences.ts     # User preferences hook
│   ├── useTheme.ts           # Theme switching hook
│   ├── useLogger.ts          # Application logging hook
│   ├── useErrorTracking.ts   # Error boundary tracking
│   ├── use-mobile.tsx        # Mobile detection hook
│   └── use-toast.ts          # Toast notification hook
│
├── services/                  # API service layer
│   ├── authService.ts        # Authentication API calls
│   └── api/                  # Module-specific API functions
│       ├── apiClient.ts      # Axios instance with interceptors
│       ├── contactsApi.ts    # Contacts CRUD operations
│       ├── salesApi.ts       # Sales CRUD operations
│       ├── offersApi.ts      # Offers CRUD operations
│       ├── serviceOrdersApi.ts   # Service orders operations
│       ├── dispatchesApi.ts  # Dispatch operations
│       ├── installationsApi.ts   # Installation operations
│       ├── usersApi.ts       # User management operations
│       ├── rolesApi.ts       # Role management operations
│       ├── permissionsApi.ts # Permission operations
│       ├── articlesApi.ts    # Articles/products operations
│       ├── lookupsApi.ts     # Lookup data operations
│       ├── notificationsApi.ts   # Notification operations
│       └── logsApi.ts        # System log operations
│
├── types/                     # TypeScript type definitions
│   ├── permissions.ts        # Permission module/action types
│   ├── contact.ts            # Contact entity types
│   ├── sale.ts               # Sale entity types
│   └── ...                   # Other entity types
│
├── utils/                     # Utility functions
│   ├── cn.ts                 # Class name utility (clsx + tailwind-merge)
│   └── formatters.ts         # Date/currency/number formatters
│
├── locales/                   # i18n translation files
│   ├── en.json               # English translations
│   └── fr.json               # French translations
│
└── modules/                   # Feature modules (28 modules)
    ├── dashboard/            # Main dashboard & layout
    ├── contacts/             # Contact management
    ├── sales/                # Sales pipeline
    ├── offers/               # Quotes & proposals
    ├── articles/             # Products & services catalog
    ├── calendar/             # Calendar & scheduling
    ├── projects/             # Project management
    ├── tasks/                # Task management
    ├── documents/            # Document management
    ├── workflow/             # Workflow automation
    ├── field/                # Field service modules
    │   ├── service-orders/   # Work order management
    │   ├── dispatches/       # Technician dispatching
    │   ├── installations/    # Installation tracking
    │   └── time-expenses/    # Time & expense tracking
    ├── dispatcher/           # Dispatcher dashboard
    ├── scheduling/           # Schedule management
    ├── settings/             # System settings
    ├── users/                # User management
    ├── lookups/              # Lookup configuration
    ├── notifications/        # Notification center
    ├── analytics/            # Analytics & reports
    ├── automation/           # Business automation
    ├── communication/        # Communication tools
    ├── inventory-services/   # Inventory management
    ├── skills/               # Skills management
    ├── support/              # Customer support
    ├── system/               # System administration
    ├── dynamic-forms/        # Custom form builder system
    └── testing/              # API testing tools`
      },
      
      modules: {
        title: "Feature Modules Detail",
        items: [
          {
            name: "Dashboard Module",
            path: "src/modules/dashboard/",
            description: "Main application layout with sidebar navigation, header, and content routing. Contains DashboardGate (lazy loading gate), DashboardContent (route definitions), and AppSidebar (navigation menu). Manages permission-based menu filtering.",
            files: ["Dashboard.tsx", "DashboardGate.tsx", "DashboardContent.tsx", "AppSidebar.tsx"]
          },
          {
            name: "Contacts Module",
            path: "src/modules/contacts/",
            description: "Complete CRM contact management with list view, grid view, detail panels, CRUD dialogs, search/filter functionality, bulk import via Excel, tag management, and activity tracking. Supports both company and individual contacts.",
            files: ["ContactsPage.tsx", "ContactsList.tsx", "ContactDialog.tsx", "ContactDetailPanel.tsx"]
          },
          {
            name: "Sales Module",
            path: "src/modules/sales/",
            description: "Sales pipeline management with stage-based workflow (Offer → Negotiation → Closed → Converted), priority levels, financial tracking (amount, taxes, discount), line item management, activity logging, and offer-to-sale conversion.",
            files: ["SalesPage.tsx", "SalesList.tsx", "SaleDialog.tsx", "SaleDetailSheet.tsx", "SaleItemsTable.tsx"]
          },
          {
            name: "Offers Module",
            path: "src/modules/offers/",
            description: "Quote and proposal management with status workflow (Draft → Sent → Accepted/Declined/Expired), line item editing, PDF generation, email sending, validity date tracking, and automatic conversion to sales.",
            files: ["OffersPage.tsx", "OffersList.tsx", "OfferDialog.tsx", "OfferItemsTable.tsx"]
          },
          {
            name: "Service Orders Module",
            path: "src/modules/field/service-orders/",
            description: "Field service work order management with job scheduling, technician assignment, status tracking (New → In Progress → Completed → Invoiced), cost tracking, parts/materials management, and integration with dispatches.",
            files: ["ServiceOrdersPage.tsx", "ServiceOrdersList.tsx", "ServiceOrderDialog.tsx", "ServiceOrderJobs.tsx"]
          },
          {
            name: "Dispatches Module",
            path: "src/modules/field/dispatches/",
            description: "Technician dispatch management with real-time status updates, time tracking (start/end times), photo uploads, signature capture, PDF work report generation, and mobile-optimized interface.",
            files: ["DispatchesPage.tsx", "DispatchesList.tsx", "DispatchDialog.tsx", "DispatchStatusBadge.tsx"]
          },
          {
            name: "Installations Module",
            path: "src/modules/field/installations/",
            description: "Equipment and site installation tracking with warranty management, customer association, location tracking, service history, and maintenance scheduling. Supports equipment model/serial tracking.",
            files: ["InstallationsPage.tsx", "InstallationsList.tsx", "InstallationDialog.tsx"]
          },
          {
            name: "Time & Expenses Module",
            path: "src/modules/field/time-expenses/",
            description: "Time tracking and expense management with calendar-based interface, hourly rate calculation, expense categories, receipt attachments, approval workflow, and billable/non-billable tracking.",
            files: ["TimeExpensesPage.tsx", "TimeEntryDialog.tsx", "ExpenseDialog.tsx", "TimeExpenseCalendar.tsx"]
          },
          {
            name: "Settings Module",
            path: "src/modules/settings/",
            description: "System configuration including user management, role/permission editing, system preferences, audit logs viewer, API testing tools, and documentation. Central hub for all administrative functions.",
            files: ["SettingsPage.tsx", "UsersSettingsPage.tsx", "RolesSettingsPage.tsx", "SystemSettingsPage.tsx"]
          },
          {
            name: "Lookups Module",
            path: "src/modules/lookups/",
            description: "Reference data configuration for dropdown values used throughout the application: priorities, service categories, article categories, locations, statuses, tags, and other configurable lists.",
            files: ["LookupsPage.tsx", "LookupsList.tsx", "LookupDialog.tsx"]
          },
          {
            name: "Dynamic Forms Module",
            path: "src/modules/dynamic-forms/",
            description: "Complete form builder system for creating custom forms, checklists, inspections, and surveys. Features drag-and-drop field arrangement, 15+ field types, conditional logic (show/hide fields based on values), multi-page stepped forms with progress indicator, bilingual support (EN/FR), PDF export with company branding, response tracking, public sharing without login, customizable thank-you pages, and full permission-based access control.",
            files: ["DynamicFormsPage.tsx", "CreateFormPage.tsx", "EditFormPage.tsx", "FormPreviewPage.tsx", "FormResponsesPage.tsx", "PublicFormPage.tsx", "FormBuilder/index.tsx", "SteppedFormPreview.tsx", "FormResponsePDF.tsx"]
          },
          {
            name: "Map & Location Features",
            path: "src/modules/dispatcher/ & src/components/shared/",
            description: "Interactive map views using Leaflet (OpenStreetMap) for visualizing jobs, technician locations, and installations. Color-coded markers by priority (urgent=red, in-progress=yellow, completed=green). Click-to-view details, automatic map centering, dark/light theme support, and responsive design for mobile.",
            files: ["DispatcherMapView.tsx", "MapView.tsx", "LeafletMapInner.tsx"]
          }
        ]
      },
      
      stateManagement: {
        title: "State Management Architecture",
        content: `The application uses a layered state management approach:

┌─────────────────────────────────────────────────────────────┐
│                    STATE MANAGEMENT LAYERS                   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │                 SERVER STATE (TanStack Query)           │ │
│  │  • All API data fetching, caching, and synchronization │ │
│  │  • Automatic background refetching (staleTime: 5min)   │ │
│  │  • Optimistic updates for instant UI feedback          │ │
│  │  • Query invalidation for data consistency             │ │
│  │  • Infinite queries for pagination                      │ │
│  │  • Mutations with onSuccess/onError callbacks          │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              GLOBAL STATE (React Context)               │ │
│  │  • AuthContext: User session, login/logout, token mgmt │ │
│  │  • PreferencesContext: Theme, language, view modes     │ │
│  │  • LoadingContext: Global loading states               │ │
│  │  • LookupsContext: Cached lookup data for dropdowns    │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │               LOCAL STATE (React Hooks)                 │ │
│  │  • useState: Component-local UI state                  │ │
│  │  • useReducer: Complex local state logic               │ │
│  │  • useMemo/useCallback: Performance optimization       │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │               FORM STATE (React Hook Form)              │ │
│  │  • useForm: Form state management                      │ │
│  │  • Zod schemas: Type-safe validation                   │ │
│  │  • Controlled/uncontrolled inputs                      │ │
│  │  • Submission states and error handling                │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
└─────────────────────────────────────────────────────────────┘

EXAMPLE QUERY USAGE:
─────────────────────
const { data, isLoading, error, refetch } = useQuery({
  queryKey: ['contacts', filters],
  queryFn: () => contactsApi.getAll(filters),
  staleTime: 5 * 60 * 1000,  // 5 minutes
});

const mutation = useMutation({
  mutationFn: contactsApi.create,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['contacts'] });
    toast.success('Contact created successfully');
  },
});`
      },
      
      permissions: {
        title: "Permission System (RBAC)",
        content: `The frontend implements a comprehensive Role-Based Access Control system:

┌─────────────────────────────────────────────────────────────┐
│                    PERMISSION MODULES                        │
├─────────────────────────────────────────────────────────────┤
│  CRM Modules:                                                │
│  • contacts      - Customer/contact management              │
│  • articles      - Products/services catalog                │
│  • offers        - Quotes and proposals                     │
│  • sales         - Sales pipeline management                │
│  • projects      - Project management                       │
│  • calendar      - Calendar and events                      │
│  • documents     - Document management                      │
│                                                              │
│  Field Service Modules:                                      │
│  • installations   - Equipment installations                │
│  • service_orders  - Work orders                            │
│  • dispatches      - Technician dispatches                  │
│  • dispatcher      - Dispatch dashboard access              │
│  • time_tracking   - Time entry management                  │
│  • expenses        - Expense tracking                       │
│                                                              │
│  System Modules:                                             │
│  • users         - User management                          │
│  • roles         - Role configuration                       │
│  • settings      - System settings access                   │
│  • audit_logs    - Audit trail viewing                      │
│  • lookups       - Lookup data configuration                │
│  • dynamic_forms - Custom forms & checklists                │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    PERMISSION ACTIONS                        │
├─────────────────────────────────────────────────────────────┤
│  Standard CRUD:                                              │
│  • create  - Create new records                             │
│  • read    - View/list records                              │
│  • update  - Edit existing records                          │
│  • delete  - Remove records                                 │
│                                                              │
│  Extended Actions:                                           │
│  • export  - Export data to Excel/CSV                       │
│  • import  - Bulk import data                               │
│  • assign  - Assign records to users                        │
│  • approve - Approval workflow actions                      │
└─────────────────────────────────────────────────────────────┘

IMPLEMENTATION DETAILS:
─────────────────────────
1. usePermissions Hook (src/hooks/usePermissions.tsx)
   - Fetches user permissions from API on login
   - Caches permissions in context
   - Provides hasPermission(module, action) function
   - Handles MainAdmin bypass (id=1 has all permissions)

2. PermissionRoute Component
   - Wraps protected routes
   - Checks read permission for module
   - Redirects to dashboard if unauthorized

3. Conditional UI Rendering
   - Sidebar filters menu items by read permission
   - Action buttons check create/update/delete
   - Export/import features check respective actions

USAGE EXAMPLE:
────────────────
const { hasPermission, isLoading } = usePermissions();

// Check single permission
if (hasPermission('contacts', 'create')) {
  // Show create button
}

// In JSX
{hasPermission('sales', 'delete') && (
  <Button onClick={handleDelete}>Delete</Button>
)}`
      },
      
      routing: {
        title: "Routing Architecture",
        content: `The application uses React Router v6 with nested routing:

┌─────────────────────────────────────────────────────────────┐
│                      ROUTE STRUCTURE                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  /                           → Login page (public)          │
│  /login                      → Admin login                  │
│  /user-login                 → Regular user login           │
│  /sso-callback               → SSO authentication callback  │
│  /onboarding                 → New user onboarding          │
│  /tests                      → API testing page (dev)       │
│                                                              │
│  /dashboard/*                → Protected dashboard routes   │
│    ├── /                     → Dashboard home               │
│    ├── /contacts             → Contact management           │
│    ├── /sales                → Sales pipeline               │
│    ├── /offers               → Offers management            │
│    ├── /calendar             → Calendar view                │
│    ├── /projects             → Project management           │
│    ├── /articles             → Articles catalog             │
│    ├── /service-orders       → Service orders               │
│    ├── /dispatches           → Dispatch management          │
│    ├── /installations        → Installation tracking        │
│    ├── /time-expenses        → Time & expenses              │
│    ├── /lookups              → Lookup configuration         │
│    ├── /settings/*           → Settings pages               │
│    │   ├── /users           → User management               │
│    │   ├── /roles           → Role management               │
│    │   ├── /logs            → System logs                   │
│    │   ├── /documentation   → This documentation            │
│    │   └── /dynamic-forms/* → Dynamic Forms builder         │
│    │       ├── /            → Forms list                    │
│    │       ├── /create      → Create new form               │
│    │       ├── /:id/edit    → Edit existing form            │
│    │       ├── /:id/preview → Preview & test form           │
│    │       └── /:id/responses → View form responses         │
│    └── /dispatcher           → Dispatcher dashboard         │
│                                                              │
│  /support/*                  → Customer support portal      │
│                                                              │
└─────────────────────────────────────────────────────────────┘

ROUTE PROTECTION:
─────────────────
<PermissionRoute module="contacts" action="read">
  <ContactsPage />
</PermissionRoute>`
      },
      
      apiClient: {
        title: "API Client Configuration",
        content: `The API client is built with Axios and includes:

┌─────────────────────────────────────────────────────────────┐
│                    API CLIENT FEATURES                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  BASE CONFIGURATION:                                         │
│  • Base URL from environment variable (VITE_API_URL)        │
│  • Default timeout: 30 seconds                              │
│  • Content-Type: application/json                           │
│                                                              │
│  REQUEST INTERCEPTORS:                                       │
│  • Automatic JWT token injection from storage               │
│  • Request logging (development mode)                       │
│                                                              │
│  RESPONSE INTERCEPTORS:                                      │
│  • Automatic token refresh on 401 errors                    │
│  • Session expiration handling (fires auth:session-expired) │
│  • Error response normalization                             │
│  • Network error handling                                   │
│                                                              │
│  TOKEN MANAGEMENT:                                           │
│  • Stored in localStorage (rememberMe) or sessionStorage    │
│  • Automatic refresh when token expires                     │
│  • Secure storage with encryption option                    │
│                                                              │
└─────────────────────────────────────────────────────────────┘

EXAMPLE API SERVICE:
────────────────────
// src/services/api/contactsApi.ts
export const contactsApi = {
  getAll: (params?: ContactSearchParams) => 
    apiClient.get<ContactListResponse>('/contacts', { params }),
    
  getById: (id: number) => 
    apiClient.get<ContactResponse>(\`/contacts/\${id}\`),
    
  create: (data: CreateContactDto) => 
    apiClient.post<ContactResponse>('/contacts', data),
    
  update: (id: number, data: UpdateContactDto) => 
    apiClient.put<ContactResponse>(\`/contacts/\${id}\`, data),
    
  delete: (id: number) => 
    apiClient.delete(\`/contacts/\${id}\`),
};`
      },

      // Deep Frontend Hooks Documentation
      hooks: {
        title: "React Hooks Deep Dive",
        items: [
          {
            name: "usePermissions",
            path: "src/hooks/usePermissions.tsx",
            description: "Core RBAC hook for permission checking throughout the application",
            code: `// Usage Example
const { 
  hasPermission,      // Check module:action permission
  canCreate,          // Shortcut for create permission
  canRead,            // Shortcut for read permission  
  canUpdate,          // Shortcut for update permission
  canDelete,          // Shortcut for delete permission
  canExport,          // Shortcut for export permission
  canImport,          // Shortcut for import permission
  isMainAdmin,        // Check if user is MainAdmin (id=1)
  isLoading,          // Permission loading state
  refetch             // Force refresh permissions
} = usePermissions();

// Check specific permission
if (hasPermission('contacts', 'create')) {
  // User can create contacts
}

// In JSX - conditional rendering
{canDelete('sales') && (
  <Button variant="destructive" onClick={handleDelete}>
    Delete Sale
  </Button>
)}

// MainAdmin bypass - always returns true for all permissions
// Users with id=1 are MainAdmin, id>=2 are regular users`,
            features: [
              "Fetches permissions from /api/permissions/user/{id}",
              "Caches permissions with TanStack Query (30s stale time)",
              "Polls every 30 seconds for permission updates",
              "Listens for cross-tab permission invalidation events",
              "MainAdmin (id=1) bypasses all permission checks",
              "Provides convenience methods: canCreate, canRead, etc."
            ]
          },
          {
            name: "useAuth (AuthContext)",
            path: "src/contexts/AuthContext.tsx",
            description: "Authentication state management and session handling",
            code: `// Usage Example
const {
  user,              // Current user data
  isAuthenticated,   // Login status boolean
  isMainAdmin,       // MainAdmin check
  isLoading,         // Auth initialization loading
  login,             // Admin login function
  userLogin,         // Regular user login function
  logout,            // Logout and clear session
  updateUser,        // Update user profile
  refreshUser        // Refresh user data from server
} = useAuth();

// Login example
const handleLogin = async () => {
  const result = await login(email, password, rememberMe);
  if (result.success) {
    navigate('/dashboard');
  } else {
    setError(result.message);
  }
};

// Logout
const handleLogout = async () => {
  await logout();
  navigate('/login');
};`,
            features: [
              "Manages JWT access and refresh tokens",
              "Auto-refreshes token before expiration",
              "Supports rememberMe (localStorage vs sessionStorage)",
              "Listens for session-expired events from API client",
              "Distinguishes MainAdmin (id=1) from regular users (id>=2)",
              "Provides login, logout, and signup functions"
            ]
          },
          {
            name: "usePreferences",
            path: "src/hooks/usePreferences.ts",
            description: "User preferences including theme, language, and view modes",
            code: `// Usage Example
const {
  preferences,       // Current preference values
  setTheme,          // Change theme (light/dark/system)
  setLanguage,       // Change language (en/fr)
  setPrimaryColor,   // Change primary color
  setViewMode,       // Set default view mode per module
  updatePreference,  // Update any preference
  isLoading
} = usePreferences();

// Change theme
setTheme('dark');

// Change language
setLanguage('fr');

// Set view preference for a module
setViewMode('contacts', 'grid');`,
            features: [
              "Persists preferences to backend for logged-in users",
              "Falls back to localStorage for guests",
              "Syncs across browser tabs",
              "Supports theme, language, colors, and view modes",
              "Auto-loads preferences on app initialization"
            ]
          },
          {
            name: "useNotifications",
            path: "src/hooks/useNotifications.ts",
            description: "Real-time notification management",
            code: `// Usage Example
const {
  notifications,     // List of notifications
  unreadCount,       // Number of unread
  markAsRead,        // Mark single as read
  markAllAsRead,     // Mark all as read
  deleteNotification,// Remove notification
  isLoading
} = useNotifications();

// Display unread count
<Badge>{unreadCount}</Badge>

// Mark as read when viewed
const handleOpen = (id) => {
  markAsRead(id);
};`,
            features: [
              "Fetches notifications from /api/notifications",
              "Tracks read/unread status",
              "Supports marking individual or all as read",
              "Auto-refetches on window focus"
            ]
          }
        ]
      },

      // Frontend Services Documentation
      services: {
        title: "API Services Layer",
        description: "All API communication is centralized in src/services/api/",
        items: [
          {
            name: "apiClient.ts",
            description: "Core fetch wrapper with authentication and error handling",
            exports: ["apiFetch<T>(endpoint, options)"],
            features: [
              "Automatic JWT token injection",
              "401 error handling with token refresh",
              "Request/response logging",
              "Standardized error format",
              "Session expiration detection"
            ]
          },
          {
            name: "contactsApi.ts",
            description: "Contact management operations",
            exports: ["getAll()", "getById(id)", "create(data)", "update(id, data)", "delete(id)", "search(term)", "exists(email)", "bulkImport(contacts)", "assignTag(contactId, tagId)", "removeTag(contactId, tagId)"]
          },
          {
            name: "salesApi.ts",
            description: "Sales pipeline operations",
            exports: ["getAll(params)", "getById(id)", "create(data)", "update(id, data)", "delete(id)", "addItem(saleId, item)", "createServiceOrder(saleId, data)", "getActivities(saleId)", "addActivity(saleId, activity)"]
          },
          {
            name: "offersApi.ts",
            description: "Quote/proposal operations",
            exports: ["getAll()", "getById(id)", "create(data)", "update(id, data)", "delete(id)", "send(id)", "addItem(offerId, item)", "convertToSale(offerId)"]
          },
          {
            name: "usersApi.ts",
            description: "User management operations",
            exports: ["getAll()", "getById(id)", "create(data)", "update(id, data)", "delete(id)", "changePassword(id, passwords)"]
          },
          {
            name: "rolesApi.ts",
            description: "Role and permission management",
            exports: ["getAll()", "getById(id)", "create(data)", "update(id, data)", "delete(id)", "getPermissions(roleId)", "updatePermissions(roleId, permissions)"]
          },
          {
            name: "permissionsApi.ts",
            description: "Permission checking operations",
            exports: ["getMyPermissions(userId)", "getUserPermissions(userId)", "checkPermission(userId, module, action)"]
          }
        ]
      },

      // Component Patterns
      patterns: {
        title: "Component Patterns",
        items: [
          {
            name: "List Page Pattern",
            description: "Standard pattern for list views with filtering, pagination, and CRUD",
            code: `// Example: ContactsPage.tsx
export function ContactsPage() {
  const [filters, setFilters] = useState<ContactSearchParams>({});
  const { hasPermission } = usePermissions();
  
  // Fetch data with TanStack Query
  const { data, isLoading, error } = useQuery({
    queryKey: ['contacts', filters],
    queryFn: () => contactsApi.getAll(filters),
  });
  
  // Mutations for CRUD
  const createMutation = useMutation({
    mutationFn: contactsApi.create,
    onSuccess: () => {
      queryClient.invalidateQueries(['contacts']);
      toast.success('Contact created');
    },
  });
  
  return (
    <div>
      {/* Search and Filters */}
      <SearchFilter onFilter={setFilters} />
      
      {/* List/Grid View */}
      <ContactsList 
        contacts={data?.contacts || []} 
        isLoading={isLoading}
      />
      
      {/* Create Button - Permission Checked */}
      {hasPermission('contacts', 'create') && (
        <CreateContactDialog onSubmit={createMutation.mutate} />
      )}
    </div>
  );
}`
          },
          {
            name: "Dialog/Modal Pattern",
            description: "Standard pattern for create/edit dialogs",
            code: `// Example: ContactDialog.tsx
export function ContactDialog({ 
  open, 
  onClose, 
  contact, // undefined = create, object = edit
  onSubmit 
}) {
  const form = useForm<ContactFormData>({
    resolver: zodResolver(contactSchema),
    defaultValues: contact || defaultValues,
  });
  
  const handleSubmit = async (data: ContactFormData) => {
    await onSubmit(data);
    onClose();
    form.reset();
  };
  
  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            {contact ? 'Edit Contact' : 'Create Contact'}
          </DialogTitle>
        </DialogHeader>
        
        <Form {...form}>
          <form onSubmit={form.handleSubmit(handleSubmit)}>
            <FormField
              control={form.control}
              name="firstName"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>First Name</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            {/* More fields... */}
            
            <DialogFooter>
              <Button type="button" variant="outline" onClick={onClose}>
                Cancel
              </Button>
              <Button type="submit">
                {contact ? 'Save' : 'Create'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}`
          },
          {
            name: "Permission Guard Pattern",
            description: "Protecting routes and UI elements based on permissions",
            code: `// Route Protection
<PermissionRoute module="sales" action="read">
  <SalesPage />
</PermissionRoute>

// Button Protection
{hasPermission('contacts', 'delete') && (
  <Button variant="destructive" onClick={handleDelete}>
    Delete
  </Button>
)}

// Component Wrapper
<PermissionGate module="offers" action="create" fallback={null}>
  <CreateOfferButton />
</PermissionGate>

// Menu Item Filtering (AppSidebar)
const visibleMenuItems = menuItems.filter(item => 
  hasPermission(item.module, 'read')
);`
          }
        ]
      }
    },

    // Business Workflow Documentation
    businessWorkflow: {
      title: "Business Workflow & Process Flow",
      subtitle: "Core business processes and data flow between modules",
      
      overview: {
        title: "End-to-End Business Process",
        description: "The system follows a structured workflow from initial contact to service delivery and invoicing.",
        diagram: `┌─────────────────────────────────────────────────────────────────────────────┐
│                        BUSINESS WORKFLOW OVERVIEW                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────────────────┐   │
│  │ CONTACT  │ → │  OFFER   │ → │   SALE   │ → │   SERVICE ORDER      │   │
│  │ (CRM)    │    │ (Quote)  │    │ (Order)  │    │   (Work Order)       │   │
│  └──────────┘    └──────────┘    └──────────┘    └──────────────────────┘   │
│       │              │              │                      │                │
│       │              │              │                      ▼                │
│       │              │              │              ┌──────────────┐         │
│       │              │              │              │     JOBS     │         │
│       │              │              │              │  (Per Task)  │         │
│       │              │              │              └──────────────┘         │
│       │              │              │                      │                │
│       │              │              │                      ▼                │
│       │              │              │              ┌──────────────┐         │
│       │              │              │              │  DISPATCHES  │         │
│       │              │              │              │  (Assigned)  │         │
│       │              │              │              └──────────────┘         │
│       │              │              │                      │                │
│       │              │              │                      ▼                │
│       │              │              │              ┌──────────────┐         │
│       │              │              │              │   TIME &     │         │
│       │              │              │              │  EXPENSES    │         │
│       │              │              │              └──────────────┘         │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

WORKFLOW STAGES:
────────────────
1. CONTACT → Create customer/prospect in CRM
2. OFFER   → Generate quote with items, taxes, discounts
3. SALE    → Convert accepted offer to sale order
4. SERVICE ORDER → Create work orders from sales with services
5. JOBS    → Individual tasks within service orders
6. DISPATCH → Assign jobs to technicians via planning board
7. TIME/EXPENSES → Track work time, materials, and expenses`
      },
      
      stages: [
        {
          name: "1. Contacts → Offers",
          description: "Sales representatives create offers for contacts",
          details: [
            "Contact must exist before creating an offer",
            "Offer includes line items (articles/services)",
            "Financial fields: amount, taxes, discount, total",
            "Status workflow: Draft → Sent → Accepted/Declined/Expired"
          ],
          code: `// Create offer linked to contact
const offer = {
  contactId: 123,
  title: "HVAC Installation Quote",
  items: [
    { articleId: 1, quantity: 2, unitPrice: 500 },
    { articleId: 2, quantity: 1, unitPrice: 1500 }
  ],
  taxes: 150.00,
  discount: 100.00,
  validUntil: "2024-12-31"
};`
        },
        {
          name: "2. Offers → Sales",
          description: "Accepted offers are converted to sales orders",
          details: [
            "Only 'Accepted' offers can be converted",
            "All financial data transfers: items, taxes, discount, total",
            "Sale created with status 'New'",
            "Offer marked as 'Converted' after successful conversion",
            "Backend endpoint: POST /api/sales/from-offer/{offerId}"
          ],
          code: `// Backend conversion logic (SaleService.cs)
public async Task<Sale> CreateSaleFromOfferAsync(int offerId)
{
    var offer = await GetOfferAsync(offerId);
    
    var sale = new Sale {
        ContactId = offer.ContactId,
        Title = offer.Title,
        Amount = offer.Amount,
        Taxes = offer.Taxes ?? 0,        // ← Taxes transferred
        Discount = offer.Discount ?? 0,  // ← Discount transferred
        TotalAmount = offer.TotalAmount, // ← Total calculated
        Status = "new"
    };
    
    // Copy all line items
    foreach (var item in offer.Items) {
        sale.Items.Add(new SaleItem {
            ArticleId = item.ArticleId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        });
    }
    
    return sale;
}`
        },
        {
          name: "3. Sales → Service Orders",
          description: "Sales with services generate service orders",
          details: [
            "Only sales containing service articles generate service orders",
            "Service order inherits contact and financial data",
            "Multiple jobs can be created per service order",
            "Status workflow: New → In Progress → Completed → Invoiced"
          ],
          code: `// Create service order from sale
POST /api/sales/{saleId}/service-order
{
  "description": "Installation work",
  "scheduledDate": "2024-02-15",
  "priority": "high"
}`
        },
        {
          name: "4. Service Orders → Jobs → Dispatches",
          description: "Jobs are scheduled and assigned to technicians",
          details: [
            "Jobs represent individual tasks within a service order",
            "Planning board shows unassigned jobs",
            "Drag-drop assignment creates dispatches",
            "Dispatches track time, expenses, materials, and photos"
          ],
          code: `// Dispatch assignment
POST /api/dispatches
{
  "jobId": 456,
  "userId": 789,        // Technician
  "scheduledDate": "2024-02-15",
  "scheduledStartTime": "09:00",
  "scheduledEndTime": "12:00"
}`
        }
      ]
    },

    // Financial Calculations Documentation
    financialCalculations: {
      title: "Financial Calculations & Logic",
      subtitle: "How amounts, taxes, discounts, and totals are calculated",
      
      overview: {
        title: "Financial Fields Overview",
        description: "Financial calculations are consistent across Offers, Sales, and Service Orders.",
        fields: [
          { name: "amount / subtotal", type: "decimal", description: "Sum of all line items (quantity × unitPrice)" },
          { name: "taxes", type: "decimal", description: "Tax amount (can be fixed or calculated)" },
          { name: "discount", type: "decimal", description: "Discount/remise amount to subtract" },
          { name: "totalAmount", type: "decimal", description: "Final total = amount + taxes - discount" }
        ]
      },
      
      calculations: {
        title: "Calculation Logic",
        formula: `┌─────────────────────────────────────────────────────────────┐
│                    TOTAL CALCULATION FORMULA                  │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   SUBTOTAL = Σ (item.quantity × item.unitPrice)             │
│                                                              │
│   TOTAL = SUBTOTAL + TAXES - DISCOUNT                        │
│                                                              │
│   Example:                                                   │
│   ─────────────────────────────────────                      │
│   Item 1: 2 × $500 = $1,000                                  │
│   Item 2: 1 × $1,500 = $1,500                                │
│   ─────────────────────────────────────                      │
│   Subtotal:           $2,500                                 │
│   Taxes (15%):        +  $375                                │
│   Discount:           -  $100                                │
│   ─────────────────────────────────────                      │
│   TOTAL:              $2,775                                 │
│                                                              │
└─────────────────────────────────────────────────────────────┘`,
        frontendCode: `// Frontend calculation (AddOffer.tsx / SaleDialog.tsx)
const calculateSubtotal = () => {
  return items.reduce((sum, item) => {
    return sum + (item.quantity * item.unitPrice);
  }, 0);
};

const calculateTotal = () => {
  const subtotal = calculateSubtotal();
  const taxes = formData.taxes || 0;
  const discount = formData.discount || 0;
  return subtotal + taxes - discount;
};`,
        backendCode: `// Backend: Database generated column (PostgreSQL)
CREATE TABLE Offers (
  ...
  "amount" DECIMAL(15,2),
  "taxes" DECIMAL(15,2) DEFAULT 0,
  "discount" DECIMAL(15,2) DEFAULT 0,
  "total_amount" DECIMAL(15,2) GENERATED ALWAYS AS (
    "amount" + COALESCE("taxes", 0) - COALESCE("discount", 0)
  ) STORED
);`,
        pdfCode: `// PDF Generation (OfferPDFDocument.tsx)
const FinancialSummary = ({ offer }) => {
  const subtotal = offer.amount || 0;
  const taxes = offer.taxes || 0;
  const discount = offer.discount || 0;
  const total = offer.totalAmount || (subtotal + taxes - discount);
  
  return (
    <View>
      <Text>{t('offers.itemsSubtotal')}: {formatCurrency(subtotal)}</Text>
      <Text>{t('offers.taxes')}: {formatCurrency(taxes)}</Text>
      <Text>{t('offers.discount')}: -{formatCurrency(discount)}</Text>
      <Text style={styles.totalBold}>{t('offers.total')}: {formatCurrency(total)}</Text>
    </View>
  );
};`
      },
      
      dataFlow: {
        title: "Financial Data Flow Between Modules",
        description: "Financial data is preserved when converting between modules",
        diagram: `OFFER                    SALE                     SERVICE ORDER
┌────────────────┐       ┌────────────────┐       ┌────────────────┐
│ amount: 2500   │ ────▶ │ amount: 2500   │ ────▶ │ amount: 2500   │
│ taxes: 375     │       │ taxes: 375     │       │ taxes: 375     │
│ discount: 100  │       │ discount: 100  │       │ discount: 100  │
│ total: 2775    │       │ total: 2775    │       │ total: 2775    │
│ items: [...]   │       │ items: [...]   │       │ items: [...]   │
└────────────────┘       └────────────────┘       └────────────────┘

✓ All financial fields are copied during conversion
✓ Items/line items are duplicated to new entity
✓ Totals are recalculated if items change after conversion`
      }
    },

    // Recent Features Documentation
    recentFeatures: {
      title: "Recently Added Features",
      subtitle: "New functionality and enhancements added to the system",
      
      features: [
        {
          name: "Taxes & Discount in Offers/Sales",
          date: "2024",
          description: "Full support for taxes and discount fields across the sales pipeline",
          details: [
            "Taxes field added to Offers and Sales forms",
            "Discount/Remise field for price reductions",
            "Automatic total calculation: subtotal + taxes - discount",
            "Values transferred during offer-to-sale conversion",
            "Included in PDF document generation",
            "Fully localized labels (EN: Taxes/Discount, FR: Taxes/Remise)"
          ],
          modules: ["offers", "sales"]
        },
        {
          name: "Dispatch Time & Expenses Tracking",
          date: "2024",
          description: "Comprehensive time entry and expense tracking for dispatches",
          details: [
            "Multiple work types: Travel, Setup, Work, Documentation, Cleanup",
            "Start/end time tracking with duration calculation",
            "Expense categories: Travel, Equipment, Materials, Meals, Other",
            "Photo attachments for receipts and work evidence",
            "Real-time total calculations for billable hours"
          ],
          modules: ["dispatches", "time-expenses"]
        },
        {
          name: "Dispatcher Planning Board",
          date: "2024",
          description: "Visual drag-and-drop scheduling interface for job assignment",
          details: [
            "Calendar view with user rows (technicians)",
            "Unassigned jobs panel with filtering",
            "Drag-and-drop job assignment to time slots",
            "Color-coded job status and priority",
            "Conflict detection for overlapping assignments",
            "Quick filters by skills, priority, and date range"
          ],
          modules: ["dispatcher", "planning"]
        },
        {
          name: "PDF Document Generation",
          date: "2024",
          description: "Professional PDF documents for offers, sales, and work reports",
          details: [
            "Offer/Quote PDF with company branding",
            "Sale order PDF with line items and totals",
            "Dispatch work report with time entries and photos",
            "Bilingual support (English and French)",
            "Download and email functionality"
          ],
          modules: ["offers", "sales", "dispatches"]
        },
        {
          name: "Bilingual Localization (EN/FR)",
          date: "2024",
          description: "Complete internationalization support for English and French",
          details: [
            "Module-specific translation namespaces",
            "All UI text localized (labels, buttons, messages)",
            "Toast notifications in user's language",
            "PDF documents respect language preference",
            "Date/currency formatting by locale"
          ],
          modules: ["system-wide"]
        },
        {
          name: "Installation Management",
          date: "2024",
          description: "Equipment installation tracking with warranty and maintenance",
          details: [
            "Equipment model and serial number tracking",
            "Warranty start/end date management",
            "Associated contact and location",
            "Service history timeline",
            "Maintenance scheduling"
          ],
          modules: ["installations"]
        },
        {
          name: "Role-Based Access Control (RBAC)",
          date: "2024",
          description: "Granular permission system for all modules and actions",
          details: [
            "Module-level permissions (contacts, sales, etc.)",
            "Action-level control (create, read, update, delete, export)",
            "MainAdmin bypass for super administrators",
            "Dynamic sidebar menu filtering",
            "Protected routes and UI elements"
          ],
          modules: ["roles", "permissions"]
        }
      ]
    },

    // Localization Documentation
    localization: {
      title: "Localization & Internationalization",
      subtitle: "Multi-language support with react-i18next",
      
      overview: {
        title: "i18n Architecture",
        description: "The application uses react-i18next with module-specific namespaces for organized translations.",
        structure: `src/
├── locales/                    # Global translations
│   ├── en.json                # English - shared terms
│   └── fr.json                # French - shared terms
│
└── modules/
    ├── offers/
    │   └── locale/
    │       ├── en.json        # English - offers module
    │       └── fr.json        # French - offers module
    ├── sales/
    │   └── locale/
    │       ├── en.json        # English - sales module
    │       └── fr.json        # French - sales module
    └── .../                    # Other modules follow same pattern`
      },
      
      namespaces: {
        title: "Translation Namespaces",
        items: [
          { namespace: "translation", description: "Shared/common terms (search, save, cancel, etc.)" },
          { namespace: "offers", description: "Offer-specific terms (quote, validity, send offer, etc.)" },
          { namespace: "sales", description: "Sales-specific terms (pipeline, stage, convert, etc.)" },
          { namespace: "contacts", description: "Contact-specific terms (customer, company, phone, etc.)" },
          { namespace: "dispatches", description: "Dispatch-specific terms (assign, schedule, technician, etc.)" },
          { namespace: "installations", description: "Installation-specific terms (equipment, warranty, serial, etc.)" },
          { namespace: "users", description: "User management terms (role, permission, active, etc.)" },
          { namespace: "lookups", description: "Lookup configuration terms (priority, status, category, etc.)" }
        ]
      },
      
      usage: {
        title: "Usage Examples",
        code: `// Basic usage with useTranslation hook
import { useTranslation } from 'react-i18next';

function OfferForm() {
  const { t } = useTranslation('offers');  // Use offers namespace
  
  return (
    <div>
      <label>{t('offers.taxes')}</label>           {/* "Taxes" / "Taxes" */}
      <label>{t('offers.discount')}</label>        {/* "Discount" / "Remise" */}
      <label>{t('offers.total')}</label>           {/* "Total" / "Total" */}
      <button>{t('offers.sendOffer')}</button>     {/* "Send Offer" / "Envoyer l'offre" */}
    </div>
  );
}

// Multiple namespaces
const { t } = useTranslation(['offers', 'translation']);
t('offers.taxes');           // From offers namespace
t('translation:search');     // From translation namespace

// With interpolation
t('offers.validUntil', { date: '2024-12-31' });
// "Valid until {{date}}" → "Valid until 2024-12-31"

// Toast notifications - also localized
toast.success(t('offers.offerCreated'));
toast.error(t('offers.failedToCreate'));`
      },
      
      addingTranslations: {
        title: "Adding New Translations",
        steps: [
          "1. Identify the module (offers, sales, contacts, etc.)",
          "2. Add key to both en.json and fr.json in module's locale folder",
          "3. Use consistent naming: moduleName.keyName (e.g., offers.taxes)",
          "4. Use the translation in component with useTranslation hook",
          "5. Test both languages to verify translations appear correctly"
        ],
        example: `// Step 1-2: Add to src/modules/offers/locale/en.json
{
  "offers": {
    "taxes": "Taxes",
    "discount": "Discount",
    "newFeature": "New Feature Label"
  }
}

// Step 1-2: Add to src/modules/offers/locale/fr.json
{
  "offers": {
    "taxes": "Taxes",
    "discount": "Remise",
    "newFeature": "Nouvelle fonctionnalité"
  }
}

// Step 3-4: Use in component
const { t } = useTranslation('offers');
<label>{t('offers.newFeature')}</label>`
      }
    },
    
    backend: {
      title: "Backend Architecture (.NET 8)",
      
      structure: {
        title: "Backend Project Structure",
        content: `FlowServiceBackend/
├── Program.cs                    # Application entry point & DI setup
├── appsettings.json              # Configuration settings
├── appsettings.Development.json  # Development overrides
│
├── Modules/                       # Feature modules (Clean Architecture)
│   ├── Auth/                     # Authentication module
│   │   ├── Controllers/         
│   │   │   └── AuthController.cs # Login/logout/refresh endpoints
│   │   ├── Services/            
│   │   │   ├── IAuthService.cs  # Service interface
│   │   │   └── AuthService.cs   # JWT generation, password validation
│   │   ├── DTOs/                
│   │   │   ├── LoginRequestDto.cs
│   │   │   └── AuthResponseDto.cs
│   │   └── Models/              
│   │       └── RefreshToken.cs
│   │
│   ├── Users/                    # User management module
│   │   ├── Controllers/         
│   │   │   └── UsersController.cs
│   │   ├── Services/            
│   │   │   └── UserService.cs
│   │   ├── DTOs/
│   │   └── Models/
│   │       ├── User.cs           # Regular staff users
│   │       └── MainAdminUser.cs  # System administrators
│   │
│   ├── Roles/                    # Role & permission management
│   │   ├── Controllers/         
│   │   │   └── RolesController.cs
│   │   ├── Services/
│   │   ├── DTOs/
│   │   └── Models/
│   │       ├── Role.cs
│   │       └── RolePermission.cs
│   │
│   ├── Contacts/                 # Contact management
│   │   ├── Controllers/         
│   │   │   └── ContactsController.cs
│   │   ├── Services/            
│   │   │   ├── IContactService.cs
│   │   │   └── ContactService.cs
│   │   ├── DTOs/
│   │   │   ├── ContactResponseDto.cs
│   │   │   ├── CreateContactRequestDto.cs
│   │   │   └── ContactSearchRequestDto.cs
│   │   └── Models/
│   │       ├── Contact.cs
│   │       └── ContactTag.cs
│   │
│   ├── Sales/                    # Sales management
│   │   ├── Controllers/
│   │   │   └── SalesController.cs
│   │   ├── Services/
│   │   │   ├── ISaleService.cs
│   │   │   └── SaleService.cs
│   │   ├── DTOs/
│   │   │   ├── SaleDto.cs
│   │   │   ├── CreateSaleDto.cs
│   │   │   └── SaleItemDto.cs
│   │   └── Models/
│   │       ├── Sale.cs
│   │       ├── SaleItem.cs
│   │       └── SaleActivity.cs
│   │
│   ├── Offers/                   # Offer/quote management
│   ├── ServiceOrders/            # Service order management
│   ├── Dispatches/               # Dispatch management
│   ├── Installations/            # Installation tracking
│   ├── Articles/                 # Product catalog
│   ├── Calendar/                 # Calendar events
│   ├── Projects/                 # Project management
│   ├── Lookups/                  # Reference data
│   ├── Preferences/              # User preferences
│   ├── Notifications/            # Notification system
│   ├── Skills/                   # Skills management
│   ├── Planning/                 # Resource planning
│   │
│   └── Shared/                   # Shared services
│       └── Services/
│           ├── ISystemLogService.cs
│           └── SystemLogService.cs  # Audit logging
│
├── Data/                          # Data access layer
│   ├── ApplicationDbContext.cs   # EF Core DbContext
│   └── EntityConfigurations/     # Fluent API configurations
│
├── Configuration/                 # App configuration
│   ├── JwtSettings.cs
│   └── CorsSettings.cs
│
├── Migrations/                    # EF Core migrations
│
└── Database/                      # SQL scripts
    ├── complete_database_recreation.sql
    └── migration_summary.md`
      },
      
      api: {
        title: "RESTful API Design",
        content: `The API follows RESTful conventions with consistent patterns:

┌─────────────────────────────────────────────────────────────┐
│                    API CONVENTIONS                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  BASE URL:  /api/{resource}                                  │
│                                                              │
│  STANDARD ENDPOINTS:                                         │
│  GET    /api/contacts           → List with pagination      │
│  GET    /api/contacts/{id}      → Get single record         │
│  POST   /api/contacts           → Create new record         │
│  PUT    /api/contacts/{id}      → Full update               │
│  PATCH  /api/contacts/{id}      → Partial update            │
│  DELETE /api/contacts/{id}      → Delete record             │
│                                                              │
│  NESTED RESOURCES:                                           │
│  GET    /api/sales/{id}/items        → Get sale items       │
│  POST   /api/sales/{id}/items        → Add item to sale     │
│  PATCH  /api/sales/{id}/items/{itemId}  → Update item       │
│  DELETE /api/sales/{id}/items/{itemId}  → Delete item       │
│                                                              │
│  SPECIAL ACTIONS:                                            │
│  POST   /api/sales/from-offer/{offerId} → Convert offer     │
│  GET    /api/contacts/search?q=...      → Search            │
│  POST   /api/contacts/import            → Bulk import       │
│                                                              │
└─────────────────────────────────────────────────────────────┘

RESPONSE FORMAT:
────────────────
// Success Response
{
  "success": true,
  "data": { ... },
  "message": "Operation successful"
}

// Error Response
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid request data",
    "details": { ... }
  }
}

// Paginated Response
{
  "success": true,
  "data": {
    "items": [...],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}

AUTHENTICATION:
───────────────
All protected endpoints require JWT Bearer token:
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

Token contains claims:
• sub (user ID)
• email
• name
• role
• exp (expiration timestamp)`
      },
      
      endpoints: {
        title: "Complete API Endpoints Reference",
        categories: [
          {
            name: "Authentication (/api/auth)",
            icon: "Key",
            endpoints: [
              { method: "POST", path: "/api/auth/admin/login", description: "Admin login with email/password → returns JWT + user data" },
              { method: "POST", path: "/api/auth/login", description: "Regular user login → returns JWT + user data + permissions" },
              { method: "POST", path: "/api/auth/refresh", description: "Refresh expired access token using refresh token" },
              { method: "POST", path: "/api/auth/logout", description: "Invalidate current session and refresh token" },
              { method: "GET", path: "/api/auth/me", description: "Get current authenticated user profile" }
            ]
          },
          {
            name: "Users (/api/users)",
            icon: "Users",
            endpoints: [
              { method: "GET", path: "/api/users", description: "List all users with pagination and filters" },
              { method: "GET", path: "/api/users/{id}", description: "Get user by ID with role information" },
              { method: "POST", path: "/api/users", description: "Create new user with role assignment" },
              { method: "PUT", path: "/api/users/{id}", description: "Update user details" },
              { method: "DELETE", path: "/api/users/{id}", description: "Deactivate/delete user" },
              { method: "POST", path: "/api/users/{id}/change-password", description: "Change user password" },
              { method: "GET", path: "/api/users/{id}/permissions", description: "Get user's effective permissions" }
            ]
          },
          {
            name: "Roles (/api/roles)",
            icon: "Shield",
            endpoints: [
              { method: "GET", path: "/api/roles", description: "List all roles with permission counts" },
              { method: "GET", path: "/api/roles/{id}", description: "Get role with full permission list" },
              { method: "POST", path: "/api/roles", description: "Create new role with permissions" },
              { method: "PUT", path: "/api/roles/{id}", description: "Update role name and permissions" },
              { method: "DELETE", path: "/api/roles/{id}", description: "Delete role (fails if users assigned)" }
            ]
          },
          {
            name: "Contacts (/api/contacts)",
            icon: "Users",
            endpoints: [
              { method: "GET", path: "/api/contacts", description: "List contacts with search, filter, pagination" },
              { method: "GET", path: "/api/contacts/{id}", description: "Get contact with tags and activity history" },
              { method: "POST", path: "/api/contacts", description: "Create new contact (individual or company)" },
              { method: "PUT", path: "/api/contacts/{id}", description: "Update contact details" },
              { method: "DELETE", path: "/api/contacts/{id}", description: "Soft delete contact" },
              { method: "GET", path: "/api/contacts/search", description: "Full-text search across contacts" },
              { method: "GET", path: "/api/contacts/exists/{email}", description: "Check if email already exists" },
              { method: "POST", path: "/api/contacts/import", description: "Bulk import contacts from array" },
              { method: "POST", path: "/api/contacts/{id}/tags/{tagId}", description: "Assign tag to contact" },
              { method: "DELETE", path: "/api/contacts/{id}/tags/{tagId}", description: "Remove tag from contact" }
            ]
          },
          {
            name: "Sales (/api/sales)",
            icon: "DollarSign",
            endpoints: [
              { method: "GET", path: "/api/sales", description: "List sales with status/stage/date filters" },
              { method: "GET", path: "/api/sales/stats", description: "Get sales statistics (totals, by status)" },
              { method: "GET", path: "/api/sales/{id}", description: "Get sale with items and activities" },
              { method: "POST", path: "/api/sales", description: "Create new sale" },
              { method: "PATCH", path: "/api/sales/{id}", description: "Update sale (partial update)" },
              { method: "DELETE", path: "/api/sales/{id}", description: "Delete sale and related items" },
              { method: "POST", path: "/api/sales/from-offer/{offerId}", description: "Convert offer to sale (copies all data)" },
              { method: "GET", path: "/api/sales/{id}/activities", description: "Get sale activity log" },
              { method: "POST", path: "/api/sales/{id}/activities", description: "Add activity to sale" },
              { method: "POST", path: "/api/sales/{id}/items", description: "Add line item to sale" },
              { method: "PATCH", path: "/api/sales/{id}/items/{itemId}", description: "Update sale item" },
              { method: "DELETE", path: "/api/sales/{id}/items/{itemId}", description: "Remove item from sale" }
            ]
          },
          {
            name: "Offers (/api/offers)",
            icon: "FileText",
            endpoints: [
              { method: "GET", path: "/api/offers", description: "List offers with status/date filters" },
              { method: "GET", path: "/api/offers/{id}", description: "Get offer with items" },
              { method: "POST", path: "/api/offers", description: "Create new offer/quote" },
              { method: "PUT", path: "/api/offers/{id}", description: "Update offer" },
              { method: "DELETE", path: "/api/offers/{id}", description: "Delete offer" },
              { method: "POST", path: "/api/offers/{id}/send", description: "Mark offer as sent to customer" },
              { method: "POST", path: "/api/offers/{id}/items", description: "Add item to offer" },
              { method: "PUT", path: "/api/offers/{id}/items/{itemId}", description: "Update offer item" },
              { method: "DELETE", path: "/api/offers/{id}/items/{itemId}", description: "Remove item from offer" }
            ]
          },
          {
            name: "Service Orders (/api/serviceorders)",
            icon: "Wrench",
            endpoints: [
              { method: "GET", path: "/api/serviceorders", description: "List service orders with filters" },
              { method: "GET", path: "/api/serviceorders/{id}", description: "Get service order with jobs" },
              { method: "POST", path: "/api/serviceorders", description: "Create new service order" },
              { method: "PUT", path: "/api/serviceorders/{id}", description: "Update service order" },
              { method: "DELETE", path: "/api/serviceorders/{id}", description: "Delete service order" },
              { method: "GET", path: "/api/serviceorders/{id}/jobs", description: "Get jobs for order" },
              { method: "POST", path: "/api/serviceorders/{id}/jobs", description: "Add job to order" }
            ]
          },
          {
            name: "Dispatches (/api/dispatches)",
            icon: "Truck",
            endpoints: [
              { method: "GET", path: "/api/dispatches", description: "List dispatches with date/status filters" },
              { method: "GET", path: "/api/dispatches/{id}", description: "Get dispatch details" },
              { method: "POST", path: "/api/dispatches", description: "Create new dispatch assignment" },
              { method: "PUT", path: "/api/dispatches/{id}", description: "Update dispatch" },
              { method: "DELETE", path: "/api/dispatches/{id}", description: "Delete dispatch" },
              { method: "PATCH", path: "/api/dispatches/{id}/status", description: "Update dispatch status" },
              { method: "POST", path: "/api/dispatches/{id}/start", description: "Start dispatch (set start time)" },
              { method: "POST", path: "/api/dispatches/{id}/complete", description: "Complete dispatch (set end time)" }
            ]
          },
          {
            name: "Installations (/api/installations)",
            icon: "Building",
            endpoints: [
              { method: "GET", path: "/api/installations", description: "List installations" },
              { method: "GET", path: "/api/installations/{id}", description: "Get installation details" },
              { method: "POST", path: "/api/installations", description: "Create new installation" },
              { method: "PUT", path: "/api/installations/{id}", description: "Update installation" },
              { method: "DELETE", path: "/api/installations/{id}", description: "Delete installation" }
            ]
          },
          {
            name: "Articles (/api/articles)",
            icon: "Package",
            endpoints: [
              { method: "GET", path: "/api/articles", description: "List articles/products" },
              { method: "GET", path: "/api/articles/{id}", description: "Get article details" },
              { method: "POST", path: "/api/articles", description: "Create new article" },
              { method: "PUT", path: "/api/articles/{id}", description: "Update article" },
              { method: "DELETE", path: "/api/articles/{id}", description: "Delete article" }
            ]
          },
          {
            name: "Calendar (/api/calendar)",
            icon: "Calendar",
            endpoints: [
              { method: "GET", path: "/api/calendar/events", description: "List calendar events in date range" },
              { method: "GET", path: "/api/calendar/events/{id}", description: "Get event details" },
              { method: "POST", path: "/api/calendar/events", description: "Create new event" },
              { method: "PUT", path: "/api/calendar/events/{id}", description: "Update event" },
              { method: "DELETE", path: "/api/calendar/events/{id}", description: "Delete event" },
              { method: "GET", path: "/api/calendar/event-types", description: "List event types" }
            ]
          },
          {
            name: "Lookups (/api/lookups)",
            icon: "List",
            endpoints: [
              { method: "GET", path: "/api/lookups/priorities", description: "Get priority options" },
              { method: "GET", path: "/api/lookups/service-categories", description: "Get service categories" },
              { method: "GET", path: "/api/lookups/article-categories", description: "Get article categories" },
              { method: "GET", path: "/api/lookups/locations", description: "Get location options" },
              { method: "GET", path: "/api/lookups/statuses", description: "Get status options" },
              { method: "GET", path: "/api/lookups/tags", description: "Get tag options" }
            ]
          }
        ],
        
        // Detailed API Examples
        examples: [
          {
            name: "Authentication - Login",
            method: "POST",
            path: "/api/auth/admin/login",
            description: "Authenticate admin user and receive JWT token",
            request: `{
  "email": "admin@example.com",
  "password": "SecurePassword123!"
}`,
            response: `{
  "success": true,
  "data": {
    "user": {
      "id": 1,
      "email": "admin@example.com",
      "firstName": "Admin",
      "lastName": "User",
      "isActive": true
    },
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
    "expiresIn": 3600
  }
}`
          },
          {
            name: "Contacts - List with Filters",
            method: "GET",
            path: "/api/contacts?searchTerm=john&status=active&pageNumber=1&pageSize=20",
            description: "Retrieve paginated list of contacts with filtering",
            request: `Query Parameters:
- searchTerm: string (optional) - Search in name, email, company
- status: string (optional) - "active" | "inactive" | "lead" | "customer"
- type: string (optional) - "individual" | "company" | "partner"
- favorite: boolean (optional) - Filter favorites only
- tagIds: number[] (optional) - Filter by tag IDs
- pageNumber: number (default: 1)
- pageSize: number (default: 20)
- sortBy: string (default: "CreatedDate")
- sortDirection: "asc" | "desc" (default: "desc")`,
            response: `{
  "contacts": [
    {
      "id": 1,
      "firstName": "John",
      "lastName": "Doe",
      "name": "John Doe",
      "email": "john.doe@example.com",
      "phone": "+1-555-0123",
      "company": "Acme Corp",
      "position": "Manager",
      "status": "active",
      "type": "individual",
      "address": "123 Main St, City",
      "favorite": true,
      "createdAt": "2024-01-15T10:30:00Z",
      "tags": [
        { "id": 1, "name": "VIP", "color": "#FF5733" }
      ]
    }
  ],
  "totalCount": 150,
  "pageSize": 20,
  "pageNumber": 1,
  "hasNextPage": true,
  "hasPreviousPage": false
}`
          },
          {
            name: "Contacts - Create",
            method: "POST",
            path: "/api/contacts",
            description: "Create a new contact record",
            request: `{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane.smith@example.com",
  "phone": "+1-555-0456",
  "company": "Tech Solutions",
  "position": "Director",
  "status": "lead",
  "type": "individual",
  "address": "456 Oak Avenue, Suite 100",
  "city": "San Francisco",
  "country": "USA",
  "postalCode": "94102",
  "notes": "Referred by John Doe",
  "favorite": false,
  "tagIds": [1, 3]
}`,
            response: `{
  "id": 25,
  "firstName": "Jane",
  "lastName": "Smith",
  "name": "Jane Smith",
  "email": "jane.smith@example.com",
  "phone": "+1-555-0456",
  "company": "Tech Solutions",
  "position": "Director",
  "status": "lead",
  "type": "individual",
  "address": "456 Oak Avenue, Suite 100",
  "city": "San Francisco",
  "country": "USA",
  "postalCode": "94102",
  "isActive": true,
  "favorite": false,
  "createdDate": "2024-01-20T14:22:00Z",
  "createdBy": "admin@example.com",
  "tags": [
    { "id": 1, "name": "VIP", "color": "#FF5733" },
    { "id": 3, "name": "New Lead", "color": "#33FF57" }
  ]
}`
          },
          {
            name: "Sales - Create with Items",
            method: "POST",
            path: "/api/sales",
            description: "Create a new sale with line items",
            request: `{
  "title": "Enterprise License Q1 2024",
  "description": "Annual enterprise license with support",
  "contactId": 25,
  "status": "draft",
  "stage": "negotiation",
  "priority": "high",
  "currency": "USD",
  "taxes": 850.00,
  "discount": 500.00,
  "estimatedCloseDate": "2024-02-15",
  "items": [
    {
      "type": "service",
      "itemName": "Enterprise License",
      "itemCode": "LIC-ENT-001",
      "description": "Annual enterprise software license",
      "quantity": 1,
      "unitPrice": 5000.00,
      "discount": 0
    },
    {
      "type": "service",
      "itemName": "Premium Support",
      "itemCode": "SUP-PREM-001",
      "description": "24/7 premium support package",
      "quantity": 1,
      "unitPrice": 1200.00,
      "discount": 10,
      "discountType": "percentage"
    }
  ]
}`,
            response: `{
  "success": true,
  "data": {
    "id": 42,
    "saleNumber": "SALE-2024-0042",
    "title": "Enterprise License Q1 2024",
    "contactId": 25,
    "contactName": "Jane Smith",
    "status": "draft",
    "stage": "negotiation",
    "priority": "high",
    "currency": "USD",
    "taxes": 850.00,
    "discount": 500.00,
    "totalAmount": 6430.00,
    "estimatedCloseDate": "2024-02-15T00:00:00Z",
    "createdDate": "2024-01-20T14:30:00Z",
    "createdByName": "Admin User",
    "items": [
      {
        "id": 101,
        "type": "service",
        "itemName": "Enterprise License",
        "quantity": 1,
        "unitPrice": 5000.00,
        "totalPrice": 5000.00
      },
      {
        "id": 102,
        "type": "service",
        "itemName": "Premium Support",
        "quantity": 1,
        "unitPrice": 1200.00,
        "discount": 10,
        "totalPrice": 1080.00
      }
    ]
  }
}`
          },
          {
            name: "Sales - Convert from Offer",
            method: "POST",
            path: "/api/sales/from-offer/15",
            description: "Convert an accepted offer to a sale (copies all data)",
            request: `// No request body needed - offer ID is in URL
// The offer with ID 15 will be converted to a sale`,
            response: `{
  "success": true,
  "data": {
    "id": 43,
    "saleNumber": "SALE-2024-0043",
    "title": "Copied from Offer OFF-2024-0015",
    "offerId": 15,
    "offerNumber": "OFF-2024-0015",
    "contactId": 12,
    "contactName": "Acme Corporation",
    "status": "won",
    "stage": "closed",
    "totalAmount": 12500.00,
    "items": [...],
    "createdDate": "2024-01-20T15:00:00Z"
  }
}`
          },
          {
            name: "Service Orders - Create from Sale",
            method: "POST",
            path: "/api/service-orders/from-sale/42",
            description: "Generate service order from a sale's service items",
            request: `{
  "priority": "high",
  "notes": "Customer requested weekend installation",
  "startDate": "2024-02-01T09:00:00Z",
  "targetCompletionDate": "2024-02-03T17:00:00Z",
  "installationIds": [5, 8],
  "tags": ["urgent", "enterprise"]
}`,
            response: `{
  "success": true,
  "data": {
    "serviceOrderId": 78,
    "orderNumber": "SO-2024-0078",
    "saleId": 42,
    "status": "pending",
    "jobs": [
      {
        "id": 201,
        "description": "Enterprise License Installation",
        "status": "pending"
      }
    ]
  }
}`
          },
          {
            name: "Bulk Import Contacts",
            method: "POST",
            path: "/api/contacts/import",
            description: "Import multiple contacts at once",
            request: `{
  "contacts": [
    {
      "firstName": "Alice",
      "lastName": "Johnson",
      "email": "alice@example.com",
      "company": "Tech Corp"
    },
    {
      "firstName": "Bob",
      "lastName": "Williams",
      "email": "bob@example.com",
      "company": "Data Inc"
    }
  ],
  "skipDuplicates": true,
  "updateExisting": false
}`,
            response: `{
  "totalProcessed": 2,
  "successCount": 2,
  "failedCount": 0,
  "skippedCount": 0,
  "errors": [],
  "importedContacts": [
    { "id": 26, "name": "Alice Johnson", ... },
    { "id": 27, "name": "Bob Williams", ... }
  ]
}`
          }
        ]
      },
      
      services: {
        title: "Service Layer Pattern",
        content: `Each module follows a consistent service pattern:

┌─────────────────────────────────────────────────────────────┐
│                    SERVICE PATTERN                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  INTERFACE (ISaleService.cs):                                │
│  ─────────────────────────────                               │
│  public interface ISaleService                               │
│  {                                                           │
│      Task<PagedResult<SaleDto>> GetSalesAsync(filters);     │
│      Task<SaleDto?> GetSaleByIdAsync(int id);               │
│      Task<SaleDto> CreateSaleAsync(CreateSaleDto dto);      │
│      Task<SaleDto> UpdateSaleAsync(int id, UpdateDto dto);  │
│      Task<bool> DeleteSaleAsync(int id);                    │
│      Task<SaleDto> CreateSaleFromOfferAsync(int offerId);   │
│  }                                                           │
│                                                              │
│  IMPLEMENTATION (SaleService.cs):                            │
│  ─────────────────────────────────                           │
│  • Dependency injection of DbContext and ILogger            │
│  • Business logic and validation                            │
│  • Entity Framework queries with Include/ThenInclude        │
│  • AutoMapper for DTO transformations                       │
│  • Exception handling and logging                           │
│                                                              │
│  CONTROLLER (SalesController.cs):                            │
│  ─────────────────────────────────                           │
│  • [Authorize] attribute for authentication                 │
│  • Route definitions (/api/sales)                           │
│  • HTTP method attributes (HttpGet, HttpPost, etc.)         │
│  • Calls service methods                                    │
│  • Returns standardized responses                           │
│  • System logging via ISystemLogService                     │
│                                                              │
└─────────────────────────────────────────────────────────────┘

DEPENDENCY INJECTION REGISTRATION:
──────────────────────────────────
// Program.cs
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
// ... all other services`
      },

      // Backend DTOs Documentation
      dtos: {
        title: "Data Transfer Objects (DTOs)",
        content: `Backend DTOs define the shape of request/response data:

┌─────────────────────────────────────────────────────────────┐
│                   CONTACT DTOs                               │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ContactResponseDto (Response):                              │
│  ──────────────────────────────                              │
│  {                                                           │
│    id: int                    // Unique identifier          │
│    firstName: string          // Required, max 100 chars    │
│    lastName: string           // Required, max 100 chars    │
│    name: string               // Computed: FirstName LastName│
│    email: string?             // Optional, validated format │
│    phone: string?             // Optional, max 20 chars     │
│    company: string?           // Optional, max 200 chars    │
│    position: string?          // Optional, max 100 chars    │
│    address: string?           // Optional, max 500 chars    │
│    city: string?              // Optional, max 100 chars    │
│    country: string?           // Optional, max 100 chars    │
│    postalCode: string?        // Optional, max 20 chars     │
│    status: string             // "active"|"inactive"|"lead" │
│    type: string               // "individual"|"company"     │
│    favorite: bool             // Favorite flag              │
│    isActive: bool             // Soft delete flag           │
│    createdDate: DateTime      // Creation timestamp         │
│    createdBy: string?         // Creator email              │
│    modifiedDate: DateTime?    // Last modification          │
│    tags: ContactTagDto[]      // Associated tags            │
│  }                                                           │
│                                                              │
│  CreateContactRequestDto (Request):                          │
│  ───────────────────────────────────                         │
│  {                                                           │
│    firstName: string          // [Required] max 100         │
│    lastName: string           // [Required] max 100         │
│    email: string?             // [EmailAddress] max 255     │
│    phone: string?             // max 20                     │
│    company: string?           // max 200                    │
│    position: string?          // max 100                    │
│    address: string?           // max 500                    │
│    status: string?            // default: "active"          │
│    type: string?              // default: "individual"      │
│    favorite: bool?            // default: false             │
│    tagIds: int[]              // Tag IDs to assign          │
│  }                                                           │
│                                                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    SALE DTOs                                 │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  SaleDto (Response):                                         │
│  ───────────────────                                         │
│  {                                                           │
│    id: int                    // Unique identifier          │
│    saleNumber: string         // Auto-generated: SALE-YYYY-N│
│    title: string              // Sale title/description     │
│    contactId: int             // Related contact ID         │
│    contactName: string        // Resolved contact name      │
│    offerId: int?              // Source offer (if converted)│
│    status: string             // "draft"|"won"|"lost"       │
│    stage: string?             // "offer"|"negotiation"|...  │
│    priority: string?          // "low"|"medium"|"high"      │
│    currency: string           // "USD"|"EUR"|"CAD"          │
│    taxes: decimal?            // Tax amount                 │
│    discount: decimal?         // Discount amount            │
│    totalAmount: decimal?      // Computed total             │
│    items: SaleItemDto[]       // Line items                 │
│    createdDate: DateTime      // Creation timestamp         │
│    createdByName: string?     // Creator name               │
│  }                                                           │
│                                                              │
│  SaleItemDto:                                                │
│  ────────────                                                │
│  {                                                           │
│    id: int                    // Item ID                    │
│    type: string               // "article"|"service"        │
│    itemName: string           // Product/service name       │
│    itemCode: string?          // SKU or reference code      │
│    description: string?       // Item description           │
│    quantity: int              // Quantity                   │
│    unitPrice: decimal         // Price per unit             │
│    discount: decimal?         // Item discount              │
│    discountType: string?      // "percentage"|"fixed"       │
│    totalPrice: decimal        // Computed line total        │
│    requiresServiceOrder: bool // Needs service order?       │
│    serviceOrderId: string?    // Generated SO ID            │
│  }                                                           │
│                                                              │
└─────────────────────────────────────────────────────────────┘`
      },
      
      serviceLogic: {
        title: "Backend Service Logic Deep Dive",
        subtitle: "Detailed C# service methods for core business operations",
        
        sections: [
          {
            name: "OfferService.cs - Offer Management",
            description: "Core offer CRUD operations and conversion logic",
            methods: [
              {
                name: "CreateOfferAsync",
                description: "Creates a new offer with items and logs activity",
                code: `// FlowServiceBackendOnlyFinal-main/Modules/Offers/Services/OfferService.cs
public async Task<OfferDto> CreateOfferAsync(CreateOfferDto createDto, string userId)
{
    // Verify contact exists if provided
    Contact? contact = null;
    if (createDto.ContactId.HasValue)
    {
        contact = await _context.Contacts.FindAsync(createDto.ContactId.Value);
        if (contact == null)
            throw new KeyNotFoundException(\$"Contact with ID {createDto.ContactId} not found");
    }

    // Generate unique offer number
    var offerNumber = await GenerateOfferNumberAsync();

    var offer = new Offer
    {
        OfferNumber = offerNumber,
        Title = createDto.Title ?? "Untitled Offer",
        ContactId = createDto.ContactId ?? 0,
        Status = createDto.Status ?? "draft",
        Currency = createDto.Currency ?? "USD",
        ValidUntil = createDto.ValidUntil,
        Taxes = createDto.Taxes ?? 0,
        Discount = createDto.Discount ?? 0,
        TotalAmount = 0,
        CreatedBy = userId,
        CreatedDate = DateTime.UtcNow
    };

    _context.Offers.Add(offer);
    await _context.SaveChangesAsync();

    // Add items if provided
    if (createDto.Items != null && createDto.Items.Any())
    {
        var items = createDto.Items.Select(itemDto => new OfferItem
        {
            OfferId = offer.Id,
            Type = itemDto.Type ?? "article",
            ArticleId = itemDto.ArticleId,
            ItemName = itemDto.ItemName,
            Quantity = itemDto.Quantity,
            UnitPrice = itemDto.UnitPrice,
            Discount = itemDto.Discount,
            InstallationId = itemDto.InstallationId,
            InstallationName = itemDto.InstallationName
        }).ToList();

        _context.OfferItems.AddRange(items);
        await _context.SaveChangesAsync();
    }

    // Log offer creation activity
    var creationActivity = new OfferActivity
    {
        OfferId = offer.Id,
        Type = "created",
        Description = \$"Offer '{offer.Title}' was created",
        CreatedAt = DateTime.UtcNow,
        CreatedByName = userId
    };
    _context.OfferActivities.Add(creationActivity);
    await _context.SaveChangesAsync();

    return await GetOfferByIdAsync(offer.Id);
}`
              },
              {
                name: "RenewOfferAsync",
                description: "Creates a copy of an existing offer for renewal",
                code: `public async Task<OfferDto> RenewOfferAsync(int id, string userId)
{
    var originalOffer = await _context.Offers
        .Include(o => o.Items)
        .FirstOrDefaultAsync(o => o.Id == id);

    if (originalOffer == null)
        throw new KeyNotFoundException(\$"Offer with ID {id} not found");

    // Generate new offer number for renewed offer
    var offerNumber = await GenerateOfferNumberAsync();

    var renewedOffer = new Offer
    {
        OfferNumber = offerNumber,
        Title = originalOffer.Title,
        Description = originalOffer.Description,
        ContactId = originalOffer.ContactId,
        Status = "draft",  // Reset to draft
        Currency = originalOffer.Currency ?? "TND",
        Taxes = originalOffer.Taxes,
        Discount = originalOffer.Discount,
        TotalAmount = 0,
        ValidUntil = DateTime.UtcNow.AddDays(30),  // New validity
        CreatedBy = userId,
        CreatedDate = DateTime.UtcNow,
        Tags = originalOffer.Tags
    };

    _context.Offers.Add(renewedOffer);
    await _context.SaveChangesAsync();

    // Copy all items from original offer
    if (originalOffer.Items != null)
    {
        var renewedItems = originalOffer.Items.Select(item => new OfferItem
        {
            OfferId = renewedOffer.Id,
            Type = item.Type,
            ArticleId = item.ArticleId,
            ItemName = item.ItemName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Discount = item.Discount,
            InstallationId = item.InstallationId,
            InstallationName = item.InstallationName
        }).ToList();

        _context.OfferItems.AddRange(renewedItems);
        await _context.SaveChangesAsync();
    }

    return await GetOfferByIdAsync(renewedOffer.Id);
}`
              }
            ]
          },
          {
            name: "SaleService.cs - Sale Order Management",
            description: "Sale creation, offer conversion, and item management",
            methods: [
              {
                name: "CreateSaleFromOfferAsync",
                description: "Converts an accepted offer to a sale order - CRITICAL CONVERSION METHOD",
                code: `// FlowServiceBackendOnlyFinal-main/Modules/Sales/Services/SaleService.cs
public async Task<SaleDto> CreateSaleFromOfferAsync(int offerId, string userId)
{
    var offer = await _context.Offers
        .Include(o => o.Items)
        .FirstOrDefaultAsync(o => o.Id == offerId);

    if (offer == null)
        throw new KeyNotFoundException(\$"Offer with ID {offerId} not found");

    // Get user name for sale and activity logging
    string createdByName = userId;
    var adminUser = await _context.MainAdminUsers
        .FirstOrDefaultAsync(u => u.Id.ToString() == userId);
    if (adminUser != null)
        createdByName = \$"{adminUser.FirstName} {adminUser.LastName}".Trim();

    var sale = new Sale
    {
        SaleNumber = \$"SALE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}",
        Title = offer.Title,
        Description = offer.Description,
        ContactId = offer.ContactId,
        Status = "created",      // Start with 'created' status
        Stage = "offer",         // Start at 'offer' stage
        Priority = "medium",
        Currency = offer.Currency ?? "TND",
        // CRITICAL: Transfer financial data from offer
        Taxes = offer.Taxes ?? 0,
        Discount = offer.Discount ?? 0,
        TotalAmount = offer.TotalAmount,
        AssignedTo = offer.AssignedTo,
        OfferId = offerId.ToString(),
        ConvertedFromOfferAt = DateTime.UtcNow,
        ActualCloseDate = DateTime.UtcNow,
        CreatedBy = userId,
        CreatedByName = createdByName,
        CreatedDate = DateTime.UtcNow
    };

    _context.Sales.Add(sale);
    await _context.SaveChangesAsync();

    // Copy all items from offer to sale
    if (offer.Items != null && offer.Items.Any())
    {
        var saleItems = offer.Items.Select(offerItem => new SaleItem
        {
            SaleId = sale.Id,
            Type = offerItem.Type,
            ArticleId = offerItem.ArticleId,
            ItemName = offerItem.ItemName,
            ItemCode = offerItem.ItemCode,
            Description = offerItem.Description ?? offerItem.ItemName ?? "Item",
            Quantity = offerItem.Quantity,
            UnitPrice = offerItem.UnitPrice,
            Discount = offerItem.Discount,
            DiscountType = offerItem.DiscountType ?? "percentage",
            InstallationId = offerItem.InstallationId,
            InstallationName = offerItem.InstallationName,
            // Mark service items for service order generation
            RequiresServiceOrder = offerItem.Type == "service",
            FulfillmentStatus = "pending"
        }).ToList();

        _context.SaleItems.AddRange(saleItems);
    }

    // Update offer status to 'accepted' and link to sale
    offer.Status = "accepted";
    offer.ConvertedToSaleId = sale.Id.ToString();
    offer.ConvertedAt = DateTime.UtcNow;

    // Log activity
    var creationActivity = new SaleActivity
    {
        SaleId = sale.Id,
        Type = "created",
        Description = \$"Sale order created from Offer #{offer.OfferNumber}",
        CreatedAt = DateTime.UtcNow,
        CreatedByName = createdByName
    };
    _context.SaleActivities.Add(creationActivity);

    await _context.SaveChangesAsync();
    return await GetSaleByIdAsync(sale.Id);
}`
              }
            ]
          },
          {
            name: "ServiceOrderService.cs - Service Order Management",
            description: "Creating service orders from sales, managing jobs and materials",
            methods: [
              {
                name: "CreateFromSaleAsync",
                description: "Creates service order from sale - separates services into Jobs and materials into Materials",
                code: `// FlowServiceBackendOnlyFinal-main/Modules/ServiceOrders/Services/ServiceOrderService.cs
public async Task<ServiceOrderDto> CreateFromSaleAsync(int saleId, CreateServiceOrderDto createDto, string userId)
{
    // Verify sale exists with its items
    var sale = await _context.Sales
        .Include(s => s.Items)
        .FirstOrDefaultAsync(s => s.Id == saleId);
    if (sale == null)
        throw new KeyNotFoundException(\$"Sale with ID {saleId} not found");

    // Check if service order already exists for this sale
    var existingOrder = await _context.ServiceOrders
        .FirstOrDefaultAsync(s => s.SaleId == saleId.ToString());
    if (existingOrder != null)
        throw new InvalidOperationException(\$"Service order already exists for sale {saleId}");

    // Get service-type items from the sale (these become JOBS)
    var serviceItems = sale.Items?
        .Where(i => i.Type?.ToLower() == "service")
        .ToList() ?? new List<SaleItem>();

    var orderNumber = \$"SO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";

    var serviceOrder = new ServiceOrder
    {
        OrderNumber = orderNumber,
        SaleId = saleId.ToString(),
        OfferId = sale.OfferId,
        ContactId = sale.ContactId,
        ServiceType = serviceItems.FirstOrDefault()?.ItemName ?? "maintenance",
        Status = "ready_for_planning",
        Priority = createDto.Priority ?? "medium",
        Description = sale.Description,
        StartDate = createDto.StartDate,
        TargetCompletionDate = createDto.TargetCompletionDate,
        EstimatedCost = sale.TotalAmount,
        ActualCost = 0,
        TotalAmount = sale.TotalAmount,
        PaymentStatus = "pending",
        CompletionPercentage = 0,
        CreatedBy = userId,
        CreatedDate = DateTime.UtcNow
    };

    _context.ServiceOrders.Add(serviceOrder);
    await _context.SaveChangesAsync();

    // Create JOBS from service-type sale items (the actual services sold)
    if (serviceItems.Any())
    {
        var jobs = serviceItems.Select(item => new ServiceOrderJob
        {
            ServiceOrderId = serviceOrder.Id,
            SaleItemId = item.Id.ToString(),
            Title = item.ItemName ?? "Service Job",
            JobDescription = item.Description ?? item.ItemName ?? "Service job",
            Status = "unscheduled",  // Ready for planning board
            Priority = createDto.Priority ?? "medium",
            InstallationId = item.InstallationId,
            InstallationName = item.InstallationName,
            WorkType = DetermineWorkType(item.ItemName),
            EstimatedCost = item.LineTotal > 0 ? item.LineTotal : (item.UnitPrice * item.Quantity),
            CompletionPercentage = 0
        }).ToList();

        _context.ServiceOrderJobs.AddRange(jobs);
        await _context.SaveChangesAsync();

        // Update sale items with service order reference
        foreach (var item in serviceItems)
        {
            item.ServiceOrderGenerated = true;
            item.ServiceOrderId = serviceOrder.Id.ToString();
        }
        await _context.SaveChangesAsync();
    }

    // Create MATERIALS from material/article-type sale items (not services)
    var materialItems = sale.Items?
        .Where(i => i.Type?.ToLower() == "material" || i.Type?.ToLower() == "article")
        .ToList() ?? new List<SaleItem>();
        
    if (materialItems.Any())
    {
        var materials = materialItems.Select(item => new ServiceOrderMaterial
        {
            ServiceOrderId = serviceOrder.Id,
            SaleItemId = item.Id,
            ArticleId = item.ArticleId,
            Name = item.ItemName ?? "Material",
            Sku = item.ItemCode,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.LineTotal > 0 ? item.LineTotal : (item.UnitPrice * item.Quantity),
            Status = "pending",
            Source = "sale_conversion",
            InstallationId = item.InstallationId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.ServiceOrderMaterials.AddRange(materials);
        await _context.SaveChangesAsync();
    }

    // Update the sale's ServiceOrdersStatus
    sale.ServiceOrdersStatus = "created";
    sale.LastActivity = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return await GetServiceOrderByIdAsync(serviceOrder.Id);
}`
              }
            ]
          },
          {
            name: "DispatchService.cs - Dispatch & Planning",
            description: "Creating dispatches from jobs, technician assignment, time/expense tracking",
            methods: [
              {
                name: "CreateFromJobAsync",
                description: "Creates a dispatch when a job is assigned to technicians on the planning board",
                code: `// FlowServiceBackendOnlyFinal-main/Modules/Dispatches/Services/DispatchService.cs
public async Task<DispatchDto> CreateFromJobAsync(int jobId, CreateDispatchFromJobDto dto, string userId)
{
    _logger.LogInformation("CreateFromJobAsync called for Job {JobId}", jobId);
    
    // Get the job to find the related ServiceOrder and Contact
    var job = await _db.ServiceOrderJobs
        .Include(j => j.ServiceOrder)
        .FirstOrDefaultAsync(j => j.Id == jobId);
    
    if (job == null)
        throw new KeyNotFoundException(\$"Job {jobId} not found");
    
    // Get ContactId from the ServiceOrder
    var contactId = dto.ContactId ?? job.ServiceOrder?.ContactId ?? 0;
    var serviceOrderId = dto.ServiceOrderId ?? job.ServiceOrderId;
    
    // Determine status based on whether technicians are assigned
    var hasTechnicians = dto.AssignedTechnicianIds != null && dto.AssignedTechnicianIds.Count > 0;
    var status = hasTechnicians ? "assigned" : "pending";

    // IMPORTANT: Prevent duplicate dispatches for the same job
    var existingDispatch = await _db.Dispatches
        .Include(d => d.AssignedTechnicians)
        .FirstOrDefaultAsync(d => d.JobId == jobId.ToString() && !d.IsDeleted);

    if (existingDispatch != null)
    {
        _logger.LogWarning("Dispatch already exists for job {JobId}", jobId);
        return DispatchMapping.ToDto(existingDispatch);
    }
    
    var dispatch = new Dispatch
    {
        DispatchNumber = \$"DISP-{DateTime.UtcNow:yyyyMMddHHmmss}",
        JobId = jobId.ToString(),
        ContactId = contactId,
        ServiceOrderId = serviceOrderId,
        Status = status,
        Priority = dto.Priority ?? job.Priority ?? "medium",
        ScheduledDate = dto.ScheduledDate,
        SiteAddress = dto.SiteAddress ?? string.Empty,
        Description = job.JobDescription ?? job.Description,
        CreatedDate = DateTime.UtcNow,
        CreatedBy = userId,
        DispatchedBy = userId,
        DispatchedAt = DateTime.UtcNow
    };

    _db.Dispatches.Add(dispatch);
    await _db.SaveChangesAsync();
    
    // Add assigned technicians to the DispatchTechnicians junction table
    if (hasTechnicians)
    {
        foreach (var techIdStr in dto.AssignedTechnicianIds!)
        {
            if (int.TryParse(techIdStr, out var techId))
            {
                var dispatchTechnician = new DispatchTechnician
                {
                    DispatchId = dispatch.Id,
                    TechnicianId = techId,
                    AssignedDate = DateTime.UtcNow,
                    Role = "technician"
                };
                _db.Set<DispatchTechnician>().Add(dispatchTechnician);
            }
        }
        await _db.SaveChangesAsync();
        
        // Update job status to dispatched
        job.Status = "dispatched";
        await _db.SaveChangesAsync();
    }

    return DispatchMapping.ToDto(dispatch);
}`
              },
              {
                name: "AddTimeEntryAsync",
                description: "Records time worked by technicians on a dispatch",
                code: `public async Task<TimeEntryDto> AddTimeEntryAsync(int dispatchId, CreateTimeEntryDto dto, string userId)
{
    var d = await _db.Dispatches.FirstOrDefaultAsync(x => x.Id == dispatchId && !x.IsDeleted);
    if (d == null) throw new KeyNotFoundException(\$"Dispatch {dispatchId} not found");

    var te = new TimeEntry
    {
        DispatchId = dispatchId,
        TechnicianId = int.TryParse(dto.TechnicianId, out var tid) ? tid : 0,
        WorkType = dto.WorkType,  // "travel", "setup", "work", "documentation", "cleanup"
        StartTime = dto.StartTime,
        EndTime = dto.EndTime,
        // Auto-calculate duration in minutes
        Duration = (decimal)(dto.EndTime - dto.StartTime).TotalMinutes,
        Description = dto.Description,
        CreatedDate = DateTime.UtcNow
    };
    _db.TimeEntries.Add(te);
    await _db.SaveChangesAsync();

    return new TimeEntryDto 
    { 
        Id = te.Id, 
        DispatchId = te.DispatchId, 
        TechnicianId = te.TechnicianId.ToString(), 
        WorkType = te.WorkType, 
        StartTime = te.StartTime,
        EndTime = te.EndTime,
        Duration = (int)(te.Duration ?? 0), 
        Description = te.Description,
        CreatedAt = te.CreatedDate 
    };
}`
              },
              {
                name: "AddExpenseAsync",
                description: "Records expenses incurred by technicians during dispatch",
                code: `public async Task<ExpenseDto> AddExpenseAsync(int dispatchId, CreateExpenseDto dto, string userId)
{
    var d = await _db.Dispatches.FirstOrDefaultAsync(x => x.Id == dispatchId && !x.IsDeleted);
    if (d == null) throw new KeyNotFoundException(\$"Dispatch {dispatchId} not found");

    var exp = new Expense
    {
        DispatchId = dispatchId,
        TechnicianId = int.TryParse(dto.TechnicianId, out var tid) ? tid : 0,
        Category = dto.Category,  // "travel", "equipment", "materials", "meals", "other"
        Amount = dto.Amount,
        Description = dto.Description,
        ReceiptUrl = dto.ReceiptUrl,  // Uploaded receipt image
        Status = "pending",  // For approval workflow
        CreatedDate = DateTime.UtcNow
    };
    _db.Expenses.Add(exp);
    await _db.SaveChangesAsync();

    return new ExpenseDto 
    { 
        Id = exp.Id, 
        DispatchId = exp.DispatchId, 
        TechnicianId = exp.TechnicianId.ToString(),
        Category = exp.Category, 
        Amount = exp.Amount, 
        Description = exp.Description,
        ReceiptUrl = exp.ReceiptUrl,
        Status = exp.Status,
        CreatedAt = exp.CreatedDate 
    };
}`
              },
              {
                name: "AddMaterialUsageAsync",
                description: "Records materials used during dispatch execution",
                code: `public async Task<MaterialUsageDto> AddMaterialUsageAsync(int dispatchId, CreateMaterialUsageDto dto, string userId)
{
    var d = await _db.Dispatches.FirstOrDefaultAsync(x => x.Id == dispatchId && !x.IsDeleted);
    if (d == null) throw new KeyNotFoundException(\$"Dispatch {dispatchId} not found");

    var usage = new MaterialUsage
    {
        DispatchId = dispatchId,
        ArticleId = dto.ArticleId,
        ArticleName = dto.ArticleName,
        Quantity = dto.Quantity,
        UnitPrice = dto.UnitPrice,
        TotalPrice = dto.Quantity * dto.UnitPrice,
        Notes = dto.Notes,
        CreatedBy = userId,
        CreatedDate = DateTime.UtcNow
    };
    _db.MaterialUsages.Add(usage);
    await _db.SaveChangesAsync();

    return new MaterialUsageDto 
    { 
        Id = usage.Id, 
        DispatchId = usage.DispatchId, 
        ArticleId = usage.ArticleId,
        ArticleName = usage.ArticleName,
        Quantity = usage.Quantity, 
        UnitPrice = usage.UnitPrice,
        TotalPrice = usage.TotalPrice,
        Notes = usage.Notes,
        CreatedAt = usage.CreatedDate 
    };
}`
              },
              {
                name: "UpdateStatusAsync",
                description: "Updates dispatch status with automatic timestamp tracking",
                code: `public async Task<DispatchDto> UpdateStatusAsync(int dispatchId, UpdateDispatchStatusDto dto, string userId)
{
    var d = await _db.Dispatches.FirstOrDefaultAsync(x => x.Id == dispatchId && !x.IsDeleted);
    if (d == null) throw new KeyNotFoundException(\$"Dispatch {dispatchId} not found");

    d.Status = dto.Status;
    d.ModifiedDate = DateTime.UtcNow;
    d.ModifiedBy = userId;
    
    // Auto-set timestamps based on status
    if (dto.Status == "in_progress") 
        d.ActualStartTime = DateTime.UtcNow;
    if (dto.Status == "completed") 
        d.ActualEndTime = DateTime.UtcNow;

    await _db.SaveChangesAsync();
    return DispatchMapping.ToDto(d);
}`
              }
            ]
          },
          {
            name: "DynamicFormsService.cs - Custom Form Builder System",
            description: "Complete form builder for creating custom forms, checklists, and surveys with conditional logic and multi-page support",
            methods: [
              {
                name: "Architecture Overview",
                description: "Dynamic Forms module structure and components",
                code: `// ============= DYNAMIC FORMS MODULE ARCHITECTURE =============
// Frontend: src/modules/dynamic-forms/
// Backend: FlowServiceBackendOnlyFinal-main/Modules/DynamicForms/

// DIRECTORY STRUCTURE:
// src/modules/dynamic-forms/
// ├── pages/
// │   ├── DynamicFormsPage.tsx      # Form list with search/filter
// │   ├── CreateFormPage.tsx        # New form wizard
// │   ├── EditFormPage.tsx          # Form editing
// │   ├── FormPreviewPage.tsx       # Live form preview & testing
// │   └── FormResponsesPage.tsx     # View submitted responses
// │
// ├── components/
// │   ├── FormBuilder/              # Drag-and-drop builder
// │   │   ├── index.tsx             # Main builder component
// │   │   ├── FieldPalette.tsx      # Available field types
// │   │   ├── FormCanvas.tsx        # Drop zone for fields
// │   │   └── FieldProperties.tsx   # Field configuration panel
// │   ├── FormsTable.tsx            # Forms listing table
// │   ├── SteppedFormPreview.tsx    # Multi-page form wizard UI
// │   └── FormResponsePDF.tsx       # PDF export component
// │
// ├── hooks/
// │   └── useDynamicForms.ts        # CRUD hooks with caching
// │
// ├── utils/
// │   ├── conditionEvaluator.ts     # Conditional logic engine
// │   └── pageUtils.ts              # Multi-page form utilities
// │
// ├── services/
// │   └── dynamicFormsService.ts    # API service layer
// │
// ├── locale/
// │   ├── en.json                   # English translations
// │   └── fr.json                   # French translations
// │
// └── types.ts                      # TypeScript interfaces`
              },
              {
                name: "Field Types",
                description: "All supported form field types with configurations",
                code: `// 15+ FIELD TYPES SUPPORTED:

export type FieldType =
  // Basic Input Fields
  | 'text'        // Single-line text input
  | 'textarea'    // Multi-line text area
  | 'number'      // Numeric input with min/max
  | 'email'       // Email validation
  | 'phone'       // Phone number format
  | 'date'        // Date picker
  | 'time'        // Time picker
  | 'datetime'    // Combined date/time
  
  // Selection Fields
  | 'select'      // Dropdown select
  | 'radio'       // Radio button group
  | 'checkbox'    // Checkbox (single or group)
  | 'yes_no'      // Yes/No toggle
  
  // Advanced Fields
  | 'signature'   // Signature capture canvas
  | 'photo'       // Photo upload/capture
  | 'rating'      // Star rating (1-5 or custom)
  | 'location'    // GPS location capture
  
  // Layout Fields
  | 'section'     // Section header/divider
  | 'page_break'; // Multi-page separator

// FIELD CONFIGURATION:
interface FormField {
  id: string;           // Unique field ID
  type: FieldType;      // Field type
  label_en: string;     // English label
  label_fr: string;     // French label
  placeholder_en?: string;
  placeholder_fr?: string;
  required: boolean;
  order: number;        // Position in form
  
  // Type-specific options
  options?: FieldOption[];  // For select/radio/checkbox
  min?: number;             // For number/rating
  max?: number;
  maxStars?: number;        // For rating field
  maxPhotos?: number;       // For photo field
  
  // Conditional Logic
  condition?: FieldCondition;
  condition_action?: 'show' | 'hide';
}`
              },
              {
                name: "Conditional Logic System",
                description: "Show/hide fields based on other field values",
                code: `// src/modules/dynamic-forms/utils/conditionEvaluator.ts

// CONDITION OPERATORS:
export type ConditionOperator =
  | 'equals'           // Exact match
  | 'not_equals'       // Not equal
  | 'contains'         // String contains
  | 'not_contains'     // String doesn't contain
  | 'greater_than'     // Numeric >
  | 'less_than'        // Numeric <
  | 'is_empty'         // Field is empty
  | 'is_not_empty';    // Field has value

// CONDITION INTERFACE:
export interface FieldCondition {
  field_id: string;           // Source field to evaluate
  operator: ConditionOperator; // Comparison operator
  value?: string | number | boolean;  // Expected value
}

// EVALUATION FUNCTION:
export function evaluateCondition(
  condition: FieldCondition,
  formValues: Record<string, any>
): boolean {
  const sourceValue = formValues[condition.field_id];
  
  switch (condition.operator) {
    case 'equals':
      return String(sourceValue) === String(condition.value);
    case 'not_equals':
      return String(sourceValue) !== String(condition.value);
    case 'contains':
      return String(sourceValue).includes(String(condition.value));
    case 'greater_than':
      return Number(sourceValue) > Number(condition.value);
    case 'is_empty':
      return !sourceValue || sourceValue === '';
    case 'is_not_empty':
      return !!sourceValue && sourceValue !== '';
    default:
      return true;
  }
}

// USAGE IN FORM PREVIEW:
const visibleFields = fields.filter(field => {
  if (!field.condition) return true;
  
  const result = evaluateCondition(field.condition, formValues);
  return field.condition_action === 'hide' ? !result : result;
});`
              },
              {
                name: "Multi-Page Forms",
                description: "Split long forms into stepped wizard with progress",
                code: `// src/modules/dynamic-forms/utils/pageUtils.ts

export interface FormPage {
  pageNumber: number;
  title: string;
  fields: FormField[];
}

// ORGANIZE FIELDS INTO PAGES:
export function organizeFieldsIntoPages(fields: FormField[]): FormPage[] {
  const pages: FormPage[] = [];
  let currentPage: FormPage = {
    pageNumber: 1,
    title: 'Page 1',
    fields: []
  };

  const sortedFields = [...fields].sort((a, b) => a.order - b.order);

  for (const field of sortedFields) {
    if (field.type === 'page_break') {
      // Save current page and start new one
      if (currentPage.fields.length > 0) {
        pages.push(currentPage);
      }
      currentPage = {
        pageNumber: pages.length + 1,
        title: field.label_en || \\\`Page \\\${pages.length + 1}\\\`,
        fields: []
      };
    } else {
      currentPage.fields.push(field);
    }
  }

  // Don't forget the last page
  if (currentPage.fields.length > 0) {
    pages.push(currentPage);
  }

  return pages;
}

// STEPPED FORM PREVIEW COMPONENT:
// SteppedFormPreview.tsx renders:
// - Progress indicator (current page / total pages)
// - Current page fields only
// - Previous/Next navigation buttons
// - Validation per page before advancing`
              },
              {
                name: "Permission-Based Access Control",
                description: "RBAC integration for Dynamic Forms module",
                code: `// PERMISSION MODULE: dynamic_forms
// AVAILABLE ACTIONS: create, read, update, delete

// FRONTEND PERMISSION CHECKS:
// DynamicFormsPage.tsx
const { isMainAdmin, hasPermission } = usePermissions();
const canView = isMainAdmin || hasPermission('dynamic_forms', 'read');
const canCreate = isMainAdmin || hasPermission('dynamic_forms', 'create');
const canEdit = isMainAdmin || hasPermission('dynamic_forms', 'update');
const canDelete = isMainAdmin || hasPermission('dynamic_forms', 'delete');

// Redirect if no view permission
useEffect(() => {
  if (!permissionsLoading && !canView) {
    toast({ title: 'Access Denied', variant: 'destructive' });
    navigate('/dashboard/settings', { replace: true });
  }
}, [canView, permissionsLoading]);

// CONDITIONAL ACTION RENDERING:
// FormsTable.tsx - Actions based on permissions
<DropdownMenuContent>
  {canView && <DropdownMenuItem>Preview</DropdownMenuItem>}
  {canEdit && <DropdownMenuItem>Edit</DropdownMenuItem>}
  {canCreate && <DropdownMenuItem>Duplicate</DropdownMenuItem>}
  {canDelete && <DropdownMenuItem>Delete</DropdownMenuItem>}
</DropdownMenuContent>

// PERMISSION BUTTON COMPONENT:
<PermissionButton
  module="dynamic_forms"
  action="create"
  onClick={handleCreate}
  tooltipWhenDisabled="No permission to create forms"
>
  Create Form
</PermissionButton>`
              },
              {
                name: "Action Logging Integration",
                description: "All CRUD operations logged to system audit trail",
                code: `// useDynamicForms.ts - Logging integration

import { useActionLogger } from '@/hooks/useActionLogger';

export function useCreateDynamicForm() {
  const { logFormSubmit } = useActionLogger('DynamicForms');
  
  return useMutation({
    mutationFn: (dto) => dynamicFormsService.create(dto),
    onSuccess: (data) => {
      logFormSubmit('Create Dynamic Form', true, {
        entityType: 'DynamicForm',
        entityId: data.id,
        details: \\\`Created form: \\\${data.name_en}\\\`,
      });
      toast({ title: 'Form created successfully' });
    },
    onError: (error) => {
      logFormSubmit('Create Dynamic Form', false, {
        entityType: 'DynamicForm',
        details: \\\`Error: \\\${error.message}\\\`,
      });
    },
  });
}

// LOGGED ACTIONS:
// - Create form      → logFormSubmit('Create Dynamic Form', ...)
// - Update form      → logFormSubmit('Update Dynamic Form', ...)
// - Delete form      → logAction('delete', 'Deleted dynamic form', ...)
// - Duplicate form   → logAction('duplicate', 'Duplicated form', ...)
// - Status change    → logAction('status_change', 'Changed status', ...)
// - Submit response  → logFormSubmit('Submit Form Response', ...)
// - Search/filter    → logSearch(), logFilter()
// - Export PDF       → logExport('PDF', count, ...)`
              },
              {
                name: "PDF Generation",
                description: "Multi-page PDF export with company branding",
                code: `// src/modules/dynamic-forms/components/FormResponsePDF.tsx

import { Document, Page, Text, View, Image } from '@react-pdf/renderer';
import { organizeFieldsIntoPages } from '../utils/pageUtils';

export function FormResponsePDF({
  form,
  response,
  companyLogo,
  companyName,
  language = 'en'
}: FormResponsePDFProps) {
  const pages = organizeFieldsIntoPages(form.fields);
  const isEnglish = language === 'en';

  return (
    <Document>
      {pages.map((page, pageIndex) => (
        <Page key={pageIndex} size="A4" style={styles.page}>
          {/* Company Header */}
          <View style={styles.header}>
            {companyLogo?.startsWith('data:image/') && <Image src={companyLogo} style={styles.logo} />}
            <Text style={styles.formTitle}>
              {isEnglish ? form.name_en : form.name_fr}
            </Text>
          </View>

          {/* Page Title (for multi-page forms) */}
          {pages.length > 1 && (
            <Text style={styles.pageTitle}>{page.title}</Text>
          )}

          {/* Fields */}
          {page.fields
            .filter(f => f.type !== 'page_break')
            .map((field) => (
              <View key={field.id} style={styles.fieldRow}>
                <Text style={styles.label}>
                  {isEnglish ? field.label_en : field.label_fr}
                </Text>
                <Text style={styles.value}>
                  {renderFieldValue(field, response.responses[field.id])}
                </Text>
              </View>
            ))}

          {/* Page Footer */}
          <Text style={styles.footer}>
            Page {pageIndex + 1} of {pages.length}
          </Text>
        </Page>
      ))}
    </Document>
  );
}

// SPECIAL FIELD RENDERING:
function renderFieldValue(field, value) {
  switch (field.type) {
    case 'signature':
      return <Image src={value} style={styles.signature} />;
    case 'rating':
      return '★'.repeat(value) + '☆'.repeat((field.maxStars || 5) - value);
    case 'checkbox':
      return value ? '✓ Yes' : '✗ No';
    default:
      return String(value || '-');
  }
}`
              },
              {
                name: "Backend Models & DTOs",
                description: "C# backend data structures",
                code: `// FlowServiceBackendOnlyFinal-main/Modules/DynamicForms/Models/DynamicForm.cs

public enum FieldType
{
    text, textarea, number, email, phone,
    date, time, datetime,
    select, radio, checkbox, yes_no,
    signature, photo, rating, location,
    section, page_break
}

public class FormField
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public FieldType Type { get; set; }
    public string LabelEn { get; set; } = string.Empty;
    public string LabelFr { get; set; } = string.Empty;
    public string? PlaceholderEn { get; set; }
    public string? PlaceholderFr { get; set; }
    public bool Required { get; set; }
    public int Order { get; set; }
    public List<FieldOption>? Options { get; set; }
    public int? Min { get; set; }
    public int? Max { get; set; }
    public int? MaxStars { get; set; }
    public int? MaxPhotos { get; set; }
    
    // Conditional logic
    public FieldCondition? Condition { get; set; }
    public string? ConditionAction { get; set; } // "show" or "hide"
}

public class FieldCondition
{
    public string FieldId { get; set; } = string.Empty;
    public string Operator { get; set; } = "equals";
    public object? Value { get; set; }
}

// DTOs for API communication
public class DynamicFormDto
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameFr { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionFr { get; set; }
    public string? Category { get; set; }
    public string Status { get; set; } = "draft";
    public int Version { get; set; } = 1;
    public List<FormField> Fields { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}`
              },
              {
                name: "Form Status Workflow",
                description: "Form lifecycle states and transitions",
                code: `// FORM STATUS WORKFLOW:
// 
// ┌─────────┐     Release      ┌──────────┐     Archive     ┌──────────┐
// │  DRAFT  │ ───────────────> │ RELEASED │ ──────────────> │ ARCHIVED │
// │         │                  │          │                 │          │
// │ - Edit  │                  │ - Active │                 │ - Hidden │
// │ - Test  │                  │ - Fillable│                │ - Readonly│
// └─────────┘ <─────────────── └──────────┘ <────────────── └──────────┘
//               Revert to Draft              Restore

export type FormStatus = 'draft' | 'released' | 'archived';

export const STATUS_COLORS = {
  draft: 'bg-warning/10 text-warning',
  released: 'bg-success/10 text-success',
  archived: 'bg-muted text-muted-foreground',
};

// STATUS TRANSITIONS:
// draft → released:    Form goes live, can accept responses
// released → archived: Form hidden from active list
// archived → draft:    Restore form for editing
// released → draft:    Revert to edit mode (not recommended)`
              },
              {
                name: "Public Forms & Sharing",
                description: "Share forms externally without requiring login",
                code: `// ============= PUBLIC FORMS FEATURE =============
// Route: /public/forms/:slug
// Backend: PublicFormsController.cs + DynamicFormService.cs

// MAKING A FORM PUBLIC:
// 1. Form must be in 'released' status
// 2. Click globe/share icon or menu → "Make Public"
// 3. System generates unique slug from form name
// 4. Share URL: /public/forms/{slug}

// FRONTEND: PublicFormPage.tsx
// - No authentication required
// - Theme support (dark/light via URL param)
// - Language toggle (EN/FR)
// - Optional submitter info (name/email)
// - Custom thank you page

// DATABASE COLUMNS (DynamicForms table):
ALTER TABLE "DynamicForms" 
ADD COLUMN "IsPublic" BOOLEAN NOT NULL DEFAULT FALSE;
ADD COLUMN "PublicSlug" VARCHAR(200);
ADD COLUMN "ThankYouSettings" JSONB;

// THANK YOU PAGE SETTINGS SCHEMA:
interface ThankYouSettings {
  default_message: {
    title_en: string;
    title_fr: string;
    message_en: string;
    message_fr: string;
    enable_redirect: boolean;
    redirect_url?: string;
    redirect_delay?: number; // seconds
  };
  rules: ThankYouRule[];  // Conditional messages
}

// CONDITIONAL RULES:
// - Show different messages based on form answers
// - Example: "Excellent!" if rating > 4
// - Priority-ordered (first match wins)
// - Same operators as field conditions

// API ENDPOINTS:
// GET  /api/PublicForms/{slug}     - Get form by slug (no auth)
// POST /api/PublicForms/{slug}     - Submit response (no auth)`
              },
              {
                name: "Form Response Tracking",
                description: "Track submissions from both authenticated and public users",
                code: `// DynamicFormResponses table additions:
ALTER TABLE "DynamicFormResponses" 
ADD COLUMN "SubmitterName" VARCHAR(200);
ADD COLUMN "SubmitterEmail" VARCHAR(200);
ADD COLUMN "IsPublicSubmission" BOOLEAN DEFAULT FALSE;

// RESPONSE TRACKING:
// - Authenticated: UserId is stored
// - Public: SubmitterName/Email optional
// - IsPublicSubmission flag differentiates

// VIEW RESPONSES:
// Route: /dashboard/settings/dynamic-forms/:id/responses
// Features:
// - View all submissions
// - Filter by date, submitter
// - Export to PDF
// - Export to Excel`
              }
            ]
          }
        ],
        
        keyPatterns: {
          title: "Key Backend Patterns",
          patterns: [
            {
              name: "Entity Conversion Pattern",
              description: "Data flows through entities with preserved relationships",
              diagram: `Offer (OfferItems) → Sale (SaleItems) → ServiceOrder (Jobs + Materials) → Dispatch (TimeEntries + Expenses + MaterialUsage)`
            },
            {
              name: "Status Tracking Pattern",
              description: "Each entity tracks its previous entity reference",
              examples: [
                "Sale.OfferId → links back to source offer",
                "ServiceOrder.SaleId → links back to source sale",
                "Dispatch.JobId → links back to source job"
              ]
            },
            {
              name: "Activity Logging Pattern",
              description: "All major operations create activity records for audit trail",
              tables: ["OfferActivity", "SaleActivity", "ServiceOrderActivity"]
            }
          ]
        }
      }
    },
    
    swagger: {
      title: "Swagger API Documentation",
      content: `Interactive API documentation is available at the Swagger endpoint:

┌─────────────────────────────────────────────────────────────┐
│                    SWAGGER UI                                │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  URL: https://api.flowentra.app/swagger               │
│  (or /api-docs depending on configuration)                   │
│                                                              │
│  FEATURES:                                                   │
│  ─────────                                                   │
│  • Interactive API testing                                  │
│  • Request/response schema documentation                    │
│  • Authentication support (click "Authorize")               │
│  • Model definitions with field descriptions                │
│  • Try-it-out functionality for all endpoints               │
│                                                              │
│  HOW TO USE:                                                 │
│  ────────────                                                │
│  1. Navigate to /swagger in your browser                    │
│  2. Click "Authorize" button in top right                   │
│  3. Enter your JWT token: Bearer <your-token>               │
│  4. Click "Authorize" and close dialog                      │
│  5. Expand any endpoint section                             │
│  6. Click "Try it out" to test                              │
│  7. Fill in parameters and click "Execute"                  │
│  8. View response in the results section                    │
│                                                              │
│  GETTING A TEST TOKEN:                                       │
│  ─────────────────────                                       │
│  In development, use /api/dev/token for a test JWT          │
│  Or login via /api/auth/admin/login to get a real token     │
│                                                              │
└─────────────────────────────────────────────────────────────┘`
    },
    
    security: {
      title: "Security Architecture",
      content: `The application implements multiple security layers:

┌─────────────────────────────────────────────────────────────┐
│                    AUTHENTICATION                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  JWT TOKEN AUTHENTICATION:                                   │
│  • Access token: Short-lived (1 hour default)               │
│  • Refresh token: Long-lived (7 days default)               │
│  • Secure token storage (HttpOnly cookies or secure storage)│
│  • Automatic token refresh before expiration                │
│                                                              │
│  PASSWORD SECURITY:                                          │
│  • BCrypt hashing with salt (work factor 12)                │
│  • No password stored in plain text                         │
│  • Password strength validation on registration             │
│                                                              │
│  TWO USER TYPES:                                             │
│  • MainAdminUser (id=1): Full system access, no permissions │
│  • User (id≥2): Regular staff with role-based permissions   │
│                                                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    AUTHORIZATION (RBAC)                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ROLE-BASED ACCESS CONTROL:                                  │
│  • Roles define sets of permissions                         │
│  • Users are assigned to one role                           │
│  • Permissions checked at route and UI level                │
│  • MainAdmin bypasses all permission checks                 │
│                                                              │
│  PERMISSION STRUCTURE:                                       │
│  • Module: contacts, sales, offers, etc.                    │
│  • Action: create, read, update, delete, export, import     │
│  • Format: "module:action" (e.g., "contacts:create")        │
│                                                              │
│  ENFORCEMENT POINTS:                                         │
│  • Frontend: Route protection, UI element hiding            │
│  • Backend: Controller authorization (future enhancement)   │
│                                                              │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    API SECURITY                              │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  REQUEST SECURITY:                                           │
│  • HTTPS enforcement in production                          │
│  • CORS configuration for allowed origins                   │
│  • Request size limits                                      │
│  • Rate limiting (configurable)                             │
│                                                              │
│  DATA SECURITY:                                              │
│  • Parameterized queries (SQL injection prevention)         │
│  • Input validation with data annotations                   │
│  • Output encoding (XSS prevention)                         │
│  • Soft delete for data recovery                            │
│                                                              │
│  AUDIT LOGGING:                                              │
│  • All CRUD operations logged                               │
│  • User ID and timestamp recorded                           │
│  • Success/failure status captured                          │
│  • Detailed error messages stored                           │
│                                                              │
└─────────────────────────────────────────────────────────────┘`
    },
    
    errorHandling: {
      title: "Error Handling",
      subtitle: "Comprehensive error handling patterns for frontend and backend",
      
      frontendErrors: {
        title: "Frontend Error Handling",
        patterns: [
          {
            name: "API Error Response Structure",
            description: "Standard error response format from the API",
            code: `// Standard API Error Response
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid request data",
    "details": {
      "email": ["Email is required", "Invalid email format"],
      "password": ["Password must be at least 8 characters"]
    }
  }
}

// Network Error Response
{
  "success": false,
  "error": {
    "code": "NETWORK_ERROR",
    "message": "Unable to connect to server"
  }
}

// Authentication Error
{
  "success": false,
  "error": {
    "code": "UNAUTHORIZED",
    "message": "Session expired. Please log in again."
  }
}`
          },
          {
            name: "TanStack Query Error Handling",
            description: "Handling errors in React Query mutations and queries",
            code: `// Mutation with Error Handling
const mutation = useMutation({
  mutationFn: contactsApi.create,
  onSuccess: (data) => {
    queryClient.invalidateQueries({ queryKey: ['contacts'] });
    toast.success('Contact created successfully');
    onClose();
  },
  onError: (error: any) => {
    // Handle different error types
    if (error.response?.status === 400) {
      // Validation error - show field-specific errors
      const errors = error.response.data.error?.details;
      if (errors) {
        Object.keys(errors).forEach(field => {
          form.setError(field as any, { 
            message: errors[field][0] 
          });
        });
      }
      toast.error('Please fix the validation errors');
    } else if (error.response?.status === 401) {
      // Auth error - handled by interceptor
      toast.error('Session expired');
    } else if (error.response?.status === 403) {
      toast.error('You do not have permission to perform this action');
    } else if (error.response?.status === 404) {
      toast.error('Resource not found');
    } else if (error.response?.status === 409) {
      toast.error('A conflict occurred. The resource may already exist.');
    } else {
      // Generic error
      toast.error(error.message || 'An unexpected error occurred');
    }
  }
});

// Query with Error State
const { data, isLoading, error, isError } = useQuery({
  queryKey: ['contacts', id],
  queryFn: () => contactsApi.getById(id),
  retry: 2,
  retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
});

// Display error state
if (isError) {
  return (
    <Alert variant="destructive">
      <AlertTriangle className="h-4 w-4" />
      <AlertTitle>Error</AlertTitle>
      <AlertDescription>
        {error.message || 'Failed to load data'}
      </AlertDescription>
    </Alert>
  );
}`
          },
          {
            name: "Form Validation Errors",
            description: "Zod schema validation with react-hook-form",
            code: `// Zod Schema with Custom Errors
const contactSchema = z.object({
  firstName: z.string()
    .min(1, 'First name is required')
    .max(100, 'First name must be less than 100 characters'),
  lastName: z.string()
    .min(1, 'Last name is required'),
  email: z.string()
    .email('Invalid email address')
    .optional()
    .or(z.literal('')),
  phone: z.string()
    .regex(/^\\+?[\\d\\s\\-()]+$/, 'Invalid phone format')
    .optional()
    .or(z.literal('')),
});

// Form with Validation
const form = useForm<ContactFormData>({
  resolver: zodResolver(contactSchema),
  defaultValues: {
    firstName: '',
    lastName: '',
    email: '',
    phone: ''
  }
});

// Display Field Errors
<FormField
  control={form.control}
  name="email"
  render={({ field, fieldState }) => (
    <FormItem>
      <FormLabel>Email</FormLabel>
      <FormControl>
        <Input 
          {...field} 
          className={fieldState.error ? 'border-destructive' : ''}
        />
      </FormControl>
      {fieldState.error && (
        <FormMessage className="text-destructive">
          {fieldState.error.message}
        </FormMessage>
      )}
    </FormItem>
  )}
/>`
          },
          {
            name: "Global Error Boundary",
            description: "Catching unexpected React errors",
            code: `// Error Boundary Component
class ErrorBoundary extends React.Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    // Log to error tracking service
    console.error('Uncaught error:', error, errorInfo);
    logger.error(\`Uncaught error: \${error.message}\`, 'App', 'error');
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="flex flex-col items-center justify-center h-screen">
          <AlertCircle className="h-16 w-16 text-destructive mb-4" />
          <h1 className="text-2xl font-bold mb-2">Something went wrong</h1>
          <p className="text-muted-foreground mb-4">
            {this.state.error?.message || 'An unexpected error occurred'}
          </p>
          <Button onClick={() => window.location.reload()}>
            Reload Page
          </Button>
        </div>
      );
    }

    return this.props.children;
  }
}`
          }
        ]
      },
      
      backendErrors: {
        title: "Backend Error Handling (.NET)",
        patterns: [
          {
            name: "Controller Error Responses",
            description: "Standard HTTP status codes and error formats",
            code: `// Controller with Error Handling
[HttpPost]
public async Task<IActionResult> CreateContact([FromBody] CreateContactRequestDto dto)
{
    // Validation Errors (400 Bad Request)
    if (!ModelState.IsValid)
    {
        return BadRequest(new ApiResponse<object>
        {
            Success = false,
            Message = "Validation failed",
            Errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
        });
    }

    try
    {
        // Check for duplicates (409 Conflict)
        if (await _service.ContactExistsAsync(dto.Email))
        {
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Message = "A contact with this email already exists"
            });
        }

        var result = await _service.CreateContactAsync(dto, User.Identity.Name);
        
        // Success (201 Created)
        return CreatedAtAction(
            nameof(GetContact), 
            new { id = result.Id }, 
            ApiResponse<ContactResponseDto>.SuccessResponse(result)
        );
    }
    catch (UnauthorizedAccessException)
    {
        // Permission denied (403 Forbidden)
        return Forbid();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating contact");
        
        // Server error (500 Internal Server Error)
        return StatusCode(500, new ApiResponse<object>
        {
            Success = false,
            Message = "An unexpected error occurred"
        });
    }
}`
          },
          {
            name: "Service Layer Exceptions",
            description: "Custom exception types for business logic errors",
            code: `// Custom Exception Types
public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with ID {key} was not found") { }
}

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }
    
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

// Usage in Service
public async Task<ContactResponseDto> GetContactByIdAsync(int id)
{
    var contact = await _context.Contacts.FindAsync(id);
    
    if (contact == null)
        throw new NotFoundException("Contact", id);
        
    return _mapper.Map<ContactResponseDto>(contact);
}`
          },
          {
            name: "Global Exception Handler Middleware",
            description: "Centralized exception handling for consistent error responses",
            code: `// Exception Handler Middleware
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ApiResponse<object> { Success = false };

        switch (ex)
        {
            case NotFoundException notFound:
                response.StatusCode = 404;
                errorResponse.Message = notFound.Message;
                break;
                
            case ValidationException validation:
                response.StatusCode = 400;
                errorResponse.Message = validation.Message;
                errorResponse.Errors = validation.Errors
                    .SelectMany(e => e.Value);
                break;
                
            case ConflictException conflict:
                response.StatusCode = 409;
                errorResponse.Message = conflict.Message;
                break;
                
            case UnauthorizedAccessException:
                response.StatusCode = 403;
                errorResponse.Message = "Access denied";
                break;
                
            default:
                response.StatusCode = 500;
                errorResponse.Message = "An internal error occurred";
                _logger.LogError(ex, "Unhandled exception");
                break;
        }

        await response.WriteAsJsonAsync(errorResponse);
    }
}

// Register in Program.cs
app.UseMiddleware<ExceptionHandlingMiddleware>();`
          }
        ]
      },
      
      httpStatusCodes: {
        title: "HTTP Status Codes Reference",
        codes: [
          { code: 200, name: "OK", description: "Request succeeded. Response contains requested data.", usage: "GET, PUT, PATCH success" },
          { code: 201, name: "Created", description: "Resource created successfully.", usage: "POST success with new resource" },
          { code: 204, name: "No Content", description: "Request succeeded with no response body.", usage: "DELETE success" },
          { code: 400, name: "Bad Request", description: "Invalid request data or validation failed.", usage: "Validation errors, malformed JSON" },
          { code: 401, name: "Unauthorized", description: "Authentication required or token expired.", usage: "Missing/invalid JWT token" },
          { code: 403, name: "Forbidden", description: "Authenticated but not authorized.", usage: "Insufficient permissions" },
          { code: 404, name: "Not Found", description: "Requested resource does not exist.", usage: "Invalid ID, deleted resource" },
          { code: 409, name: "Conflict", description: "Request conflicts with current state.", usage: "Duplicate email, concurrent edit" },
          { code: 422, name: "Unprocessable Entity", description: "Semantic errors in request.", usage: "Business rule violations" },
          { code: 429, name: "Too Many Requests", description: "Rate limit exceeded.", usage: "API throttling" },
          { code: 500, name: "Internal Server Error", description: "Unexpected server error.", usage: "Database errors, unhandled exceptions" },
          { code: 503, name: "Service Unavailable", description: "Server temporarily unavailable.", usage: "Maintenance, overload" }
        ]
      }
    },
    
    troubleshooting: {
      title: "Troubleshooting Guide",
      subtitle: "Common issues and their solutions",
      
      categories: [
        {
          name: "Authentication Issues",
          icon: "Key",
          issues: [
            {
              problem: "401 Unauthorized - Token expired",
              symptoms: ["Automatic logout", "API calls failing", "Redirect to login page"],
              causes: ["JWT token expired", "Token not refreshed", "Invalid token format"],
              solutions: [
                "Clear localStorage and log in again",
                "Check if refresh token endpoint is working",
                "Verify token expiration time in JWT settings",
                "Check browser console for token refresh errors"
              ],
              codeExample: `// Check token expiration
const token = localStorage.getItem('access_token');
if (token) {
  const payload = JSON.parse(atob(token.split('.')[1]));
  const expiry = new Date(payload.exp * 1000);
  console.log('Token expires:', expiry);
  console.log('Is expired:', expiry < new Date());
}

// Force token refresh
import { authService } from '@/services/authService';
await authService.refreshToken();`
            },
            {
              problem: "403 Forbidden - Permission denied",
              symptoms: ["Button/menu hidden", "Page shows 'Access Denied'", "API returns 403"],
              causes: ["User role missing permission", "Wrong module/action check", "MainAdmin check failing"],
              solutions: [
                "Verify user's role has the required permission",
                "Check RolesController permission assignments",
                "Use hasPermission() hook to debug permissions",
                "Verify MainAdmin user has id=1"
              ],
              codeExample: `// Debug permissions
const { hasPermission, isMainAdmin } = usePermissions();
console.log('Is MainAdmin:', isMainAdmin);
console.log('Has contacts:read:', hasPermission('contacts', 'read'));
console.log('Has sales:create:', hasPermission('sales', 'create'));

// Check user data
const userData = JSON.parse(localStorage.getItem('user_data') || '{}');
console.log('User ID:', userData.id);
console.log('Role:', userData.role);`
            },
            {
              problem: "Login fails - Invalid credentials",
              symptoms: ["Login form shows error", "No token returned", "User stays on login page"],
              causes: ["Wrong email/password", "User account deactivated", "Backend not accessible"],
              solutions: [
                "Verify email and password are correct",
                "Check if user account is active in database",
                "Test API endpoint directly with Swagger",
                "Check network tab for API response"
              ],
              codeExample: `// Test login API directly
fetch('https://api.flowentra.app/api/auth/admin/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: 'test@example.com', password: 'password' })
})
.then(r => r.json())
.then(console.log)
.catch(console.error);`
            }
          ]
        },
        {
          name: "API Connection Issues",
          icon: "Globe",
          issues: [
            {
              problem: "Network Error - API unreachable",
              symptoms: ["Requests timeout", "CORS errors", "Connection refused"],
              causes: ["Backend server down", "Wrong API URL", "CORS not configured", "Firewall blocking"],
              solutions: [
                "Check if backend is running (visit /swagger)",
                "Verify VITE_API_URL in environment",
                "Check CORS settings in backend",
                "Try accessing API from different network"
              ],
              codeExample: `// Check API URL
console.log('API URL:', import.meta.env.VITE_API_URL);

// Test API health
fetch('https://api.flowentra.app/api/health')
  .then(r => console.log('API Status:', r.status))
  .catch(e => console.error('API Error:', e));

// Check CORS
// In backend appsettings.json:
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "https://yourdomain.com"]
  }
}`
            },
            {
              problem: "Slow API responses",
              symptoms: ["Long loading times", "Timeouts on large requests", "UI freezing"],
              causes: ["Large dataset without pagination", "Missing database indexes", "Cold start on serverless"],
              solutions: [
                "Implement pagination (page, limit params)",
                "Add indexes to frequently queried columns",
                "Use React Query caching effectively",
                "Show loading states to improve perceived performance"
              ],
              codeExample: `// Use pagination
const { data } = useQuery({
  queryKey: ['contacts', { page: 1, limit: 20 }],
  queryFn: () => contactsApi.getAll({ pageNumber: 1, pageSize: 20 }),
  staleTime: 5 * 60 * 1000, // Cache for 5 minutes
});

// Optimistic updates for better UX
const mutation = useMutation({
  mutationFn: contactsApi.update,
  onMutate: async (newData) => {
    await queryClient.cancelQueries(['contacts', id]);
    const previous = queryClient.getQueryData(['contacts', id]);
    queryClient.setQueryData(['contacts', id], newData);
    return { previous };
  },
  onError: (err, newData, context) => {
    queryClient.setQueryData(['contacts', id], context.previous);
  }
});`
            }
          ]
        },
        {
          name: "Data Issues",
          icon: "Database",
          issues: [
            {
              problem: "Data not updating after save",
              symptoms: ["Old data still showing", "Need to refresh page", "Changes not persisted"],
              causes: ["Query cache not invalidated", "Optimistic update failed", "Backend save failed silently"],
              solutions: [
                "Invalidate relevant queries after mutation",
                "Check network tab for successful API response",
                "Verify data in database directly",
                "Add onSuccess callback to mutations"
              ],
              codeExample: `// Proper cache invalidation
const mutation = useMutation({
  mutationFn: contactsApi.update,
  onSuccess: () => {
    // Invalidate all related queries
    queryClient.invalidateQueries({ queryKey: ['contacts'] });
    queryClient.invalidateQueries({ queryKey: ['contact', id] });
    toast.success('Saved successfully');
  }
});

// Force refetch
const { refetch } = useQuery({...});
await refetch();

// Clear all cache
queryClient.clear();`
            },
            {
              problem: "Duplicate records created",
              symptoms: ["Same record appears twice", "Form submitted multiple times", "Race conditions"],
              causes: ["Double-click on submit", "No duplicate checking", "Optimistic update + server response"],
              solutions: [
                "Disable submit button during mutation",
                "Implement backend uniqueness constraints",
                "Use mutation.isPending to prevent double submit",
                "Add client-side debouncing"
              ],
              codeExample: `// Prevent double submission
<Button 
  type="submit" 
  disabled={mutation.isPending}
>
  {mutation.isPending ? 'Saving...' : 'Save'}
</Button>

// Backend uniqueness check
if (await _context.Contacts.AnyAsync(c => c.Email == dto.Email))
{
    return Conflict("Email already exists");
}`
            }
          ]
        },
        {
          name: "UI/Display Issues",
          icon: "Code",
          issues: [
            {
              problem: "Component not rendering",
              symptoms: ["Blank area on page", "Component shows nothing", "No errors in console"],
              causes: ["Conditional rendering false", "Data still loading", "Empty array returned", "Permission check failing"],
              solutions: [
                "Add console.log to check render conditions",
                "Verify isLoading and error states",
                "Check if data array has items",
                "Verify permission checks with hasPermission()"
              ],
              codeExample: `// Debug rendering
console.log('Data:', data);
console.log('Is Loading:', isLoading);
console.log('Error:', error);
console.log('Has Permission:', hasPermission('contacts', 'read'));

// Show loading state
if (isLoading) return <Skeleton />;
if (error) return <ErrorMessage error={error} />;
if (!data?.length) return <EmptyState />;

return <DataList data={data} />;`
            },
            {
              problem: "Style/Layout broken",
              symptoms: ["Elements overlapping", "Wrong colors", "Responsive issues"],
              causes: ["CSS conflicts", "Missing Tailwind classes", "Dark mode not handled", "Mobile viewport issues"],
              solutions: [
                "Check browser DevTools for CSS conflicts",
                "Verify Tailwind classes are correct",
                "Test both light and dark modes",
                "Use responsive prefixes (sm:, md:, lg:)"
              ],
              codeExample: `// Responsive and theme-aware component
<div className="
  p-4 
  bg-background 
  text-foreground
  dark:bg-background
  sm:p-6 
  md:p-8
  lg:flex lg:gap-4
">
  <Card className="
    w-full 
    lg:w-1/2
    border-border
  ">
    ...
  </Card>
</div>`
            }
          ]
        }
      ],
      
      debuggingTools: {
        title: "Debugging Tools & Techniques",
        tools: [
          {
            name: "Browser DevTools",
            description: "Essential browser debugging features",
            tips: [
              "Console tab: View logs, errors, and run JavaScript",
              "Network tab: Inspect API requests/responses, check status codes",
              "Application tab: View localStorage, sessionStorage, cookies",
              "React DevTools: Inspect component state and props"
            ]
          },
          {
            name: "React Query DevTools",
            description: "Debug TanStack Query cache and mutations",
            tips: [
              "View all cached queries and their state",
              "Manually invalidate or refetch queries",
              "See query timing and stale status",
              "Debug mutation states"
            ]
          },
          {
            name: "System Logs",
            description: "Application audit trail and error logs",
            tips: [
              "Navigate to Settings > System Logs",
              "Filter by level (error, warning, info)",
              "Search by module or action",
              "Export logs for analysis"
            ]
          },
          {
            name: "Swagger UI",
            description: "Test API endpoints directly",
            tips: [
              "Access at /swagger endpoint",
              "Try endpoints with different parameters",
              "View request/response schemas",
              "Test authentication flow"
            ]
          }
        ]
      },
      
      commonErrorMessages: {
        title: "Common Error Messages",
        errors: [
          { message: "Failed to fetch", cause: "Network error or API unavailable", solution: "Check internet connection and API status" },
          { message: "JWT token is expired", cause: "Access token has expired", solution: "Log out and log in again, or check refresh token flow" },
          { message: "CORS policy blocked", cause: "API not configured for this origin", solution: "Add frontend URL to backend CORS settings" },
          { message: "Cannot read properties of undefined", cause: "Accessing property on null/undefined", solution: "Add null checks or optional chaining (?. operator)" },
          { message: "Duplicate key value violates unique constraint", cause: "Trying to insert duplicate record", solution: "Check for existing record before insert" },
          { message: "Foreign key constraint fails", cause: "Referenced record doesn't exist", solution: "Ensure parent record exists before creating child" },
          { message: "Maximum update depth exceeded", cause: "Infinite re-render loop", solution: "Check useEffect dependencies and state updates" },
          { message: "Hydration failed", cause: "Server/client HTML mismatch", solution: "Ensure consistent rendering between server and client" }
        ]
      }
    },
    
    files: {
      title: "Key Files Reference",
      categories: [
        {
          name: "Frontend Core Files",
          items: [
            { path: "src/App.tsx", description: "Application root with all providers, routing setup, and global error handling" },
            { path: "src/main.tsx", description: "Entry point that renders App into DOM" },
            { path: "src/index.css", description: "Global styles, Tailwind config, CSS variables, and theme tokens" },
            { path: "src/contexts/AuthContext.tsx", description: "Authentication state management, login/logout methods, token handling" },
            { path: "src/hooks/usePermissions.tsx", description: "RBAC hook for checking user permissions across the app" },
            { path: "src/services/api/apiClient.ts", description: "Axios instance with interceptors for auth and error handling" },
            { path: "src/services/authService.ts", description: "Authentication API calls, token storage, session management" }
          ]
        },
        {
          name: "Frontend Module Files",
          items: [
            { path: "src/modules/dashboard/pages/Dashboard.tsx", description: "Main dashboard layout with sidebar and content area" },
            { path: "src/modules/dashboard/components/AppSidebar.tsx", description: "Navigation sidebar with permission-based menu filtering" },
            { path: "src/modules/dashboard/components/DashboardContent.tsx", description: "Route definitions for all dashboard pages" },
            { path: "src/components/permissions/PermissionRoute.tsx", description: "Route wrapper that checks permissions before rendering" },
            { path: "src/modules/contacts/pages/ContactsPage.tsx", description: "Contacts list view with CRUD operations" },
            { path: "src/modules/sales/pages/SalesPage.tsx", description: "Sales pipeline with filtering and management" },
            { path: "src/modules/settings/pages/SettingsPage.tsx", description: "Settings hub with navigation to sub-pages" }
          ]
        },
        {
          name: "Backend Core Files",
          items: [
            { path: "Program.cs", description: "Application entry, DI configuration, middleware setup" },
            { path: "Data/ApplicationDbContext.cs", description: "Entity Framework DbContext with all entity configurations" },
            { path: "Modules/Auth/Controllers/AuthController.cs", description: "Authentication endpoints (login, logout, refresh)" },
            { path: "Modules/Auth/Services/AuthService.cs", description: "JWT generation, password validation, token refresh" },
            { path: "Modules/Shared/Services/SystemLogService.cs", description: "Centralized audit logging service" }
          ]
        },
        {
          name: "Backend Module Files",
          items: [
            { path: "Modules/Contacts/Controllers/ContactsController.cs", description: "Contact CRUD endpoints with search and bulk import" },
            { path: "Modules/Sales/Controllers/SalesController.cs", description: "Sales endpoints with offer conversion and item management" },
            { path: "Modules/Roles/Controllers/RolesController.cs", description: "Role and permission management endpoints" },
            { path: "Modules/Users/Controllers/UsersController.cs", description: "User management endpoints" }
          ]
        },
        {
          name: "Configuration Files",
          items: [
            { path: "package.json", description: "Frontend dependencies and scripts" },
            { path: "vite.config.ts", description: "Vite build configuration" },
            { path: "tailwind.config.ts", description: "Tailwind CSS configuration with custom theme" },
            { path: "tsconfig.json", description: "TypeScript compiler configuration" },
            { path: "appsettings.json", description: "Backend application settings" }
          ]
        }
      ]
    }
  },
  
  // French translations
  fr: {
    title: "Documentation Technique",
    subtitle: "Architecture Système Complète & Référence API",
    
    overview: {
      title: "Vue d'ensemble du Système",
      description: "FlowService est une plateforme CRM et de gestion des services sur le terrain de niveau entreprise, conçue pour gérer les clients, les pipelines de ventes, les ordres de service, le dispatching des techniciens, les installations et le suivi du temps/des dépenses.",
      
      techStack: {
        title: "Stack Technologique",
        frontend: [
          { name: "React 18", version: "^18.3.1", description: "Bibliothèque UI avec fonctionnalités concurrentes et Suspense" },
          { name: "TypeScript", version: "Latest", description: "JavaScript typé pour une meilleure expérience développeur" },
          { name: "Vite", version: "Latest", description: "Outil de build nouvelle génération avec HMR" },
          { name: "TailwindCSS", version: "Latest", description: "Framework CSS utility-first avec tokens de design personnalisés" },
          { name: "Shadcn/ui", version: "Latest", description: "Bibliothèque de composants accessibles basée sur Radix UI" },
          { name: "TanStack Query", version: "^5.87.1", description: "Gestion d'état asynchrone puissante pour les appels API" },
          { name: "React Router", version: "^6.26.2", description: "Routage côté client avec routes imbriquées" },
          { name: "React Hook Form", version: "^7.53.0", description: "Gestion performante des formulaires avec validation Zod" },
          { name: "i18next", version: "^25.3.2", description: "Framework d'internationalisation (support FR/EN)" },
          { name: "Recharts", version: "^2.12.7", description: "Bibliothèque de visualisation de données" },
          { name: "Lucide React", version: "^0.541.0", description: "Bibliothèque d'icônes élégantes" }
        ],
        backend: [
          { name: ".NET 8", version: "8.0", description: "Runtime multiplateforme haute performance" },
          { name: "ASP.NET Core", version: "8.0", description: "Framework Web API avec support des APIs minimales" },
          { name: "Entity Framework Core", version: "8.0", description: "ORM moderne avec support LINQ" },
          { name: "SQL Server", version: "2019+", description: "Base de données relationnelle d'entreprise" },
          { name: "JWT Authentication", version: "Latest", description: "Authentification sécurisée par token" },
          { name: "BCrypt.Net", version: "Latest", description: "Algorithme de hachage de mot de passe sécurisé" },
          { name: "Swagger/OpenAPI", version: "Latest", description: "Documentation et test API" },
          { name: "Serilog", version: "Latest", description: "Framework de journalisation structurée" }
        ]
      },
      
      architecture: {
        title: "Architecture Système",
        content: `┌─────────────────────────────────────────────────────────────────┐
│                      COUCHE CLIENT                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   React     │  │  TanStack   │  │    React Router         │  │
│  │   18 + TS   │  │   Query     │  │    Routes Protégées     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  Shadcn/ui  │  │ TailwindCSS │  │   i18next (FR/EN)       │  │
│  │  Composants │  │   Styling   │  │ Internationalisation    │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │ HTTP/REST
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      COUCHE API                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  ASP.NET    │  │    JWT      │  │      Swagger/OpenAPI    │  │
│  │  Core 8.0   │  │    Auth     │  │      Documentation      │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Contrôleurs │  │  Services   │  │        DTOs             │  │
│  │   (REST)    │  │  (Logique)  │  │  (Transfert Données)    │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │ Entity Framework Core
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      COUCHE DONNÉES                              │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                  Base de Données SQL Server                  │ │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐   │ │
│  │  │  Users   │ │ Contacts │ │  Sales   │ │ServiceOrders │   │ │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────┘   │ │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐   │ │
│  │  │  Roles   │ │  Offers  │ │Dispatches│ │ Installations│   │ │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────┘   │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘`
      }
    },
    
    frontend: {
      title: "Architecture Frontend",
      
      structure: {
        title: "Structure des Répertoires du Projet",
        content: `src/
├── App.tsx                    # Composant racine avec providers & routage
├── main.tsx                   # Point d'entrée de l'application
├── index.css                  # Styles globaux & config Tailwind
│
├── assets/                    # Ressources statiques (images, polices)
│
├── components/                # Composants UI partagés
│   ├── ui/                   # Composants Shadcn/ui (40+ composants)
│   ├── shared/               # Composants métier partagés
│   ├── navigation/           # Composants de navigation
│   └── permissions/          # Composants basés sur les permissions
│
├── contexts/                  # Providers React Context
│   ├── AuthContext.tsx       # État et méthodes d'authentification
│   ├── PreferencesContext.tsx# État des préférences utilisateur
│   └── LoadingContext.tsx    # État de chargement global
│
├── hooks/                     # Hooks React personnalisés
│   ├── usePermissions.tsx    # Hook de vérification des permissions (RBAC)
│   ├── useUserType.ts        # Détection MainAdmin vs User
│   └── ...                   # Autres hooks utilitaires
│
├── services/                  # Couche de services API
│   ├── authService.ts        # Appels API d'authentification
│   └── api/                  # Fonctions API par module
│       ├── apiClient.ts      # Instance Axios avec intercepteurs
│       ├── contactsApi.ts    # Opérations CRUD contacts
│       ├── salesApi.ts       # Opérations CRUD ventes
│       └── ...               # Autres services API
│
├── types/                     # Définitions de types TypeScript
├── utils/                     # Fonctions utilitaires
├── locales/                   # Fichiers de traduction i18n
│
└── modules/                   # Modules fonctionnels (28 modules)
    ├── dashboard/            # Tableau de bord principal & layout
    ├── contacts/             # Gestion des contacts
    ├── sales/                # Pipeline de ventes
    ├── offers/               # Devis & propositions
    ├── field/                # Modules service terrain
    │   ├── service-orders/   # Gestion des ordres de travail
    │   ├── dispatches/       # Dispatching des techniciens
    │   ├── installations/    # Suivi des installations
    │   └── time-expenses/    # Suivi temps & dépenses
    ├── settings/             # Paramètres système
    └── ...                   # Autres modules`
      },
      
      modules: {
        title: "Détail des Modules Fonctionnels",
        items: [
          {
            name: "Module Tableau de Bord",
            path: "src/modules/dashboard/",
            description: "Layout principal avec navigation latérale, en-tête et routage du contenu. Contient DashboardGate (lazy loading), DashboardContent (définitions de routes), et AppSidebar (menu de navigation). Gère le filtrage des menus basé sur les permissions.",
            files: ["Dashboard.tsx", "DashboardGate.tsx", "DashboardContent.tsx", "AppSidebar.tsx"]
          },
          {
            name: "Module Contacts",
            path: "src/modules/contacts/",
            description: "Gestion CRM complète des contacts avec vue liste, vue grille, panneaux de détail, dialogues CRUD, recherche/filtrage, import Excel en masse, gestion des tags et suivi d'activité. Supporte les contacts entreprise et individuels.",
            files: ["ContactsPage.tsx", "ContactsList.tsx", "ContactDialog.tsx", "ContactDetailPanel.tsx"]
          },
          {
            name: "Module Ventes",
            path: "src/modules/sales/",
            description: "Gestion du pipeline de ventes avec workflow par étapes (Offre → Négociation → Clôturé → Converti), niveaux de priorité, suivi financier (montant, taxes, remise), gestion des lignes d'articles, journalisation d'activité et conversion offre-vente.",
            files: ["SalesPage.tsx", "SalesList.tsx", "SaleDialog.tsx", "SaleDetailSheet.tsx", "SaleItemsTable.tsx"]
          },
          {
            name: "Module Offres",
            path: "src/modules/offers/",
            description: "Gestion des devis et propositions avec workflow de statut (Brouillon → Envoyé → Accepté/Refusé/Expiré), édition des lignes d'articles, génération PDF, envoi email, suivi de validité et conversion automatique en ventes.",
            files: ["OffersPage.tsx", "OffersList.tsx", "OfferDialog.tsx", "OfferItemsTable.tsx"]
          },
          {
            name: "Module Ordres de Service",
            path: "src/modules/field/service-orders/",
            description: "Gestion des ordres de travail terrain avec planification, affectation des techniciens, suivi de statut (Nouveau → En cours → Terminé → Facturé), suivi des coûts, gestion des pièces/matériaux et intégration avec les dispatches.",
            files: ["ServiceOrdersPage.tsx", "ServiceOrdersList.tsx", "ServiceOrderDialog.tsx", "ServiceOrderJobs.tsx"]
          },
          {
            name: "Module Dispatches",
            path: "src/modules/field/dispatches/",
            description: "Gestion des dispatches techniciens avec mises à jour de statut en temps réel, suivi du temps (début/fin), upload de photos, capture de signature, génération de rapport PDF et interface optimisée mobile.",
            files: ["DispatchesPage.tsx", "DispatchesList.tsx", "DispatchDialog.tsx", "DispatchStatusBadge.tsx"]
          },
          {
            name: "Module Installations",
            path: "src/modules/field/installations/",
            description: "Suivi des équipements et installations avec gestion de garantie, association client, suivi de localisation, historique de service et planification de maintenance. Supporte le suivi modèle/série des équipements.",
            files: ["InstallationsPage.tsx", "InstallationsList.tsx", "InstallationDialog.tsx"]
          },
          {
            name: "Module Temps & Dépenses",
            path: "src/modules/field/time-expenses/",
            description: "Suivi du temps et gestion des dépenses avec interface calendrier, calcul du taux horaire, catégories de dépenses, pièces jointes de reçus, workflow d'approbation et suivi facturable/non-facturable.",
            files: ["TimeExpensesPage.tsx", "TimeEntryDialog.tsx", "ExpenseDialog.tsx", "TimeExpenseCalendar.tsx"]
          },
          {
            name: "Module Paramètres",
            path: "src/modules/settings/",
            description: "Configuration système incluant gestion utilisateurs, édition rôles/permissions, préférences système, visualiseur de logs d'audit, outils de test API et documentation. Hub central pour toutes les fonctions administratives.",
            files: ["SettingsPage.tsx", "UsersSettingsPage.tsx", "RolesSettingsPage.tsx", "SystemSettingsPage.tsx"]
          },
          {
            name: "Module Lookups",
            path: "src/modules/lookups/",
            description: "Configuration des données de référence pour les valeurs de listes déroulantes utilisées dans l'application : priorités, catégories de service, catégories d'articles, emplacements, statuts, tags et autres listes configurables.",
            files: ["LookupsPage.tsx", "LookupsList.tsx", "LookupDialog.tsx"]
          },
          {
            name: "Module Formulaires Dynamiques",
            path: "src/modules/dynamic-forms/",
            description: "Système complet de création de formulaires personnalisés, listes de contrôle, inspections et sondages. Fonctionnalités : arrangement des champs par glisser-déposer, 15+ types de champs, logique conditionnelle, formulaires multi-pages, support bilingue (FR/EN), export PDF avec branding entreprise, suivi des réponses, partage public sans connexion, pages de remerciement personnalisables, et contrôle d'accès basé sur les permissions.",
            files: ["DynamicFormsPage.tsx", "CreateFormPage.tsx", "EditFormPage.tsx", "FormPreviewPage.tsx", "FormResponsesPage.tsx", "PublicFormPage.tsx", "FormBuilder/index.tsx", "SteppedFormPreview.tsx", "FormResponsePDF.tsx"]
          },
          {
            name: "Fonctionnalités Carte & Localisation",
            path: "src/modules/dispatcher/ & src/components/shared/",
            description: "Vues cartographiques interactives utilisant Leaflet (OpenStreetMap) pour visualiser les travaux, positions des techniciens et installations. Marqueurs colorés par priorité (urgent=rouge, en cours=jaune, terminé=vert). Cliquer pour voir les détails, centrage automatique, support thème clair/sombre, et design responsive pour mobile.",
            files: ["DispatcherMapView.tsx", "MapView.tsx", "LeafletMapInner.tsx"]
          }
        ]
      },
      
      stateManagement: {
        title: "Architecture de Gestion d'État",
        content: `L'application utilise une approche de gestion d'état en couches:

┌─────────────────────────────────────────────────────────────┐
│              COUCHES DE GESTION D'ÉTAT                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              ÉTAT SERVEUR (TanStack Query)              │ │
│  │  • Fetching, cache et synchronisation des données API  │ │
│  │  • Rafraîchissement automatique (staleTime: 5min)      │ │
│  │  • Mises à jour optimistes pour feedback UI instant    │ │
│  │  • Invalidation de requêtes pour cohérence données     │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              ÉTAT GLOBAL (React Context)                │ │
│  │  • AuthContext: Session utilisateur, login/logout      │ │
│  │  • PreferencesContext: Thème, langue, modes d'affichage│ │
│  │  • LoadingContext: États de chargement globaux         │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │               ÉTAT LOCAL (React Hooks)                  │ │
│  │  • useState: État UI local au composant                │ │
│  │  • useReducer: Logique d'état locale complexe          │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │            ÉTAT FORMULAIRES (React Hook Form)           │ │
│  │  • useForm: Gestion d'état des formulaires             │ │
│  │  • Schémas Zod: Validation typée                       │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
└─────────────────────────────────────────────────────────────┘`
      },
      
      permissions: {
        title: "Système de Permissions (RBAC)",
        content: `Le frontend implémente un système complet de contrôle d'accès basé sur les rôles:

MODULES DE PERMISSION:
──────────────────────
CRM: contacts, articles, offers, sales, projects, calendar, documents
Terrain: installations, service_orders, dispatches, dispatcher, time_tracking, expenses
Système: users, roles, settings, audit_logs, lookups, dynamic_forms

ACTIONS DE PERMISSION:
──────────────────────
CRUD: create, read, update, delete
Étendues: export, import, assign, approve

IMPLÉMENTATION:
───────────────
1. Hook usePermissions - Vérifie les permissions utilisateur
2. Composant PermissionRoute - Protège les routes
3. Rendu UI conditionnel - Masque les boutons non autorisés
4. Filtrage sidebar - Cache les menus sans permission read`
      },
      
      routing: {
        title: "Architecture de Routage",
        content: `L'application utilise React Router v6 avec routage imbriqué:

ROUTES PRINCIPALES:
───────────────────
/                    → Page de connexion (publique)
/login               → Connexion admin
/user-login          → Connexion utilisateur régulier
/dashboard/*         → Routes tableau de bord protégées

ROUTES DASHBOARD:
─────────────────
/dashboard/contacts      → Gestion contacts
/dashboard/sales         → Pipeline ventes
/dashboard/offers        → Gestion offres
/dashboard/service-orders → Ordres de service
/dashboard/dispatches    → Gestion dispatches
/dashboard/installations → Suivi installations
/dashboard/settings/*    → Pages paramètres
  └─ /dynamic-forms/*    → Constructeur formulaires dynamiques`
      },
      
      apiClient: {
        title: "Configuration Client API",
        content: `Le client API est construit avec Axios:

FONCTIONNALITÉS:
────────────────
• URL de base depuis variable d'environnement (VITE_API_URL)
• Timeout par défaut: 30 secondes
• Intercepteurs de requête: injection automatique du token JWT
• Intercepteurs de réponse: rafraîchissement automatique du token sur erreur 401
• Gestion de l'expiration de session
• Normalisation des erreurs`
      }
    },

    // Flux de Travail Métier (French)
    businessWorkflow: {
      title: "Flux de Travail & Processus Métier",
      subtitle: "Processus métier principaux et flux de données entre modules",
      
      overview: {
        title: "Processus Métier de Bout en Bout",
        description: "Le système suit un flux de travail structuré du contact initial à la livraison du service et à la facturation.",
        diagram: `┌─────────────────────────────────────────────────────────────────────────────┐
│                     VUE D'ENSEMBLE DU FLUX DE TRAVAIL                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────────────────┐   │
│  │ CONTACT  │ → │  OFFRE   │ → │  VENTE   │ → │   ORDRE DE SERVICE   │   │
│  │ (CRM)    │    │ (Devis)  │    │(Commande)│    │   (Bon de travail)   │   │
│  └──────────┘    └──────────┘    └──────────┘    └──────────────────────┘   │
│       │              │              │                      │                │
│       │              │              │                      ▼                │
│       │              │              │              ┌──────────────┐         │
│       │              │              │              │   TRAVAUX    │         │
│       │              │              │              │ (Par tâche)  │         │
│       │              │              │              └──────────────┘         │
│       │              │              │                      │                │
│       │              │              │                      ▼                │
│       │              │              │              ┌──────────────┐         │
│       │              │              │              │  DISPATCHES  │         │
│       │              │              │              │  (Assignés)  │         │
│       │              │              │              └──────────────┘         │
│       │              │              │                      │                │
│       │              │              │                      ▼                │
│       │              │              │              ┌──────────────┐         │
│       │              │              │              │   TEMPS &    │         │
│       │              │              │              │   DÉPENSES   │         │
│       │              │              │              └──────────────┘         │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘

ÉTAPES DU FLUX:
───────────────
1. CONTACT → Créer client/prospect dans le CRM
2. OFFRE   → Générer devis avec articles, taxes, remises
3. VENTE   → Convertir offre acceptée en bon de commande
4. ORDRE DE SERVICE → Créer bons de travail depuis ventes avec services
5. TRAVAUX → Tâches individuelles dans les ordres de service
6. DISPATCH → Assigner travaux aux techniciens via tableau de planification
7. TEMPS/DÉPENSES → Suivre temps de travail, matériaux et dépenses`
      },
      
      stages: [
        {
          name: "1. Contacts → Offres",
          description: "Les commerciaux créent des offres pour les contacts",
          details: [
            "Le contact doit exister avant de créer une offre",
            "L'offre inclut des lignes d'articles (produits/services)",
            "Champs financiers : montant, taxes, remise, total",
            "Workflow de statut : Brouillon → Envoyé → Accepté/Refusé/Expiré"
          ]
        },
        {
          name: "2. Offres → Ventes",
          description: "Les offres acceptées sont converties en bons de commande",
          details: [
            "Seules les offres 'Acceptées' peuvent être converties",
            "Toutes les données financières sont transférées : articles, taxes, remise, total",
            "Vente créée avec statut 'Nouveau'",
            "Offre marquée comme 'Convertie' après conversion réussie",
            "Endpoint backend : POST /api/sales/from-offer/{offerId}"
          ]
        },
        {
          name: "3. Ventes → Ordres de Service",
          description: "Les ventes avec services génèrent des ordres de service",
          details: [
            "Seules les ventes contenant des articles de service génèrent des ordres de service",
            "L'ordre de service hérite du contact et des données financières",
            "Plusieurs travaux peuvent être créés par ordre de service",
            "Workflow de statut : Nouveau → En cours → Terminé → Facturé"
          ]
        },
        {
          name: "4. Ordres de Service → Travaux → Dispatches",
          description: "Les travaux sont planifiés et assignés aux techniciens",
          details: [
            "Les travaux représentent des tâches individuelles dans un ordre de service",
            "Le tableau de planification affiche les travaux non assignés",
            "L'assignation par glisser-déposer crée des dispatches",
            "Les dispatches suivent le temps, les dépenses, les matériaux et les photos"
          ]
        }
      ]
    },

    // Calculs Financiers (French)
    financialCalculations: {
      title: "Calculs & Logique Financière",
      subtitle: "Comment les montants, taxes, remises et totaux sont calculés",
      
      overview: {
        title: "Vue d'ensemble des Champs Financiers",
        description: "Les calculs financiers sont cohérents entre Offres, Ventes et Ordres de Service.",
        fields: [
          { name: "montant / sous-total", type: "decimal", description: "Somme de toutes les lignes (quantité × prixUnitaire)" },
          { name: "taxes", type: "decimal", description: "Montant des taxes (peut être fixe ou calculé)" },
          { name: "remise", type: "decimal", description: "Montant de remise à soustraire" },
          { name: "totalAmount", type: "decimal", description: "Total final = montant + taxes - remise" }
        ]
      },
      
      calculations: {
        title: "Logique de Calcul",
        formula: `┌─────────────────────────────────────────────────────────────┐
│                 FORMULE DE CALCUL DU TOTAL                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   SOUS-TOTAL = Σ (article.quantité × article.prixUnitaire)  │
│                                                              │
│   TOTAL = SOUS-TOTAL + TAXES - REMISE                        │
│                                                              │
│   Exemple:                                                   │
│   ─────────────────────────────────────                      │
│   Article 1: 2 × 500€ = 1 000€                               │
│   Article 2: 1 × 1 500€ = 1 500€                             │
│   ─────────────────────────────────────                      │
│   Sous-total:          2 500€                                │
│   Taxes (15%):        +  375€                                │
│   Remise:             -  100€                                │
│   ─────────────────────────────────────                      │
│   TOTAL:              2 775€                                 │
│                                                              │
└─────────────────────────────────────────────────────────────┘`
      },
      
      dataFlow: {
        title: "Flux de Données Financières Entre Modules",
        description: "Les données financières sont préservées lors des conversions entre modules",
        diagram: `OFFRE                    VENTE                    ORDRE DE SERVICE
┌────────────────┐       ┌────────────────┐       ┌────────────────┐
│ montant: 2500  │ ────▶ │ montant: 2500  │ ────▶ │ montant: 2500  │
│ taxes: 375     │       │ taxes: 375     │       │ taxes: 375     │
│ remise: 100    │       │ remise: 100    │       │ remise: 100    │
│ total: 2775    │       │ total: 2775    │       │ total: 2775    │
│ articles:[...] │       │ articles:[...] │       │ articles:[...] │
└────────────────┘       └────────────────┘       └────────────────┘

✓ Tous les champs financiers sont copiés lors de la conversion
✓ Les articles/lignes sont dupliqués vers la nouvelle entité
✓ Les totaux sont recalculés si les articles changent après conversion`
      }
    },

    // Fonctionnalités Récentes (French)
    recentFeatures: {
      title: "Fonctionnalités Récemment Ajoutées",
      subtitle: "Nouvelles fonctionnalités et améliorations ajoutées au système",
      
      features: [
        {
          name: "Taxes & Remise dans Offres/Ventes",
          date: "2024",
          description: "Support complet des champs taxes et remise dans le pipeline de ventes",
          details: [
            "Champ Taxes ajouté aux formulaires Offres et Ventes",
            "Champ Remise pour les réductions de prix",
            "Calcul automatique du total : sous-total + taxes - remise",
            "Valeurs transférées lors de la conversion offre-vente",
            "Inclus dans la génération de documents PDF",
            "Labels entièrement localisés (EN: Taxes/Discount, FR: Taxes/Remise)"
          ],
          modules: ["offres", "ventes"]
        },
        {
          name: "Suivi Temps & Dépenses des Dispatches",
          date: "2024",
          description: "Suivi complet des entrées de temps et dépenses pour les dispatches",
          details: [
            "Types de travail multiples : Déplacement, Installation, Travail, Documentation, Nettoyage",
            "Suivi heure début/fin avec calcul de durée",
            "Catégories de dépenses : Déplacement, Équipement, Matériaux, Repas, Autre",
            "Pièces jointes photos pour reçus et preuves de travail",
            "Calculs en temps réel des heures facturables"
          ],
          modules: ["dispatches", "temps-dépenses"]
        },
        {
          name: "Tableau de Planification Dispatcher",
          date: "2024",
          description: "Interface visuelle glisser-déposer pour l'assignation des travaux",
          details: [
            "Vue calendrier avec lignes utilisateurs (techniciens)",
            "Panneau travaux non assignés avec filtrage",
            "Assignation travaux par glisser-déposer sur créneaux horaires",
            "Statut et priorité des travaux codés par couleur",
            "Détection des conflits pour assignations qui se chevauchent",
            "Filtres rapides par compétences, priorité et plage de dates"
          ],
          modules: ["dispatcher", "planification"]
        },
        {
          name: "Génération de Documents PDF",
          date: "2024",
          description: "Documents PDF professionnels pour offres, ventes et rapports de travail",
          details: [
            "PDF Offre/Devis avec branding entreprise",
            "PDF Bon de commande avec lignes et totaux",
            "Rapport de travail dispatch avec entrées de temps et photos",
            "Support bilingue (Anglais et Français)",
            "Fonctionnalité téléchargement et envoi email"
          ],
          modules: ["offres", "ventes", "dispatches"]
        },
        {
          name: "Localisation Bilingue (EN/FR)",
          date: "2024",
          description: "Support complet d'internationalisation pour Anglais et Français",
          details: [
            "Espaces de noms de traduction par module",
            "Tout le texte UI localisé (labels, boutons, messages)",
            "Notifications toast dans la langue de l'utilisateur",
            "Documents PDF respectent la préférence de langue",
            "Formatage date/devise par locale"
          ],
          modules: ["système"]
        },
        {
          name: "Gestion des Installations",
          date: "2024",
          description: "Suivi des installations d'équipements avec garantie et maintenance",
          details: [
            "Suivi modèle et numéro de série équipement",
            "Gestion dates début/fin garantie",
            "Contact et emplacement associés",
            "Chronologie historique de service",
            "Planification maintenance"
          ],
          modules: ["installations"]
        },
        {
          name: "Contrôle d'Accès Basé sur les Rôles (RBAC)",
          date: "2024",
          description: "Système de permissions granulaires pour tous les modules et actions",
          details: [
            "Permissions au niveau module (contacts, ventes, etc.)",
            "Contrôle au niveau action (créer, lire, modifier, supprimer, exporter)",
            "Bypass MainAdmin pour super administrateurs",
            "Filtrage dynamique du menu sidebar",
            "Routes et éléments UI protégés"
          ],
          modules: ["rôles", "permissions"]
        }
      ]
    },

    // Localisation (French)
    localization: {
      title: "Localisation & Internationalisation",
      subtitle: "Support multi-langues avec react-i18next",
      
      overview: {
        title: "Architecture i18n",
        description: "L'application utilise react-i18next avec des espaces de noms spécifiques par module pour des traductions organisées.",
        structure: `src/
├── locales/                    # Traductions globales
│   ├── en.json                # Anglais - termes partagés
│   └── fr.json                # Français - termes partagés
│
└── modules/
    ├── offers/
    │   └── locale/
    │       ├── en.json        # Anglais - module offres
    │       └── fr.json        # Français - module offres
    ├── sales/
    │   └── locale/
    │       ├── en.json        # Anglais - module ventes
    │       └── fr.json        # Français - module ventes
    └── .../                    # Autres modules suivent le même pattern`
      },
      
      namespaces: {
        title: "Espaces de Noms de Traduction",
        items: [
          { namespace: "translation", description: "Termes partagés/communs (rechercher, enregistrer, annuler, etc.)" },
          { namespace: "offers", description: "Termes spécifiques offres (devis, validité, envoyer offre, etc.)" },
          { namespace: "sales", description: "Termes spécifiques ventes (pipeline, étape, convertir, etc.)" },
          { namespace: "contacts", description: "Termes spécifiques contacts (client, entreprise, téléphone, etc.)" },
          { namespace: "dispatches", description: "Termes spécifiques dispatches (assigner, planifier, technicien, etc.)" },
          { namespace: "installations", description: "Termes spécifiques installations (équipement, garantie, série, etc.)" },
          { namespace: "users", description: "Termes gestion utilisateurs (rôle, permission, actif, etc.)" },
          { namespace: "lookups", description: "Termes configuration lookups (priorité, statut, catégorie, etc.)" }
        ]
      },
      
      usage: {
        title: "Exemples d'Utilisation",
        code: `// Utilisation basique avec hook useTranslation
import { useTranslation } from 'react-i18next';

function FormulaireOffre() {
  const { t } = useTranslation('offers');  // Utiliser namespace offres
  
  return (
    <div>
      <label>{t('offers.taxes')}</label>           {/* "Taxes" / "Taxes" */}
      <label>{t('offers.discount')}</label>        {/* "Discount" / "Remise" */}
      <label>{t('offers.total')}</label>           {/* "Total" / "Total" */}
      <button>{t('offers.sendOffer')}</button>     {/* "Envoyer l'offre" */}
    </div>
  );
}

// Plusieurs espaces de noms
const { t } = useTranslation(['offers', 'translation']);
t('offers.taxes');           // Depuis namespace offres
t('translation:search');     // Depuis namespace translation

// Avec interpolation
t('offers.validUntil', { date: '2024-12-31' });
// "Valide jusqu'au {{date}}" → "Valide jusqu'au 2024-12-31"`
      },
      
      addingTranslations: {
        title: "Ajouter de Nouvelles Traductions",
        steps: [
          "1. Identifier le module (offres, ventes, contacts, etc.)",
          "2. Ajouter la clé dans en.json et fr.json du dossier locale du module",
          "3. Utiliser un nommage cohérent : nomModule.nomClé (ex: offers.taxes)",
          "4. Utiliser la traduction dans le composant avec hook useTranslation",
          "5. Tester les deux langues pour vérifier l'affichage correct"
        ]
      }
    },
    
    backend: {
      title: "Architecture Backend (.NET 8)",
      
      structure: {
        title: "Structure du Projet Backend",
        content: `FlowServiceBackend/
├── Program.cs                    # Point d'entrée & config DI
├── appsettings.json              # Paramètres de configuration
│
├── Modules/                       # Modules fonctionnels (Clean Architecture)
│   ├── Auth/                     # Module authentification
│   │   ├── Controllers/AuthController.cs
│   │   ├── Services/AuthService.cs
│   │   ├── DTOs/
│   │   └── Models/
│   │
│   ├── Users/                    # Module gestion utilisateurs
│   ├── Roles/                    # Module rôles & permissions
│   ├── Contacts/                 # Module gestion contacts
│   ├── Sales/                    # Module gestion ventes
│   ├── Offers/                   # Module gestion offres
│   ├── ServiceOrders/            # Module ordres de service
│   ├── Dispatches/               # Module gestion dispatches
│   ├── Installations/            # Module suivi installations
│   ├── Articles/                 # Module catalogue produits
│   ├── Calendar/                 # Module événements calendrier
│   ├── Lookups/                  # Module données de référence
│   └── Shared/                   # Services partagés
│       └── Services/SystemLogService.cs
│
├── Data/                          # Couche accès données
│   └── ApplicationDbContext.cs   # DbContext EF Core
│
├── Migrations/                    # Migrations EF Core
│
└── Database/                      # Scripts SQL`
      },
      
      api: {
        title: "Conception API RESTful",
        content: `L'API suit les conventions RESTful:

CONVENTIONS:
────────────
URL DE BASE: /api/{ressource}

ENDPOINTS STANDARDS:
GET    /api/contacts           → Liste avec pagination
GET    /api/contacts/{id}      → Obtenir un enregistrement
POST   /api/contacts           → Créer un nouvel enregistrement
PUT    /api/contacts/{id}      → Mise à jour complète
PATCH  /api/contacts/{id}      → Mise à jour partielle
DELETE /api/contacts/{id}      → Supprimer enregistrement

FORMAT DE RÉPONSE:
──────────────────
{
  "success": true/false,
  "data": { ... },
  "error": { "code": "...", "message": "..." }
}

AUTHENTIFICATION:
─────────────────
Authorization: Bearer <jwt_token>`
      },
      
      endpoints: {
        title: "Référence Complète des Endpoints API",
        categories: [
          {
            name: "Authentification (/api/auth)",
            icon: "Key",
            endpoints: [
              { method: "POST", path: "/api/auth/admin/login", description: "Connexion admin → retourne JWT + données utilisateur" },
              { method: "POST", path: "/api/auth/login", description: "Connexion utilisateur → retourne JWT + permissions" },
              { method: "POST", path: "/api/auth/refresh", description: "Rafraîchir le token d'accès" },
              { method: "POST", path: "/api/auth/logout", description: "Invalider la session" },
              { method: "GET", path: "/api/auth/me", description: "Obtenir le profil utilisateur authentifié" }
            ]
          },
          {
            name: "Utilisateurs (/api/users)",
            icon: "Users",
            endpoints: [
              { method: "GET", path: "/api/users", description: "Lister tous les utilisateurs avec pagination" },
              { method: "GET", path: "/api/users/{id}", description: "Obtenir utilisateur par ID" },
              { method: "POST", path: "/api/users", description: "Créer nouvel utilisateur" },
              { method: "PUT", path: "/api/users/{id}", description: "Mettre à jour utilisateur" },
              { method: "DELETE", path: "/api/users/{id}", description: "Désactiver/supprimer utilisateur" }
            ]
          },
          {
            name: "Rôles (/api/roles)",
            icon: "Shield",
            endpoints: [
              { method: "GET", path: "/api/roles", description: "Lister tous les rôles" },
              { method: "GET", path: "/api/roles/{id}", description: "Obtenir rôle avec permissions" },
              { method: "POST", path: "/api/roles", description: "Créer nouveau rôle" },
              { method: "PUT", path: "/api/roles/{id}", description: "Mettre à jour rôle et permissions" },
              { method: "DELETE", path: "/api/roles/{id}", description: "Supprimer rôle" }
            ]
          },
          {
            name: "Contacts (/api/contacts)",
            icon: "Users",
            endpoints: [
              { method: "GET", path: "/api/contacts", description: "Lister contacts avec recherche, filtre, pagination" },
              { method: "GET", path: "/api/contacts/{id}", description: "Obtenir contact avec tags et historique" },
              { method: "POST", path: "/api/contacts", description: "Créer nouveau contact" },
              { method: "PUT", path: "/api/contacts/{id}", description: "Mettre à jour contact" },
              { method: "DELETE", path: "/api/contacts/{id}", description: "Supprimer contact (soft delete)" },
              { method: "POST", path: "/api/contacts/import", description: "Import en masse de contacts" }
            ]
          },
          {
            name: "Ventes (/api/sales)",
            icon: "DollarSign",
            endpoints: [
              { method: "GET", path: "/api/sales", description: "Lister ventes avec filtres" },
              { method: "GET", path: "/api/sales/stats", description: "Statistiques de ventes" },
              { method: "GET", path: "/api/sales/{id}", description: "Obtenir vente avec articles" },
              { method: "POST", path: "/api/sales", description: "Créer nouvelle vente" },
              { method: "PATCH", path: "/api/sales/{id}", description: "Mettre à jour vente" },
              { method: "DELETE", path: "/api/sales/{id}", description: "Supprimer vente" },
              { method: "POST", path: "/api/sales/from-offer/{offerId}", description: "Convertir offre en vente" }
            ]
          },
          {
            name: "Offres (/api/offers)",
            icon: "FileText",
            endpoints: [
              { method: "GET", path: "/api/offers", description: "Lister offres" },
              { method: "GET", path: "/api/offers/{id}", description: "Obtenir offre" },
              { method: "POST", path: "/api/offers", description: "Créer nouvelle offre" },
              { method: "PUT", path: "/api/offers/{id}", description: "Mettre à jour offre" },
              { method: "DELETE", path: "/api/offers/{id}", description: "Supprimer offre" }
            ]
          },
          {
            name: "Ordres de Service (/api/serviceorders)",
            icon: "Wrench",
            endpoints: [
              { method: "GET", path: "/api/serviceorders", description: "Lister ordres de service" },
              { method: "GET", path: "/api/serviceorders/{id}", description: "Obtenir ordre de service" },
              { method: "POST", path: "/api/serviceorders", description: "Créer ordre de service" },
              { method: "PUT", path: "/api/serviceorders/{id}", description: "Mettre à jour ordre" },
              { method: "DELETE", path: "/api/serviceorders/{id}", description: "Supprimer ordre" }
            ]
          },
          {
            name: "Dispatches (/api/dispatches)",
            icon: "Truck",
            endpoints: [
              { method: "GET", path: "/api/dispatches", description: "Lister dispatches" },
              { method: "GET", path: "/api/dispatches/{id}", description: "Obtenir dispatch" },
              { method: "POST", path: "/api/dispatches", description: "Créer dispatch" },
              { method: "PUT", path: "/api/dispatches/{id}", description: "Mettre à jour dispatch" },
              { method: "DELETE", path: "/api/dispatches/{id}", description: "Supprimer dispatch" }
            ]
          }
        ],
        
        // API Examples (French)
        examples: [
          {
            name: "Authentification - Connexion",
            method: "POST",
            path: "/api/auth/admin/login",
            description: "Authentifier un utilisateur admin et recevoir un token JWT",
            request: `{
  "email": "admin@example.com",
  "password": "MotDePasseSecure123!"
}`,
            response: `{
  "success": true,
  "data": {
    "user": {
      "id": 1,
      "email": "admin@example.com",
      "firstName": "Admin",
      "lastName": "Utilisateur"
    },
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
    "expiresIn": 3600
  }
}`
          },
          {
            name: "Contacts - Liste avec filtres",
            method: "GET",
            path: "/api/contacts?searchTerm=jean&status=active&pageNumber=1&pageSize=20",
            description: "Récupérer une liste paginée de contacts avec filtrage",
            request: `Paramètres de requête:
- searchTerm: string - Recherche dans nom, email, entreprise
- status: string - "active" | "inactive" | "lead" | "customer"
- type: string - "individual" | "company" | "partner"
- pageNumber: number (défaut: 1)
- pageSize: number (défaut: 20)`,
            response: `{
  "contacts": [
    {
      "id": 1,
      "firstName": "Jean",
      "lastName": "Dupont",
      "name": "Jean Dupont",
      "email": "jean.dupont@example.com",
      "phone": "+33-1-23-45-67-89",
      "company": "Acme SARL",
      "status": "active",
      "type": "individual",
      "tags": [{ "id": 1, "name": "VIP", "color": "#FF5733" }]
    }
  ],
  "totalCount": 150,
  "pageSize": 20,
  "pageNumber": 1,
  "hasNextPage": true
}`
          },
          {
            name: "Ventes - Créer avec articles",
            method: "POST",
            path: "/api/sales",
            description: "Créer une nouvelle vente avec lignes d'articles",
            request: `{
  "title": "Licence Enterprise T1 2024",
  "contactId": 25,
  "status": "draft",
  "priority": "high",
  "currency": "EUR",
  "taxes": 850.00,
  "items": [
    {
      "type": "service",
      "itemName": "Licence Enterprise",
      "quantity": 1,
      "unitPrice": 5000.00
    }
  ]
}`,
            response: `{
  "success": true,
  "data": {
    "id": 42,
    "saleNumber": "SALE-2024-0042",
    "title": "Licence Enterprise T1 2024",
    "totalAmount": 5850.00,
    "items": [...]
  }
}`
          }
        ]
      },
      
      services: {
        title: "Patron de Couche Service",
        content: `Chaque module suit un patron de service cohérent:

STRUCTURE:
──────────
Interface (ISaleService.cs) - Contrat du service
Implémentation (SaleService.cs) - Logique métier
Contrôleur (SalesController.cs) - Endpoints REST

CARACTÉRISTIQUES:
─────────────────
• Injection de dépendances (DbContext, ILogger)
• Logique métier et validation
• Requêtes Entity Framework avec Include
• Transformation DTO
• Gestion des exceptions et journalisation`
      },

      // DTOs Documentation (French)
      dtos: {
        title: "Objets de Transfert de Données (DTOs)",
        content: `Les DTOs backend définissent la structure des données requête/réponse:

┌─────────────────────────────────────────────────────────────┐
│                   DTOs CONTACT                               │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ContactResponseDto (Réponse):                               │
│  ─────────────────────────────                               │
│  {                                                           │
│    id: int                    // Identifiant unique         │
│    firstName: string          // Requis, max 100 caractères │
│    lastName: string           // Requis, max 100 caractères │
│    name: string               // Calculé: Prénom Nom        │
│    email: string?             // Optionnel, format validé   │
│    phone: string?             // Optionnel, max 20 car.     │
│    company: string?           // Optionnel, max 200 car.    │
│    status: string             // "active"|"inactive"|"lead" │
│    type: string               // "individual"|"company"     │
│    favorite: bool             // Indicateur favori          │
│    tags: ContactTagDto[]      // Tags associés              │
│  }                                                           │
│                                                              │
│  CreateContactRequestDto (Requête):                          │
│  ───────────────────────────────────                         │
│  {                                                           │
│    firstName: string          // [Requis] max 100           │
│    lastName: string           // [Requis] max 100           │
│    email: string?             // [FormatEmail] max 255      │
│    phone: string?             // max 20                     │
│    company: string?           // max 200                    │
│    status: string?            // défaut: "active"           │
│    type: string?              // défaut: "individual"       │
│    tagIds: int[]              // IDs des tags à assigner    │
│  }                                                           │
│                                                              │
└─────────────────────────────────────────────────────────────┘`
      }
    },
    
    swagger: {
      title: "Documentation API Swagger",
      content: `Documentation API interactive disponible à l'endpoint Swagger:

URL: https://api.flowentra.app/swagger

FONCTIONNALITÉS:
────────────────
• Test API interactif
• Documentation des schémas requête/réponse
• Support d'authentification (cliquez "Authorize")
• Définitions de modèles avec descriptions de champs
• Fonctionnalité Try-it-out pour tous les endpoints

UTILISATION:
────────────
1. Naviguez vers /swagger
2. Cliquez sur "Authorize"
3. Entrez votre token JWT: Bearer <votre-token>
4. Développez une section d'endpoint
5. Cliquez "Try it out" pour tester`
    },
    
    security: {
      title: "Architecture de Sécurité",
      content: `L'application implémente plusieurs couches de sécurité:

AUTHENTIFICATION:
─────────────────
• Token d'accès JWT: Courte durée (1 heure par défaut)
• Token de rafraîchissement: Longue durée (7 jours par défaut)
• Hachage BCrypt avec sel (facteur de travail 12)
• Deux types d'utilisateurs: MainAdmin (id=1) et User (id≥2)

AUTORISATION (RBAC):
────────────────────
• Les rôles définissent des ensembles de permissions
• Les utilisateurs sont assignés à un rôle
• Permissions vérifiées au niveau route et UI
• MainAdmin contourne toutes les vérifications

SÉCURITÉ API:
─────────────
• Application HTTPS en production
• Configuration CORS
• Limitation de débit (configurable)
• Requêtes paramétrées (prévention injection SQL)
• Validation des entrées

JOURNALISATION D'AUDIT:
───────────────────────
• Toutes les opérations CRUD journalisées
• ID utilisateur et horodatage enregistrés
• Statut succès/échec capturé`
    },
    
    errorHandling: {
      title: "Gestion des Erreurs",
      subtitle: "Patrons complets de gestion des erreurs pour frontend et backend",
      
      frontendErrors: {
        title: "Gestion des Erreurs Frontend",
        patterns: [
          {
            name: "Structure de Réponse d'Erreur API",
            description: "Format standard de réponse d'erreur de l'API",
            code: `// Réponse d'erreur standard de l'API
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Données de requête invalides",
    "details": {
      "email": ["L'email est requis", "Format d'email invalide"],
      "password": ["Le mot de passe doit avoir au moins 8 caractères"]
    }
  }
}`
          },
          {
            name: "Gestion des Erreurs TanStack Query",
            description: "Gestion des erreurs dans les mutations et queries React Query",
            code: `// Mutation avec gestion des erreurs
const mutation = useMutation({
  mutationFn: contactsApi.create,
  onSuccess: (data) => {
    queryClient.invalidateQueries({ queryKey: ['contacts'] });
    toast.success('Contact créé avec succès');
  },
  onError: (error: any) => {
    if (error.response?.status === 400) {
      toast.error('Erreur de validation');
    } else if (error.response?.status === 401) {
      toast.error('Session expirée');
    } else {
      toast.error('Une erreur inattendue est survenue');
    }
  }
});`
          }
        ]
      },
      
      backendErrors: {
        title: "Gestion des Erreurs Backend (.NET)",
        patterns: [
          {
            name: "Réponses d'Erreur du Contrôleur",
            description: "Codes de statut HTTP standards et formats d'erreur",
            code: `// Contrôleur avec gestion des erreurs
[HttpPost]
public async Task<IActionResult> CreateContact([FromBody] CreateContactRequestDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(new ApiResponse<object>
        {
            Success = false,
            Message = "Validation échouée"
        });

    try
    {
        var result = await _service.CreateContactAsync(dto);
        return CreatedAtAction(nameof(GetContact), new { id = result.Id }, result);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new ApiResponse<object>
        {
            Success = false,
            Message = "Une erreur interne est survenue"
        });
    }
}`
          }
        ]
      },
      
      httpStatusCodes: {
        title: "Référence des Codes de Statut HTTP",
        codes: [
          { code: 200, name: "OK", description: "Requête réussie.", usage: "Succès GET, PUT, PATCH" },
          { code: 201, name: "Created", description: "Ressource créée avec succès.", usage: "Succès POST" },
          { code: 204, name: "No Content", description: "Requête réussie sans contenu.", usage: "Succès DELETE" },
          { code: 400, name: "Bad Request", description: "Données invalides ou validation échouée.", usage: "Erreurs de validation" },
          { code: 401, name: "Unauthorized", description: "Authentification requise ou token expiré.", usage: "Token JWT manquant/invalide" },
          { code: 403, name: "Forbidden", description: "Authentifié mais non autorisé.", usage: "Permissions insuffisantes" },
          { code: 404, name: "Not Found", description: "Ressource non trouvée.", usage: "ID invalide, ressource supprimée" },
          { code: 409, name: "Conflict", description: "Conflit avec l'état actuel.", usage: "Email en double" },
          { code: 500, name: "Internal Server Error", description: "Erreur serveur inattendue.", usage: "Erreurs base de données" }
        ]
      }
    },
    
    troubleshooting: {
      title: "Guide de Dépannage",
      subtitle: "Problèmes courants et leurs solutions",
      
      categories: [
        {
          name: "Problèmes d'Authentification",
          icon: "Key",
          issues: [
            {
              problem: "401 Non autorisé - Token expiré",
              symptoms: ["Déconnexion automatique", "Appels API échouent", "Redirection vers la page de connexion"],
              causes: ["Token JWT expiré", "Token non rafraîchi", "Format de token invalide"],
              solutions: [
                "Videz le localStorage et reconnectez-vous",
                "Vérifiez si l'endpoint de rafraîchissement fonctionne",
                "Vérifiez le temps d'expiration du token",
                "Consultez la console du navigateur pour les erreurs"
              ],
              codeExample: `// Vérifier l'expiration du token
const token = localStorage.getItem('access_token');
if (token) {
  const payload = JSON.parse(atob(token.split('.')[1]));
  const expiry = new Date(payload.exp * 1000);
  console.log('Token expire:', expiry);
}`
            },
            {
              problem: "403 Interdit - Permission refusée",
              symptoms: ["Bouton/menu caché", "Page affiche 'Accès Refusé'", "API retourne 403"],
              causes: ["Le rôle n'a pas la permission", "Mauvaise vérification module/action"],
              solutions: [
                "Vérifiez les permissions du rôle de l'utilisateur",
                "Vérifiez les assignations de permissions dans RolesController",
                "Utilisez le hook hasPermission() pour déboguer"
              ],
              codeExample: `// Déboguer les permissions
const { hasPermission, isMainAdmin } = usePermissions();
console.log('Est MainAdmin:', isMainAdmin);
console.log('A contacts:read:', hasPermission('contacts', 'read'));`
            }
          ]
        },
        {
          name: "Problèmes de Connexion API",
          icon: "Globe",
          issues: [
            {
              problem: "Erreur Réseau - API inaccessible",
              symptoms: ["Timeouts", "Erreurs CORS", "Connexion refusée"],
              causes: ["Serveur backend arrêté", "Mauvaise URL API", "CORS non configuré"],
              solutions: [
                "Vérifiez si le backend fonctionne (visitez /swagger)",
                "Vérifiez VITE_API_URL dans l'environnement",
                "Vérifiez les paramètres CORS dans le backend"
              ],
              codeExample: `// Vérifier l'URL API
console.log('URL API:', import.meta.env.VITE_API_URL);

// Tester la santé de l'API
fetch('https://api.flowentra.app/api/health')
  .then(r => console.log('Statut API:', r.status));`
            }
          ]
        },
        {
          name: "Problèmes de Données",
          icon: "Database",
          issues: [
            {
              problem: "Données non mises à jour après sauvegarde",
              symptoms: ["Anciennes données affichées", "Besoin de rafraîchir la page"],
              causes: ["Cache de query non invalidé", "Mise à jour optimiste échouée"],
              solutions: [
                "Invalidez les queries pertinentes après mutation",
                "Vérifiez la réponse API dans l'onglet réseau",
                "Ajoutez un callback onSuccess aux mutations"
              ],
              codeExample: `// Invalidation correcte du cache
const mutation = useMutation({
  mutationFn: contactsApi.update,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['contacts'] });
    toast.success('Sauvegardé avec succès');
  }
});`
            }
          ]
        }
      ],
      
      debuggingTools: {
        title: "Outils et Techniques de Débogage",
        tools: [
          {
            name: "DevTools du Navigateur",
            description: "Fonctionnalités essentielles de débogage",
            tips: [
              "Onglet Console: Voir les logs, erreurs et exécuter du JavaScript",
              "Onglet Réseau: Inspecter les requêtes/réponses API",
              "Onglet Application: Voir localStorage, sessionStorage, cookies",
              "React DevTools: Inspecter l'état et les props des composants"
            ]
          },
          {
            name: "Logs Système",
            description: "Piste d'audit et logs d'erreurs de l'application",
            tips: [
              "Naviguez vers Paramètres > Logs Système",
              "Filtrez par niveau (error, warning, info)",
              "Recherchez par module ou action"
            ]
          }
        ]
      },
      
      commonErrorMessages: {
        title: "Messages d'Erreur Courants",
        errors: [
          { message: "Failed to fetch", cause: "Erreur réseau ou API indisponible", solution: "Vérifiez la connexion internet et le statut de l'API" },
          { message: "JWT token is expired", cause: "Token d'accès expiré", solution: "Déconnectez-vous et reconnectez-vous" },
          { message: "CORS policy blocked", cause: "API non configurée pour cette origine", solution: "Ajoutez l'URL frontend aux paramètres CORS" },
          { message: "Cannot read properties of undefined", cause: "Accès à une propriété sur null/undefined", solution: "Ajoutez des vérifications null ou chaînage optionnel (?.)" }
        ]
      }
    },
    
    files: {
      title: "Référence des Fichiers Clés",
      categories: [
        {
          name: "Fichiers Frontend Core",
          items: [
            { path: "src/App.tsx", description: "Racine de l'application avec tous les providers et configuration du routage" },
            { path: "src/contexts/AuthContext.tsx", description: "Gestion d'état d'authentification, méthodes login/logout" },
            { path: "src/hooks/usePermissions.tsx", description: "Hook RBAC pour vérifier les permissions utilisateur" },
            { path: "src/services/api/apiClient.ts", description: "Instance Axios avec intercepteurs pour auth et erreurs" },
            { path: "src/services/authService.ts", description: "Appels API d'authentification, stockage de tokens" }
          ]
        },
        {
          name: "Fichiers Modules Frontend",
          items: [
            { path: "src/modules/dashboard/pages/Dashboard.tsx", description: "Layout principal du tableau de bord" },
            { path: "src/modules/dashboard/components/AppSidebar.tsx", description: "Sidebar de navigation avec filtrage par permissions" },
            { path: "src/components/permissions/PermissionRoute.tsx", description: "Wrapper de route qui vérifie les permissions" }
          ]
        },
        {
          name: "Fichiers Backend Core",
          items: [
            { path: "Program.cs", description: "Entrée de l'application, configuration DI, setup middleware" },
            { path: "Data/ApplicationDbContext.cs", description: "DbContext Entity Framework avec toutes les configurations" },
            { path: "Modules/Auth/Controllers/AuthController.cs", description: "Endpoints d'authentification" },
            { path: "Modules/Shared/Services/SystemLogService.cs", description: "Service de journalisation d'audit centralisé" }
          ]
        },
        {
          name: "Fichiers de Configuration",
          items: [
            { path: "package.json", description: "Dépendances et scripts frontend" },
            { path: "vite.config.ts", description: "Configuration de build Vite" },
            { path: "tailwind.config.ts", description: "Configuration Tailwind CSS avec thème personnalisé" },
            { path: "appsettings.json", description: "Paramètres de l'application backend" }
          ]
        }
      ]
    }
  }
};

export default function DocumentationPage() {
  const navigate = useNavigate();
  const [language, setLanguage] = useState<Language>('en');
  const [copiedPath, setCopiedPath] = useState<string | null>(null);
  
  const content = documentationContent[language];

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    setCopiedPath(text);
    toast.success(language === 'en' ? 'Copied to clipboard' : 'Copié dans le presse-papiers');
    setTimeout(() => setCopiedPath(null), 2000);
  };

  const getMethodColor = (method: string) => {
    switch (method) {
      case 'GET': return 'bg-success/10 text-success';
      case 'POST': return 'bg-primary/10 text-primary';
      case 'PUT': return 'bg-warning/10 text-warning';
      case 'PATCH': return 'bg-warning/10 text-warning';
      case 'DELETE': return 'bg-destructive/10 text-destructive';
      default: return 'bg-muted text-muted-foreground';
    }
  };

  return (
    <div className="flex flex-col min-h-screen bg-background">
      {/* Header */}
      <header className="sticky top-0 z-10 flex items-center justify-between p-4 border-b border-border bg-card/95 backdrop-blur">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="icon" onClick={() => navigate('/dashboard/settings')}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div className="p-2 rounded-lg bg-primary/10">
            <Book className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-xl font-semibold text-foreground">{content.title}</h1>
            <p className="text-sm text-muted-foreground">{content.subtitle}</p>
          </div>
        </div>
        
        {/* Language Toggle */}
        <div className="flex items-center gap-2">
          <Languages className="h-4 w-4 text-muted-foreground" />
          <Button
            variant={language === 'en' ? 'default' : 'outline'}
            size="sm"
            onClick={() => setLanguage('en')}
          >
            EN
          </Button>
          <Button
            variant={language === 'fr' ? 'default' : 'outline'}
            size="sm"
            onClick={() => setLanguage('fr')}
          >
            FR
          </Button>
        </div>
      </header>

      {/* Content */}
      <ScrollArea className="flex-1">
        <div className="max-w-7xl mx-auto p-4 md:p-6 space-y-6">
          
          {/* Overview Section */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-primary/10">
                  <Layers className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <CardTitle>{content.overview.title}</CardTitle>
                  <CardDescription className="mt-1">{content.overview.description}</CardDescription>
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-6">
              {/* Tech Stack */}
              <div>
                <h4 className="font-semibold mb-4 flex items-center gap-2">
                  <Layers2 className="h-4 w-4" />
                  {content.overview.techStack.title}
                </h4>
                <div className="grid md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <h5 className="text-sm font-medium flex items-center gap-2 text-primary">
                      <Code className="h-4 w-4" /> Frontend
                    </h5>
                    <div className="space-y-1">
                      {content.overview.techStack.frontend.map((tech, i) => (
                        <div key={i} className="flex items-center justify-between p-2 rounded bg-muted/50 text-sm">
                          <span className="font-medium">{tech.name}</span>
                          <Badge variant="outline" className="text-xs">{tech.version}</Badge>
                        </div>
                      ))}
                    </div>
                  </div>
                  <div className="space-y-2">
                    <h5 className="text-sm font-medium flex items-center gap-2 text-success">
                      <Server className="h-4 w-4" /> Backend
                    </h5>
                    <div className="space-y-1">
                      {content.overview.techStack.backend.map((tech, i) => (
                        <div key={i} className="flex items-center justify-between p-2 rounded bg-muted/50 text-sm">
                          <span className="font-medium">{tech.name}</span>
                          <Badge variant="outline" className="text-xs">{tech.version}</Badge>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
              
              <Separator />
              
              {/* Architecture Diagram */}
              <div>
                <h4 className="font-semibold mb-3 flex items-center gap-2">
                  <Network className="h-4 w-4" />
                  {content.overview.architecture.title}
                </h4>
                <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre">
                  {content.overview.architecture.content}
                </pre>
              </div>
            </CardContent>
          </Card>

          {/* Main Documentation Tabs */}
          <Tabs defaultValue="ai" className="w-full">
            <TabsList className="grid w-full grid-cols-4 md:grid-cols-7 mb-4">
              <TabsTrigger value="ai" className="flex items-center gap-2">
                <Bot className="h-4 w-4" />
                <span className="hidden sm:inline">AI</span>
              </TabsTrigger>
              <TabsTrigger value="frontend" className="flex items-center gap-2">
                <Code className="h-4 w-4" />
                <span className="hidden sm:inline">Frontend</span>
              </TabsTrigger>
              <TabsTrigger value="backend" className="flex items-center gap-2">
                <Server className="h-4 w-4" />
                <span className="hidden sm:inline">Backend</span>
              </TabsTrigger>
              <TabsTrigger value="security" className="flex items-center gap-2">
                <Shield className="h-4 w-4" />
                <span className="hidden sm:inline">{language === 'en' ? 'Security' : 'Sécurité'}</span>
              </TabsTrigger>
              <TabsTrigger value="errors" className="flex items-center gap-2">
                <AlertTriangle className="h-4 w-4" />
                <span className="hidden sm:inline">{language === 'en' ? 'Errors' : 'Erreurs'}</span>
              </TabsTrigger>
              <TabsTrigger value="troubleshooting" className="flex items-center gap-2">
                <LifeBuoy className="h-4 w-4" />
                <span className="hidden sm:inline">{language === 'en' ? 'Debug' : 'Dépannage'}</span>
              </TabsTrigger>
              <TabsTrigger value="files" className="flex items-center gap-2">
                <Folder className="h-4 w-4" />
                <span className="hidden sm:inline">{language === 'en' ? 'Files' : 'Fichiers'}</span>
              </TabsTrigger>
            </TabsList>

            {/* AI Documentation Tab */}
            <TabsContent value="ai" className="space-y-4">
              <Suspense fallback={<div className="p-8 text-center text-muted-foreground">{language === 'en' ? 'Loading AI Documentation...' : 'Chargement Documentation IA...'}</div>}>
                <AiDocumentation language={language} />
              </Suspense>
            </TabsContent>

            {/* Frontend Tab */}
            <TabsContent value="frontend" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Code className="h-5 w-5" />
                    {content.frontend.title}
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-6">
                  {/* Project Structure */}
                  <div>
                    <h4 className="font-semibold mb-3 flex items-center gap-2">
                      <Folder className="h-4 w-4" />
                      {content.frontend.structure.title}
                    </h4>
                    <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre max-h-96 overflow-y-auto">
                      {content.frontend.structure.content}
                    </pre>
                  </div>

                  <Separator />

                  {/* Feature Modules */}
                  <div>
                    <h4 className="font-semibold mb-3 flex items-center gap-2">
                      <Package className="h-4 w-4" />
                      {content.frontend.modules.title}
                    </h4>
                    <Accordion type="multiple" className="w-full">
                      {content.frontend.modules.items.map((module, index) => (
                        <AccordionItem key={index} value={`module-${index}`}>
                          <AccordionTrigger className="hover:no-underline">
                            <div className="flex items-center gap-2">
                              <Badge variant="outline">{module.files.length} files</Badge>
                              {module.name}
                            </div>
                          </AccordionTrigger>
                          <AccordionContent>
                            <div className="space-y-3 pl-2">
                              <p className="text-sm text-muted-foreground">{module.description}</p>
                              <div className="flex items-center gap-2">
                                <code className="text-xs text-primary bg-primary/10 px-2 py-1 rounded">{module.path}</code>
                                <Button variant="ghost" size="sm" onClick={() => copyToClipboard(module.path)}>
                                  {copiedPath === module.path ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
                                </Button>
                              </div>
                              <div className="flex flex-wrap gap-1">
                                {module.files.map((file, i) => (
                                  <Badge key={i} variant="secondary" className="text-xs">{file}</Badge>
                                ))}
                              </div>
                            </div>
                          </AccordionContent>
                        </AccordionItem>
                      ))}
                    </Accordion>
                  </div>

                  <Separator />

                  {/* State Management */}
                  <div>
                    <h4 className="font-semibold mb-3 flex items-center gap-2">
                      <Workflow className="h-4 w-4" />
                      {content.frontend.stateManagement.title}
                    </h4>
                    <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre max-h-80 overflow-y-auto">
                      {content.frontend.stateManagement.content}
                    </pre>
                  </div>

                  <Separator />

                  {/* Permissions */}
                  <div>
                    <h4 className="font-semibold mb-3 flex items-center gap-2">
                      <Lock className="h-4 w-4" />
                      {content.frontend.permissions.title}
                    </h4>
                    <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre max-h-80 overflow-y-auto">
                      {content.frontend.permissions.content}
                    </pre>
                  </div>

                  <Separator />

                  {/* Routing */}
                  <div>
                    <h4 className="font-semibold mb-3 flex items-center gap-2">
                      <GitBranch className="h-4 w-4" />
                      {content.frontend.routing.title}
                    </h4>
                    <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre">
                      {content.frontend.routing.content}
                    </pre>
                  </div>

                  <Separator />

                  {/* API Client */}
                  <div>
                    <h4 className="font-semibold mb-3 flex items-center gap-2">
                      <Globe className="h-4 w-4" />
                      {content.frontend.apiClient.title}
                    </h4>
                    <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre">
                      {content.frontend.apiClient.content}
                    </pre>
                  </div>

                  {/* Hooks Deep Dive - Only show if hooks exist */}
                  {'hooks' in content.frontend && content.frontend.hooks && (
                    <>
                      <Separator />
                      <div>
                        <h4 className="font-semibold mb-3 flex items-center gap-2">
                          <Zap className="h-4 w-4" />
                          {(content.frontend as any).hooks.title}
                        </h4>
                        <Accordion type="multiple" className="w-full">
                          {(content.frontend as any).hooks.items.map((hook: any, hookIndex: number) => (
                            <AccordionItem key={hookIndex} value={`hook-${hookIndex}`}>
                              <AccordionTrigger className="hover:no-underline">
                                <div className="flex items-center gap-2">
                                  <Badge variant="secondary">{hook.name}</Badge>
                                </div>
                              </AccordionTrigger>
                              <AccordionContent>
                                <div className="space-y-3 pl-2">
                                  <p className="text-sm text-muted-foreground">{hook.description}</p>
                                  <code className="text-xs text-primary bg-primary/10 px-2 py-1 rounded">{hook.path}</code>
                                  
                                  {hook.features && (
                                    <div>
                                      <h6 className="text-xs font-medium mb-2">{language === 'en' ? 'Features:' : 'Fonctionnalités:'}</h6>
                                      <ul className="text-xs text-muted-foreground space-y-1">
                                        {hook.features.map((f: string, i: number) => (
                                          <li key={i} className="flex items-start gap-2">
                                            <CheckCircle className="h-3 w-3 text-success mt-0.5 shrink-0" />
                                            {f}
                                          </li>
                                        ))}
                                      </ul>
                                    </div>
                                  )}
                                  
                                  <pre className="bg-muted p-3 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre max-h-64 overflow-y-auto">
                                    {hook.code}
                                  </pre>
                                </div>
                              </AccordionContent>
                            </AccordionItem>
                          ))}
                        </Accordion>
                      </div>
                    </>
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            {/* Backend Tab */}
            <TabsContent value="backend" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Server className="h-5 w-5" />
                    {content.backend.title}
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-6">
                  {/* Backend Structure */}
                  <div>
                    <h4 className="font-semibold mb-3 flex items-center gap-2">
                      <Folder className="h-4 w-4" />
                      {content.backend.structure.title}
                    </h4>
                    <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre max-h-96 overflow-y-auto">
                      {content.backend.structure.content}
                    </pre>
                  </div>

                  <Separator />

                  {/* API Design */}
                  <div>
                    <h4 className="font-semibold mb-3 flex items-center gap-2">
                      <Globe className="h-4 w-4" />
                      {content.backend.api.title}
                    </h4>
                    <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre">
                      {content.backend.api.content}
                    </pre>
                  </div>

                  <Separator />

                  {/* API Endpoints */}
                  <div>
                    <h4 className="font-semibold mb-3 flex items-center gap-2">
                      <Terminal className="h-4 w-4" />
                      {content.backend.endpoints.title}
                    </h4>
                    <Accordion type="multiple" className="w-full">
                      {content.backend.endpoints.categories.map((category, catIndex) => (
                        <AccordionItem key={catIndex} value={`cat-${catIndex}`}>
                          <AccordionTrigger className="hover:no-underline">
                            <div className="flex items-center gap-2">
                              <Badge variant="outline">{category.endpoints.length}</Badge>
                              {category.name}
                            </div>
                          </AccordionTrigger>
                          <AccordionContent>
                            <div className="space-y-2">
                              {category.endpoints.map((endpoint, endIndex) => (
                                <div key={endIndex} className="flex items-start gap-3 p-2 rounded-lg bg-muted/50">
                                  <Badge className={`${getMethodColor(endpoint.method)} shrink-0 w-16 justify-center`}>
                                    {endpoint.method}
                                  </Badge>
                                  <div className="flex-1 min-w-0">
                                    <code className="text-sm block truncate">{endpoint.path}</code>
                                    <span className="text-xs text-muted-foreground">
                                      {endpoint.description}
                                    </span>
                                  </div>
                                </div>
                              ))}
                            </div>
                          </AccordionContent>
                        </AccordionItem>
                      ))}
                    </Accordion>
                  </div>

                  <Separator />

                  {/* API Examples */}
                  {content.backend.endpoints.examples && (
                    <div>
                      <h4 className="font-semibold mb-3 flex items-center gap-2">
                        <FileJson className="h-4 w-4" />
                        {language === 'en' ? 'API Request/Response Examples' : 'Exemples Requête/Réponse API'}
                      </h4>
                      <Accordion type="multiple" className="w-full">
                        {content.backend.endpoints.examples.map((example, exIndex) => (
                          <AccordionItem key={exIndex} value={`example-${exIndex}`}>
                            <AccordionTrigger className="hover:no-underline">
                              <div className="flex items-center gap-2">
                                <Badge className={getMethodColor(example.method)}>
                                  {example.method}
                                </Badge>
                                <span className="text-left">{example.name}</span>
                              </div>
                            </AccordionTrigger>
                            <AccordionContent>
                              <div className="space-y-4 pl-2">
                                <div>
                                  <code className="text-sm text-primary bg-primary/10 px-2 py-1 rounded">
                                    {example.path}
                                  </code>
                                  <p className="text-sm text-muted-foreground mt-2">{example.description}</p>
                                </div>
                                
                                <div>
                                  <h5 className="text-sm font-medium mb-2 flex items-center gap-2">
                                    <Upload className="h-3 w-3" />
                                    {language === 'en' ? 'Request' : 'Requête'}
                                  </h5>
                                  <pre className="bg-muted p-3 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre max-h-48 overflow-y-auto">
                                    {example.request}
                                  </pre>
                                </div>
                                
                                <div>
                                  <h5 className="text-sm font-medium mb-2 flex items-center gap-2">
                                    <Download className="h-3 w-3" />
                                    {language === 'en' ? 'Response' : 'Réponse'}
                                  </h5>
                                  <pre className="bg-success/5 p-3 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre max-h-64 overflow-y-auto border border-success/20">
                                    {example.response}
                                  </pre>
                                </div>
                              </div>
                            </AccordionContent>
                          </AccordionItem>
                        ))}
                      </Accordion>
                    </div>
                  )}

                  <Separator />

                  {/* DTOs Documentation */}
                  {content.backend.dtos && (
                    <div>
                      <h4 className="font-semibold mb-3 flex items-center gap-2">
                        <Database className="h-4 w-4" />
                        {content.backend.dtos.title}
                      </h4>
                      <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre max-h-96 overflow-y-auto">
                        {content.backend.dtos.content}
                      </pre>
                    </div>
                  )}

                  <Separator />

                  {/* Service Pattern */}
                  <div>
                    <h4 className="font-semibold mb-3 flex items-center gap-2">
                      <Cog className="h-4 w-4" />
                      {content.backend.services.title}
                    </h4>
                    <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre">
                      {content.backend.services.content}
                    </pre>
                  </div>

                  <Separator />

                  {/* Swagger */}
                  <div>
                    <h4 className="font-semibold mb-3 flex items-center gap-2">
                      <FileCode className="h-4 w-4" />
                      {content.swagger.title}
                    </h4>
                    <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre">
                      {content.swagger.content}
                    </pre>
                    <Button variant="outline" className="mt-4" onClick={() => window.open(SWAGGER_URL, '_blank')}>
                      <ExternalLink className="h-4 w-4 mr-2" />
                      {language === 'en' ? 'Open Swagger UI' : 'Ouvrir Swagger UI'}
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            {/* Security Tab */}
            <TabsContent value="security" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Shield className="h-5 w-5" />
                    {content.security.title}
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <pre className="bg-muted p-4 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre">
                    {content.security.content}
                  </pre>
                </CardContent>
              </Card>
            </TabsContent>

            {/* Error Handling Tab */}
            <TabsContent value="errors" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <AlertTriangle className="h-5 w-5 text-warning" />
                    {content.errorHandling.title}
                  </CardTitle>
                  <CardDescription>{content.errorHandling.subtitle}</CardDescription>
                </CardHeader>
                <CardContent className="space-y-6">
                  {/* Frontend Error Handling */}
                  <div>
                    <h4 className="font-semibold mb-4 flex items-center gap-2">
                      <Code className="h-4 w-4" />
                      {content.errorHandling.frontendErrors.title}
                    </h4>
                    <Accordion type="multiple" className="w-full">
                      {content.errorHandling.frontendErrors.patterns.map((pattern, index) => (
                        <AccordionItem key={index} value={`fe-error-${index}`}>
                          <AccordionTrigger className="hover:no-underline">
                            <div className="flex items-center gap-2">
                              <Badge variant="secondary">{pattern.name}</Badge>
                            </div>
                          </AccordionTrigger>
                          <AccordionContent>
                            <div className="space-y-3 pl-2">
                              <p className="text-sm text-muted-foreground">{pattern.description}</p>
                              <pre className="bg-muted p-3 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre max-h-64 overflow-y-auto">
                                {pattern.code}
                              </pre>
                            </div>
                          </AccordionContent>
                        </AccordionItem>
                      ))}
                    </Accordion>
                  </div>

                  <Separator />

                  {/* Backend Error Handling */}
                  <div>
                    <h4 className="font-semibold mb-4 flex items-center gap-2">
                      <Server className="h-4 w-4" />
                      {content.errorHandling.backendErrors.title}
                    </h4>
                    <Accordion type="multiple" className="w-full">
                      {content.errorHandling.backendErrors.patterns.map((pattern, index) => (
                        <AccordionItem key={index} value={`be-error-${index}`}>
                          <AccordionTrigger className="hover:no-underline">
                            <div className="flex items-center gap-2">
                              <Badge variant="outline">{pattern.name}</Badge>
                            </div>
                          </AccordionTrigger>
                          <AccordionContent>
                            <div className="space-y-3 pl-2">
                              <p className="text-sm text-muted-foreground">{pattern.description}</p>
                              <pre className="bg-muted p-3 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre max-h-64 overflow-y-auto">
                                {pattern.code}
                              </pre>
                            </div>
                          </AccordionContent>
                        </AccordionItem>
                      ))}
                    </Accordion>
                  </div>

                  <Separator />

                  {/* HTTP Status Codes Reference */}
                  <div>
                    <h4 className="font-semibold mb-4 flex items-center gap-2">
                      <Activity className="h-4 w-4" />
                      {content.errorHandling.httpStatusCodes.title}
                    </h4>
                    <div className="grid gap-2">
                      {content.errorHandling.httpStatusCodes.codes.map((code, index) => (
                        <div key={index} className="flex items-start gap-3 p-3 rounded-lg bg-muted/50 hover:bg-muted transition-colors">
                          <Badge 
                            variant="outline" 
                            className={`shrink-0 w-14 justify-center ${
                              code.code >= 500 ? 'bg-destructive/10 text-destructive' :
                              code.code >= 400 ? 'bg-warning/10 text-warning' :
                              code.code >= 300 ? 'bg-primary/10 text-primary' :
                              'bg-success/10 text-success'
                            }`}
                          >
                            {code.code}
                          </Badge>
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2">
                              <span className="font-medium text-sm">{code.name}</span>
                            </div>
                            <p className="text-xs text-muted-foreground mt-1">{code.description}</p>
                            <p className="text-xs text-primary mt-1">{language === 'en' ? 'Usage:' : 'Usage:'} {code.usage}</p>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            {/* Troubleshooting Tab */}
            <TabsContent value="troubleshooting" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <LifeBuoy className="h-5 w-5 text-primary" />
                    {content.troubleshooting.title}
                  </CardTitle>
                  <CardDescription>{content.troubleshooting.subtitle}</CardDescription>
                </CardHeader>
                <CardContent className="space-y-6">
                  {/* Issue Categories */}
                  {content.troubleshooting.categories.map((category, catIndex) => (
                    <div key={catIndex}>
                      <h4 className="font-semibold mb-4 flex items-center gap-2">
                        {category.icon === 'Key' && <Key className="h-4 w-4" />}
                        {category.icon === 'Globe' && <Globe className="h-4 w-4" />}
                        {category.icon === 'Database' && <Database className="h-4 w-4" />}
                        {category.icon === 'Code' && <Code className="h-4 w-4" />}
                        {category.name}
                      </h4>
                      <Accordion type="multiple" className="w-full">
                        {category.issues.map((issue, issueIndex) => (
                          <AccordionItem key={issueIndex} value={`issue-${catIndex}-${issueIndex}`}>
                            <AccordionTrigger className="hover:no-underline">
                              <div className="flex items-center gap-2">
                                <XCircle className="h-4 w-4 text-destructive shrink-0" />
                                <span className="text-left">{issue.problem}</span>
                              </div>
                            </AccordionTrigger>
                            <AccordionContent>
                              <div className="space-y-4 pl-2">
                                {/* Symptoms */}
                                <div>
                                  <h6 className="text-xs font-semibold text-muted-foreground mb-2 flex items-center gap-1">
                                    <AlertCircle className="h-3 w-3" />
                                    {language === 'en' ? 'Symptoms:' : 'Symptômes:'}
                                  </h6>
                                  <ul className="text-xs text-muted-foreground space-y-1">
                                    {issue.symptoms.map((s, i) => (
                                      <li key={i} className="flex items-start gap-2">
                                        <span className="text-warning">•</span>
                                        {s}
                                      </li>
                                    ))}
                                  </ul>
                                </div>
                                
                                {/* Causes */}
                                <div>
                                  <h6 className="text-xs font-semibold text-muted-foreground mb-2 flex items-center gap-1">
                                    <Bug className="h-3 w-3" />
                                    {language === 'en' ? 'Possible Causes:' : 'Causes Possibles:'}
                                  </h6>
                                  <ul className="text-xs text-muted-foreground space-y-1">
                                    {issue.causes.map((c, i) => (
                                      <li key={i} className="flex items-start gap-2">
                                        <span className="text-destructive">•</span>
                                        {c}
                                      </li>
                                    ))}
                                  </ul>
                                </div>
                                
                                {/* Solutions */}
                                <div>
                                  <h6 className="text-xs font-semibold text-muted-foreground mb-2 flex items-center gap-1">
                                    <Wrench className="h-3 w-3" />
                                    {language === 'en' ? 'Solutions:' : 'Solutions:'}
                                  </h6>
                                  <ul className="text-xs space-y-1">
                                    {issue.solutions.map((s, i) => (
                                      <li key={i} className="flex items-start gap-2 text-success">
                                        <CheckCircle className="h-3 w-3 mt-0.5 shrink-0" />
                                        {s}
                                      </li>
                                    ))}
                                  </ul>
                                </div>
                                
                                {/* Code Example */}
                                {issue.codeExample && (
                                  <div>
                                    <h6 className="text-xs font-semibold text-muted-foreground mb-2">
                                      {language === 'en' ? 'Debug Code:' : 'Code de Débogage:'}
                                    </h6>
                                    <pre className="bg-muted p-3 rounded-lg text-xs font-mono overflow-x-auto whitespace-pre max-h-48 overflow-y-auto">
                                      {issue.codeExample}
                                    </pre>
                                  </div>
                                )}
                              </div>
                            </AccordionContent>
                          </AccordionItem>
                        ))}
                      </Accordion>
                      {catIndex < content.troubleshooting.categories.length - 1 && <Separator className="mt-4" />}
                    </div>
                  ))}

                  <Separator />

                  {/* Debugging Tools */}
                  <div>
                    <h4 className="font-semibold mb-4 flex items-center gap-2">
                      <Wrench className="h-4 w-4" />
                      {content.troubleshooting.debuggingTools.title}
                    </h4>
                    <div className="grid md:grid-cols-2 gap-4">
                      {content.troubleshooting.debuggingTools.tools.map((tool, index) => (
                        <Card key={index} className="bg-muted/30">
                          <CardHeader className="pb-2">
                            <CardTitle className="text-sm font-medium">{tool.name}</CardTitle>
                            <CardDescription className="text-xs">{tool.description}</CardDescription>
                          </CardHeader>
                          <CardContent>
                            <ul className="text-xs space-y-1">
                              {tool.tips.map((tip, i) => (
                                <li key={i} className="flex items-start gap-2">
                                  <Info className="h-3 w-3 text-primary mt-0.5 shrink-0" />
                                  <span className="text-muted-foreground">{tip}</span>
                                </li>
                              ))}
                            </ul>
                          </CardContent>
                        </Card>
                      ))}
                    </div>
                  </div>

                  <Separator />

                  {/* Common Error Messages */}
                  <div>
                    <h4 className="font-semibold mb-4 flex items-center gap-2">
                      <AlertCircle className="h-4 w-4" />
                      {content.troubleshooting.commonErrorMessages.title}
                    </h4>
                    <div className="space-y-2">
                      {content.troubleshooting.commonErrorMessages.errors.map((error, index) => (
                        <div key={index} className="p-3 rounded-lg border border-border hover:bg-muted/50 transition-colors">
                          <div className="flex items-start gap-3">
                            <XCircle className="h-4 w-4 text-destructive mt-0.5 shrink-0" />
                            <div className="flex-1 min-w-0">
                              <code className="text-xs font-mono bg-destructive/10 text-destructive px-2 py-0.5 rounded">
                                {error.message}
                              </code>
                              <p className="text-xs text-muted-foreground mt-1">
                                <span className="font-medium">{language === 'en' ? 'Cause:' : 'Cause:'}</span> {error.cause}
                              </p>
                              <p className="text-xs text-success mt-1">
                                <span className="font-medium">{language === 'en' ? 'Solution:' : 'Solution:'}</span> {error.solution}
                              </p>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            {/* Files Tab */}
            <TabsContent value="files" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Folder className="h-5 w-5" />
                    {content.files.title}
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-6">
                  {content.files.categories.map((category, catIndex) => (
                    <div key={catIndex}>
                      <h4 className="font-semibold mb-3 text-sm">{category.name}</h4>
                      <div className="space-y-2">
                        {category.items.map((file, index) => (
                          <div key={index} className="flex items-center gap-3 p-3 rounded-lg bg-muted/50 hover:bg-muted transition-colors">
                            <FileCode className="h-5 w-5 text-primary shrink-0" />
                            <div className="flex-1 min-w-0">
                              <code className="text-sm font-medium text-foreground block truncate">{file.path}</code>
                              <p className="text-xs text-muted-foreground">{file.description}</p>
                            </div>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => copyToClipboard(file.path)}
                              className="shrink-0"
                            >
                              {copiedPath === file.path ? (
                                <Check className="h-4 w-4 text-green-500" />
                              ) : (
                                <Copy className="h-4 w-4" />
                              )}
                            </Button>
                          </div>
                        ))}
                      </div>
                      {catIndex < content.files.categories.length - 1 && <Separator className="mt-4" />}
                    </div>
                  ))}
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>

          {/* Quick Links */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <GitBranch className="h-5 w-5" />
                {language === 'en' ? 'Quick Links' : 'Liens Rapides'}
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
                <Button variant="outline" className="justify-start" onClick={() => window.open(SWAGGER_URL, '_blank')}>
                  <FileCode className="h-4 w-4 mr-2" />
                  Swagger
                </Button>
                <Button variant="outline" className="justify-start" onClick={() => navigate('/dashboard/settings/logs')}>
                  <Terminal className="h-4 w-4 mr-2" />
                  {language === 'en' ? 'System Logs' : 'Logs Système'}
                </Button>
                <Button variant="outline" className="justify-start" onClick={() => navigate('/dashboard/lookups')}>
                  <Settings className="h-4 w-4 mr-2" />
                  Lookups
                </Button>
              </div>
            </CardContent>
          </Card>

        </div>
      </ScrollArea>
    </div>
  );
}
