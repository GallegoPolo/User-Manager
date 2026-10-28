using UserManager.Domain.Entities;
using UserManager.Domain.Events.Entities;
using UserManager.Domain.Exceptions;
using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.UnitTests.Entities;

public sealed class ContactTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Email AnaEmail = Email.Create("ana@example.com");
    private static readonly PhoneNumber AnaPhone = PhoneNumber.Create("11987654321");

    [Fact]
    public void Create_WithValidData_RaisesContactCreatedEvent()
    {
        // Act
        var contact = Contact.Create(OwnerId, "Ana", AnaEmail, AnaPhone);

        // Assert
        var created = Assert.IsType<ContactCreatedDomainEvent>(Assert.Single(contact.DomainEvents));
        Assert.Equal(contact.Id, created.ContactId);
        Assert.Equal(OwnerId, created.OwnerId);
        Assert.Equal("Ana", created.Name);
        Assert.Equal("ana@example.com", created.Email);
        Assert.Equal("11987654321", created.Phone);
    }

    [Fact]
    public void Create_TrimsName()
    {
        // Act
        var contact = Contact.Create(OwnerId, "  Ana  ", AnaEmail, phone: null);

        // Assert
        Assert.Equal("Ana", contact.Name);
    }

    [Fact]
    public void Create_WithOnlyPhone_Succeeds()
    {
        // Act
        var contact = Contact.Create(OwnerId, "Ana", email: null, AnaPhone);

        // Assert
        Assert.Null(contact.Email);
        Assert.Equal(AnaPhone, contact.Phone);
    }

    [Fact]
    public void Create_WithoutEmailAndPhone_ThrowsDomainException()
    {
        // Act
        var create = () => Contact.Create(OwnerId, "Ana", email: null, phone: null);

        // Assert
        Assert.Throws<DomainException>(create);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainException(string name)
    {
        // Act
        var create = () => Contact.Create(OwnerId, name, AnaEmail, phone: null);

        // Assert
        Assert.Throws<DomainException>(create);
    }

    [Fact]
    public void Create_WithNameLongerThanMax_ThrowsDomainException()
    {
        // Act
        var create = () => Contact.Create(OwnerId, new string('a', Contact.MaxNameLength + 1), AnaEmail, phone: null);

        // Assert
        Assert.Throws<DomainException>(create);
    }

    [Fact]
    public void Create_WithEmptyOwner_ThrowsDomainException()
    {
        // Act
        var create = () => Contact.Create(Guid.Empty, "Ana", AnaEmail, phone: null);

        // Assert
        Assert.Throws<DomainException>(create);
    }

    [Fact]
    public void Update_ChangesDataAndRaisesContactUpdatedEvent()
    {
        // Arrange
        var contact = Contact.Create(OwnerId, "Ana", AnaEmail, phone: null);
        contact.ClearDomainEvents();
        var newEmail = Email.Create("ana.silva@example.com");

        // Act
        contact.Update("Ana Silva", newEmail, AnaPhone);

        // Assert
        Assert.Equal("Ana Silva", contact.Name);
        Assert.Equal(newEmail, contact.Email);
        Assert.Equal(AnaPhone, contact.Phone);
        var updated = Assert.IsType<ContactUpdatedDomainEvent>(Assert.Single(contact.DomainEvents));
        Assert.Equal("ana.silva@example.com", updated.Email);
    }

    [Fact]
    public void Update_RemovingEmailAndPhone_ThrowsDomainException()
    {
        // Arrange
        var contact = Contact.Create(OwnerId, "Ana", AnaEmail, phone: null);

        // Act
        var update = () => contact.Update("Ana", email: null, phone: null);

        // Assert
        Assert.Throws<DomainException>(update);
    }

    [Fact]
    public void Update_WithInvalidData_KeepsPreviousState()
    {
        // Arrange
        var contact = Contact.Create(OwnerId, "Ana", AnaEmail, AnaPhone);
        contact.ClearDomainEvents();

        // Act
        var update = () => contact.Update("Bruno", email: null, phone: null);

        // Assert
        Assert.Throws<DomainException>(update);
        Assert.Equal("Ana", contact.Name);
        Assert.Equal(AnaEmail, contact.Email);
        Assert.Equal(AnaPhone, contact.Phone);
        Assert.Empty(contact.DomainEvents);
    }

    [Fact]
    public void Update_WithBlankName_ThrowsDomainException()
    {
        // Arrange
        var contact = Contact.Create(OwnerId, "Ana", AnaEmail, phone: null);

        // Act
        var update = () => contact.Update("   ", AnaEmail, phone: null);

        // Assert
        Assert.Throws<DomainException>(update);
    }

    [Fact]
    public void Create_WithNullName_ThrowsDomainException()
    {
        // Act
        var create = () => Contact.Create(OwnerId, null!, AnaEmail, phone: null);

        // Assert
        Assert.Throws<DomainException>(create);
    }

    [Fact]
    public void Delete_RaisesContactDeletedEvent()
    {
        // Arrange
        var contact = Contact.Create(OwnerId, "Ana", AnaEmail, phone: null);
        contact.ClearDomainEvents();

        // Act
        contact.Delete();

        // Assert
        var deleted = Assert.IsType<ContactDeletedDomainEvent>(Assert.Single(contact.DomainEvents));
        Assert.Equal(contact.Id, deleted.ContactId);
        Assert.Equal(OwnerId, deleted.OwnerId);
    }
}
