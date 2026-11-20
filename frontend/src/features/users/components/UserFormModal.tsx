import { useState, type FormEvent } from "react";
import { Modal } from "../../../shared/components/Modal";
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
    <Modal title="Nome de exibição" onClose={onClose} dismissible={!isSaving}>
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
    </Modal>
  );
}
