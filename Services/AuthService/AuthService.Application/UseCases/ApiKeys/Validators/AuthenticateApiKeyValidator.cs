using AuthService.Application.UseCases.ApiKeys.Commands;
using FluentValidation;

namespace AuthService.Application.UseCases.ApiKeys.Validators
{
    public class AuthenticateApiKeyValidator : AbstractValidator<AuthenticateApiKeyCommand>
    {
        public AuthenticateApiKeyValidator()
        {
            RuleFor(x => x.ApiKey)
                .NotEmpty().WithMessage("API Key is required")
                .MinimumLength(32).WithMessage("Invalid API Key format");
        }
    }
}
