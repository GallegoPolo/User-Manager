import { useState, type FormEvent } from "react";
import type { User, UserFormData } from "../types/user";

interface Props {
  open: boolean;
  user: User | null; // null = criar, preenchido = editar
  isSaving: boolean;
  onClose: () => void;
  onSubmit: (data: UserFormData) => void;
}

export function UserFormModal({
  open,
  user,
  isSaving,
  onClose,
  onSubmit,
}: Props) {
  // valor inicial já nasce correto — sem useEffect
  const [name, setName] = useState(user?.name ?? "");
  const [email, setEmail] = useState(user?.email ?? "");

  if (!open) return null;

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    onSubmit({ name, email });
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <h2>{user ? "Editar usuário" : "Novo usuário"}</h2>
        <form onSubmit={handleSubmit}>
          <label htmlFor="name">Nome</label>
          <input
            id="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />
          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <div className="modal-actions">
            <button type="button" onClick={onClose} disabled={isSaving}>
              Cancelar
            </button>
            <button type="submit" disabled={isSaving}>
              {isSaving ? "Salvando..." : "Salvar"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
