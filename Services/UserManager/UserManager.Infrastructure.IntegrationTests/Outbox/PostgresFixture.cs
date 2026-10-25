using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using UserManager.Infrastructure.Persistence;

namespace UserManager.Infrastructure.IntegrationTests.Outbox;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public string ConnectionString => _container.GetConnectionString();

    public UserManagerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UserManagerDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new UserManagerDbContext(options);
    }
}
