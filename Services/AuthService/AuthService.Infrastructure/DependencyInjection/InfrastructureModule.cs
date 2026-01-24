using AuthService.Domain.Interfaces.Repositories;
using AuthService.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure.DependencyInjection
{
    public static class InfrastructureModule
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            services.AddScoped<IApiKeyHasher, ApiKeyHasher>();

            return services;
        }
    }
}
