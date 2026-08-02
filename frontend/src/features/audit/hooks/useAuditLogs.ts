import { useQuery } from "@tanstack/react-query";
import { getAuditLogs } from "../api/AuditService";

export function useAuditLogs() {
  return useQuery({
    queryKey: ["audit-logs"],
    queryFn: getAuditLogs,
    refetchOnWindowFocus: true,
  });
}
