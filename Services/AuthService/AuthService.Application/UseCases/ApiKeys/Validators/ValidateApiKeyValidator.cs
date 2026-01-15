using AuthService.Application.UseCases.ApiKeys.Queries;
using FluentValidation;

namespace AuthService.Application.UseCases.ApiKeys.Validators
{
    public class ValidateApiKeyValidator : AbstractValidator<ValidateApiKeyQuery>
    {
        public ValidateApiKeyValidator()
        {
            RuleFor(x => x.ApiKey)
                .NotEmpty()
                .WithMessage("API Key is required")
                .MinimumLength(10)
                .WithMessage("API Key must have at least 10 characters");
        }
    }
}
