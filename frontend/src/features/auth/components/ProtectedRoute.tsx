import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../context/useAuth";

export interface ReturnState {
  from?: string;
}

export function ProtectedRoute() {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return <p className="loading">Carregando...</p>;
  }

  if (!isAuthenticated) {
    const state: ReturnState = { from: location.pathname + location.search };
    return <Navigate to="/" replace state={state} />;
  }

  return <Outlet />;
}
