using AuthService.Application.Common;
using AuthService.Domain.Commons;
using FluentValidation;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next(cancellationToken);

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null);

            if (failures.Any())
            {
                var errors = failures.ToValidationErrors();

                if (IsResultType(typeof(TResponse)))
                    return CreateFailureResult(errors);

                throw new ValidationException(failures);
            }

            return await next(cancellationToken);
        }

        private static bool IsResultType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>);
        }

        private static TResponse CreateFailureResult(IReadOnlyCollection<ValidationError> errors)
        {
            var resultType = typeof(TResponse).GetGenericArguments()[0];

            var failureMethod = typeof(Result<>).MakeGenericType(resultType).GetMethod("Failure", new[] { typeof(IReadOnlyCollection<ValidationError>) });

            if (failureMethod == null)
                throw new InvalidOperationException("Result.Failure method not found");

            var result = failureMethod.Invoke(null, [errors]);
            return (TResponse)result!;
        }
    }
}
