using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserManager.Api.Contracts.Requests;
using UserManager.Api.Contracts.Responses;
using UserManager.Application.UseCases.Users.Commands;

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
        public async Task<IActionResult> Create(CreateUserRequest request)
        {
            var result = await _mediator.Send(new CreateUserCommand(request.Name, request.Email));
            
            if (!result.IsSuccess)
                return BadRequest(result.Errors);
            
            var response = new CreateUserResponse(result.Value);
            return CreatedAtAction(nameof(Create), response);
        }
    }
}
