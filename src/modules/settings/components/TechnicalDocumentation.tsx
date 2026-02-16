import React from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { 
  Database, Server, Layers, ArrowRight, Code, FileCode, 
  Users, Package, Workflow, Settings, Shield, Clock
} from "lucide-react";
import { useTranslation } from "react-i18next";

// ==================== DATA FLOW DOCUMENTATION ====================
const dataFlowSteps = [
  {
    step: 1,
    title: "Offer Creation",
    entity: "Offer",
    description: "A sales representative creates an offer for a contact (client). The offer contains items which can be materials (articles) or services.",
    tables: ["crm.offers", "crm.offer_items"],
    statuses: ["draft", "sent", "negotiation", "accepted", "declined", "expired"],
    fields: ["OfferNumber", "ContactId", "Title", "TotalAmount", "ValidUntil", "Items[]"]
  },
  {
    step: 2,
    title: "Offer → Sale Conversion",
    entity: "Sale",
    description: "When an offer is accepted, it converts to a Sale Order. All offer items are copied to sale items with fulfillment tracking.",
    tables: ["crm.sales", "crm.sale_items"],
    statuses: ["created", "in_progress", "closed", "invoiced", "cancelled"],
    fields: ["SaleNumber", "OfferId", "ContactId", "Stage", "TotalAmount", "Items[]"]
  },
  {
    step: 3,
    title: "Sale → Service Order",
    entity: "ServiceOrder",
    description: "If a sale contains service-type items, a Service Order is created. Service items become Jobs, material items become Materials.",
    tables: ["field.service_orders", "field.service_order_jobs", "field.service_order_materials"],
    statuses: ["draft", "ready_for_planning", "in_progress", "completed", "cancelled"],
    fields: ["OrderNumber", "SaleId", "ContactId", "Jobs[]", "Materials[]", "Priority"]
  },
  {
    step: 4,
    title: "Job → Dispatch (Planning)",
    entity: "Dispatch",
    description: "When a job is dragged to a technician on the Planning Board, it becomes a Dispatch. This is the actual work assignment.",
    tables: ["field.dispatches", "field.dispatch_technicians"],
    statuses: ["pending", "assigned", "in_progress", "completed", "cancelled"],
    fields: ["DispatchNumber", "JobId", "ServiceOrderId", "ScheduledDate", "AssignedTechnicians[]"]
  },
  {
    step: 5,
    title: "Dispatch Execution",
    entity: "Dispatch Data",
    description: "Technicians execute the dispatch and log their work: time entries, expenses, materials used, notes, and attachments.",
    tables: ["field.time_entries", "field.expenses", "field.material_usage", "field.dispatch_notes", "field.dispatch_attachments"],
    statuses: ["pending", "approved", "rejected"],
    fields: ["TechnicianId", "StartTime", "EndTime", "Amount", "Description"]
  }
];

