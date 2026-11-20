export interface Contact {
  id: string;
  name: string;
  email: string | null;
  phone: string | null;
  createdAt: string;
}

export interface ContactData {
  name: string;
  email: string | null;
  phone: string | null;
}
