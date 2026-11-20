import { afterEach, describe, expect, it, vi } from "vitest";
import { redirectToLogin } from "./bffSession";

afterEach(() => vi.unstubAllGlobals());

describe("bffSession", () => {
  it("RedirectToLogin_SendsEncodedReturnUrlToBff", () => {
    const assign = vi.fn();
    vi.stubGlobal("location", { assign });

    redirectToLogin("/audit?page=2&x=1");

    expect(assign).toHaveBeenCalledWith("/bff/login?returnUrl=%2Faudit%3Fpage%3D2%26x%3D1");
  });
});
