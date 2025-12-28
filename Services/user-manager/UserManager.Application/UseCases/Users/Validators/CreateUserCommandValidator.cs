using FluentValidation;
using UserManager.Application.UseCases.Users.Commands;

namespace UserManager.Application.UseCases.Users.Validators
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Name)
                  .NotEmpty().WithMessage("Name is required")
                  .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");
        }
    }
}
