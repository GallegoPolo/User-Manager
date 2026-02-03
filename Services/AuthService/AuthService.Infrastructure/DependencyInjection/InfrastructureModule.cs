using AuthService.Domain.Interfaces.Repositories;
using AuthService.Infrastructure.Caching.Services;
using AuthService.Infrastructure.Configurations;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure.DependencyInjection
{
    public static class InfrastructureModule
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, IConfiguration configuration)
        {
            services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));

            services.Configure<RedisSettings>(configuration.GetSection("Redis"));

            var redisConnection = configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string not found");

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "AuthService:";
            });

            services.AddScoped<IApiKeyCacheService, ApiKeyCacheService>();

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
