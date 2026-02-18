import { Routes, Route } from 'react-router-dom';
import DynamicFormsPage from './pages/DynamicFormsPage';
import CreateFormPage from './pages/CreateFormPage';
import EditFormPage from './pages/EditFormPage';
import FormPreviewPage from './pages/FormPreviewPage';
import FormResponsesPage from './pages/FormResponsesPage';

export function DynamicFormsModule() {
  return (
    <Routes>
      <Route index element={<DynamicFormsPage />} />
      <Route path="create" element={<CreateFormPage />} />
      <Route path=":id/edit" element={<EditFormPage />} />
      <Route path=":id/preview" element={<FormPreviewPage />} />
      <Route path=":id/responses" element={<FormResponsesPage />} />
    </Routes>
  );
}

export default DynamicFormsModule;
