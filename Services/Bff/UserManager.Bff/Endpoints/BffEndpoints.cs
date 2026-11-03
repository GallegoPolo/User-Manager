using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace UserManager.Bff.Endpoints;

public static class BffEndpoints
{
    public static IEndpointRouteBuilder MapBffEndpoints(this IEndpointRouteBuilder app)
    {
        var bff = app.MapGroup("/bff");

        bff.MapGet("/login", Login).AllowAnonymous();
        bff.MapGet("/logout", LogoutAsync).AllowAnonymous();
        bff.MapGet("/user", CurrentUser).RequireAuthorization();

        return app;
    }

    public static string SafeReturnUrl(string? returnUrl)
    {
        var isLocalPath = returnUrl is not null
            && returnUrl.StartsWith('/')
            && !returnUrl.StartsWith("//")
            && !returnUrl.StartsWith("/\\")
            && !returnUrl.Any(char.IsControl);

        return isLocalPath ? returnUrl! : "/";
    }

    private static IResult Login(string? returnUrl) =>
        Results.Challenge(new AuthenticationProperties { RedirectUri = SafeReturnUrl(returnUrl) }, [OpenIdConnectDefaults.AuthenticationScheme]);

    private static async Task<IResult> LogoutAsync(HttpContext context, string? sid)
    {
        var session = await context.AuthenticateAsync();

        if (!session.Succeeded)
            return Results.Redirect("/");

        if (string.IsNullOrEmpty(sid) || sid != session.Principal!.FindFirstValue("sid"))
            return Results.BadRequest();

        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
    }

    private static IResult CurrentUser(ClaimsPrincipal user) =>
        Results.Ok(new BffUserResponse(
            user.FindFirstValue("sub")!,
            user.FindFirstValue("name"),
            user.FindFirstValue("email"),
            user.FindAll("roles").Select(role => role.Value).ToArray(),
            $"/bff/logout?sid={Uri.EscapeDataString(user.FindFirstValue("sid") ?? string.Empty)}"));
}
