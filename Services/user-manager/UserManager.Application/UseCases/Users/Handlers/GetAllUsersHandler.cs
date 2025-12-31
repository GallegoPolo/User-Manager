using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.DTOs;
using UserManager.Application.UseCases.Users.Queries;
using UserManager.Domain.Interfaces;

namespace UserManager.Application.UseCases.Users.Handlers
{
    public class GetAllUsersHandler : IRequestHandler<GetAllUsersQuery, Result<IEnumerable<UserDTO>>>
    {
        private readonly IUserRepository _repository;

        public GetAllUsersHandler(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IEnumerable<UserDTO>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _repository.GetAllAsync();

            var usersDTO = users.Select(user => new UserDTO(user.Id, user.Name, user.Email, user.CreatedAt));

            return Result<IEnumerable<UserDTO>>.Success(usersDTO);
        }
    }
}
