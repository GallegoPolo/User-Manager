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
        var user = User.Create("Ana", Email.Create($"  {localPart.ToUpperInvariant()}@Example.com "));

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
    public async Task EmailExistsAsync_ComparesNormalizedEmail()
    {
        // Arrange
        var email = $"{Guid.NewGuid():N}@example.com";
        await AddUserAsync(User.Create("Ana", Email.Create(email)));

        // Act
        await using var context = _postgres.CreateContext();
        var exists = await new UserRepository(context).EmailExistsAsync(Email.Create($" {email.ToUpperInvariant()} "), CancellationToken.None);

        // Assert
        Assert.True(exists);
    }

    private async Task AddUserAsync(User user)
    {
        await using var context = _postgres.CreateContext();
        await new UserRepository(context).AddAsync(user);
        await context.SaveChangesAsync();
    }
}
