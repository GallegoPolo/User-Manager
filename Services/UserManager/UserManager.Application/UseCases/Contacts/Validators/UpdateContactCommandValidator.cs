using FluentValidation;
using UserManager.Application.UseCases.Contacts.Commands;

namespace UserManager.Application.UseCases.Contacts.Validators;

public sealed class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactCommandValidator()
    {
        Include(new ContactDetailsValidator());
    }
}
