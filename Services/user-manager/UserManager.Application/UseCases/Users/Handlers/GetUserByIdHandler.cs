using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.Queries;
using UserManager.Application.UseCases.Users.Responses;
using UserManager.Domain.Common;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Users.Handlers
{
    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, Result<GetUserByIdResponse>>
    {
        private readonly IUserRepository _repository;

        public GetUserByIdHandler(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<GetUserByIdResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(request.Id);

            if (user == null)
                return Result<GetUserByIdResponse>.Failure(new ValidationError("Id", "User not found"));

            var response = new GetUserByIdResponse(user.Id, user.Name, user.Email, user.CreatedAt);

            return Result<GetUserByIdResponse>.Success(response);
        }
    }
}
