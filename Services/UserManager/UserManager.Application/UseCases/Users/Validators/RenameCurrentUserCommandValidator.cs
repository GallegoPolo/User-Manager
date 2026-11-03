using FluentValidation;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Domain.Entities;

namespace UserManager.Application.UseCases.Users.Validators;

public sealed class RenameCurrentUserCommandValidator : AbstractValidator<RenameCurrentUserCommand>
{
    public RenameCurrentUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(User.MAX_NAME_LENGTH).WithMessage($"Name must not exceed {User.MAX_NAME_LENGTH} characters");
    }
}
