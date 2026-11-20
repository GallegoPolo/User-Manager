import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { aSession, renderWithProviders } from "../../../test/renderWithProviders";
import type { User } from "../types/user";
import { UsersPage } from "./UsersPage";

let users: User[];

function json(status: number, body?: unknown) {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

beforeEach(() => {
  users = [{ id: "u1", name: "Otto Lima", email: "otto@user-manager.local", createdAt: "2026-11-20T10:00:00Z" }];
  vi.stubGlobal(
    "fetch",
    vi.fn(async (url: string, init?: RequestInit) => {
      if (init?.method === "DELETE") {
        users = users.filter((user) => !url.endsWith(user.id));
        return json(204);
      }
      return json(200, users);
    }),
  );
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe("UsersPage", () => {
  it("UsersPage_OperatorDeletes_AfterInAppConfirmation", async () => {
    renderWithProviders(<UsersPage />, { session: aSession({ roles: ["operator"] }), route: "/users" });
    const table = await screen.findByRole("table");

    await userEvent.click(within(table).getByRole("button", { name: "Excluir Otto Lima" }));
    const dialog = screen.getByRole("dialog", { name: "Excluir dados do usuário" });
    await userEvent.click(within(dialog).getByRole("button", { name: "Excluir" }));

    await waitFor(() => expect(screen.queryByText("Otto Lima")).toBeNull());
    expect(screen.queryByRole("dialog")).toBeNull();
  });
});
