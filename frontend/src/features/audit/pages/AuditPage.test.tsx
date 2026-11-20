import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, screen, within } from "@testing-library/react";
import { aSession, renderWithProviders } from "../../../test/renderWithProviders";
import type { AuditLog } from "../types/auditLog";
import { AuditPage } from "./AuditPage";

const anaId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
const removedId = "9b2e1c7d-0000-4000-8000-000000000001";

function aLog(id: string, performedBy: string): AuditLog {
  return {
    id,
    eventId: id,
    aggregateId: "c1",
    aggregateType: "Contact",
    eventType: "ContactCreated",
    performedBy,
    timestamp: "2026-11-20T10:00:00Z",
    payload: {},
  };
}

function json(body: unknown) {
  return new Response(JSON.stringify(body), { status: 200, headers: { "Content-Type": "application/json" } });
}

beforeEach(() => {
  const logs = [aLog("l1", anaId), aLog("l2", "system"), aLog("l3", removedId)];
  vi.stubGlobal(
    "fetch",
    vi.fn(async (url: string) =>
      url.startsWith("/api/users")
        ? json([{ id: anaId, name: "Ana Souza", email: "ana@user-manager.local", createdAt: "2026-11-20T10:00:00Z" }])
        : json({ items: logs, pageNumber: 1, pageSize: 20, totalCount: 3, totalPages: 1, hasPreviousPage: false, hasNextPage: false }),
    ),
  );
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe("AuditPage", () => {
  it("AuditPage_PerformedBy_ShowsNameSystemOrShortId", async () => {
    renderWithProviders(<AuditPage />, { session: aSession({ roles: ["viewer"] }), route: "/audit" });

    const rows = await screen.findAllByRole("row");
    const performers = rows.slice(1).map((row) => within(row).getAllByRole("cell").at(-1)!);

    expect(await within(performers[0]).findByText("Ana Souza")).toBeDefined();
    expect(performers[1].textContent).toBe("Sistema");
    expect(performers[2].textContent).toBe("9b2e1c7d");
    expect(within(performers[2]).getByTitle(removedId)).toBeDefined();
  });
});
