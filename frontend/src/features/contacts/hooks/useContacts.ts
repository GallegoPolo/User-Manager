import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createContact, deleteContact, getContacts, updateContact } from "../api/ContactService";
import type { ContactData } from "../types/contact";

const contactsKey = ["contacts"];

export interface SaveContactInput {
  id?: string;
  data: ContactData;
}

export function useContacts() {
  return useQuery({ queryKey: contactsKey, queryFn: getContacts });
}

export function useRefreshContacts() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: contactsKey });
}

export function useSaveContact() {
  return useMutation({
    mutationFn: ({ id, data }: SaveContactInput) => (id ? updateContact(id, data) : createContact(data)),
  });
}

export function useDeleteContact() {
  return useMutation({ mutationFn: deleteContact });
}
