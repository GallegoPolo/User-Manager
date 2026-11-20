import { useMemo } from "react";
import { LoadError } from "../../../shared/components/LoadError";
import { useUsers } from "../../users/hooks/useUsers";
import { Performer } from "../components/Performer";
import { useAuditLogs } from "../hooks/useAuditLogs";

export function AuditPage() {
  const { data, isLoading, error, refetch, isFetching } = useAuditLogs();
  const { data: users } = useUsers();
  const namesById = useMemo(() => new Map(users?.map((user) => [user.id, user.name])), [users]);

  return (
    <main className="page">
      <div className="page-header">
        <h1>Auditoria</h1>
        <button onClick={() => refetch()} disabled={isFetching}>
          {isFetching ? "Atualizando..." : "Atualizar"}
        </button>
      </div>

      {isLoading && <p className="loading">Carregando logs...</p>}
      {error && <LoadError error={error} what="a auditoria" />}

      {data && data.items.length === 0 && <p>Nenhum log encontrado.</p>}

      {data && data.items.length > 0 && (
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>Tipo</th>
                <th>Data</th>
                <th>Aggregate ID</th>
                <th>Quem fez</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((log) => (
                <tr key={log.id}>
                  <td>{log.eventType}</td>
                  <td>{new Date(log.timestamp).toLocaleString("pt-BR")}</td>
                  <td className="mono">{log.aggregateId}</td>
                  <td>
                    <Performer performedBy={log.performedBy} namesById={namesById} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}
