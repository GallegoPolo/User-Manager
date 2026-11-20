export interface FieldError {
  property: string;
  message: string;
}

export class ApiError extends Error {
  readonly status: number;
  readonly errors: FieldError[];

  constructor(status: number, url: string, errors: FieldError[] = []) {
    super(`Erro ${status} em ${url}`);
    this.status = status;
    this.errors = errors;
  }
}

const generalFailure = "Não foi possível salvar. Tente de novo.";

function isFieldError(value: unknown): value is FieldError {
  return (
    typeof value === "object" &&
    value !== null &&
    typeof (value as FieldError).property === "string" &&
    typeof (value as FieldError).message === "string"
  );
}

function fromDictionary(errors: object): FieldError[] {
  return Object.entries(errors).flatMap(([key, messages]) =>
    Array.isArray(messages)
      ? messages
          .filter((message): message is string => typeof message === "string")
          .map((message) => ({ property: key.replace(/^\$\./, ""), message }))
      : [],
  );
}

async function readFieldErrors(response: Response): Promise<FieldError[]> {
  try {
    const body: unknown = await response.json();
    const errors = Array.isArray(body) ? body : (body as { errors?: unknown } | null)?.errors;

    if (Array.isArray(errors)) return errors.filter(isFieldError);
    if (typeof errors === "object" && errors !== null) return fromDictionary(errors);
    return [];
  } catch {
    return [];
  }
}

export function fieldErrorsOf(error: unknown, knownFields: string[]): Record<string, string> {
  if (!error) return {};
  if (!(error instanceof ApiError) || error.errors.length === 0) return { general: generalFailure };

  const result: Record<string, string> = {};

  for (const { property, message } of error.errors) {
    const field = property.toLowerCase();
    const key = knownFields.includes(field) ? field : "general";
    result[key] ??= message;
  }

  return result;
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
    throw new ApiError(response.status, url, await readFieldErrors(response));
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
