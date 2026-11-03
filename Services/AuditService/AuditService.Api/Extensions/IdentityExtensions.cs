using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace AuditService.Api.Extensions;

public static class IdentityExtensions
{
    public const string AuditRead = "audit:read";

    private const string ViewerRole = "viewer";
    private const string OperatorRole = "operator";

    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authentication = configuration.GetSection("Authentication");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authentication["Authority"];
                options.Audience = authentication["Audience"];
                options.RequireHttpsMetadata = authentication.GetValue("RequireHttpsMetadata", true);
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "sub";
                options.TokenValidationParameters.RoleClaimType = "roles";
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AuditRead, policy => policy.RequireRole(ViewerRole, OperatorRole));

        return services;
    }
}
