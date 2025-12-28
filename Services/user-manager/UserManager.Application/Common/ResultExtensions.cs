using FluentValidation.Results;
using Flunt.Notifications;
using UserManager.Domain.Common;

namespace UserManager.Application.Common
{
    public static class ResultExtensions
    {
        public static IReadOnlyCollection<ValidationError> ToValidationErrors(this IEnumerable<ValidationFailure> failures)
        {
            return (IReadOnlyCollection<ValidationError>)failures.Select(f => new ValidationError(f.PropertyName, f.ErrorMessage));
        }

        public static IReadOnlyCollection<ValidationError> ToValidationErrors(this IEnumerable<Notification> notifications)
        {
            return (IReadOnlyCollection<ValidationError>)notifications.Select(n => new ValidationError(n.Key, n.Message));
        }
    }
}
