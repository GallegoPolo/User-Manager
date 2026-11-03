using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using UserManager.Infrastructure.Persistence;

namespace UserManager.Api.IntegrationTests.Fixtures;

public sealed class UserManagerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Issuer = "https://keycloak.test/realms/user-manager";
    private const string Audience = "user-manager-api";
    private const string BrokerUser = "test";

    private readonly RsaSecurityKey _signingKey = new(RSA.Create(2048)) { KeyId = "test-key" };
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management")
        .WithUsername(BrokerUser)
        .WithPassword(BrokerUser)
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<UserManagerDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    public HttpClient ClientFor(TestPerson person)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(person));
        return client;
    }

    public string TokenFor(TestPerson person)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = person.Sub,
            ["name"] = person.Name,
            ["roles"] = person.Roles
        };

        if (person.Email is not null)
            claims["email"] = person.Email;

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
        });
    }

    public UserManagerDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<UserManagerDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("RabbitMq:HostName", _rabbitMq.Hostname);
        builder.UseSetting("RabbitMq:Port", _rabbitMq.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort).ToString());
        builder.UseSetting("RabbitMq:UserName", BrokerUser);
        builder.UseSetting("RabbitMq:Password", BrokerUser);
        builder.UseSetting("Authentication:Authority", Issuer);
        builder.UseSetting("Authentication:Audience", Audience);

        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                configuration.SigningKeys.Add(_signingKey);

                options.Configuration = configuration;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            }));
    }
}
