import { Routes, Route } from "react-router-dom";
import { ArticlesList } from "./components/ArticlesList";
import ArticleDetail from "./pages/ArticleDetail";
import AddArticle from "./pages/AddArticle";

export function ArticlesModule() {
  return (
    <Routes>
      <Route index element={<ArticlesList />} />
      <Route path="add" element={<AddArticle />} />
      <Route path=":id" element={<ArticleDetail />} />
    </Routes>
  );
}