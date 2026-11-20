import { useEffect, useRef, type ReactNode } from "react";
import { Modal } from "./Modal";

interface Props {
  title: string;
  confirmLabel: string;
  pendingLabel: string;
  isPending: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  children: ReactNode;
}

export function ConfirmDialog({ title, confirmLabel, pendingLabel, isPending, onConfirm, onCancel, children }: Props) {
  const cancelRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    cancelRef.current?.focus();
  }, []);

  return (
    <Modal title={title} onClose={onCancel} dismissible={!isPending}>
      <div className="confirm-message">{children}</div>
      <div className="modal-actions">
        <button ref={cancelRef} type="button" onClick={onCancel} disabled={isPending}>
          Cancelar
        </button>
        <button type="button" className="button-danger-solid" onClick={onConfirm} disabled={isPending}>
          {isPending ? pendingLabel : confirmLabel}
        </button>
      </div>
    </Modal>
  );
}
