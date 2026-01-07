namespace UserManager.Application.UseCases.Users.DTOs
{
    public record UserDTO(Guid Id, string Name, string Email, DateTime CreatedAt);
}
