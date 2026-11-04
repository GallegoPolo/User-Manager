using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace UserManager.Infrastructure.IntegrationTests.Fixtures;

public sealed partial class BrokerBrowser : IDisposable
{
    private const int MaxRedirects = 20;

    private readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
    private readonly Dictionary<string, string> _cookies = [];
    private readonly string _keycloakUrl;

    public BrokerBrowser(string keycloakUrl)
    {
        _keycloakUrl = keycloakUrl;
        CodeVerifier = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
    }

    public string CodeVerifier { get; }

    public string Html { get; private set; } = string.Empty;

    public string? AuthorizationCode { get; private set; }

    public async Task<Uri> ClickProviderButtonAsync(string alias)
    {
        await OpenLoginPageAsync();

        var startBroker = await SendAsync(HttpMethod.Get, Absolute(ProviderButtonHref(alias)));

        return startBroker.Headers.Location
            ?? throw new InvalidOperationException($"Sem redirecionamento para {alias}: {startBroker.StatusCode}");
    }

    public async Task SignInWithGitHubAsync()
    {
        var authorize = await ClickProviderButtonAsync("github");
        var state = QueryValue(authorize, "state");

        await NavigateAsync($"{_keycloakUrl}/realms/{KeycloakFixture.RealmName}/broker/github/endpoint?code=fake-code&state={Uri.EscapeDataString(state)}");
    }

    public async Task SubmitFirstFormAsync(params (string Name, string Value)[] fields)
    {
        var form = FormAction().Match(Html);
        if (!form.Success)
            throw new InvalidOperationException("A página atual não tem formulário.");

        var action = WebUtility.HtmlDecode(form.Groups[1].Value);
        var content = new FormUrlEncodedContent(fields.ToDictionary(field => field.Name, field => field.Value));

        await FollowAsync(await SendAsync(HttpMethod.Post, Absolute(action), content));
    }

    public Task OpenLinkAsync(string url) => NavigateAsync(url);

    public async Task ClickForgotPasswordAsync()
    {
        var match = ForgotPasswordLink().Match(Html);
        if (!match.Success)
            throw new InvalidOperationException("A página atual não tem o link de esqueci a senha.");

        await NavigateAsync(Absolute(WebUtility.HtmlDecode(match.Groups[1].Value)));
    }

    public async Task OpenLoginPageAsync()
    {
        var challenge = Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(CodeVerifier)));
        var query = string.Join('&',
            $"client_id={KeycloakFixture.BffClientId}",
            $"redirect_uri={Uri.EscapeDataString(KeycloakFixture.BffRedirectUri)}",
            "response_type=code",
            "scope=openid",
            "state=test-state",
            $"code_challenge={challenge}",
            "code_challenge_method=S256");

        await NavigateAsync($"{_keycloakUrl}/realms/{KeycloakFixture.RealmName}/protocol/openid-connect/auth?{query}");
    }

    public void Dispose() => _http.Dispose();

    private async Task NavigateAsync(string url) => await FollowAsync(await SendAsync(HttpMethod.Get, url));

    private async Task FollowAsync(HttpResponseMessage response)
    {
        for (var hops = 0; IsRedirect(response.StatusCode); hops++)
        {
            if (hops == MaxRedirects)
                throw new InvalidOperationException($"Mais de {MaxRedirects} redirecionamentos.");

            var location = response.Headers.Location!.IsAbsoluteUri
                ? response.Headers.Location
                : new Uri(response.RequestMessage!.RequestUri!, response.Headers.Location);

            if (location.AbsoluteUri.StartsWith(KeycloakFixture.BffRedirectUri, StringComparison.Ordinal))
            {
                AuthorizationCode = QueryValue(location, "code");
                Html = string.Empty;
                return;
            }

            response = await SendAsync(HttpMethod.Get, location.AbsoluteUri);
        }

        Html = await response.Content.ReadAsStringAsync();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        if (_cookies.Count > 0)
            request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));

        var response = await _http.SendAsync(request);
        KeepCookies(response);

        return response;
    }

    private void KeepCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return;

        foreach (var cookie in cookies)
        {
            var nameAndValue = cookie.Split(';', 2)[0].Split('=', 2);
            if (cookie.Contains("Max-Age=0", StringComparison.OrdinalIgnoreCase))
                _cookies.Remove(nameAndValue[0]);
            else
                _cookies[nameAndValue[0]] = nameAndValue[1];
        }
    }

    private string ProviderButtonHref(string alias)
    {
        var match = Regex.Match(Html, $@"id=""social-{alias}""[^>]*?href=""([^""]+)""", RegexOptions.Singleline);
        if (!match.Success)
            throw new InvalidOperationException($"A tela de login não tem o botão de {alias}.");

        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private string Absolute(string url) => url.StartsWith("http", StringComparison.Ordinal) ? url : _keycloakUrl + url;

    private static bool IsRedirect(HttpStatusCode status) => (int)status is >= 300 and < 400;

    private static string QueryValue(Uri uri, string name)
    {
        var pair = uri.Query.TrimStart('?').Split('&').FirstOrDefault(part => part.StartsWith(name + "=", StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Sem {name} em {uri}.");

        return Uri.UnescapeDataString(pair[(name.Length + 1)..]);
    }

    [GeneratedRegex(@"href=""([^""]*reset-credentials[^""]*)""")]
    private static partial Regex ForgotPasswordLink();

    [GeneratedRegex(@"<form[^>]*?action=""([^""]+)""", RegexOptions.Singleline)]
    private static partial Regex FormAction();
}
