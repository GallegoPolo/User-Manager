using FluentValidation.Results;
using UserManager.Domain.Common;

namespace UserManager.Application.Common
{
    public static class ResultExtensions
    {
        public static IReadOnlyCollection<ValidationError> ToValidationErrors(this IEnumerable<ValidationFailure> failures)
        {
            return failures.Select(f => new ValidationError(f.PropertyName, f.ErrorMessage)).ToList();
        }
    }
}
