import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { LoadError } from "../../../shared/components/LoadError";
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
          <button type="button" className="button-primary" onClick={() => setEditing(true)}>
            Editar nome
          </button>
        )}
      </div>

      {isLoading && <p className="loading">Carregando...</p>}
      {error && <LoadError error={error} what="sua conta" />}

      {me && (
        <dl className="details">
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
