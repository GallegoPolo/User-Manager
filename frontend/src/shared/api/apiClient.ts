export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, url: string) {
    super(`Erro ${status} em ${url}`);
    this.status = status;
  }
}

async function request<T>(url: string, options: RequestInit = {}): Promise<T> {
  const { headers, ...rest } = options;

  const response = await fetch(url, {
    ...rest,
    credentials: "same-origin",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF": "1",
      ...headers,
    },
  });

  if (!response.ok) {
    throw new ApiError(response.status, url);
  }

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
