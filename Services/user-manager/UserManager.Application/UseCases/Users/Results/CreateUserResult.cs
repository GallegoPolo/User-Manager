using Flunt.Notifications;

namespace UserManager.Application.UseCases.Users.Results
{
    public record CreateUserResult(Guid? UserId, IReadOnlyCollection<Notification>? Errors);
}
