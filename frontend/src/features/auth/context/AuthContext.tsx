import { useCallback, useMemo, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { getSessionUser, redirectToLogin } from "../api/bffSession";
import type { Role } from "../types/auth";
import { AuthContext } from "./AuthContextDefinition";

export function AuthProvider({ children }: { children: ReactNode }) {
  const { data: user = null, isLoading } = useQuery({
    queryKey: ["session"],
    queryFn: getSessionUser,
    staleTime: Infinity,
    retry: false,
  });

  const hasRole = useCallback(
    (...roles: Role[]) => roles.some((role) => user?.roles.includes(role) ?? false),
    [user],
  );

  const login = useCallback(() => redirectToLogin(window.location.pathname), []);

  const logout = useCallback(() => {
    if (user) window.location.assign(user.logoutUrl);
  }, [user]);

  const value = useMemo(
    () => ({
      user,
      isLoading,
      isAuthenticated: user !== null,
      hasRole,
      login,
      logout,
    }),
    [user, isLoading, hasRole, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
