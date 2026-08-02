import { useAuditLogs } from "../hooks/useAuditLogs";

export function AuditPage() {
  const { data, isLoading, isError, refetch, isFetching } = useAuditLogs();

  return (
    <main className="page">
      <div className="page-header">
        <h1>Auditoria</h1>
        <button onClick={() => refetch()} disabled={isFetching}>
          {isFetching ? "Atualizando..." : "Atualizar"}
        </button>
      </div>

      {isLoading && <p>Carregando logs...</p>}
      {isError && <p>Falha ao carregar logs de auditoria.</p>}

      {data && data.items.length === 0 && <p>Nenhum log encontrado.</p>}

      {data && data.items.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Tipo</th>
              <th>Data</th>
              <th>Aggregate ID</th>
            </tr>
          </thead>
          <tbody>
            {data.items.map((log) => (
              <tr key={log.id}>
                <td>{log.eventType}</td>
                <td>{new Date(log.timestamp).toLocaleString("pt-BR")}</td>
                <td>{log.aggregateId}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  );
}
