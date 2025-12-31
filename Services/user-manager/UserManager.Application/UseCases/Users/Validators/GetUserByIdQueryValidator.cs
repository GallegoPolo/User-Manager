using FluentValidation;
using UserManager.Application.UseCases.Users.Queries;

namespace UserManager.Application.UseCases.Users.Validators
{
    public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
    {
        public GetUserByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("User ID is required");
        }
    }
}
