using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Commands
{
    public class CreateApiKeyCommand : IRequest<Result<ApiKeyDTO>>
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Scopes { get; set; } = new();
        public DateTime? ExpiresAt { get; set; }
    }
}
