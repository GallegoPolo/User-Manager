using AuthService.Application.UseCases.ApiKeys.Commands;
using FluentValidation;

namespace AuthService.Application.UseCases.ApiKeys.Validators
{
    public class CreateAdminValidator : AbstractValidator<CreateAdminCommand>
    {
        public CreateAdminValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(200).WithMessage("Email must not exceed 200 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must have at least 8 characters")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters");
        }
    }
}
