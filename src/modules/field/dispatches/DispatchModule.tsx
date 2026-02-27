import { Routes, Route, Navigate } from "react-router-dom";
import DispatchesList from "./pages/DispatchesList";
import DispatchJobDetail from "./pages/DispatchJobDetail";

export default function DispatchModule() {
  return (
    <Routes>
      <Route index element={<Navigate to="list" replace />} />
      <Route path="list" element={<DispatchesList />} />
      <Route path=":id" element={<DispatchJobDetail />} />
    </Routes>
  );
}