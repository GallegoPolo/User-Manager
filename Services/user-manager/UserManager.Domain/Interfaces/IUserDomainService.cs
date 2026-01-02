namespace UserManager.Domain.Interfaces
{
    public interface IUserDomainService
    {
        Task<bool> IsEmailUniqueAsync(string email, CancellationToken cancellationToken);
    }
}
