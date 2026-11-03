export type Role = "viewer" | "operator";

export interface BffUser {
  sub: string;
  name: string | null;
  email: string | null;
  roles: string[];
  logoutUrl: string;
}
