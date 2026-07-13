import type { User, UserFormData } from "../types/user";
import { apiClient } from "../../../shared/api/apiClient"; // ajuste o path conforme a pasta real

export async function getUsers(): Promise<User[]> {
  return apiClient.get<User[]>("/api/users");
}

export async function createUser(data: UserFormData): Promise<void> {
  await apiClient.post<void>("/api/users", data);
}

export async function updateUser(
  id: string,
  data: UserFormData,
): Promise<void> {
  await apiClient.put<void>(`/api/users/${id}`, data);
}

export async function deleteUser(id: string): Promise<void> {
  await apiClient.delete<void>(`/api/users/${id}`);
}
