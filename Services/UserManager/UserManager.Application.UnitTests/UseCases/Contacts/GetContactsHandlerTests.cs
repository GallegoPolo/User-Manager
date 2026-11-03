using UserManager.Application.Common;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Contacts.Handlers;
using UserManager.Application.UseCases.Contacts.Queries;
using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class GetContactsHandlerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private readonly FakeUserRepository _users = new FakeUserRepository().WithUser(OwnerId);
    private readonly FakeContactRepository _contacts = new();

    [Fact]
    public async Task Handle_WhenCurrentUserNoLongerExists_ReturnsNotFound()
    {
        // Act
        var result = await CreateHandler(Guid.NewGuid()).Handle(new GetContactsQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyOwnersContactsAsDtos()
    {
        // Arrange
        var ana = Contact.Create(OwnerId, "Ana", Email.Create("ana@example.com"), PhoneNumber.Create("11987654321"));
        var bruno = Contact.Create(OwnerId, "Bruno", email: null, PhoneNumber.Create("11912345678"));
        _contacts.With(ana).With(bruno)
                 .With(Contact.Create(Guid.NewGuid(), "Outro dono", Email.Create("x@example.com"), phone: null));

        // Act
        var result = await CreateHandler().Handle(new GetContactsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Collection(result.Value!,
            dto =>
            {
                Assert.Equal(ana.Id, dto.Id);
                Assert.Equal("Ana", dto.Name);
                Assert.Equal("ana@example.com", dto.Email);
                Assert.Equal("11987654321", dto.Phone);
                Assert.Equal(ana.CreatedAt, dto.CreatedAt);
            },
            dto =>
            {
                Assert.Equal(bruno.Id, dto.Id);
                Assert.Null(dto.Email);
            });
    }

    private GetContactsHandler CreateHandler(Guid? currentUserId = null) => new(new FakeCurrentUser().ProvisionedAs(currentUserId ?? OwnerId), _users, _contacts);
}
