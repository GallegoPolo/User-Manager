import { useCallback, useMemo, type ReactNode } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { getSessionUser, redirectToLogin } from "../api/bffSession";
import type { Role } from "../types/auth";
import { AuthContext } from "./AuthContextDefinition";

const sessionKey = ["session"];

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const { data: user = null, isLoading } = useQuery({
    queryKey: sessionKey,
    queryFn: getSessionUser,
    staleTime: Infinity,
    retry: false,
  });

  const hasRole = useCallback(
    (...roles: Role[]) => roles.some((role) => user?.roles.includes(role) ?? false),
    [user],
  );

  const login = useCallback(
    (returnUrl?: string) => redirectToLogin(returnUrl ?? window.location.pathname),
    [],
  );

  const logout = useCallback(() => {
    void queryClient
      .fetchQuery({ queryKey: sessionKey, queryFn: getSessionUser, staleTime: 0, retry: false })
      .catch(() => user)
      .then((current) => window.location.assign(current?.logoutUrl ?? "/"));
  }, [queryClient, user]);

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
