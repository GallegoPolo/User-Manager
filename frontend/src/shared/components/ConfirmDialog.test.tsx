import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ConfirmDialog } from "./ConfirmDialog";

afterEach(cleanup);

function renderDialog(overrides: Partial<Parameters<typeof ConfirmDialog>[0]> = {}) {
  const props = {
    title: "Excluir contato",
    confirmLabel: "Excluir",
    pendingLabel: "Excluindo...",
    isPending: false,
    onConfirm: vi.fn(),
    onCancel: vi.fn(),
    children: <p>Bruno Lima sai da sua agenda.</p>,
    ...overrides,
  };
  render(<ConfirmDialog {...props} />);
  return props;
}

describe("ConfirmDialog", () => {
  it("ConfirmDialog_Opens_WithMessageAndFocusOnCancel", () => {
    renderDialog();

    const dialog = screen.getByRole("dialog", { name: "Excluir contato" });
    expect(dialog.textContent).toContain("Bruno Lima sai da sua agenda.");
    expect(document.activeElement).toBe(screen.getByRole("button", { name: "Cancelar" }));
  });

  it("ConfirmDialog_Confirm_CallsOnConfirm", async () => {
    const props = renderDialog();

    await userEvent.click(screen.getByRole("button", { name: "Excluir" }));

    expect(props.onConfirm).toHaveBeenCalledTimes(1);
    expect(props.onCancel).not.toHaveBeenCalled();
  });

  it("ConfirmDialog_Cancel_CallsOnCancel", async () => {
    const props = renderDialog();

    await userEvent.click(screen.getByRole("button", { name: "Cancelar" }));

    expect(props.onCancel).toHaveBeenCalledTimes(1);
    expect(props.onConfirm).not.toHaveBeenCalled();
  });

  it("ConfirmDialog_Pending_LocksButtonsAndEscape", () => {
    const props = renderDialog({ isPending: true });

    expect((screen.getByRole("button", { name: "Excluindo..." }) as HTMLButtonElement).disabled).toBe(true);
    expect((screen.getByRole("button", { name: "Cancelar" }) as HTMLButtonElement).disabled).toBe(true);

    fireEvent(screen.getByRole("dialog"), new Event("cancel", { cancelable: true }));

    expect(props.onCancel).not.toHaveBeenCalled();
  });
});
