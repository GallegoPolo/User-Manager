import { getStoredToken } from "../../features/auth/api/tokenStore";

interface RequestOptions extends RequestInit {
  skipAuth?: boolean;
}

async function request<T>(
  url: string,
  options: RequestOptions = {},
): Promise<T> {
  const { skipAuth, headers, ...rest } = options;
  const token = getStoredToken();

  const finalHeaders: HeadersInit = {
    "Content-Type": "application/json",
    ...headers,
    ...(token && !skipAuth ? { Authorization: `Bearer ${token}` } : {}),
  };

  const response = await fetch(url, { ...rest, headers: finalHeaders });

  if (!response.ok) {
    throw new Error(`Erro ${response.status} em ${url}`);
  }

  // 204 No Content (comum em DELETE) não tem body pra parsear
  if (response.status === 204) return undefined as T;

  return response.json();
}

export const apiClient = {
  get: <T>(url: string) => request<T>(url),
  post: <T>(url: string, body: unknown) =>
    request<T>(url, { method: "POST", body: JSON.stringify(body) }),
  put: <T>(url: string, body: unknown) =>
    request<T>(url, { method: "PUT", body: JSON.stringify(body) }),
  delete: <T>(url: string) => request<T>(url, { method: "DELETE" }),
};
