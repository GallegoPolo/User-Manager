using AuthService.Api.Contracts.Requests;
using AuthService.Api.Contracts.Responses;
using AuthService.Application.UseCases.ApiKeys.Commands;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using AuthService.Application.UseCases.ApiKeys.Queries;
using AuthService.Domain.Commons;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokenResponse>> LoginAdmin([FromBody] LoginAdminRequest request, CancellationToken ct)
        {
            var command = new LoginAdminCommand(request.Email, request.Password);
            var result = await _mediator.Send(command, ct);

            if (!result.IsSuccess)
                return Unauthorized(result.Errors);

            var dto = result.Value!;
            var response = new TokenResponse(dto.AccessToken, dto.TokenType, dto.ExpiresIn);

            return Ok(response);
        }

        [HttpPost("token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokenResponse>> AuthenticateApiKey([FromHeader(Name = "X-API-KEY")] string apiKey, CancellationToken ct)
        {
            var command = new AuthenticateApiKeyCommand(apiKey);
            var result = await _mediator.Send(command, ct);

            if (!result.IsSuccess)
                return Unauthorized(result.Errors);

            var dto = result.Value!;
            var response = new TokenResponse(dto.AccessToken, dto.TokenType, dto.ExpiresIn);

            return Ok(response);
        }

        [HttpPost("admins")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(CreateAdminResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateAdminResponse>> CreateAdmin([FromBody] CreateAdminRequest request, CancellationToken ct)
        {
            var command = new CreateAdminCommand(request.Email, request.Password);
            var result = await _mediator.Send(command, ct);

            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            var response = new CreateAdminResponse(result.Value, request.Email);

            return CreatedAtAction(nameof(CreateAdmin), new { id = result.Value }, response);
        }

        [HttpPost("api-keys")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(CreateApiKeyResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateApiKeyResponse>> CreateApiKey([FromBody] CreateApiKeyRequest request, CancellationToken ct)
        {
            var command = new CreateApiKeyCommand(request.Name,
                                                  request.Scopes,
                                                  request.ExpiresAt);

            var result = await _mediator.Send(command, ct);

            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            var dto = result.Value!;
            var response = new CreateApiKeyResponse(Id: dto.Id,
                                                    Name: dto.Name,
                                                    ApiKey: dto.ApiKey,
                                                    Scopes: dto.Scopes,
                                                    Status: dto.Status,
                                                    ExpiresAt: dto.ExpiresAt,
                                                    CreatedAt: dto.CreatedAt
            );

            return CreatedAtAction(nameof(GetApiKeyById), new { id = dto.Id }, response);
        }

        [HttpPost("api-keys/validate")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ValidateApiKeyResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ValidateApiKeyResponse>> ValidateApiKey([FromBody] ValidateApiKeyRequest request, CancellationToken ct)
        {
            var query = new ValidateApiKeyQuery(request.ApiKey);
            var result = await _mediator.Send(query, ct);

            if (!result.IsSuccess)
                return Unauthorized(result.Errors);

            var dto = result.Value!;
            var response = new ValidateApiKeyResponse(Token: dto.Token, ExpiresAt: dto.ExpiresAt);

            return Ok(response);
        }

        [HttpGet("api-keys")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(IEnumerable<ApiKeyDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<ApiKeyDTO>>> ListApiKeys()
        {
            var query = new ListApiKeysQuery();
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            return Ok(result.Value);
        }

        [HttpDelete("api-keys/{id}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(RevokeApiKeyResponse), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RevokeApiKeyResponse>> RevokeApiKey(Guid id, CancellationToken ct)
        {
            var command = new RevokeApiKeyCommand(id);
            var result = await _mediator.Send(command, ct);

            if (!result.IsSuccess)
                return NotFound(result.Errors);

            return NoContent();
        }

        [HttpGet("api-keys/{id}")]
        [Authorize(Policy = "AdminOnly")]
        [ProducesResponseType(typeof(GetApiKeyByIdResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GetApiKeyByIdResponse>> GetApiKeyById(Guid id, CancellationToken ct)
        {
            var query = new GetApiKeyByIdQuery(id);
            var result = await _mediator.Send(query, ct);

            if (!result.IsSuccess)
                return NotFound(result.Errors);

            var dto = result.Value!;
            var response = new GetApiKeyByIdResponse(Id: dto.Id,
                                                     Name: dto.Name,
                                                     Scopes: dto.Scopes,
                                                     Status: dto.Status,
                                                     ExpiresAt: dto.ExpiresAt,
                                                     CreatedAt: dto.CreatedAt);
            return Ok(response);
        }
    }
}