// ==================== BACKEND ENDPOINTS ====================
const backendModules = [
  {
    module: "Auth",
    description: "User authentication and authorization",
    endpoints: [
      { method: "POST", path: "/api/Auth/login", description: "User login with email/password, returns JWT token" },
      { method: "POST", path: "/api/Auth/signup", description: "Register new user account" },
      { method: "POST", path: "/api/Auth/refresh", description: "Refresh JWT token" },
      { method: "GET", path: "/api/Auth/me", description: "Get current authenticated user" }
    ]
  },
  {
    module: "Contacts",
    description: "Customer/Client management",
    endpoints: [
      { method: "GET", path: "/api/Contacts", description: "List contacts with pagination, filtering, search" },
      { method: "GET", path: "/api/Contacts/{id}", description: "Get contact by ID with full details" },
      { method: "POST", path: "/api/Contacts", description: "Create new contact" },
      { method: "PUT", path: "/api/Contacts/{id}", description: "Update contact" },
      { method: "DELETE", path: "/api/Contacts/{id}", description: "Soft delete contact" },
      { method: "GET", path: "/api/ContactNotes/contact/{id}", description: "Get notes for contact" },
      { method: "GET", path: "/api/ContactTags", description: "Get all contact tags" }
    ]
  },
  {
    module: "Offers",
    description: "Quotation/Proposal management",
    endpoints: [
      { method: "GET", path: "/api/Offers", description: "List offers with filtering (status, category, date range)" },
      { method: "GET", path: "/api/Offers/{id}", description: "Get offer with items" },
      { method: "POST", path: "/api/Offers", description: "Create offer with items" },
      { method: "PUT", path: "/api/Offers/{id}", description: "Update offer" },
      { method: "POST", path: "/api/Offers/{id}/renew", description: "Create new offer from existing (copy)" },
      { method: "POST", path: "/api/Offers/{id}/items", description: "Add item to offer" },
      { method: "GET", path: "/api/Offers/stats", description: "Get offer statistics" },
      { method: "GET", path: "/api/Offers/{id}/activities", description: "Get offer activity log" }
    ]
  },
  {
    module: "Sales",
    description: "Sale Order management (converted offers)",
    endpoints: [
      { method: "GET", path: "/api/Sales", description: "List sales with filtering" },
      { method: "GET", path: "/api/Sales/{id}", description: "Get sale with items" },
      { method: "POST", path: "/api/Sales", description: "Create sale directly" },
      { method: "POST", path: "/api/Sales/from-offer/{offerId}", description: "Convert offer to sale" },
      { method: "PUT", path: "/api/Sales/{id}", description: "Update sale" },
      { method: "POST", path: "/api/Sales/{id}/items", description: "Add/update sale items" },
      { method: "GET", path: "/api/Sales/stats", description: "Get sales statistics" },
      { method: "GET", path: "/api/Sales/{id}/activities", description: "Get sale activity log" }
    ]
  },
  {
    module: "ServiceOrders",
    description: "Field service work orders",
    endpoints: [
      { method: "GET", path: "/api/ServiceOrders", description: "List service orders" },
      { method: "GET", path: "/api/ServiceOrders/{id}", description: "Get service order with jobs and materials" },
      { method: "POST", path: "/api/ServiceOrders/from-sale/{saleId}", description: "Create service order from sale" },
      { method: "PUT", path: "/api/ServiceOrders/{id}", description: "Update service order" },
      { method: "GET", path: "/api/ServiceOrders/{id}/jobs", description: "Get jobs for service order" },
      { method: "POST", path: "/api/ServiceOrders/{id}/jobs", description: "Add job to service order" },
      { method: "GET", path: "/api/ServiceOrders/{id}/materials", description: "Get materials for service order" }
    ]
  },
  {
    module: "Dispatches",
    description: "Technician work assignments",
    endpoints: [
      { method: "GET", path: "/api/Dispatches", description: "List dispatches with filtering" },
      { method: "GET", path: "/api/Dispatches/{id}", description: "Get dispatch with all related data" },
      { method: "POST", path: "/api/Dispatches/from-job/{jobId}", description: "Create dispatch from job (planning)" },
      { method: "PUT", path: "/api/Dispatches/{id}", description: "Update dispatch" },
      { method: "PATCH", path: "/api/Dispatches/{id}/status", description: "Update dispatch status only" },
      { method: "POST", path: "/api/Dispatches/{id}/start", description: "Start dispatch (set in_progress)" },
      { method: "POST", path: "/api/Dispatches/{id}/complete", description: "Complete dispatch" },
      { method: "GET", path: "/api/Dispatches/{id}/time-entries", description: "Get time entries" },
      { method: "POST", path: "/api/Dispatches/{id}/time-entries", description: "Add time entry" },
      { method: "GET", path: "/api/Dispatches/{id}/expenses", description: "Get expenses" },
      { method: "POST", path: "/api/Dispatches/{id}/expenses", description: "Add expense" },
      { method: "GET", path: "/api/Dispatches/{id}/materials", description: "Get materials used" },
      { method: "POST", path: "/api/Dispatches/{id}/materials", description: "Add material usage" },
      { method: "GET", path: "/api/Dispatches/{id}/notes", description: "Get dispatch notes" },
      { method: "POST", path: "/api/Dispatches/{id}/notes", description: "Add dispatch note" },
      { method: "POST", path: "/api/Dispatches/{id}/technicians", description: "Assign technician" },
      { method: "DELETE", path: "/api/Dispatches/{id}/technicians/{techId}", description: "Unassign technician" }
    ]
  },
  {
    module: "Planning",
    description: "Drag & drop planning board",
    endpoints: [
      { method: "GET", path: "/api/Planning/board", description: "Get planning board data (technicians, unplanned jobs)" },
      { method: "POST", path: "/api/Planning/assign", description: "Assign job to technician (creates dispatch)" },
      { method: "PUT", path: "/api/Planning/reschedule/{dispatchId}", description: "Reschedule dispatch" }
    ]
  },
  {
    module: "Articles",
    description: "Inventory/Product management",
    endpoints: [
      { method: "GET", path: "/api/Articles", description: "List articles with filtering" },
      { method: "GET", path: "/api/Articles/{id}", description: "Get article details" },
      { method: "POST", path: "/api/Articles", description: "Create article" },
      { method: "PUT", path: "/api/Articles/{id}", description: "Update article" },
      { method: "DELETE", path: "/api/Articles/{id}", description: "Delete article" }
    ]
  },
  {
    module: "Installations",
    description: "Equipment/Asset tracking",
    endpoints: [
      { method: "GET", path: "/api/Installations", description: "List installations" },
      { method: "GET", path: "/api/Installations/{id}", description: "Get installation details" },
      { method: "GET", path: "/api/Installations/contact/{contactId}", description: "Get installations for contact" },
      { method: "POST", path: "/api/Installations", description: "Create installation" },
      { method: "PUT", path: "/api/Installations/{id}", description: "Update installation" }
    ]
  },
  {
    module: "Users",
    description: "User and technician management",
    endpoints: [
      { method: "GET", path: "/api/Users", description: "List users" },
      { method: "GET", path: "/api/Users/{id}", description: "Get user details" },
      { method: "GET", path: "/api/Users/technicians", description: "Get technicians only" },
      { method: "POST", path: "/api/Users", description: "Create user" },
      { method: "PUT", path: "/api/Users/{id}", description: "Update user" }
    ]
  },
  {
    module: "Lookups",
    description: "System reference data",
    endpoints: [
      { method: "GET", path: "/api/Lookups/{type}", description: "Get lookup values by type (priorities, statuses, etc.)" },
      { method: "POST", path: "/api/Lookups", description: "Create lookup value" },
      { method: "PUT", path: "/api/Lookups/{id}", description: "Update lookup value" }
    ]
  }
];

