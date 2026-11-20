import { afterEach, describe, expect, it } from "vitest";
import { cleanup, screen } from "@testing-library/react";
import { aSession, renderWithProviders } from "../../test/renderWithProviders";
import { Sidebar } from "./Sidebar";

afterEach(cleanup);

describe("Sidebar", () => {
  it("Sidebar_WithoutRole_ShowsOnlyMyAccount", () => {
    renderWithProviders(<Sidebar />, { session: aSession() });

    expect(screen.getByRole("link", { name: "Minha conta" })).toBeDefined();
    expect(screen.queryByRole("link", { name: "Usuários" })).toBeNull();
    expect(screen.queryByRole("link", { name: "Auditoria" })).toBeNull();
  });

  it("Sidebar_WithViewer_ShowsOperations", () => {
    renderWithProviders(<Sidebar />, { session: aSession({ roles: ["viewer"] }) });

    expect(screen.getByRole("link", { name: "Usuários" })).toBeDefined();
    expect(screen.getByRole("link", { name: "Auditoria" })).toBeDefined();
  });
});
