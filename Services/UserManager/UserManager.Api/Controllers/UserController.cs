using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManager.Api.Contracts.Requests;
using UserManager.Api.Contracts.Responses;
using UserManager.Api.Identity;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Queries;

namespace UserManager.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.UsersRead)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllUsersQuery(), cancellationToken);

        if (!result.IsSuccess)
            return this.Failure(result);

        return Ok(result.Value!.Select(user => new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.UsersRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);

        if (!result.IsSuccess)
            return this.Failure(result);

        var user = result.Value!;
        return Ok(new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<IActionResult> Rename(Guid id, [FromBody] RenameUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RenameUserCommand(id, request.Name), cancellationToken);

        if (!result.IsSuccess)
            return this.Failure(result);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteUserCommand(id), cancellationToken);

        if (!result.IsSuccess)
            return this.Failure(result);

        return NoContent();
    }
}
