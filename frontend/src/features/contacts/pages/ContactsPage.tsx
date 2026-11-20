import { useState } from "react";
import { ApiError } from "../../../shared/api/apiClient";
import { ConfirmDialog } from "../../../shared/components/ConfirmDialog";
import { LoadError } from "../../../shared/components/LoadError";
import { ContactFormModal } from "../components/ContactFormModal";
import {
  useContacts,
  useDeleteContact,
  useRefreshContacts,
  useSaveContact,
  type SaveContactInput,
} from "../hooks/useContacts";
import type { Contact } from "../types/contact";

type Editing = { contact?: Contact } | null;
type Notice = { kind: "ok" | "warn"; text: string } | null;

const missingContactNotice: Notice = { kind: "warn", text: "Esse contato não existe mais. A lista foi atualizada." };

function isNotFound(error: unknown) {
  return error instanceof ApiError && error.status === 404;
}

function countLabel(total: number) {
  return total === 1 ? "1 contato" : `${total} contatos`;
}

export function ContactsPage() {
  const { data: contacts, isLoading, error: loadError } = useContacts();
  const refreshContacts = useRefreshContacts();
  const saveContact = useSaveContact();
  const removeContact = useDeleteContact();
  const [editing, setEditing] = useState<Editing>(null);
  const [notice, setNotice] = useState<Notice>(null);
  const [deleting, setDeleting] = useState<Contact | null>(null);

  function openForm(contact?: Contact) {
    saveContact.reset();
    setNotice(null);
    setEditing({ contact });
  }

  function handleSave(input: SaveContactInput) {
    saveContact.mutate(input, {
      onSuccess: () => {
        setEditing(null);
        setNotice({ kind: "ok", text: input.id ? "Contato atualizado." : "Contato criado." });
        refreshContacts();
      },
      onError: (error) => {
        if (!isNotFound(error)) return;
        setEditing(null);
        setNotice(missingContactNotice);
        refreshContacts();
      },
    });
  }

  function askToDelete(contact: Contact) {
    setNotice(null);
    setDeleting(contact);
  }

  function handleDelete(contact: Contact) {
    removeContact.mutate(contact.id, {
      onSuccess: () => setNotice({ kind: "ok", text: "Contato excluído." }),
      onError: (error) =>
        setNotice(isNotFound(error) ? missingContactNotice : { kind: "warn", text: "Não foi possível excluir. Tente de novo." }),
      onSettled: () => {
        setDeleting(null);
        refreshContacts();
      },
    });
  }

  return (
    <main className="page">
      <div className="page-header">
        <div>
          <h1>Agenda</h1>
          {contacts && <p className="page-subtitle">{countLabel(contacts.length)}</p>}
        </div>
        <button type="button" className="button-primary" onClick={() => openForm()}>
          Novo contato
        </button>
      </div>

      {notice && (
        <p role="status" className={`notice notice--${notice.kind}`}>
          {notice.text}
        </p>
      )}

      {isLoading && <p className="loading">Carregando...</p>}
      {loadError && <LoadError error={loadError} what="a agenda" />}

      {contacts?.length === 0 && (
        <div className="empty-state">
          <h2>Nenhum contato ainda</h2>
          <p>Guarde aqui o nome e o e-mail ou telefone das pessoas com quem você fala.</p>
        </div>
      )}

      {contacts && contacts.length > 0 && (
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>Nome</th>
                <th>E-mail</th>
                <th>Telefone</th>
                <th className="table-actions">Ações</th>
              </tr>
            </thead>
            <tbody>
              {contacts.map((contact) => (
                <tr key={contact.id}>
                  <td>{contact.name}</td>
                  <td>{contact.email ?? "—"}</td>
                  <td>{contact.phone ?? "—"}</td>
                  <td className="table-actions">
                    <button type="button" aria-label={`Editar ${contact.name}`} onClick={() => openForm(contact)}>
                      Editar
                    </button>
                    <button
                      type="button"
                      className="button-danger"
                      aria-label={`Excluir ${contact.name}`}
                      onClick={() => askToDelete(contact)}
                    >
                      Excluir
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editing && (
        <ContactFormModal
          key={editing.contact?.id ?? "new"}
          contact={editing.contact}
          isSaving={saveContact.isPending}
          error={saveContact.error}
          onClose={() => setEditing(null)}
          onSubmit={(data) => handleSave({ id: editing.contact?.id, data })}
        />
      )}

      {deleting && (
        <ConfirmDialog
          title="Excluir contato"
          confirmLabel="Excluir"
          pendingLabel="Excluindo..."
          isPending={removeContact.isPending}
          onConfirm={() => handleDelete(deleting)}
          onCancel={() => setDeleting(null)}
        >
          <p>
            <strong>{deleting.name}</strong> sai da sua agenda. Isso não pode ser desfeito.
          </p>
        </ConfirmDialog>
      )}
    </main>
  );
}