// ==================== DATABASE SCHEMA ====================
const databaseSchema = {
  crm: [
    {
      table: "contacts",
      description: "Customers, clients, and companies",
      columns: ["id", "first_name", "last_name", "email", "phone", "company", "type", "status", "address", "created_by", "created_at"]
    },
    {
      table: "offers",
      description: "Quotations/Proposals sent to contacts",
      columns: ["id", "offer_number", "contact_id", "title", "description", "status", "total_amount", "valid_until", "created_by", "created_at"]
    },
    {
      table: "offer_items",
      description: "Line items in offers (materials or services)",
      columns: ["id", "offer_id", "type", "article_id", "item_name", "quantity", "unit_price", "discount", "installation_id"]
    },
    {
      table: "sales",
      description: "Confirmed orders (converted from offers)",
      columns: ["id", "sale_number", "offer_id", "contact_id", "title", "status", "stage", "total_amount", "created_by"]
    },
    {
      table: "sale_items",
      description: "Line items in sales",
      columns: ["id", "sale_id", "type", "article_id", "item_name", "quantity", "unit_price", "requires_service_order", "service_order_id"]
    }
  ],
  field: [
    {
      table: "service_orders",
      description: "Work orders for field service",
      columns: ["id", "order_number", "sale_id", "contact_id", "status", "priority", "start_date", "target_completion_date", "total_amount"]
    },
    {
      table: "service_order_jobs",
      description: "Individual jobs within a service order",
      columns: ["id", "service_order_id", "sale_item_id", "title", "description", "status", "priority", "installation_id", "estimated_duration"]
    },
    {
      table: "service_order_materials",
      description: "Materials allocated to service order",
      columns: ["id", "service_order_id", "article_id", "name", "quantity", "unit_price", "status"]
    },
    {
      table: "dispatches",
      description: "Planned work assignments for technicians",
      columns: ["id", "dispatch_number", "job_id", "service_order_id", "contact_id", "status", "priority", "scheduled_date", "site_address"]
    },
    {
      table: "dispatch_technicians",
      description: "Technicians assigned to dispatches",
      columns: ["id", "dispatch_id", "technician_id", "assigned_date", "role"]
    },
    {
      table: "time_entries",
      description: "Time logged by technicians",
      columns: ["id", "dispatch_id", "technician_id", "work_type", "start_time", "end_time", "duration", "description"]
    },
    {
      table: "expenses",
      description: "Expenses logged by technicians",
      columns: ["id", "dispatch_id", "technician_id", "category", "amount", "description", "receipt_url", "status"]
    },
    {
      table: "material_usage",
      description: "Materials used during dispatch",
      columns: ["id", "dispatch_id", "article_id", "quantity", "unit_price", "notes"]
    },
    {
      table: "dispatch_notes",
      description: "Notes added to dispatches",
      columns: ["id", "dispatch_id", "content", "created_by", "created_at"]
    },
    {
      table: "dispatch_attachments",
      description: "Files attached to dispatches",
      columns: ["id", "dispatch_id", "file_name", "file_url", "file_type", "created_by"]
    }
  ],
  inventory: [
    {
      table: "articles",
      description: "Products, parts, and services catalog",
      columns: ["id", "sku", "name", "type", "category", "price", "sell_price", "stock", "min_stock", "location"]
    },
    {
      table: "installations",
      description: "Equipment/assets at customer sites",
      columns: ["id", "contact_id", "name", "serial_number", "model", "location", "status", "warranty_end"]
    }
  ]
};

