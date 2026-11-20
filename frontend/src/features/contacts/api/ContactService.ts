import { apiClient } from "../../../shared/api/apiClient";
import type { Contact, ContactData } from "../types/contact";

const contactsUrl = "/api/me/contacts";

export function getContacts(): Promise<Contact[]> {
  return apiClient.get<Contact[]>(contactsUrl);
}

export async function createContact(data: ContactData): Promise<void> {
  await apiClient.post<{ id: string }>(contactsUrl, data);
}

export async function updateContact(id: string, data: ContactData): Promise<void> {
  await apiClient.put<void>(`${contactsUrl}/${id}`, data);
}

export async function deleteContact(id: string): Promise<void> {
  await apiClient.delete<void>(`${contactsUrl}/${id}`);
}
