using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UseCases.Contacts.Commands;

public static class ContactDetailsExtensions
{
    public static Email? ToEmail(this IContactDetails details) =>
        string.IsNullOrWhiteSpace(details.Email) ? null : Email.Create(details.Email);

    public static PhoneNumber? ToPhoneNumber(this IContactDetails details) =>
        string.IsNullOrWhiteSpace(details.Phone) ? null : PhoneNumber.Create(details.Phone);
}
