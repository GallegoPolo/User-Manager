import { describe, expect, it } from "vitest";
import { ApiError } from "./apiClient";
import { retryOnlyTransientFailures } from "./retryPolicy";

describe("retryOnlyTransientFailures", () => {
  it.each([401, 403, 404, 409])("RetryPolicy_ClientError%s_DoesNotRetry", (status) => {
    expect(retryOnlyTransientFailures(0, new ApiError(status, "/api/x"))).toBe(false);
  });

  it("RetryPolicy_ServerError_RetriesUpToThreeTimes", () => {
    const error = new ApiError(503, "/api/x");

    expect(retryOnlyTransientFailures(2, error)).toBe(true);
    expect(retryOnlyTransientFailures(3, error)).toBe(false);
  });

  it("RetryPolicy_NetworkFailure_Retries", () => {
    expect(retryOnlyTransientFailures(0, new TypeError("Failed to fetch"))).toBe(true);
  });
});
