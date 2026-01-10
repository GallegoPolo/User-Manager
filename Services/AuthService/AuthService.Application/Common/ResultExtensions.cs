using AuthService.Domain.Common;
using FluentValidation.Results;
using Flunt.Notifications;

namespace AuthService.Application.Common
{
    public static class ResultExtensions
    {
        public static IReadOnlyCollection<ValidationError> ToValidationErrors(this IEnumerable<ValidationFailure> failures)
        {
            return failures.Select(f => new ValidationError(f.PropertyName, f.ErrorMessage)).ToList().AsReadOnly();
        }

        public static IReadOnlyCollection<ValidationError> ToValidationErrors(this IEnumerable<Notification> notifications)
        {
            return notifications.Select(n => new ValidationError(n.Key, n.Message)).ToList().AsReadOnly();
        }
    }
}
