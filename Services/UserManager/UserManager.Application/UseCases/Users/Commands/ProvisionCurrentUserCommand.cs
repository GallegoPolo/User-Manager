using MediatR;
using UserManager.Application.Common;

namespace UserManager.Application.UseCases.Users.Commands;

public sealed record ProvisionCurrentUserCommand : IRequest<Result<Guid>>;
