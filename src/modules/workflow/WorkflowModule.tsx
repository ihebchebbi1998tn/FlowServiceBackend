import { Routes, Route } from "react-router-dom";
import { WorkflowDashboard } from "./components/WorkflowDashboard";
import { OfferDetail } from "./components/OfferDetail";
import { SaleDetail } from "./components/SaleDetail";
import { ServiceOrderDetail } from "./components/ServiceOrderDetail";
import { DispatchBoard } from "./components/DispatchBoard";
import { WorkflowCalendar } from "./components/WorkflowCalendar";

export function WorkflowModule() {
  return (
    <Routes>
      <Route index element={<WorkflowDashboard />} />
      <Route path="offers/:id" element={<OfferDetail />} />
      <Route path="sales/:id" element={<SaleDetail />} />
      <Route path="service-orders/:id" element={<ServiceOrderDetail />} />
      <Route path="dispatch-board" element={<DispatchBoard />} />
      <Route path="calendar" element={<WorkflowCalendar />} />
    </Routes>
  );
}