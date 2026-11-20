import { useEffect, useId, useRef, type MouseEvent, type ReactNode, type SyntheticEvent } from "react";

interface Props {
  title: string;
  onClose: () => void;
  dismissible?: boolean;
  children: ReactNode;
}

export function Modal({ title, onClose, dismissible = true, children }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const pressStartedOnBackdrop = useRef(false);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    const opener = document.activeElement;
    if (!dialog.open) dialog.showModal();

    return () => {
      if (dialog.open) dialog.close();
      if (opener instanceof HTMLElement && opener.isConnected) opener.focus();
    };
  }, []);

  function handleCancel(event: SyntheticEvent<HTMLDialogElement>) {
    event.preventDefault();
    if (dismissible) onClose();
  }

  function rememberPressTarget(event: MouseEvent<HTMLDialogElement>) {
    pressStartedOnBackdrop.current = event.target === event.currentTarget;
  }

  function handleBackdropClick(event: MouseEvent<HTMLDialogElement>) {
    const clickedBackdrop = pressStartedOnBackdrop.current && event.target === event.currentTarget;
    pressStartedOnBackdrop.current = false;
    if (dismissible && clickedBackdrop) onClose();
  }

  return (
    <dialog
      ref={dialogRef}
      className="modal-card"
      aria-labelledby={titleId}
      onCancel={handleCancel}
      onMouseDown={rememberPressTarget}
      onClick={handleBackdropClick}
    >
      <div className="modal-body">
        <h2 id={titleId}>{title}</h2>
        {children}
      </div>
    </dialog>
  );
}
