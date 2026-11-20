import { useId, useState, type FormEvent, type InputHTMLAttributes } from "react";
import { fieldErrorsOf } from "../../../shared/api/apiClient";
import { Modal } from "../../../shared/components/Modal";
import type { Contact, ContactData } from "../types/contact";

interface Props {
  contact?: Contact;
  isSaving: boolean;
  error: unknown;
  onClose: () => void;
  onSubmit: (data: ContactData) => void;
}

const formFields = ["name", "email", "phone"];

function emptyToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed === "" ? null : trimmed;
}

interface FieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
}

function Field({ label, error, ...input }: FieldProps) {
  const id = useId();
  const errorId = `${id}-error`;

  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        {...input}
      />
      {error && (
        <span id={errorId} className="field-error">
          {error}
        </span>
      )}
    </div>
  );
}

export function ContactFormModal({ contact, isSaving, error, onClose, onSubmit }: Props) {
  const [name, setName] = useState(contact?.name ?? "");
  const [email, setEmail] = useState(contact?.email ?? "");
  const [phone, setPhone] = useState(contact?.phone ?? "");
  const errors = fieldErrorsOf(error, formFields);

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({ name: name.trim(), email: emptyToNull(email), phone: emptyToNull(phone) });
  }

  return (
    <Modal title={contact ? "Editar contato" : "Novo contato"} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        {errors.general && (
          <p role="alert" className="form-alert">
            {errors.general}
          </p>
        )}
        <Field label="Nome" value={name} onChange={(e) => setName(e.target.value)} error={errors.name} required autoComplete="name" />
        <Field
          label="E-mail"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          error={errors.email}
          inputMode="email"
          autoComplete="email"
          placeholder="opcional se tiver telefone"
        />
        <Field
          label="Telefone"
          type="tel"
          value={phone}
          onChange={(e) => setPhone(e.target.value)}
          error={errors.phone}
          autoComplete="tel"
          placeholder="opcional se tiver e-mail"
        />
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
