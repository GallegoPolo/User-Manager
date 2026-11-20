import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Route, Routes } from "react-router-dom";
import { aSession, renderWithProviders } from "../../../test/renderWithProviders";
import { ProtectedRoute } from "../../auth/components/ProtectedRoute";
import { LandingPage } from "./LandingPage";

afterEach(cleanup);

describe("LandingPage", () => {
  it("LandingPage_WithoutSession_ShowsEnterThatLogsInToAgenda", async () => {
    const login = vi.fn();
    renderWithProviders(<LandingPage />, { session: null, login });

    await userEvent.click(screen.getByRole("button", { name: "Entrar na minha agenda" }));

    expect(login).toHaveBeenCalledWith("/contacts");
    expect(screen.queryByRole("link", { name: "Abrir minha agenda" })).toBeNull();
  });

  it("LandingPage_WithSession_ShowsOpenAgendaLinkToAgenda", () => {
    renderWithProviders(<LandingPage />, { session: aSession() });

    expect(screen.getByRole("link", { name: "Abrir minha agenda" }).getAttribute("href")).toBe("/contacts");
    expect(screen.queryByRole("button", { name: /Entrar/ })).toBeNull();
  });

  it("LandingPage_WhileSessionLoads_ShowsNoAction", () => {
    renderWithProviders(<LandingPage />, { session: undefined });

    expect(screen.queryByRole("button", { name: /Entrar/ })).toBeNull();
    expect(screen.queryByRole("link", { name: "Abrir minha agenda" })).toBeNull();
  });

  it("LandingPage_AfterFailedLogin_ExplainsAndOffersToEnterAgain", () => {
    renderWithProviders(<LandingPage />, { session: null, route: "/?login=falhou" });

    expect(screen.getByRole("alert").textContent).toBe(
      "Esse login não pôde ser concluído. Se ele já foi feito em outra aba, use aquela aba; senão, entre de novo.",
    );
    expect(screen.getByRole("button", { name: "Entrar na minha agenda" })).toBeDefined();
  });

  it("LandingPage_AfterFailedLoginButSignedInElsewhere_ShowsNoWarning", () => {
    renderWithProviders(<LandingPage />, { session: aSession(), route: "/?login=falhou" });

    expect(screen.queryByRole("alert")).toBeNull();
  });

  it("LandingPage_AuditFeed_SaysItIsAnExample", () => {
    renderWithProviders(<LandingPage />, { session: null });

    const feed = screen.getByRole("region", { name: "Exemplo de auditoria" });

    expect(within(feed).getAllByText("contact.created").length).toBeGreaterThan(0);
  });
});

describe("ProtectedRoute", () => {
  it("ProtectedRoute_WithoutSession_RedirectsToLanding", () => {
    renderWithProviders(
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route element={<ProtectedRoute />}>
          <Route path="/me" element={<p>área logada</p>} />
        </Route>
      </Routes>,
      { session: null, route: "/me" },
    );

    expect(screen.queryByText("área logada")).toBeNull();
    expect(screen.getByRole("button", { name: "Entrar na minha agenda" })).toBeDefined();
  });

  it("OldLoginRoute_WithSession_GoesToLanding", async () => {
    const { default: App } = await import("../../../app/App");
    renderWithProviders(<App />, { session: aSession(), route: "/login" });

    expect(screen.getByRole("link", { name: "Abrir minha agenda" })).toBeDefined();
  });
});
