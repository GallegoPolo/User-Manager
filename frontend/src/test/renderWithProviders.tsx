import type { ReactElement } from "react";
import { render } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { AuthContext, type AuthContextValue } from "../features/auth/context/AuthContextDefinition";
import type { BffUser, Role } from "../features/auth/types/auth";

interface Options {
  session?: BffUser | null;
  route?: string;
  login?: AuthContextValue["login"];
}

export function aSession(overrides: Partial<BffUser> = {}): BffUser {
  return {
    sub: "sub-ana",
    name: "Ana Souza",
    email: "ana@user-manager.local",
    roles: [],
    logoutUrl: "/bff/logout?sid=1",
    ...overrides,
  };
}

function authValue(session: BffUser | null | undefined, login: AuthContextValue["login"]): AuthContextValue {
  const user = session ?? null;

  return {
    user,
    isLoading: session === undefined,
    isAuthenticated: user !== null,
    hasRole: (...roles: Role[]) => roles.some((role) => user?.roles.includes(role) ?? false),
    login,
    logout: () => {},
  };
}

export function renderWithProviders(ui: ReactElement, options: Options = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const session = "session" in options ? options.session : null;

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[options.route ?? "/"]}>
        <AuthContext.Provider value={authValue(session, options.login ?? (() => {}))}>{ui}</AuthContext.Provider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}
