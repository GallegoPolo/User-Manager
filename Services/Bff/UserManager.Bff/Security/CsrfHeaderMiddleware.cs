namespace UserManager.Bff.Security;

public sealed class CsrfHeaderMiddleware
{
    public const string HeaderName = "X-CSRF";

    private readonly RequestDelegate _next;

    public CsrfHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresHeader(context.Request.Path) && context.Request.Headers[HeaderName] != "1")
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = $"Missing {HeaderName} header." }, context.RequestAborted);
            return;
        }

        await _next(context);
    }

    private static bool RequiresHeader(PathString path) =>
        path.StartsWithSegments("/api") || path.StartsWithSegments("/bff/user");
}
