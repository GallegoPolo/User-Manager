using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using UserManager.Application.Common;

namespace UserManager.Api.Identity;

public static class IdentityExtensions
{
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
            .AddPolicy(Permissions.UsersRead, policy => policy.RequireRole(ViewerRole, OperatorRole))
            .AddPolicy(Permissions.UsersManage, policy => policy.RequireRole(OperatorRole));

        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<CurrentUser>());

        return services;
    }
}
