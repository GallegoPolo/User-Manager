using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.DTOs;

namespace UserManager.Application.UseCases.Users.Queries;

public sealed record GetCurrentUserQuery : IRequest<Result<UserDTO>>;
