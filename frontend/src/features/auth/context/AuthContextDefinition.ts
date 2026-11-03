import { createContext } from "react";
import type { BffUser, Role } from "../types/auth";

export interface AuthContextValue {
  user: BffUser | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  hasRole: (...roles: Role[]) => boolean;
  login: () => void;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
