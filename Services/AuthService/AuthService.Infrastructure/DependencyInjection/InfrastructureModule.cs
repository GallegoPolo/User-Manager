using AuthService.Domain.Interfaces.Repositories;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure.DependencyInjection
{
    public static class InfrastructureModule
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));

            services.AddScoped<IApiKeyHasher, ApiKeyHasher>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ITokenGenerator, JwtTokenGenerator>();

            services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
            services.AddScoped<IUserCredentialRepository, UserCredentialRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
