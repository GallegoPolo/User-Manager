using MediatR;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;
using UserManager.Domain.ValueObjects;

namespace UserManager.Application.UseCases.Users.Handlers;

public sealed class ProvisionCurrentUserHandler : IRequestHandler<ProvisionCurrentUserCommand, Result<Guid>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public ProvisionCurrentUserHandler(ICurrentUser currentUser, IUserRepository users, IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(ProvisionCurrentUserCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.IdentityProviderId))
            return Result<Guid>.Failure("IdentityProviderId", "O token não identifica a pessoa (sub).");

        if (!Email.IsValid(_currentUser.Email))
            return Result<Guid>.Failure("Email", "O token não traz um e-mail válido.");

        var email = Email.Create(_currentUser.Email!);
        var existing = await _users.GetByIdentityProviderIdAsync(_currentUser.IdentityProviderId, cancellationToken);

        if (existing is not null)
            return await SyncEmailAsync(existing, email, cancellationToken);

        return await ProvisionAsync(email, cancellationToken);
    }

    private async Task<Result<Guid>> SyncEmailAsync(User user, Email email, CancellationToken cancellationToken)
    {
        if (user.SyncEmail(email))
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }

    private async Task<Result<Guid>> ProvisionAsync(Email email, CancellationToken cancellationToken)
    {
        var user = User.Provision(_currentUser.IdentityProviderId, DisplayNameFor(email), email);
        await _users.AddAsync(user);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(user.Id);
        }
        catch (UniqueConstraintViolationException)
        {
            var winner = await _users.GetByIdentityProviderIdAsync(_currentUser.IdentityProviderId, cancellationToken);

            if (winner is null)
                throw;

            return Result<Guid>.Success(winner.Id);
        }
    }

    private string DisplayNameFor(Email email)
    {
        var name = string.IsNullOrWhiteSpace(_currentUser.Name) ? email.Value : _currentUser.Name.Trim();

        return name.Length > User.MAX_NAME_LENGTH ? name[..User.MAX_NAME_LENGTH] : name;
    }
}
