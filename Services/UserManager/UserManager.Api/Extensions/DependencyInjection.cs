using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserManager.Application.Events.Interfaces;
using UserManager.Application.Outbox.Interfaces;
using UserManager.Application.UseCases.Users.Behaviors;
using UserManager.Application.UseCases.Users.Handlers;
using UserManager.Domain.Interfaces;
using UserManager.Domain.Services;
using UserManager.Infrastructure.Messaging;
using UserManager.Infrastructure.Outbox;
using UserManager.Infrastructure.Persistence;
using UserManager.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace UserManager.Api.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(CreateUserHandler).Assembly);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }

        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            services.AddScoped<IUserDomainService, UserDomainService>();

            return services;
        }

        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddScoped<OutboxRepository>();
            services.AddScoped<IAuditEventPublisher, RabbitMqAuditEventPublisher>();
            services.AddHostedService<OutboxProcessorBackgroundService>();
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
            services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    RoleClaimType = "role",
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
                options.AddPolicy("ServiceOnly", policy => policy.RequireRole("SERVICE"));
            });

            return services;
        }
    }
}
