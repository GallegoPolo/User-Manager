import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ConfirmDialog } from "../../../shared/components/ConfirmDialog";
import { LoadError } from "../../../shared/components/LoadError";
import { useAuth } from "../../auth/context/useAuth";
import { deleteUser, getUsers, renameUser } from "../api/UserService";
import { UserFormModal } from "../components/UserFormModal";
import type { RenameUserData, User } from "../types/user";

export function UsersPage() {
  const queryClient = useQueryClient();
  const { hasRole } = useAuth();
  const canManage = hasRole("operator");
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [deletingUser, setDeletingUser] = useState<User | null>(null);

  const {
    data: users,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["users"],
    queryFn: getUsers,
  });

  const renameMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: RenameUserData }) => renameUser(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      setEditingUser(null);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteUser,
    onSettled: () => {
      setDeletingUser(null);
      queryClient.invalidateQueries({ queryKey: ["users"] });
    },
  });

  return (
    <main className="page">
      <div className="page-header">
        <h1>Usuários</h1>
      </div>

      {isLoading && <p className="loading">Carregando...</p>}
      {error && <LoadError error={error} what="os usuários" />}
      {users?.length === 0 && <p>Ninguém entrou no sistema ainda.</p>}

      {users && users.length > 0 && (
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>Nome</th>
                <th>Email</th>
                <th>Criado em</th>
                {canManage && <th>Ações</th>}
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id}>
                  <td>{user.name}</td>
                  <td>{user.email}</td>
                  <td>{new Date(user.createdAt).toLocaleDateString("pt-BR")}</td>
                  {canManage && (
                    <td className="table-actions">
                      <button type="button" onClick={() => setEditingUser(user)}>
                        Editar nome
                      </button>
                      <button
                        type="button"
                        className="button-danger"
                        aria-label={`Excluir ${user.name}`}
                        onClick={() => setDeletingUser(user)}
                      >
                        Excluir
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editingUser && (
        <UserFormModal
          key={editingUser.id}
          user={editingUser}
          isSaving={renameMutation.isPending}
          onClose={() => setEditingUser(null)}
          onSubmit={(data) => renameMutation.mutate({ id: editingUser.id, data })}
        />
      )}

      {deletingUser && (
        <ConfirmDialog
          title="Excluir dados do usuário"
          confirmLabel="Excluir"
          pendingLabel="Excluindo..."
          isPending={deleteMutation.isPending}
          onConfirm={() => deleteMutation.mutate(deletingUser.id)}
          onCancel={() => setDeletingUser(null)}
        >
          <p>
            A ficha e a agenda de <strong>{deletingUser.name}</strong> saem do User Manager. A conta de login
            continua no Keycloak.
          </p>
        </ConfirmDialog>
      )}
    </main>
  );
}
