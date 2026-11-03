import { NavLink } from "react-router-dom";
import { useAuth } from "../../features/auth/context/useAuth";

function linkClass({ isActive }: { isActive: boolean }) {
  return isActive ? "sidebar-link active" : "sidebar-link";
}

export function Sidebar() {
  const { user, hasRole, logout } = useAuth();
  const canSeeOperations = hasRole("viewer", "operator");

  return (
    <aside className="sidebar">
      <div className="sidebar-brand">
        <strong>User Manager</strong>
        {user?.name && <small>{user.name}</small>}
      </div>

      <nav className="sidebar-nav">
        <NavLink to="/me" className={linkClass}>
          Minha conta
        </NavLink>

        {canSeeOperations && (
          <>
            <NavLink to="/users" className={linkClass}>
              Usuários
            </NavLink>

            <NavLink to="/audit" className={linkClass}>
              Auditoria
            </NavLink>
          </>
        )}
      </nav>

      <button type="button" className="sidebar-logout" onClick={logout}>
        Sair
      </button>
    </aside>
  );
}
