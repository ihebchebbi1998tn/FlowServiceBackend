import { Routes, Route } from 'react-router-dom';
import StockManagementPage from './pages/StockManagementPage';

export default function StockManagementModule() {
  return (
    <Routes>
      <Route index element={<StockManagementPage />} />
    </Routes>
  );
}
