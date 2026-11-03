using UserManager.Application.Common;
using UserManager.Application.UnitTests.Fakes;
using UserManager.Application.UseCases.Contacts.Handlers;
using UserManager.Application.UseCases.Contacts.Queries;
using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UnitTests.UseCases.Contacts;

public sealed class GetContactByIdHandlerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private readonly Contact _ana = Contact.Create(OwnerId, "Ana", Email.Create("ana@example.com"), PhoneNumber.Create("11987654321"));
    private readonly FakeContactRepository _contacts = new();

    public GetContactByIdHandlerTests()
    {
        _contacts.With(_ana);
    }

    [Fact]
    public async Task Handle_WhenContactBelongsToAnotherOwner_ReturnsNotFound()
    {
        // Act
        var result = await CreateHandler(Guid.NewGuid()).Handle(new GetContactByIdQuery(_ana.Id), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Handle_WhenContactDoesNotExist_ReturnsNotFound()
    {
        // Act
        var result = await CreateHandler().Handle(new GetContactByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenContactBelongsToOwner_ReturnsDto()
    {
        // Act
        var result = await CreateHandler().Handle(new GetContactByIdQuery(_ana.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal(_ana.Id, dto.Id);
        Assert.Equal("Ana", dto.Name);
        Assert.Equal("ana@example.com", dto.Email);
        Assert.Equal("11987654321", dto.Phone);
        Assert.Equal(_ana.CreatedAt, dto.CreatedAt);
    }

    private GetContactByIdHandler CreateHandler(Guid? currentUserId = null) => new(new FakeCurrentUser().ProvisionedAs(currentUserId ?? OwnerId), _contacts);
}
