import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../context/useAuth";

export function ProtectedRoute() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <p className="loading">Carregando...</p>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
