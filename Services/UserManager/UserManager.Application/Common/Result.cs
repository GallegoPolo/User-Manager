using UserManager.Domain.Common;

namespace UserManager.Application.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public ErrorType ErrorType { get; }
        public IReadOnlyCollection<ValidationError> Errors { get; }

        private Result(bool isSuccess, T? value, ErrorType errorType, IReadOnlyCollection<ValidationError>? errors)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorType = errorType;
            Errors = errors ?? Array.Empty<ValidationError>();
        }

        public static Result<T> Success(T value)
            => new(true, value, ErrorType.None, null);

        public static Result<T> Failure(IReadOnlyCollection<ValidationError> errors)
            => new(false, default, ErrorType.Validation, errors);

        public static Result<T> Failure(ValidationError error)
            => new(false, default, ErrorType.Validation, [error]);

        public static Result<T> Failure(string propertyName, string errorMessage)
            => new(false, default, ErrorType.Validation, [new ValidationError(propertyName, errorMessage)]);

        public static Result<T> NotFound(string propertyName, string errorMessage)
            => new(false, default, ErrorType.NotFound, [new ValidationError(propertyName, errorMessage)]);

        public static Result<T> Conflict(string propertyName, string errorMessage)
            => new(false, default, ErrorType.Conflict, [new ValidationError(propertyName, errorMessage)]);
    }
}
