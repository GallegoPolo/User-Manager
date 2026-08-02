import type { PagedAuditLogs } from "../types/auditLog";
import { apiClient } from "../../../shared/api/apiClient";

export async function getAuditLogs(): Promise<PagedAuditLogs> {
  return apiClient.get<PagedAuditLogs>("/api/Audit/logs");
}
