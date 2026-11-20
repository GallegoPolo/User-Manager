import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ApiError } from "../../../shared/api/apiClient";
import type { Contact } from "../types/contact";
import { ContactFormModal } from "./ContactFormModal";

afterEach(cleanup);

const bruno: Contact = {
  id: "c1",
  name: "Bruno Lima",
  email: "bruno@exemplo.com",
  phone: null,
  createdAt: "2026-11-20T10:00:00Z",
};

function renderForm(props: Partial<Parameters<typeof ContactFormModal>[0]> = {}) {
  const onSubmit = vi.fn();
  render(<ContactFormModal isSaving={false} error={null} onClose={() => {}} onSubmit={onSubmit} {...props} />);
  return { onSubmit };
}

describe("ContactFormModal", () => {
  it("ContactForm_EmptyEmailAndPhone_SubmitsNulls", async () => {
    const { onSubmit } = renderForm();

    await userEvent.type(screen.getByLabelText("Nome"), "Carla");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    expect(onSubmit).toHaveBeenCalledWith({ name: "Carla", email: null, phone: null });
  });

  it("ContactForm_Conflict_ShowsMessageUnderEmail", () => {
    renderForm({
      error: new ApiError(409, "/api/me/contacts", [
        { property: "Email", message: "Já existe um contato com este e-mail na agenda." },
      ]),
    });

    const email = screen.getByLabelText("E-mail");

    expect(email.getAttribute("aria-invalid")).toBe("true");
    expect(document.getElementById(email.getAttribute("aria-describedby")!)?.textContent).toBe(
      "Já existe um contato com este e-mail na agenda.",
    );
  });

  it("ContactForm_ValidationOnTwoFields_ShowsBoth", () => {
    renderForm({
      error: new ApiError(400, "/api/me/contacts", [
        { property: "Name", message: "O nome é obrigatório." },
        { property: "Phone", message: "Telefone inválido." },
      ]),
    });

    expect(screen.getByText("O nome é obrigatório.")).toBeDefined();
    expect(screen.getByText("Telefone inválido.")).toBeDefined();
    expect(screen.getByLabelText("E-mail").getAttribute("aria-invalid")).toBeNull();
  });

  it("ContactForm_UnknownField_ShowsErrorOnTop", () => {
    renderForm({
      error: new ApiError(404, "/api/me/contacts/c1", [{ property: "ContactId", message: "Contato não encontrado." }]),
    });

    expect(screen.getByRole("alert").textContent).toBe("Contato não encontrado.");
  });

  it("ContactForm_Edit_StartsWithContactValues", async () => {
    const { onSubmit } = renderForm({ contact: bruno });

    expect(screen.getByRole("dialog", { name: "Editar contato" })).toBeDefined();
    await userEvent.type(screen.getByLabelText("Telefone"), "11 99876-5432");
    await userEvent.click(screen.getByRole("button", { name: "Salvar" }));

    expect(onSubmit).toHaveBeenCalledWith({ name: "Bruno Lima", email: "bruno@exemplo.com", phone: "11 99876-5432" });
  });
});
