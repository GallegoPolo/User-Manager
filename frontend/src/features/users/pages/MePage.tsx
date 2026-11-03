import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { getMe, renameMe } from "../api/UserService";
import { UserFormModal } from "../components/UserFormModal";

export function MePage() {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);

  const { data: me, isLoading, error } = useQuery({
    queryKey: ["me"],
    queryFn: getMe,
  });

  const renameMutation = useMutation({
    mutationFn: renameMe,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["me"] });
      setEditing(false);
    },
  });

  return (
    <main className="page">
      <div className="page-header">
        <h1>Minha conta</h1>
        {me && (
          <button type="button" onClick={() => setEditing(true)}>
            Editar nome
          </button>
        )}
      </div>

      {isLoading && <p>Carregando...</p>}
      {error && <p className="page-error">Erro ao carregar.</p>}

      {me && (
        <dl>
          <dt>Nome</dt>
          <dd>{me.name}</dd>
          <dt>E-mail</dt>
          <dd>{me.email}</dd>
          <dt>Desde</dt>
          <dd>{new Date(me.createdAt).toLocaleDateString("pt-BR")}</dd>
        </dl>
      )}

      {editing && me && (
        <UserFormModal
          user={me}
          isSaving={renameMutation.isPending}
          onClose={() => setEditing(false)}
          onSubmit={(data) => renameMutation.mutate(data)}
        />
      )}
    </main>
  );
}
