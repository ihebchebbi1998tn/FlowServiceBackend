import { Routes, Route } from "react-router-dom";
import { CalendarPage } from "./components/CalendarPage";

export function CalendarModule() {
  return (
    <Routes>
      <Route index element={<CalendarPage />} />
    </Routes>
  );
}
