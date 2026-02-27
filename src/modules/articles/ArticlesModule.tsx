import { Routes, Route } from "react-router-dom";
import ArticlesPage from "./pages/ArticlesPage";

export function ArticlesModule() {
  return (
    <Routes>
      <Route index element={<ArticlesPage />} />
    </Routes>
  );
}