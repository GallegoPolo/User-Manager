import { Navigate, Route, Routes } from "react-router-dom";
import { ProtectedRoute } from "../features/auth/components/ProtectedRoute";
import { LoginPage } from "../features/auth/pages/LoginPage";
import { UsersPage } from "../features/users/pages/UsersPage";
import { AuditPage } from "../features/audit/pages/AuditPage";
import { AppLayout } from "../shared/components/AppLayout";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/users" element={<UsersPage />} />
          <Route path="/audit" element={<AuditPage />} />
        </Route>
      </Route>

      <Route path="/" element={<Navigate to="/users" replace />} />
      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  );
}
