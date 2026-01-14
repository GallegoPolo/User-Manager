namespace AuthService.Application.UseCases.ApiKeys.DTOs
{
    public class ApiKeyDTO 
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Scopes { get; set; } = new();
        //Status é uma string, deveria ser um enum com description
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
