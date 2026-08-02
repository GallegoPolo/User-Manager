export interface AuditLog {
  id: string;
  eventId: string;
  aggregateId: string;
  aggregateType: string;
  eventType: string;
  performedBy: string;
  timestamp: string;
  payload: Record<string, unknown>;
}

export interface PagedAuditLogs {
  items: AuditLog[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
