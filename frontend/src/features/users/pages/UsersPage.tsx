import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import {
  createUser,
  deleteUser,
  getUsers,
  updateUser,
} from "../api/UserService";
import { UserFormModal } from "../components/UserFormModal";
import type { User, UserFormData } from "../types/user";

export function UsersPage() {
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);

  const {
    data: users,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["users"],
    queryFn: getUsers,
  });

  const saveMutation = useMutation({
    mutationFn: (data: UserFormData) =>
      editingUser ? updateUser(editingUser.id, data) : createUser(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      closeModal();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteUser,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["users"] }),
  });

  function openCreate() {
    setEditingUser(null);
    setModalOpen(true);
  }

  function openEdit(user: User) {
    setEditingUser(user);
    setModalOpen(true);
  }

  function closeModal() {
    setModalOpen(false);
    setEditingUser(null);
  }

  function handleDelete(user: User) {
    if (window.confirm(`Excluir ${user.name}?`)) {
      deleteMutation.mutate(user.id);
    }
  }

  return (
    <main className="page">
      <div className="page-header">
        <h1>Usuários</h1>
        <button type="button" onClick={openCreate}>
          Novo usuário
        </button>
      </div>

      {isLoading && <p>Carregando...</p>}
      {error && <p className="page-error">Erro ao carregar.</p>}
      {users?.length === 0 && <p>Nenhum usuário cadastrado.</p>}

      {users && users.length > 0 && (
        <table className="data-table">
          <thead>
            <tr>
              <th>Nome</th>
              <th>Email</th>
              <th>Criado em</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id}>
                <td>{user.name}</td>
                <td>{user.email}</td>
                <td>{new Date(user.createdAt).toLocaleDateString("pt-BR")}</td>
                <td className="table-actions">
                  <button type="button" onClick={() => openEdit(user)}>
                    Editar
                  </button>
                  <button type="button" onClick={() => handleDelete(user)}>
                    Excluir
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {modalOpen && (
        <UserFormModal
          key={editingUser?.id ?? "create"}
          open={modalOpen}
          user={editingUser}
          isSaving={saveMutation.isPending}
          onClose={closeModal}
          onSubmit={(data) => saveMutation.mutate(data)}
        />
      )}
    </main>
  );
}
