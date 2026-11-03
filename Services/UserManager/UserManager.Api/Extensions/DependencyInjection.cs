using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserManager.Application.UseCases.Users.Behaviors;
using UserManager.Application.UseCases.Users.Handlers;
using UserManager.Domain.Interfaces;
using UserManager.Infrastructure.Messaging;
using UserManager.Infrastructure.Persistence;
using UserManager.Infrastructure.Repositories;
using Npgsql;
using System.Diagnostics;

namespace UserManager.Api.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(RenameUserHandler).Assembly);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RenameUserHandler).Assembly));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IContactRepository, ContactRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }

        public static IServiceCollection AddPersistence(this IServiceCollection services, ConfigurationManager configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<UserManagerDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.ConfigureDataSource(TraceOnlyCommandsInsideATrace)));

            return services;
        }

        private static void TraceOnlyCommandsInsideATrace(NpgsqlDataSourceBuilder dataSource)
        {
            dataSource.ConfigureTracing(tracing => tracing.ConfigureCommandFilter(_ => Activity.Current is not null));
        }

        public static IServiceCollection AddMessaging(this IServiceCollection services, ConfigurationManager configuration)
        {
            var rabbitMqSettings = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();
            return services.AddIntegrationMessaging(rabbitMqSettings);
        }
    }
}
