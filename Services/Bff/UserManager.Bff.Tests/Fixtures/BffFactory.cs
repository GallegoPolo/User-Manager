using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace UserManager.Bff.Tests.Fixtures;

public sealed class BffFactory : WebApplicationFactory<Program>
{
    public const string AuthorizationEndpoint = "https://keycloak.test/auth";
    public const string EndSessionEndpoint = "https://keycloak.test/logout";

    private readonly Dictionary<string, string?> _settings = new()
    {
        ["ConnectionStrings:Redis"] = "localhost:1,abortConnect=false,connectTimeout=100",
        ["Oidc:Authority"] = "https://keycloak.test/realms/user-manager",
        ["Oidc:ClientId"] = "user-manager-bff",
        ["Oidc:ClientSecret"] = "test-secret"
    };

    public BffFactory WithSetting(string key, string value)
    {
        _settings[key] = value;
        return this;
    }

    public HttpClient CreateBrowserClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (key, value) in _settings)
            builder.UseSetting(key, value);

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestSessionHandler>(TestSessionHandler.SchemeName, null);

            services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                options.ForwardDefaultSelector = context =>
                    context.Request.Headers.ContainsKey(TestSessionHandler.UserHeader) ? TestSessionHandler.SchemeName : null);

            services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration
                {
                    Issuer = options.Authority,
                    AuthorizationEndpoint = AuthorizationEndpoint,
                    EndSessionEndpoint = EndSessionEndpoint
                };
                options.Configuration = configuration;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });
        });
    }
}
