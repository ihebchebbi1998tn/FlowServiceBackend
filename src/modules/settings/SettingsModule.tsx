import { Routes, Route } from "react-router-dom";
import { SettingsLayoutNew } from "./components/SettingsLayoutNew";
import DatabaseFullView from "./pages/DatabaseFullView";
import SettingsPage from "./pages/SettingsPage";
import { SettingsLayout } from "./components/SettingsLayout";
import SystemLogsPage from "./pages/SystemLogsPage";
import DocumentationPage from "./pages/DocumentationPage";
import { DynamicFormsModule } from "@/modules/dynamic-forms";

export function SettingsModule() {
  return (
    <Routes>
      <Route index element={<SettingsPage />} />
      <Route path="advanced" element={<SettingsLayoutNew />} />
      <Route path="system" element={<SettingsLayout />} />
      <Route path="logs" element={<SystemLogsPage />} />
      <Route path="database-full-view" element={<DatabaseFullView />} />
      <Route path="documentation" element={<DocumentationPage />} />
      <Route path="dynamic-forms/*" element={<DynamicFormsModule />} />
    </Routes>
  );
}
