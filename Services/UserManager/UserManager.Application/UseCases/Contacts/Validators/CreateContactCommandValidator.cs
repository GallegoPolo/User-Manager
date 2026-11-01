using FluentValidation;
using UserManager.Application.UseCases.Contacts.Commands;

namespace UserManager.Application.UseCases.Contacts.Validators;

public sealed class CreateContactCommandValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactCommandValidator()
    {
        Include(new ContactDetailsValidator());
    }
}
