using AuthService.Api.Contracts.Requests;
using AuthService.Api.Contracts.Responses;
using AuthService.Application.UseCases.ApiKeys.Commands;
using AuthService.Application.UseCases.ApiKeys.DTOs;
using AuthService.Application.UseCases.ApiKeys.Queries;
using AuthService.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers
{
    [ApiController]
    [Route("api/auth/api-keys")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateApiKeyResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateApiKeyResponse>> CreateApiKey([FromBody] CreateApiKeyRequest request)
        {
            var command = new CreateApiKeyCommand
            {
                Name = request.Name,
                Scopes = request.Scopes,
                ExpiresAt = request.ExpiresAt
            };

            var result = await _mediator.Send(command);

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

        [HttpPost("validate")]
        [ProducesResponseType(typeof(ValidateApiKeyResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ValidateApiKeyResponse>> ValidateApiKey([FromBody] ValidateApiKeyRequest request)
        {
            var query = new ValidateApiKeyQuery(request.ApiKey);
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
                return Unauthorized(result.Errors);

            var dto = result.Value!;
            var response = new ValidateApiKeyResponse(Token: dto.Token, ExpiresAt: dto.ExpiresAt);

            return Ok(response);
        }

        [HttpGet]
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

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(RevokeApiKeyResponse), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RevokeApiKeyResponse>> RevokeApiKey(Guid id)
        {
            var command = new RevokeApiKeyCommand(id);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return NotFound(result.Errors);

            return NoContent();
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(GetApiKeyByIdResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationError), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GetApiKeyByIdResponse>> GetApiKeyById(Guid id)
        {
            var query = new GetApiKeyByIdQuery(id);
            var result = await _mediator.Send(query);

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
