using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace UserManager.Bff.Authentication;

public sealed class TokenRefresher
{
    public static readonly TimeSpan TokenEndpointTimeout = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan RefreshAhead = TimeSpan.FromSeconds(60);

    private readonly HttpClient _http;
    private readonly IOptionsMonitor<OpenIdConnectOptions> _oidcOptions;
    private readonly TimeProvider _timeProvider;

    public TokenRefresher(HttpClient http, IOptionsMonitor<OpenIdConnectOptions> oidcOptions, TimeProvider timeProvider)
    {
        _http = http;
        _oidcOptions = oidcOptions;
        _timeProvider = timeProvider;
    }

    public async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = ParseExpiresAt(context.Properties.GetTokenValue("expires_at"));

        if (expiresAt - now > RefreshAhead)
            return;

        var refreshToken = context.Properties.GetTokenValue("refresh_token");

        if (refreshToken is null)
        {
            context.RejectPrincipal();
            return;
        }

        var refresh = await RequestNewTokensAsync(refreshToken, context.HttpContext.RequestAborted);

        switch (refresh.Outcome)
        {
            case RefreshOutcome.Unavailable when expiresAt > now:
                return;
            case RefreshOutcome.Refreshed when TryReadTokens(refresh.Body, out var accessToken, out var expiresIn):
                context.Properties.UpdateTokenValue("access_token", accessToken);
                context.Properties.UpdateTokenValue("refresh_token", ReadString(refresh.Body, "refresh_token") ?? refreshToken);
                context.Properties.UpdateTokenValue("expires_at", now.AddSeconds(expiresIn).ToString("o", CultureInfo.InvariantCulture));
                context.ShouldRenew = true;
                return;
            default:
                context.RejectPrincipal();
                return;
        }
    }

    private async Task<(RefreshOutcome Outcome, JsonElement Body)> RequestNewTokensAsync(string refreshToken, CancellationToken cancellationToken)
    {
        try
        {
            var options = _oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
            var configuration = await options.ConfigurationManager!.GetConfigurationAsync(cancellationToken);

            using var response = await _http.PostAsync(configuration.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = options.ClientId!,
                ["client_secret"] = options.ClientSecret!
            }), cancellationToken);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
                return (RefreshOutcome.Rejected, default);

            if (!response.IsSuccessStatusCode)
                return (RefreshOutcome.Unavailable, default);

            return (RefreshOutcome.Refreshed, await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return (RefreshOutcome.Unavailable, default);
        }
    }

    private static DateTimeOffset ParseExpiresAt(string? expiresAt) =>
        DateTimeOffset.TryParse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : DateTimeOffset.MinValue;

    private static bool TryReadTokens(JsonElement body, out string accessToken, out int expiresIn)
    {
        accessToken = ReadString(body, "access_token") ?? string.Empty;
        expiresIn = body.TryGetProperty("expires_in", out var value) && value.TryGetInt32(out var seconds) ? seconds : 0;

        return accessToken.Length > 0 && expiresIn > 0;
    }

    private static string? ReadString(JsonElement body, string property) =>
        body.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private enum RefreshOutcome
    {
        Refreshed,
        Rejected,
        Unavailable
    }
}
