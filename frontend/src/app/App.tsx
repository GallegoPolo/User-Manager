import { Navigate, Route, Routes } from "react-router-dom";
import { ProtectedRoute } from "../features/auth/components/ProtectedRoute";
import { ContactsPage } from "../features/contacts/pages/ContactsPage";
import { LandingPage } from "../features/landing/pages/LandingPage";
import { MePage } from "../features/users/pages/MePage";
import { UsersPage } from "../features/users/pages/UsersPage";
import { AuditPage } from "../features/audit/pages/AuditPage";
import { AppLayout } from "../shared/components/AppLayout";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/contacts" element={<ContactsPage />} />
          <Route path="/me" element={<MePage />} />
          <Route path="/users" element={<UsersPage />} />
          <Route path="/audit" element={<AuditPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
