using AuthService.Application.UseCases.ApiKeys.Commands;
using FluentValidation;

namespace AuthService.Application.UseCases.ApiKeys.Validators
{
    public class CreateApiKeyValidator : AbstractValidator<CreateApiKeyCommand>
    {
        public CreateApiKeyValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithMessage("Name is required")
                .MinimumLength(3)
                    .WithMessage("Name must have at least 3 characters")
                .MaximumLength(200)
                    .WithMessage("Name must not exceed 200 characters");

            RuleFor(x => x.Scopes)
                .NotNull()
                    .WithMessage("Scopes list is required")
                .NotEmpty()
                    .WithMessage("At least one scope is required");

            RuleForEach(x => x.Scopes)
                .NotEmpty()
                    .WithMessage("Scope cannot be empty")
                .MaximumLength(100)
                    .WithMessage("Each scope must not exceed 100 characters");

            RuleFor(x => x.ExpiresAt)
                .GreaterThan(DateTime.UtcNow)
                    .When(x => x.ExpiresAt.HasValue)
                    .WithMessage("ExpiresAt must be in the future");
        }
    }
}
