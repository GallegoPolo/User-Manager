using System.Diagnostics;
using System.Text.Json;
using MediatR;
using UserManager.Api.Contracts.Errors;
using UserManager.Application.UseCases.Users.Commands;

namespace UserManager.Api.Identity;

public sealed class ProvisionCurrentUserMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;

    public ProvisionCurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMediator mediator, CurrentUser currentUser)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var result = await mediator.Send(new ProvisionCurrentUserCommand(), context.RequestAborted);

        if (!result.IsSuccess)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new ErrorResponse("Forbidden", StatusCodes.Status403Forbidden, result.Errors, Activity.Current?.Id ?? context.TraceIdentifier, DateTime.UtcNow, null),
                JsonOptions,
                context.RequestAborted);
            return;
        }

        currentUser.MarkProvisioned(result.Value);
        await _next(context);
    }
}
