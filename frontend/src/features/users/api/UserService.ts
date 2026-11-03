import type { RenameUserData, User } from "../types/user";
import { apiClient } from "../../../shared/api/apiClient";

export async function getUsers(): Promise<User[]> {
  return apiClient.get<User[]>("/api/users");
}

export async function renameUser(id: string, data: RenameUserData): Promise<void> {
  await apiClient.put<void>(`/api/users/${id}`, data);
}

export async function deleteUser(id: string): Promise<void> {
  await apiClient.delete<void>(`/api/users/${id}`);
}

export async function getMe(): Promise<User> {
  return apiClient.get<User>("/api/me");
}

export async function renameMe(data: RenameUserData): Promise<void> {
  await apiClient.put<void>("/api/me", data);
}
