import type { LoginRequest, LoginResponse } from "../types/auth";

export async function login(credentials: LoginRequest): Promise<LoginResponse> {
  const response = await fetch("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(credentials),
  });

  if (!response.ok) {
    throw new Error("Credenciais inválidas");
  }

  return response.json();
}
