import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, apiClient, fieldErrorsOf } from "./apiClient";

function respondWith(status: number, body: string, contentType = "application/json") {
  vi.stubGlobal(
    "fetch",
    vi.fn(async () => new Response(body, { status, headers: { "Content-Type": contentType } })),
  );
}

async function errorOf(promise: Promise<unknown>): Promise<ApiError> {
  try {
    await promise;
  } catch (error) {
    if (error instanceof ApiError) return error;
  }
  throw new Error("esperava ApiError");
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("apiClient", () => {
  it("ApiError_WithResultArray_HasFieldErrors", async () => {
    respondWith(409, JSON.stringify([{ property: "Email", message: "Já existe um contato com este e-mail na agenda." }]));

    const error = await errorOf(apiClient.post("/api/me/contacts", {}));

    expect(error.status).toBe(409);
    expect(error.errors).toEqual([{ property: "Email", message: "Já existe um contato com este e-mail na agenda." }]);
  });

  it("ApiError_WithErrorResponse_HasFieldErrors", async () => {
    respondWith(409, JSON.stringify({ title: "Conflict", status: 409, errors: [{ property: "Database", message: "A record with this value already exists." }] }));

    const error = await errorOf(apiClient.post("/api/me/contacts", {}));

    expect(error.errors).toEqual([{ property: "Database", message: "A record with this value already exists." }]);
  });

  it("ApiError_WithValidationProblemDictionary_HasFieldErrors", async () => {
    respondWith(400, JSON.stringify({ title: "One or more validation errors occurred.", errors: { "$.name": ["The JSON value could not be converted."] } }));

    const error = await errorOf(apiClient.post("/api/me/contacts", {}));

    expect(error.errors).toEqual([{ property: "name", message: "The JSON value could not be converted." }]);
  });

  it("ApiError_WithNonJsonBody_HasNoFieldErrors", async () => {
    respondWith(502, "<html>Bad Gateway</html>", "text/html");

    const error = await errorOf(apiClient.get("/api/me/contacts"));

    expect(error.status).toBe(502);
    expect(error.errors).toEqual([]);
  });
});

describe("fieldErrorsOf", () => {
  it("FieldErrorsOf_MapsKnownFieldsAndSendsTheRestToGeneral", () => {
    const error = new ApiError(400, "/api/me/contacts", [
      { property: "Name", message: "O nome é obrigatório." },
      { property: "Email", message: "E-mail inválido." },
      { property: "ContactId", message: "Contato não encontrado." },
    ]);

    expect(fieldErrorsOf(error, ["name", "email", "phone"])).toEqual({
      name: "O nome é obrigatório.",
      email: "E-mail inválido.",
      general: "Contato não encontrado.",
    });
  });

  it("FieldErrorsOf_WithoutFieldErrors_ExplainsGenerally", () => {
    expect(fieldErrorsOf(new ApiError(500, "/api/me/contacts", []), ["name"])).toEqual({
      general: "Não foi possível salvar. Tente de novo.",
    });
    expect(fieldErrorsOf(null, ["name"])).toEqual({});
  });
});
