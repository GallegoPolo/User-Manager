using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.DTOs;
using UserManager.Application.UseCases.Users.Queries;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Users.Handlers
{
    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, Result<UserDTO>>
    {
        private readonly IUserRepository _repository;

        public GetUserByIdHandler(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<UserDTO>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(request.Id);

            if (user == null)
                return Result<UserDTO>.NotFound("Id", "Usuário não encontrado.");

            var response = new UserDTO(user.Id, user.Name, user.Email.Value, user.CreatedAt);

            return Result<UserDTO>.Success(response);
        }
    }
}
