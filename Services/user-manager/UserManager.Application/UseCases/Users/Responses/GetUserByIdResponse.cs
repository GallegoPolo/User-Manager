namespace UserManager.Application.UseCases.Users.Responses
{
    public record GetUserByIdResponse(Guid Id, string Name, string Email, DateTime CreatedAt);
}
