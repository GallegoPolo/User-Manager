using Microsoft.EntityFrameworkCore;
using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;
using UserManager.Infrastructure.IntegrationTests.Fixtures;
using UserManager.Infrastructure.Repositories;

namespace UserManager.Infrastructure.IntegrationTests.Repositories;

public sealed class UserRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public UserRepositoryTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task AddAsync_StoresNormalizedEmailInColumn()
    {
        // Arrange
        var localPart = Guid.NewGuid().ToString("N");
        var user = User.Provision($"kc-{Guid.NewGuid():N}", "Ana", Email.Create($"  {localPart.ToUpperInvariant()}@Example.com "));

        // Act
        await AddUserAsync(user);

        // Assert
        await using var context = _postgres.CreateContext();
        var storedEmail = await context.Database
            .SqlQuery<string>($"""SELECT "Email" AS "Value" FROM "Users" WHERE "Id" = {user.Id}""")
            .SingleAsync();
        Assert.Equal($"{localPart}@example.com", storedEmail);
    }

    [Fact]
    public async Task GetByIdentityProviderIdAsync_ReturnsStoredUser()
    {
        // Arrange
        var sub = $"kc-{Guid.NewGuid():N}";
        var user = User.Provision(sub, "Ana", Email.Create($"{Guid.NewGuid():N}@example.com"));
        await AddUserAsync(user);

        // Act
        await using var context = _postgres.CreateContext();
        var loaded = await new UserRepository(context).GetByIdentityProviderIdAsync(sub, CancellationToken.None);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded.Id);
        Assert.Equal(sub, loaded.IdentityProviderId);
    }

    [Fact]
    public async Task GetByIdentityProviderIdAsync_WhenMissing_ReturnsNull()
    {
        // Act
        await using var context = _postgres.CreateContext();
        var loaded = await new UserRepository(context).GetByIdentityProviderIdAsync($"kc-{Guid.NewGuid():N}", CancellationToken.None);

        // Assert
        Assert.Null(loaded);
    }

    private async Task AddUserAsync(User user)
    {
        await using var context = _postgres.CreateContext();
        await new UserRepository(context).AddAsync(user);
        await context.SaveChangesAsync();
    }
}
