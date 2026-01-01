using FluentValidation;
using UserManager.Application.UseCases.Users.Commands;

namespace UserManager.Application.UseCases.Users.Validators
{
    public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
    {
        public DeleteUserCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("User ID is required");
        }
    }
}
