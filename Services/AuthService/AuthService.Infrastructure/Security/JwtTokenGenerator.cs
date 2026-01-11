using AuthService.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Infrastructure.Security
{
    public class JwtTokenGenerator : ITokenGenerator
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly int _expirationMinutes;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
            _secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            _issuer = _configuration["Jwt:Issuer"] ?? "AuthService";
            _expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
        }

        public Task<string> GenerateTokenAsync(string subject,
                                               IEnumerable<string> roles,
                                               IEnumerable<string> scopes,
                                               Dictionary<string, string>? additionalClaims = null,
                                               CancellationToken cancellationToken = default)
        {
            var claims = new List<Claim>{new Claim(JwtRegisteredClaimNames.Sub, subject),
                                         new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                                         new Claim(JwtRegisteredClaimNames.Iat,
                                         new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                                         ClaimValueTypes.Integer64)};

            foreach (var role in roles ?? Enumerable.Empty<string>())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            foreach (var scope in scopes ?? Enumerable.Empty<string>())
            {
                claims.Add(new Claim("scope", scope));
            }

            if (additionalClaims != null)
            {
                foreach (var claim in additionalClaims)
                {
                    claims.Add(new Claim(claim.Key, claim.Value));
                }
            }

            if (additionalClaims == null || !additionalClaims.ContainsKey("correlation_id"))
            {
                claims.Add(new Claim("correlation_id", Guid.NewGuid().ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _configuration["Jwt:Audience"] ?? "AuthService",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            return Task.FromResult(tokenHandler.WriteToken(token));
        }
    }
}
