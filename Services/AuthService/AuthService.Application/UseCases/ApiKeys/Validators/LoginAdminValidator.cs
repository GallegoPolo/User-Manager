using AuthService.Application.UseCases.ApiKeys.Commands;
using FluentValidation;

namespace AuthService.Application.UseCases.ApiKeys.Validators
{
    public class LoginAdminValidator : AbstractValidator<LoginAdminCommand>
    {
        public LoginAdminValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must have at least 6 characters");
        }
    }
}
