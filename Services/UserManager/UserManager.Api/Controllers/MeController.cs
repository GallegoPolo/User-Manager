using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManager.Api.Contracts.Requests;
using UserManager.Api.Contracts.Responses;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Queries;

namespace UserManager.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);

        if (!result.IsSuccess)
            return this.Failure(result);

        var user = result.Value!;
        return Ok(new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt));
    }

    [HttpPut]
    public async Task<IActionResult> Rename([FromBody] RenameUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RenameCurrentUserCommand(request.Name), cancellationToken);

        if (!result.IsSuccess)
            return this.Failure(result);

        return NoContent();
    }
}
