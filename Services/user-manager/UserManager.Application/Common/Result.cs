using UserManager.Domain.Comon;

namespace UserManager.Application.Comon
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public IReadOnlyCollection<ValidationError> Errors { get; }

        private Result(bool isSuccess, T? value, IReadOnlyCollection<ValidationError>? errors)
        {
            IsSuccess = isSuccess;
            Value = value;
            Errors = errors ?? Array.Empty<ValidationError>();
        }

        public static Result<T> Success(T value)
            => new(true, value, null);

        public static Result<T> Failure(IReadOnlyCollection<ValidationError> errors)
            => new(false, default, errors);

        public static Result<T> Failure(ValidationError error)
            => new(false, default, [error]);
    }
}
