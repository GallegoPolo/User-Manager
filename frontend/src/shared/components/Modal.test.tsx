import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { Modal } from "./Modal";

afterEach(cleanup);

describe("Modal", () => {
  it("Modal_Mounted_IsOpenWithTitle", () => {
    render(
      <Modal title="Novo contato" onClose={() => {}}>
        <p>conteúdo</p>
      </Modal>,
    );

    const dialog = screen.getByRole("dialog", { name: "Novo contato" }) as HTMLDialogElement;

    expect(dialog.open).toBe(true);
    expect(screen.getByText("conteúdo")).toBeDefined();
  });

  it("Modal_Escape_CallsOnClose", () => {
    const onClose = vi.fn();
    render(
      <Modal title="Novo contato" onClose={onClose}>
        <p>conteúdo</p>
      </Modal>,
    );

    fireEvent(screen.getByRole("dialog"), new Event("cancel", { cancelable: true }));

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("Modal_NotDismissible_IgnoresEscape", () => {
    const onClose = vi.fn();
    render(
      <Modal title="Novo contato" onClose={onClose} dismissible={false}>
        <p>conteúdo</p>
      </Modal>,
    );

    fireEvent(screen.getByRole("dialog"), new Event("cancel", { cancelable: true }));

    expect(onClose).not.toHaveBeenCalled();
  });

  it("Modal_Unmounted_ReturnsFocusToOpener", () => {
    const opener = document.createElement("button");
    document.body.appendChild(opener);
    opener.focus();

    const { unmount } = render(
      <Modal title="Novo contato" onClose={() => {}}>
        <input aria-label="campo" />
      </Modal>,
    );
    screen.getByLabelText("campo").focus();
    unmount();

    expect(document.activeElement).toBe(opener);
    opener.remove();
  });
});
