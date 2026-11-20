import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { retryOnlyTransientFailures } from "../../../shared/api/retryPolicy";
import { aSession } from "../../../test/renderWithProviders";
import type { BffUser } from "../types/auth";
import { AuthProvider } from "./AuthContext";
import { useAuth } from "./useAuth";

let sessions: (BffUser | null | "offline")[];
let assign: ReturnType<typeof vi.fn>;

function LogoutButton() {
  const { user, logout } = useAuth();
  return user ? <button onClick={logout}>Sair</button> : null;
}

function renderProvider() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: retryOnlyTransientFailures } } });
  render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <LogoutButton />
      </AuthProvider>
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  assign = vi.fn();
  vi.stubGlobal("location", { assign, pathname: "/contacts" });
  vi.stubGlobal(
    "fetch",
    vi.fn(async () => {
      const session = sessions.length > 1 ? sessions.shift()! : (sessions[0] ?? null);
      if (session === "offline") throw new TypeError("Failed to fetch");
      return session
        ? new Response(JSON.stringify(session), { status: 200, headers: { "Content-Type": "application/json" } })
        : new Response(null, { status: 401 });
    }),
  );
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe("AuthProvider", () => {
  it("Logout_AfterAnotherTabLoggedInAgain_UsesCurrentSid", async () => {
    sessions = [aSession({ logoutUrl: "/bff/logout?sid=old" }), aSession({ logoutUrl: "/bff/logout?sid=new" })];
    renderProvider();

    await userEvent.click(await screen.findByRole("button", { name: "Sair" }));

    await waitFor(() => expect(assign).toHaveBeenCalledWith("/bff/logout?sid=new"));
  });

  it("Logout_WhenBffUnreachable_UsesCachedLogoutUrlWithoutRetrying", async () => {
    sessions = [aSession({ logoutUrl: "/bff/logout?sid=old" }), "offline"];
    renderProvider();

    await userEvent.click(await screen.findByRole("button", { name: "Sair" }));

    await waitFor(() => expect(assign).toHaveBeenCalledWith("/bff/logout?sid=old"), { timeout: 500 });
    expect(vi.mocked(fetch)).toHaveBeenCalledTimes(2);
  });

  it("Logout_WhenSessionAlreadyEnded_GoesHome", async () => {
    sessions = [aSession({ logoutUrl: "/bff/logout?sid=old" }), null];
    renderProvider();

    await userEvent.click(await screen.findByRole("button", { name: "Sair" }));

    await waitFor(() => expect(assign).toHaveBeenCalledWith("/"));
  });
});
