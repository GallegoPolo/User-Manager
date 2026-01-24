using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.ValueObjects;

namespace AuthService.Api.Services
{
    public class ApiKeyBootstrapService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApiKeyBootstrapService> _logger;

        public ApiKeyBootstrapService(IServiceProvider serviceProvider, ILogger<ApiKeyBootstrapService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting API Key bootstrap process...");

            using var scope = _serviceProvider.CreateScope();

            var repository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
            var domainService = scope.ServiceProvider.GetRequiredService<IApiKeyDomainService>();
            var hasher = scope.ServiceProvider.GetRequiredService<IApiKeyHasher>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var existingApiKeys = await repository.GetAllAsync(cancellationToken);

            if (existingApiKeys.Any())
            {
                _logger.LogInformation("API Keys already exist. Skipping bootstrap.");
                return;
            }

            _logger.LogInformation("No API Keys found. Creating initial API Key...");

            var plainApiKey = domainService.GenerateApiKey();
            var keyHash = hasher.Hash(plainApiKey);
            var scopes = new List<Scope>
            {
                new Scope("read"),
                new Scope("write"),
                new Scope("admin")
            };

            var initialApiKey = ApiKey.Create(
                name: "Initial Bootstrap API Key",
                keyHash: keyHash,
                scopes: scopes,
                expiresAt: null
            );

            if (!initialApiKey.IsValid)
            {
                _logger.LogError("Failed to create initial API Key. Validation errors: {Errors}",
                    string.Join(", ", initialApiKey.Notifications.Select(n => n.Message)));
                return;
            }

            await repository.AddAsync(initialApiKey, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("⚠️ INITIAL API KEY CREATED ⚠️\n" + "API Key: {ApiKey}\n" + "⚠️ SAVE THIS KEY - IT WILL NOT BE SHOWN AGAIN ⚠️",  plainApiKey);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("API Key bootstrap service stopped.");
            return Task.CompletedTask;
        }
    }
}
