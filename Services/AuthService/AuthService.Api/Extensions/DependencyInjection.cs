using AuthService.Api.Services;
using AuthService.Application.UseCases.ApiKeys.Behaviors;
using AuthService.Application.UseCases.ApiKeys.Handlers;
using AuthService.Domain.Interfaces.Repositories;
using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.Services;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Security;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Api.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(CreateApiKeyHandler).Assembly);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateApiKeyHandler).Assembly));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }

        public static IServiceCollection AddBootstrap(this IServiceCollection services)
        {
            services.AddHostedService<ApiKeyBootstrapService>();
            return services;
        }

        public static IServiceCollection AddPersistence(this IServiceCollection services, ConfigurationManager configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
            return services;
        }

        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            services.AddScoped<IApiKeyDomainService, ApiKeyDomainService>();
            return services;
        }

        public static IServiceCollection AddSecurity(this IServiceCollection services)
        {
            services.AddScoped<IApiKeyHasher, ApiKeyHasher>();
            services.AddScoped<ITokenGenerator, JwtTokenGenerator>();
            return services;
        }
    }
}
