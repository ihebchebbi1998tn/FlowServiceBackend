import { Routes, Route } from "react-router-dom";
import ContactsPage from "./pages/ContactsPage";
import ContactDetailPage from "./pages/ContactDetailPage";
import AddContactPage from "./pages/AddContactPage";

export function ContactsModule() {
  return (
    <Routes>
      <Route index element={<ContactsPage />} />
      <Route path="add" element={<AddContactPage />} />
      <Route path=":id" element={<ContactDetailPage />} />
    </Routes>
  );
}