// ==================== FRONTEND ARCHITECTURE ====================
const frontendModules = [
  {
    module: "contacts",
    path: "/modules/contacts",
    description: "Contact/Customer management with DataGrid, detail views, notes, tags",
    components: ["ContactsModule", "ContactsTable", "ContactDetailDrawer", "ContactForm"],
    services: ["contacts.service.ts", "contactsApi.ts"]
  },
  {
    module: "offers",
    path: "/modules/offers",
    description: "Offer creation, item management, conversion to sales",
    components: ["OffersModule", "OffersTable", "OfferDetailPage", "OfferItemsCard"],
    services: ["offers.service.ts"]
  },
  {
    module: "sales",
    path: "/modules/sales",
    description: "Sales order management, fulfillment tracking",
    components: ["SalesModule", "SalesTable", "SaleDetailPage", "SaleItemsCard"],
    services: ["sales.service.ts"]
  },
  {
    module: "field",
    path: "/modules/field",
    description: "Service orders, jobs, dispatches, planning board",
    components: ["FieldModule", "ServiceOrdersPage", "DispatchDetailPage", "PlanningBoard"],
    services: ["serviceOrders.service.ts", "dispatches.service.ts", "planning.service.ts"]
  },
  {
    module: "articles",
    path: "/modules/articles",
    description: "Inventory management with stock tracking",
    components: ["ArticlesModule", "ArticlesTable", "ArticleForm"],
    services: ["articles.service.ts"]
  }
];

// ==================== BUSINESS LOGIC RULES ====================
const businessRules = [
  {
    category: "Offer → Sale Conversion",
    rules: [
      "Offer must be in 'accepted' or 'sent' status to convert",
      "All offer items are copied to sale items",
      "Offer status is updated to 'accepted' after conversion",
      "Offer's ConvertedToSaleId is set to the new sale ID",
      "Sale is created with status 'created' and stage 'offer'"
    ]
  },
  {
    category: "Sale → Service Order",
    rules: [
      "Only sales with service-type items can generate service orders",
      "Each service item becomes a ServiceOrderJob",
      "Each material/article item becomes a ServiceOrderMaterial",
      "Sale's ServiceOrdersStatus is updated to 'created'",
      "Installation references are preserved from sale items to jobs"
    ]
  },
  {
    category: "Job → Dispatch (Planning)",
    rules: [
      "A job can only have one active (non-deleted) dispatch",
      "When technicians are assigned, dispatch status becomes 'assigned'",
      "Without technicians, dispatch status is 'pending'",
      "Job status is updated to 'dispatched' when dispatch is created with technicians",
      "DispatchNumber is auto-generated with format DISP-{timestamp}"
    ]
  },
  {
    category: "Dispatch Lifecycle",
    rules: [
      "Status flow: pending → assigned → in_progress → completed",
      "Starting dispatch sets ActualStartTime",
      "Completing dispatch sets ActualEndTime and CompletionPercentage",
      "Time entries, expenses, materials, notes can be added at any active status",
      "Deleting dispatch is soft-delete (IsDeleted = true)"
    ]
  },
  {
    category: "Time & Expense Tracking",
    rules: [
      "Time entries require technician, start time, end time, work type",
      "Duration is auto-calculated from start/end times",
      "Expenses require category, amount, and optional receipt",
      "Material usage reduces stock from articles table",
      "All entries are linked to dispatch via DispatchId"
    ]
  }
];

