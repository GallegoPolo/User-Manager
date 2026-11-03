import { ApiError, apiClient } from "../../../shared/api/apiClient";
import type { BffUser } from "../types/auth";

export async function getSessionUser(): Promise<BffUser | null> {
  try {
    return await apiClient.get<BffUser>("/bff/user");
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) return null;
    throw error;
  }
}

export function redirectToLogin(returnUrl: string) {
  window.location.assign(`/bff/login?returnUrl=${encodeURIComponent(returnUrl)}`);
}
