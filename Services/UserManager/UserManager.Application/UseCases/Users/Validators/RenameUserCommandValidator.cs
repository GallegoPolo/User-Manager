using FluentValidation;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Domain.Entities;

namespace UserManager.Application.UseCases.Users.Validators;

public sealed class RenameUserCommandValidator : AbstractValidator<RenameUserCommand>
{
    public RenameUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(User.MAX_NAME_LENGTH).WithMessage($"Name must not exceed {User.MAX_NAME_LENGTH} characters");
    }
}
