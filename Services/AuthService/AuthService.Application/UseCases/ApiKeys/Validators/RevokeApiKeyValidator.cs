using AuthService.Application.UseCases.ApiKeys.Commands;
using FluentValidation;

namespace AuthService.Application.UseCases.ApiKeys.Validators
{
    public class RevokeApiKeyValidator : AbstractValidator<RevokeApiKeyCommand>
    {
        public RevokeApiKeyValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Id is required");
        }
    }
}
