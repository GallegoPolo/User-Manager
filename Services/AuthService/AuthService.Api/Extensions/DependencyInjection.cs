using AuthService.Api.Services;
using AuthService.Application.UseCases.ApiKeys.Behaviors;
using AuthService.Application.UseCases.ApiKeys.Handlers;
using AuthService.Domain.Interfaces.Services;
using AuthService.Domain.Services;
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
            services.AddHostedService<AdminBootstrapService>();
            return services;
        }

        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            services.AddScoped<IApiKeyDomainService, ApiKeyDomainService>();
            return services;
        }
    }
}
