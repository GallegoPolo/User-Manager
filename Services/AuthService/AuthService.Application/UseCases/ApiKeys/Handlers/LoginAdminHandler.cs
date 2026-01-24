using AuthService.Application.Common;
using AuthService.Application.UseCases.ApiKeys.Commands;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using AuthService.Domain.Interfaces.Repositories;
using MediatR;

namespace AuthService.Application.UseCases.ApiKeys.Handlers
{
    public class LoginAdminHandler : IRequestHandler<LoginAdminCommand, Result<TokenDTO>>
    {
        private readonly IUserCredentialRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IUnitOfWork _unitOfWork;

        public LoginAdminHandler(IUserCredentialRepository userRepository, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenGenerator = tokenGenerator;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<TokenDTO>> Handle(LoginAdminCommand request, CancellationToken cancellationToken)
        {
            var userCredential = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

            if (userCredential == null)
                return Result<TokenDTO>.Failure("User", "Invalid email or password");

            if (!userCredential.IsActive)
                return Result<TokenDTO>.Failure(userCredential.Notifications.ToValidationErrors());

            var isPasswordValid = _passwordHasher.Verify(request.Password, userCredential.PasswordHash);

            if (!isPasswordValid)
                return Result<TokenDTO>.Failure("Password", "Invalid email or password");

            userCredential.UpdateLastLogin();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var tokenResult = await _tokenGenerator.GenerateTokenAsync(subject: userCredential.Id.ToString(),
                                                                       roles: new[] { userCredential.Role },
                                                                       scopes: new[] { "admin" },
                                                                       additionalClaims: null,
                                                                       cancellationToken: cancellationToken);

            var expiresIn = (int)(tokenResult.ExpiresAt - DateTime.UtcNow).TotalSeconds;

            var tokenDto = new TokenDTO(AccessToken: tokenResult.Token, TokenType: "Bearer", ExpiresIn: expiresIn);

            return Result<TokenDTO>.Success(tokenDto);
        }
    }
}