export function TechnicalDocumentation() {
  const { t } = useTranslation();

  return (
    <div className="space-y-6">
      <Card className="border-border/50 shadow-soft">
        <CardHeader>
          <CardTitle className="text-foreground flex items-center gap-2">
            <FileCode className="h-5 w-5" />
            Technical Documentation
          </CardTitle>
          <CardDescription>
            Comprehensive documentation covering frontend architecture, backend APIs, database schema, and business logic
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Tabs defaultValue="data-flow" className="w-full">
            <TabsList className="grid w-full grid-cols-5">
              <TabsTrigger value="data-flow" className="text-xs">Data Flow</TabsTrigger>
              <TabsTrigger value="backend" className="text-xs">Backend API</TabsTrigger>
              <TabsTrigger value="database" className="text-xs">Database</TabsTrigger>
              <TabsTrigger value="frontend" className="text-xs">Frontend</TabsTrigger>
              <TabsTrigger value="business" className="text-xs">Business Rules</TabsTrigger>
            </TabsList>

            {/* DATA FLOW TAB */}
            <TabsContent value="data-flow" className="mt-4">
              <ScrollArea className="h-[600px] pr-4">
                <div className="space-y-4">
                  <div className="p-4 bg-primary/5 rounded-lg border border-primary/20">
                    <h3 className="font-semibold text-lg mb-2 flex items-center gap-2">
                      <Workflow className="h-5 w-5 text-primary" />
                      Complete Data Flow: Offer → Sale → Service Order → Dispatch
                    </h3>
                    <p className="text-sm text-muted-foreground">
                      This flow represents the complete lifecycle of a customer transaction from initial quotation to field service execution.
                    </p>
                  </div>

                  {dataFlowSteps.map((step, index) => (
                    <Card key={step.step} className="border-l-4 border-l-primary">
                      <CardContent className="pt-4">
                        <div className="flex items-start gap-4">
                          <div className="flex-shrink-0 w-10 h-10 rounded-full bg-primary text-primary-foreground flex items-center justify-center font-bold">
                            {step.step}
                          </div>
                          <div className="flex-1">
                            <div className="flex items-center gap-2 mb-2">
                              <h4 className="font-semibold text-lg">{step.title}</h4>
                              <Badge variant="outline">{step.entity}</Badge>
                            </div>
                            <p className="text-sm text-muted-foreground mb-3">{step.description}</p>
                            
                            <div className="grid grid-cols-3 gap-4 text-sm">
                              <div>
                                <span className="font-medium text-muted-foreground">Tables:</span>
                                <div className="flex flex-wrap gap-1 mt-1">
                                  {step.tables.map(table => (
                                    <Badge key={table} variant="secondary" className="text-xs">{table}</Badge>
                                  ))}
                                </div>
                              </div>
                              <div>
                                <span className="font-medium text-muted-foreground">Statuses:</span>
                                <div className="flex flex-wrap gap-1 mt-1">
                                  {step.statuses.map(status => (
                                    <Badge key={status} variant="outline" className="text-xs">{status}</Badge>
                                  ))}
                                </div>
                              </div>
                              <div>
                                <span className="font-medium text-muted-foreground">Key Fields:</span>
                                <div className="text-xs text-muted-foreground mt-1">
                                  {step.fields.join(", ")}
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>
                        {index < dataFlowSteps.length - 1 && (
                          <div className="flex justify-center mt-4">
                            <ArrowRight className="h-6 w-6 text-muted-foreground" />
                          </div>
                        )}
                      </CardContent>
                    </Card>
                  ))}
                </div>
              </ScrollArea>
            </TabsContent>

            {/* BACKEND API TAB */}
            <TabsContent value="backend" className="mt-4">
              <ScrollArea className="h-[600px] pr-4">
                <div className="space-y-4">
                  <div className="p-4 bg-primary/10 rounded-lg border border-primary/20">
                    <h3 className="font-semibold flex items-center gap-2">
                      <Server className="h-5 w-5 text-primary" />
                      Backend: .NET 8 Web API
                    </h3>
                    <p className="text-sm text-muted-foreground mt-1">
                      Base URL: <code className="bg-muted px-2 py-0.5 rounded">https://co-mouhibilpadrino.com</code>
                    </p>
                    <p className="text-xs text-muted-foreground mt-1">
                      All endpoints require Bearer token authentication (JWT)
                    </p>
                  </div>

                  <Accordion type="multiple" className="w-full">
                    {backendModules.map((module) => (
                      <AccordionItem key={module.module} value={module.module}>
                        <AccordionTrigger className="hover:no-underline">
                          <div className="flex items-center gap-3">
                            <Badge variant="default">{module.module}</Badge>
                            <span className="text-sm text-muted-foreground">{module.description}</span>
                          </div>
                        </AccordionTrigger>
                        <AccordionContent>
                          <div className="space-y-2 pl-4">
                            {module.endpoints.map((endpoint, idx) => (
                              <div key={idx} className="flex items-start gap-3 p-2 rounded border bg-muted/30">
                                <Badge 
                                  variant={
                                    endpoint.method === "GET" ? "default" : 
                                    endpoint.method === "POST" ? "secondary" : 
                                    endpoint.method === "PUT" ? "outline" :
                                    endpoint.method === "PATCH" ? "outline" :
                                    "destructive"
                                  }
                                  className="w-16 justify-center"
                                >
                                  {endpoint.method}
                                </Badge>
                                <div className="flex-1">
                                  <code className="text-xs font-mono">{endpoint.path}</code>
                                  <p className="text-xs text-muted-foreground mt-0.5">{endpoint.description}</p>
                                </div>
                              </div>
                            ))}
                          </div>
                        </AccordionContent>
                      </AccordionItem>
                    ))}
                  </Accordion>
                </div>
              </ScrollArea>
            </TabsContent>

            {/* DATABASE TAB */}
            <TabsContent value="database" className="mt-4">
              <ScrollArea className="h-[600px] pr-4">
                <div className="space-y-6">
                  <div className="p-4 bg-success/10 rounded-lg border border-success/20">
                    <h3 className="font-semibold flex items-center gap-2">
                      <Database className="h-5 w-5 text-success" />
                      PostgreSQL Database Schema
                    </h3>
                    <p className="text-sm text-muted-foreground mt-1">
                      Database is organized into logical schemas: CRM, Field, and Inventory
                    </p>
                  </div>

                  {Object.entries(databaseSchema).map(([schema, tables]) => (
                    <div key={schema}>
                      <h4 className="font-semibold text-lg mb-3 flex items-center gap-2">
                        <Layers className="h-4 w-4" />
                        {schema.toUpperCase()} Schema
                      </h4>
                      <div className="grid gap-3">
                        {tables.map((table) => (
                          <Card key={table.table} className="border-l-4 border-l-green-500">
                            <CardContent className="pt-4">
                              <div className="flex items-center gap-2 mb-2">
                                <code className="font-mono text-sm font-semibold">{schema}.{table.table}</code>
                              </div>
                              <p className="text-sm text-muted-foreground mb-2">{table.description}</p>
                              <div className="flex flex-wrap gap-1">
                                {table.columns.map((col) => (
                                  <Badge key={col} variant="outline" className="text-xs font-mono">
                                    {col}
                                  </Badge>
                                ))}
                              </div>
                            </CardContent>
                          </Card>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              </ScrollArea>
            </TabsContent>

            {/* FRONTEND TAB */}
            <TabsContent value="frontend" className="mt-4">
              <ScrollArea className="h-[600px] pr-4">
                <div className="space-y-4">
                  <div className="p-4 bg-chart-5/10 rounded-lg border border-chart-5/20">
                    <h3 className="font-semibold flex items-center gap-2">
                      <Code className="h-5 w-5 text-chart-5" />
                      Frontend: React + TypeScript + Vite
                    </h3>
                    <p className="text-sm text-muted-foreground mt-1">
                      Modular architecture with feature-based organization. Each module contains its own components, services, types, and hooks.
                    </p>
                  </div>

                  <div className="grid gap-4">
                    <Card className="border-border/50">
                      <CardHeader className="pb-2">
                        <CardTitle className="text-base flex items-center gap-2">
                          <Package className="h-4 w-4" />
                          Tech Stack
                        </CardTitle>
                      </CardHeader>
                      <CardContent>
                        <div className="flex flex-wrap gap-2">
                          <Badge>React 18</Badge>
                          <Badge>TypeScript</Badge>
                          <Badge>Vite</Badge>
                          <Badge>TailwindCSS</Badge>
                          <Badge>shadcn/ui</Badge>
                          <Badge>React Query</Badge>
                          <Badge>React Router</Badge>
                          <Badge>React Hook Form</Badge>
                          <Badge>i18next</Badge>
                          <Badge>Axios</Badge>
                          <Badge>date-fns</Badge>
                          <Badge>Recharts</Badge>
                        </div>
                      </CardContent>
                    </Card>

                    {frontendModules.map((mod) => (
                      <Card key={mod.module} className="border-l-4 border-l-purple-500">
                        <CardContent className="pt-4">
                          <div className="flex items-center gap-2 mb-2">
                            <Badge variant="default">{mod.module}</Badge>
                            <code className="text-xs text-muted-foreground">{mod.path}</code>
                          </div>
                          <p className="text-sm text-muted-foreground mb-3">{mod.description}</p>
                          <div className="grid grid-cols-2 gap-4 text-sm">
                            <div>
                              <span className="font-medium text-muted-foreground">Components:</span>
                              <div className="flex flex-wrap gap-1 mt-1">
                                {mod.components.map(comp => (
                                  <Badge key={comp} variant="outline" className="text-xs">{comp}</Badge>
                                ))}
                              </div>
                            </div>
                            <div>
                              <span className="font-medium text-muted-foreground">Services:</span>
                              <div className="flex flex-wrap gap-1 mt-1">
                                {mod.services.map(svc => (
                                  <Badge key={svc} variant="secondary" className="text-xs font-mono">{svc}</Badge>
                                ))}
                              </div>
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    ))}
                  </div>
                </div>
              </ScrollArea>
            </TabsContent>

            {/* BUSINESS RULES TAB */}
            <TabsContent value="business" className="mt-4">
              <ScrollArea className="h-[600px] pr-4">
                <div className="space-y-4">
                  <div className="p-4 bg-warning/10 rounded-lg border border-warning/20">
                    <h3 className="font-semibold flex items-center gap-2">
                      <Shield className="h-5 w-5 text-warning" />
                      Business Logic & Validation Rules
                    </h3>
                    <p className="text-sm text-muted-foreground mt-1">
                      Core business rules enforced by the backend services
                    </p>
                  </div>

                  {businessRules.map((category) => (
                    <Card key={category.category} className="border-l-4 border-l-warning">
                      <CardHeader className="pb-2">
                        <CardTitle className="text-base">{category.category}</CardTitle>
                      </CardHeader>
                      <CardContent>
                        <ul className="space-y-2">
                          {category.rules.map((rule, idx) => (
                            <li key={idx} className="flex items-start gap-2 text-sm">
                              <span className="text-warning mt-1">•</span>
                              <span>{rule}</span>
                            </li>
                          ))}
                        </ul>
                      </CardContent>
                    </Card>
                  ))}

                  <Card className="border-border/50">
                    <CardHeader className="pb-2">
                      <CardTitle className="text-base flex items-center gap-2">
                        <Clock className="h-4 w-4" />
                        Status Transitions
                      </CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                      <div>
                        <h5 className="font-medium mb-2">Offer Statuses</h5>
                        <div className="flex items-center gap-2 text-xs">
                          <Badge variant="outline">draft</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="outline">sent</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="outline">negotiation</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="default">accepted</Badge>
                          <span className="text-muted-foreground">/ declined / expired</span>
                        </div>
                      </div>
                      <div>
                        <h5 className="font-medium mb-2">Sale Statuses</h5>
                        <div className="flex items-center gap-2 text-xs">
                          <Badge variant="outline">created</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="outline">in_progress</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="default">closed</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="secondary">invoiced</Badge>
                        </div>
                      </div>
                      <div>
                        <h5 className="font-medium mb-2">Service Order Statuses</h5>
                        <div className="flex items-center gap-2 text-xs">
                          <Badge variant="outline">draft</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="outline">ready_for_planning</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="outline">in_progress</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="default">completed</Badge>
                        </div>
                      </div>
                      <div>
                        <h5 className="font-medium mb-2">Dispatch Statuses</h5>
                        <div className="flex items-center gap-2 text-xs">
                          <Badge variant="outline">pending</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="outline">assigned</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="outline">in_progress</Badge>
                          <ArrowRight className="h-3 w-3" />
                          <Badge variant="default">completed</Badge>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                </div>
              </ScrollArea>
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>
    </div>
  );
}

export default TechnicalDocumentation;
