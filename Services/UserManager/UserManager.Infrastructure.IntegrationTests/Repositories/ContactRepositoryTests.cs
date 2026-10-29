using Microsoft.EntityFrameworkCore;
using Npgsql;
using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;
using UserManager.Infrastructure.IntegrationTests.Outbox;
using UserManager.Infrastructure.Repositories;

namespace UserManager.Infrastructure.IntegrationTests.Repositories;

public sealed class ContactRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public ContactRepositoryTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task GetByOwnerAsync_ReturnsOnlyOwnersContactsOrderedByNameWithValueObjects()
    {
        // Arrange
        var ownerId = await CreateUserAsync();
        var otherOwnerId = await CreateUserAsync();
        await AddContactsAsync(Contact.Create(ownerId, "Carla", Email.Create("a@example.com"), phone: null));
        await AddContactsAsync(Contact.Create(ownerId, "Bruno", Email.Create("b@example.com"), phone: null));
        await AddContactsAsync(Contact.Create(ownerId, "Ana", email: null, PhoneNumber.Create("(11) 91234-5678")));
        await AddContactsAsync(Contact.Create(otherOwnerId, "Aaron", Email.Create("c@example.com"), phone: null));

        // Act
        await using var context = _postgres.CreateContext();
        var contacts = await new ContactRepository(context).GetByOwnerAsync(ownerId, CancellationToken.None);

        // Assert
        Assert.Collection(contacts,
            ana => Assert.Equal(PhoneNumber.Create("11912345678"), ana.Phone),
            bruno => Assert.Equal("Bruno", bruno.Name),
            carla => Assert.Equal(Email.Create("a@example.com"), carla.Email));
    }

    [Fact]
    public async Task EmailExistsAsync_IsScopedToOwner()
    {
        // Arrange
        var ownerId = await CreateUserAsync();
        var otherOwnerId = await CreateUserAsync();
        await AddContactsAsync(Contact.Create(ownerId, "Ana", Email.Create("ana@example.com"), phone: null));

        // Act
        await using var context = _postgres.CreateContext();
        var repository = new ContactRepository(context);
        var existsForOwner = await repository.EmailExistsAsync(ownerId, Email.Create(" ANA@example.com "), CancellationToken.None);
        var existsForOther = await repository.EmailExistsAsync(otherOwnerId, Email.Create("ana@example.com"), CancellationToken.None);

        // Assert
        Assert.True(existsForOwner);
        Assert.False(existsForOther);
    }

    [Fact]
    public async Task SaveChanges_WithSameEmailInSameBook_ViolatesUniqueIndex()
    {
        // Arrange
        var ownerId = await CreateUserAsync();
        await AddContactsAsync(Contact.Create(ownerId, "Ana", Email.Create("ana@example.com"), phone: null));

        // Act
        var duplicate = () => AddContactsAsync(Contact.Create(ownerId, "Ana 2", Email.Create("ana@example.com"), phone: null));

        // Assert
        var exception = await Assert.ThrowsAsync<DbUpdateException>(duplicate);
        var postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
    }

    [Fact]
    public async Task SaveChanges_WithSameEmailInDifferentBooks_IsAllowed()
    {
        // Arrange
        var ownerId = await CreateUserAsync();
        var otherOwnerId = await CreateUserAsync();

        // Act
        await AddContactsAsync(
            Contact.Create(ownerId, "Ana", Email.Create("ana@example.com"), phone: null),
            Contact.Create(otherOwnerId, "Ana", Email.Create("ana@example.com"), phone: null));

        // Assert
        Assert.Equal(2, await CountContactsAsync(ownerId, otherOwnerId));
    }

    [Fact]
    public async Task SaveChanges_WithSeveralPhoneOnlyContactsInSameBook_IsAllowed()
    {
        // Arrange
        var ownerId = await CreateUserAsync();

        // Act
        await AddContactsAsync(
            Contact.Create(ownerId, "Ana", email: null, PhoneNumber.Create("11987654321")),
            Contact.Create(ownerId, "Bruno", email: null, PhoneNumber.Create("11912345678")));

        // Assert
        Assert.Equal(2, await CountContactsAsync(ownerId));
    }

    [Fact]
    public async Task DeletingOwner_DeletesTheirContacts()
    {
        // Arrange
        var ownerId = await CreateUserAsync();
        await AddContactsAsync(Contact.Create(ownerId, "Ana", Email.Create("ana@example.com"), phone: null));

        // Act
        await using (var context = _postgres.CreateContext())
        {
            await context.Users.Where(u => u.Id == ownerId).ExecuteDeleteAsync();
        }

        // Assert
        Assert.Equal(0, await CountContactsAsync(ownerId));
    }

    private async Task<Guid> CreateUserAsync()
    {
        var user = User.Create("Dono", $"{Guid.NewGuid()}@example.com");

        await using var context = _postgres.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }

    private async Task AddContactsAsync(params Contact[] contacts)
    {
        await using var context = _postgres.CreateContext();
        var repository = new ContactRepository(context);

        foreach (var contact in contacts)
            await repository.AddAsync(contact, CancellationToken.None);

        await context.SaveChangesAsync();
    }

    private async Task<int> CountContactsAsync(params Guid[] ownerIds)
    {
        await using var context = _postgres.CreateContext();
        return await context.Contacts.CountAsync(c => ownerIds.Contains(c.OwnerId));
    }
}
