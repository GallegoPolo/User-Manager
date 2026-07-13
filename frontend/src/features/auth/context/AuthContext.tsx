import { useCallback, useMemo, useState, type ReactNode } from "react";
import { login as loginRequest } from "../api/authService";
import type { LoginRequest } from "../types/auth";
import { setStoredToken } from "../api/tokenStore.ts";
import { AuthContext } from "./AuthContextDefinition";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(null);

  const login = useCallback(async (credentials: LoginRequest) => {
    const response = await loginRequest(credentials);
    setToken(response.accessToken);
    setStoredToken(response.accessToken);
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    setStoredToken(null);
  }, []);

  const value = useMemo(
    () => ({
      token,
      isAuthenticated: token !== null,
      login,
      logout,
    }),
    [token, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
