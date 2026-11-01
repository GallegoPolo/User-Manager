using FluentValidation;
using UserManager.Application.UseCases.Contacts.Commands;
using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UseCases.Contacts.Validators;

public sealed class ContactDetailsValidator : AbstractValidator<IContactDetails>
{
    public ContactDetailsValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(Contact.MaxNameLength).WithMessage($"O nome deve ter no máximo {Contact.MaxNameLength} caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().When(x => string.IsNullOrWhiteSpace(x.Phone)).WithMessage("Informe um e-mail ou um telefone.");

        RuleFor(x => x.Email)
            .Must(Email.IsValid).When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("E-mail inválido.");

        RuleFor(x => x.Phone)
            .Must(PhoneNumber.IsValid).When(x => !string.IsNullOrWhiteSpace(x.Phone)).WithMessage("Telefone inválido.");
    }
}
