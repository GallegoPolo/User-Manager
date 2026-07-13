import { NavLink } from "react-router-dom";
import { useAuth } from "../../features/auth/context/useAuth";

export function Sidebar() {
  const { logout } = useAuth();

  return (
    <aside className="sidebar">
      <div className="sidebar-brand">
        <strong>User Manager</strong>
      </div>

      <nav className="sidebar-nav">
        <NavLink
          to="/users"
          className={({ isActive }) =>
            isActive ? "sidebar-link active" : "sidebar-link"
          }
        >
          Usuários
        </NavLink>

        <NavLink
          to="/audit"
          className={({ isActive }) =>
            isActive ? "sidebar-link active" : "sidebar-link"
          }
        >
          Auditoria
        </NavLink>
      </nav>

      <button type="button" className="sidebar-logout" onClick={logout}>
        Sair
      </button>
    </aside>
  );
}
