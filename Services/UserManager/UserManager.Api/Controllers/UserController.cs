using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserManager.Api.Contracts.Requests;
using UserManager.Api.Contracts.Responses;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Queries;

namespace UserManager.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            var result = await _mediator.Send(new CreateUserCommand(request.Name, request.Email));

            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            var response = new CreateUserResponse(result.Value);
            return CreatedAtAction(nameof(Create), response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetUserByIdQuery(id));

            if (!result.IsSuccess)
                return NotFound(result.Errors);

            var response = new GetUserByIdResponse(result.Value!.Id, result.Value.Name, result.Value.Email, result.Value.CreatedAt);

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllUsersQuery());

            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            var response = result.Value!.Select(dto => new GetAllUsersResponse(dto.Id, dto.Name, dto.Email, dto.CreatedAt));

            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            var command = new UpdateUserCommand(id, request.Name, request.Email);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return BadRequest(result.Errors);

            var response = new UpdateUserResponse(result.Value);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteUserCommand(id);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
                return NotFound(result.Errors);

            return NoContent();
        }
    }
}
