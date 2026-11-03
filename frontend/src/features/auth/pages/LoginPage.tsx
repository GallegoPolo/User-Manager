import type { SubmitEvent } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "../context/useAuth";

export function LoginPage() {
  const { login, isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <p className="page">Carregando...</p>;
  }

  if (isAuthenticated) {
    return <Navigate to="/me" replace />;
  }

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    login();
  }

  return (
    <main className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <h1>User Manager</h1>
        <p>Entre ou crie sua conta com e-mail e senha.</p>
        <button type="submit">Entrar</button>
      </form>
    </main>
  );
}
