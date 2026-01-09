namespace AuthService.Domain.Interfaces.Repositories
{
    public interface ITokenGenerator
    {
        Task<string> GenerateTokenAsync(string subject, IEnumerable<string> roles, IEnumerable<string> scopes, Dictionary<string, string>? additionalClaims = null, CancellationToken cancellationToken = default);
    }
}
