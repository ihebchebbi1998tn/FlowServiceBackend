import { Routes, Route } from "react-router-dom";
import React, { Suspense } from "react";
import { PermissionRoute } from "@/components/permissions/PermissionRoute";
import { PageSkeleton } from "@/components/ui/page-skeleton";

// Lazy load CRM Dashboard for better initial load
const DashboardOverview = React.lazy(() => import("./DashboardOverview"));
const DashboardBuilderPage = React.lazy(() => import("@/modules/dashboard-builder/components/DashboardManager").then(m => ({ default: m.DashboardManager })));
// Lazy load heavy table/data modules for better performance
const ContactsModule = React.lazy(() => import("@/modules/contacts/ContactsModule").then(m => ({ default: m.ContactsModule })));
const SalesModule = React.lazy(() => import("@/modules/sales").then(m => ({ default: m.SalesModule })));
const OffersModule = React.lazy(() => import("@/modules/offers").then(m => ({ default: m.OffersModule })));
const ArticlesModule = React.lazy(() => import("@/modules/articles/ArticlesModule").then(m => ({ default: m.ArticlesModule })));
const InventoryServicesModule = React.lazy(() => import("@/modules/inventory-services/InventoryServicesModule"));
const StockManagementModule = React.lazy(() => import("@/modules/stock-management"));

// Lazy load heavy visualization/complex modules
const CalendarModule = React.lazy(() => import("@/modules/calendar/CalendarModule").then(m => ({ default: m.CalendarModule })));
const FieldModule = React.lazy(() => import("@/modules/field/FieldModule").then(m => ({ default: m.FieldModule })));
const AnalyticsModule = React.lazy(() => import("@/modules/analytics/AnalyticsModule").then(m => ({ default: m.AnalyticsModule })));
const TasksModule = React.lazy(() => import("@/modules/tasks/TasksModule"));
const WorkflowModule = React.lazy(() => import("@/modules/workflow/WorkflowModule").then(m => ({ default: m.WorkflowModule })));
const AutomationModule = React.lazy(() => import("@/modules/automation/AutomationModule").then(m => ({ default: m.AutomationModule })));
const LookupsPage = React.lazy(() => import("@/modules/lookups/pages/LookupsPage"));
const WebsiteBuilderModule = React.lazy(() => import("@/modules/website-builder/WebsiteBuilderModule"));

// Lightweight modules - eager load
import { DocumentsModule } from "@/modules/documents";
import { DealsModule } from "@/modules/deals/DealsModule";
import { CommunicationModule } from "@/modules/communication/CommunicationModule";
const EmailCalendarModule = React.lazy(() => import("@/modules/email-calendar/EmailCalendarModule").then(m => ({ default: m.EmailCalendarModule })));
import { SettingsModule } from "@/modules/settings/SettingsModule";
import { NotificationsModule } from "@/modules/notifications/NotificationsModule";
import HelpModule from "./HelpModule";

export function DashboardContent() {
  return (
    <div>
      <Routes>
        <Route index element={
          <Suspense fallback={<PageSkeleton />}>
            <DashboardBuilderPage />
          </Suspense>
        } />
        <Route path="dashboards" element={
          <Suspense fallback={<PageSkeleton />}>
            <DashboardBuilderPage />
          </Suspense>
        } />
        <Route path="contacts/*" element={
          <PermissionRoute module="contacts" action="read">
            <Suspense fallback={<PageSkeleton />}>
              <ContactsModule />
            </Suspense>
          </PermissionRoute>
        } />
        <Route path="offers/*" element={
          <PermissionRoute module="offers" action="read">
            <Suspense fallback={<PageSkeleton />}>
              <OffersModule />
            </Suspense>
          </PermissionRoute>
        } />
        <Route path="sales/*" element={
          <PermissionRoute module="sales" action="read">
            <Suspense fallback={<PageSkeleton />}>
              <SalesModule />
            </Suspense>
          </PermissionRoute>
        } />
        <Route path="documents/*" element={
          <PermissionRoute module="documents" action="read">
            <DocumentsModule />
          </PermissionRoute>
        } />
        <Route path="workflow/*" element={
          <PermissionRoute module="settings" action="read">
            <Suspense fallback={<PageSkeleton />}>
              <WorkflowModule />
            </Suspense>
          </PermissionRoute>
        } />
        <Route path="articles/*" element={
          <PermissionRoute module="articles" action="read">
            <Suspense fallback={<PageSkeleton />}>
              <ArticlesModule />
            </Suspense>
          </PermissionRoute>
        } />
        <Route path="inventory-services/*" element={
          <PermissionRoute module="articles" action="read">
            <Suspense fallback={<PageSkeleton />}>
              <InventoryServicesModule />
            </Suspense>
          </PermissionRoute>
        } />
        <Route path="stock-management/*" element={
          <PermissionRoute module="articles" action="read">
            <Suspense fallback={<PageSkeleton />}>
              <StockManagementModule />
            </Suspense>
          </PermissionRoute>
        } />
        <Route path="deals/*" element={
          <PermissionRoute module="offers" action="read">
            <DealsModule />
          </PermissionRoute>
        } />
        <Route path="communication/*" element={
          <PermissionRoute module="contacts" action="read">
            <CommunicationModule />
          </PermissionRoute>
        } />
        <Route path="tasks/*" element={
          <Suspense fallback={<PageSkeleton />}>
            <TasksModule />
          </Suspense>
        } />
        <Route path="automation/*" element={
          <PermissionRoute module="settings" action="read">
            <Suspense fallback={<PageSkeleton />}>
              <AutomationModule />
            </Suspense>
          </PermissionRoute>
        } />
        <Route path="analytics/*" element={
          <PermissionRoute module="sales" action="read">
            <Suspense fallback={<PageSkeleton />}>
              <AnalyticsModule />
            </Suspense>
          </PermissionRoute>
        } />
        <Route path="calendar/*" element={
          <Suspense fallback={<PageSkeleton />}>
            <CalendarModule />
          </Suspense>
        } />
        <Route path="field/*" element={
          <PermissionRoute module="service_orders" action="read">
            <Suspense fallback={<PageSkeleton />}>
              <FieldModule />
            </Suspense>
          </PermissionRoute>
        } />
        <Route path="email-calendar/*" element={
          <Suspense fallback={<PageSkeleton />}>
            <EmailCalendarModule />
          </Suspense>
        } />
        <Route path="notifications" element={<NotificationsModule />} />
        <Route path="lookups/*" element={
          <PermissionRoute module="settings" action="read">
            <Suspense fallback={<PageSkeleton />}>
              <LookupsPage />
            </Suspense>
          </PermissionRoute>
        } />
        <Route path="settings/*" element={<SettingsModule />} />
        <Route path="website-builder/*" element={
          <Suspense fallback={<PageSkeleton />}>
            <WebsiteBuilderModule />
          </Suspense>
        } />
        {/* Help/Support route */}
        <Route path="help/*" element={<HelpModule />} />
      </Routes>
    </div>
  );
}
