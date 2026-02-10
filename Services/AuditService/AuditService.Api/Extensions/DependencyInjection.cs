using AuditService.Application.UseCases.Handlers;
using AuditService.Domain.Interfaces.Repositories;
using AuditService.Infrastructure.Persistences;
using AuditService.Infrastructure.Repositories;

namespace AuditService.Api.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAuditServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMongoDbPersistence(configuration);
            services.AddRepositories();
            services.AddMediatRHandlers();

            return services;
        }

        private static IServiceCollection AddMongoDbPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MongoDbSettings>(configuration.GetSection("MongoDb"));

            services.AddSingleton<MongoDbContext>();

            return services;
        }

        private static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();

            return services;
        }

        private static IServiceCollection AddMediatRHandlers(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetAuditLogsHandler).Assembly));

            return services;
        }
    }
}
