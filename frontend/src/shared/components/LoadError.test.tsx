import { afterEach, describe, expect, it } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { ApiError } from "../api/apiClient";
import { LoadError } from "./LoadError";

afterEach(cleanup);

describe("LoadError", () => {
  it("LoadError_Forbidden_SaysNoAccess", () => {
    render(<LoadError error={new ApiError(403, "/api/users")} what="os usuários" />);

    expect(screen.getByRole("alert").textContent).toBe("Você não tem acesso a esta página.");
  });

  it("LoadError_OtherFailure_SaysCouldNotLoad", () => {
    render(<LoadError error={new ApiError(500, "/api/users")} what="os usuários" />);

    expect(screen.getByRole("alert").textContent).toBe("Não foi possível carregar os usuários. Tente de novo.");
  });

  it("LoadError_NetworkFailure_SaysCouldNotLoad", () => {
    render(<LoadError error={new TypeError("Failed to fetch")} what="a auditoria" />);

    expect(screen.getByRole("alert").textContent).toBe("Não foi possível carregar a auditoria. Tente de novo.");
  });
});
