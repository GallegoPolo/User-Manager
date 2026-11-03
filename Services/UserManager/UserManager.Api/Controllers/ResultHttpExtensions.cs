using Microsoft.AspNetCore.Mvc;
using UserManager.Application.Common;

namespace UserManager.Api.Controllers;

public static class ResultHttpExtensions
{
    public static IActionResult Failure<T>(this ControllerBase controller, Result<T> result) => result.ErrorType switch
    {
        ErrorType.NotFound => controller.NotFound(result.Errors),
        ErrorType.Conflict => controller.Conflict(result.Errors),
        _ => controller.BadRequest(result.Errors)
    };
}
