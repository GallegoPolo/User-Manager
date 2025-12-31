namespace UserManager.Api.Contracts.Responses
{
    public record GetAllUsersResponse(Guid Id, string Name, string Email, DateTime CreatedAt);
}
