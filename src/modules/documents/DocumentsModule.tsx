import { Routes, Route } from "react-router-dom";
import { DocumentsList } from "./components/DocumentsList";

export function DocumentsModule() {
  return (
    <Routes>
      <Route index element={<DocumentsList />} />
    </Routes>
  );
}