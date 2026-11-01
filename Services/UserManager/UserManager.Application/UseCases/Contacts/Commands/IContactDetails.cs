namespace UserManager.Application.UseCases.Contacts.Commands;

public interface IContactDetails
{
    string Name { get; }
    string? Email { get; }
    string? Phone { get; }
}
