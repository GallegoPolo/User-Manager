import { useState, type FormEvent } from "react";
import type { RenameUserData, User } from "../types/user";

interface Props {
  user: User;
  isSaving: boolean;
  onClose: () => void;
  onSubmit: (data: RenameUserData) => void;
}

export function UserFormModal({ user, isSaving, onClose, onSubmit }: Props) {
  const [name, setName] = useState(user.name);

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    onSubmit({ name });
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-card" role="dialog" aria-modal="true" aria-labelledby="user-form-title" onClick={(e) => e.stopPropagation()}>
        <h2 id="user-form-title">Nome de exibição</h2>
        <form onSubmit={handleSubmit}>
          <div className="field">
            <label htmlFor="name">Nome</label>
            <input id="name" value={name} onChange={(e) => setName(e.target.value)} required />
            <span className="field-hint">E-mail: {user.email} (gerenciado pelo Keycloak)</span>
          </div>
          <div className="modal-actions">
            <button type="button" onClick={onClose} disabled={isSaving}>
              Cancelar
            </button>
            <button type="submit" className="button-primary" disabled={isSaving}>
              {isSaving ? "Salvando..." : "Salvar"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
