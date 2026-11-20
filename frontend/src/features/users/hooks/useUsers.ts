import { useQuery } from "@tanstack/react-query";
import { getUsers } from "../api/UserService";

export const usersKey = ["users"];

export function useUsers() {
  return useQuery({ queryKey: usersKey, queryFn: getUsers });
}
