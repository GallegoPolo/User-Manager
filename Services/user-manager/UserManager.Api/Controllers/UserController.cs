using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Handlers;

namespace UserManager.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        public UsersController(CreateUserHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserRequest request)
        {
            var result = await _handler.Handle(
                new CreateUserCommand(request.Name, request.Email)
            );

            return CreatedAtAction(nameof(Create), new { id = result.UserId }, result);
        }
    }
}
