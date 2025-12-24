using UserManager.Application.UseCases.Users.Handlers;
using UserManager.Domain.Interfaces;
using UserManager.Infrastructure.Repositories;

namespace UserManager.Api.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<CreateUserHandler>();
            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            return services;
        }
    }
}
