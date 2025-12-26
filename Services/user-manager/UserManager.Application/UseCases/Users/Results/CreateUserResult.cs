using Flunt.Notifications;

namespace UserManager.Application.UseCases.Users.Results
{
    //TODO: Implementar Result Pattern
    public record CreateUserResult(Guid? UserId, IReadOnlyCollection<Notification>? Errors);
}
