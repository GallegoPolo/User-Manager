import { ApiError } from "./apiClient";

const maxRetries = 3;

export function retryOnlyTransientFailures(failureCount: number, error: unknown): boolean {
  if (error instanceof ApiError && error.status < 500) return false;
  return failureCount < maxRetries;
}
