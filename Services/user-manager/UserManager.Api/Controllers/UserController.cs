using Microsoft.AspNetCore.Mvc;
using UserManager.Api.Contracts.Requests;
using UserManager.Api.Contracts.Responses;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Handlers;

namespace UserManager.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly CreateUserHandler _handler;

        public UserController(CreateUserHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserRequest request)
        {
            var result = await _handler.Handle(new CreateUserCommand(request.Name, request.Email));

            if (result.Errors != null && result.Errors.Any())
                return BadRequest(result.Errors);

            if (result.UserId == null)
                return StatusCode(StatusCodes.Status500InternalServerError, "UserId was not generated.");

            var response = new CreateUserResponse(result.UserId.Value);
            return CreatedAtAction(nameof(Create), response);
        }
    }
}
