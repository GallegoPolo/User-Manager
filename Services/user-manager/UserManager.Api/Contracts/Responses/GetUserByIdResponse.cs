namespace UserManager.Api.Contracts.Responses
{
    public record GetUserByIdResponse(Guid Id, string Name, string Email, DateTime CreatedAt);
}
