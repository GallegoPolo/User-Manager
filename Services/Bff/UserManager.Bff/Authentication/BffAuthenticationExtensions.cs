using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using UserManager.Bff.Sessions;

namespace UserManager.Bff.Authentication;

public static class BffAuthenticationExtensions
{
    public const string SessionCookieName = "__Host-user-manager";

    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    public static IServiceCollection AddBffAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var oidc = configuration.GetSection("Oidc");

        services.AddStackExchangeRedisCache(options => options.Configuration = configuration.GetConnectionString("Redis"));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITicketStore, RedisTicketStore>();
        services.AddHttpClient<TokenRefresher>(client => client.Timeout = TokenRefresher.TokenEndpointTimeout);

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = SessionCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = SessionLifetime;
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context => RespondWith(context.Response, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = context => RespondWith(context.Response, StatusCodes.Status403Forbidden);
                options.Events.OnValidatePrincipal = RefreshTokensOrEndSessionAsync;
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = oidc["Authority"];
                options.ClientId = oidc["ClientId"];
                options.ClientSecret = oidc["ClientSecret"];
                options.RequireHttpsMetadata = oidc.GetValue("RequireHttpsMetadata", true);
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.UsePkce = true;
                options.SaveTokens = true;
                options.MapInboundClaims = false;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "roles";
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
            });

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((options, store) => options.SessionStore = store);

        services.AddAuthorization();

        return services;
    }

    private static async Task RefreshTokensOrEndSessionAsync(CookieValidatePrincipalContext context)
    {
        await context.HttpContext.RequestServices.GetRequiredService<TokenRefresher>().ValidateAsync(context);

        if (context.Principal is null)
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static Task RespondWith(HttpResponse response, int statusCode)
    {
        response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
