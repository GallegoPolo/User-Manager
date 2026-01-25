using AuthService.Application.UseCases.ApiKeys.Commands;
using AuthService.Domain.Interfaces.Repositories;
using MediatR;

namespace AuthService.Api.Services
{
    public class AdminBootstrapService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminBootstrapService> _logger;

        public AdminBootstrapService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<AdminBootstrapService> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserCredentialRepository>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var adminEmail = _configuration["Bootstrap:AdminEmail"] ?? "admin@authservice.com";
            var adminPassword = _configuration["Bootstrap:AdminPassword"] ?? "Admin@123456";

            var adminExists = await userRepository.EmailExistsAsync(adminEmail, cancellationToken);

            if (!adminExists)
            {
                _logger.LogInformation("Creating bootstrap admin user...");

                var command = new CreateAdminCommand(adminEmail, adminPassword);
                var result = await mediator.Send(command, cancellationToken);

                if (result.IsSuccess)
                    _logger.LogInformation("Bootstrap admin created successfully with email: {Email}", adminEmail);
                else
                    _logger.LogError("Failed to create bootstrap admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Message)));
            }
            else
                _logger.LogInformation("Bootstrap admin already exists.");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
