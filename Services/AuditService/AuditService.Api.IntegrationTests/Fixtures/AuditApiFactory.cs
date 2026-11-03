using System.Net.Http.Headers;
using System.Security.Cryptography;
using AuditService.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AuditService.Api.IntegrationTests.Fixtures;

public sealed class AuditApiFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "https://keycloak.test/realms/user-manager";
    private const string Audience = "audit-api";

    private readonly RsaSecurityKey _signingKey = new(RSA.Create(2048)) { KeyId = "test-key" };

    public HttpClient ClientWithRoles(params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithRoles(roles));
        return client;
    }

    private string TokenWithRoles(string[] roles) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Claims = new Dictionary<string, object> { ["sub"] = $"kc-{Guid.NewGuid():N}", ["roles"] = roles },
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
        });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Authentication:Authority", Issuer);
        builder.UseSetting("Authentication:Audience", Audience);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IAuditLogRepository, EmptyAuditLogRepository>();
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(_signingKey);

                options.Configuration = configuration;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });
        });
    }
}
