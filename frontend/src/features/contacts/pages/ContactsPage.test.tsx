import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { aSession, renderWithProviders } from "../../../test/renderWithProviders";
import type { Contact } from "../types/contact";
import { ContactsPage } from "./ContactsPage";

interface FakeApi {
  contacts: Contact[];
  nextFailure?: { status: number; body: unknown };
  deletedElsewhere: Set<string>;
}

let api: FakeApi;

function json(status: number, body?: unknown) {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function handle(url: string, init?: RequestInit): Response {
  const method = init?.method ?? "GET";
  const id = url.split("/api/me/contacts/")[1];

  if (api.nextFailure && method !== "GET") {
    const failure = api.nextFailure;
    api.nextFailure = undefined;
    return json(failure.status, failure.body);
  }

  if (id && api.deletedElsewhere.has(id)) {
    api.contacts = api.contacts.filter((contact) => contact.id !== id);
    return json(404, [{ property: "ContactId", message: "Contato não encontrado." }]);
  }

  if (method === "GET") return json(200, api.contacts);

  if (method === "POST") {
    const data = JSON.parse(String(init?.body));
    const contact = { id: `n${api.contacts.length + 1}`, createdAt: "2026-11-20T10:00:00Z", ...data };
    api.contacts = [...api.contacts, contact];
    return json(201, { id: contact.id });
  }

  if (method === "DELETE") {
    api.contacts = api.contacts.filter((contact) => contact.id !== id);
    return json(204);
  }

  return json(204);
}

beforeEach(() => {
  api = {
    contacts: [
      { id: "c1", name: "Bruno Lima", email: "bruno@exemplo.com", phone: null, createdAt: "2026-11-20T10:00:00Z" },
      { id: "c2", name: "Carla Mendes", email: null, phone: "11998765432", createdAt: "2026-11-20T10:00:00Z" },
    ],
    deletedElsewhere: new Set(),
  };
  vi.stubGlobal("fetch", vi.fn(async (url: string, init?: RequestInit) => handle(url, init)));
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

async function confirmDeletion() {
  await userEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Excluir" }));
}

function renderPage() {
  return renderWithProviders(<ContactsPage />, { session: aSession(), route: "/contacts" });
}

describe("ContactsPage", () => {
  it("ContactsPage_ListsContacts", async () => {
    renderPage();

    const table = await screen.findByRole("table");

    expect(within(table).getByText("Bruno Lima")).toBeDefined();
    expect(within(table).getByText("11998765432")).toBeDefined();
    expect(screen.getByText("2 contatos")).toBeDefined();
  });

  it("ContactsPage_Empty_ShowsEmptyState", async () => {
    api.contacts = [];
    renderPage();

    expect(await screen.findByText("Nenhum contato ainda")).toBeDefined();
    expect(screen.queryByRole("table")).toBeNull();
  });

  it("ContactsPage_Create_ClosesModalAndAnnounces", async () => {
    renderPage();
    await screen.findByRole("table");

    await userEvent.click(screen.getByRole("button", { name: "Novo contato" }));
    await userEvent.type(screen.getByLabelText("Nome"), "Diego Rocha");
    await userEvent.type(screen.getByLabelText("E-mail"), "diego@exemplo.com");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    expect((await screen.findByRole("status")).textContent).toBe("Contato criado.");
    expect(screen.queryByRole("dialog")).toBeNull();
    expect(await screen.findByText("Diego Rocha")).toBeDefined();
  });

  it("ContactsPage_ReopenAfterError_StartsClean", async () => {
    renderPage();
    await screen.findByRole("table");
    api.nextFailure = { status: 409, body: [{ property: "Email", message: "Já existe um contato com este e-mail na agenda." }] };

    await userEvent.click(screen.getByRole("button", { name: "Novo contato" }));
    await userEvent.type(screen.getByLabelText("Nome"), "Outro Bruno");
    await userEvent.type(screen.getByLabelText("E-mail"), "bruno@exemplo.com");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));
    expect(await screen.findByText("Já existe um contato com este e-mail na agenda.")).toBeDefined();

    await userEvent.click(screen.getByRole("button", { name: "Cancelar" }));
    await userEvent.click(screen.getByRole("button", { name: "Novo contato" }));

    expect((screen.getByLabelText("Nome") as HTMLInputElement).value).toBe("");
    expect(screen.queryByText("Já existe um contato com este e-mail na agenda.")).toBeNull();
  });

  it("ContactsPage_DeleteOfMissingContact_WarnsAndRefreshes", async () => {
    renderPage();
    const table = await screen.findByRole("table");
    api.deletedElsewhere.add("c1");

    await userEvent.click(within(table).getByRole("button", { name: "Excluir Bruno Lima" }));
    await confirmDeletion();

    expect((await screen.findByRole("status")).textContent).toBe("Esse contato não existe mais. A lista foi atualizada.");
    await waitFor(() => expect(screen.queryByText("Bruno Lima")).toBeNull());
  });

  it("ContactsPage_Delete_AsksInAppAndRemoves", async () => {
    renderPage();
    const table = await screen.findByRole("table");

    await userEvent.click(within(table).getByRole("button", { name: "Excluir Bruno Lima" }));
    const dialog = screen.getByRole("dialog", { name: "Excluir contato" });
    expect(dialog.textContent).toContain("Bruno Lima");
    await confirmDeletion();

    expect((await screen.findByRole("status")).textContent).toBe("Contato excluído.");
    expect(screen.queryByRole("dialog")).toBeNull();
    await waitFor(() => expect(screen.queryByText("Bruno Lima")).toBeNull());
  });

  it("ContactsPage_DeleteCancelled_KeepsContact", async () => {
    renderPage();
    const table = await screen.findByRole("table");

    await userEvent.click(within(table).getByRole("button", { name: "Excluir Bruno Lima" }));
    await userEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Cancelar" }));

    expect(screen.queryByRole("dialog")).toBeNull();

    expect(within(table).getByText("Bruno Lima")).toBeDefined();
    expect(vi.mocked(fetch).mock.calls.some(([, init]) => init?.method === "DELETE")).toBe(false);
  });
});
