using FluentValidation;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UseCases.Users.Validators
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(User.MAX_NAME_LENGTH)
                .WithMessage($"Name must not exceed {User.MAX_NAME_LENGTH} characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .Must(Email.IsValid).WithMessage("Invalid email format");
        }
    }
}
