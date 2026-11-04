using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.Keycloak;

namespace UserManager.Infrastructure.IntegrationTests.Fixtures;

public sealed partial class KeycloakBrokerFixture : IAsyncLifetime
{
    private const string RealmName = KeycloakFixture.RealmName;
    private const string FakeGitHubUrl = "http://fake-github:8080";
    private const string FakeClientId = "fake-client-id";
    private const string FakeClientSecret = "fake-client-secret";
    private const int WireMockPort = 8080;
    private const int MailpitPort = 8025;

    private readonly INetwork _network = new NetworkBuilder().Build();
    private readonly IContainer _fakeGitHub;
    private readonly IContainer _mailpit;
    private readonly KeycloakContainer _keycloak;
    private readonly string _realmDirectory = Directory.CreateTempSubdirectory("user-manager-realm-").FullName;

    private HttpClient _admin = null!;
    private HttpClient _wireMock = null!;
    private HttpClient _mail = null!;

    public KeycloakBrokerFixture()
    {
        _fakeGitHub = new ContainerBuilder("wiremock/wiremock:3.13.2")
            .WithNetwork(_network)
            .WithNetworkAliases("fake-github")
            .WithPortBinding(WireMockPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(WireMockPort).ForPath("/__admin/health")))
            .Build();

        _mailpit = new ContainerBuilder("axllent/mailpit:v1.33")
            .WithNetwork(_network)
            .WithNetworkAliases("mailpit")
            .WithPortBinding(MailpitPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(MailpitPort).ForPath("/api/v1/info")))
            .Build();

        _keycloak = new KeycloakBuilder(KeycloakFixture.Image)
            .WithNetwork(_network)
            .WithRealm(WriteRealmPointingToFakeGitHub())
            .WithEnvironment("GITHUB_ENABLED", "true")
            .WithEnvironment("GITHUB_CLIENT_ID", FakeClientId)
            .WithEnvironment("GITHUB_CLIENT_SECRET", FakeClientSecret)
            .Build();
    }

    public string KeycloakUrl => _keycloak.GetBaseAddress().TrimEnd('/');

    public async Task InitializeAsync()
    {
        await _network.CreateAsync();
        await Task.WhenAll(_fakeGitHub.StartAsync(), _mailpit.StartAsync());
        await _keycloak.StartAsync();

        _wireMock = new HttpClient { BaseAddress = new Uri($"http://{_fakeGitHub.Hostname}:{_fakeGitHub.GetMappedPublicPort(WireMockPort)}") };
        _mail = new HttpClient { BaseAddress = new Uri($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(MailpitPort)}") };
        _admin = new HttpClient { BaseAddress = new Uri(KeycloakUrl) };
        _admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());
    }

    public async Task DisposeAsync()
    {
        _admin?.Dispose();
        _wireMock?.Dispose();
        _mail?.Dispose();
        await _keycloak.DisposeAsync();
        await _mailpit.DisposeAsync();
        await _fakeGitHub.DisposeAsync();
        await _network.DisposeAsync();
        Directory.Delete(_realmDirectory, recursive: true);
    }

    public async Task GitHubAnswersAsAsync(long id, string login, string? name, string email)
    {
        (await _wireMock.DeleteAsync("/__admin/mappings")).EnsureSuccessStatusCode();

        await StubTokenEndpointAsync();
        await StubAsync("GET", "/user",JsonSerializer.Serialize(new { id, login, name, email = (string?)null }), "application/json");
        await StubAsync("GET", "/user/emails", JsonSerializer.Serialize(new[] { new { email, primary = true, verified = false } }), "application/json");
    }

    public async Task<JsonElement> FindUserByEmailAsync(string email)
    {
        var users = await _admin.GetFromJsonAsync<JsonElement>($"admin/realms/{RealmName}/users?email={Uri.EscapeDataString(email)}&exact=true");
        return users.GetArrayLength() == 0 ? default : users[0];
    }

    public async Task<int> CountFederatedIdentitiesAsync(string userId)
    {
        var links = await _admin.GetFromJsonAsync<JsonElement>($"admin/realms/{RealmName}/users/{userId}/federated-identity");
        return links.GetArrayLength();
    }

    public async Task<string> WaitForActionLinkAsync(string recipient)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var link = await FindActionLinkAsync(recipient);
            if (link is not null)
                return link;

            await Task.Delay(250);
        }

        throw new TimeoutException($"Nenhum e-mail com link de ação para {recipient}.");
    }

    public async Task<int> CountMailsToAsync(string recipient)
    {
        var inbox = await _mail.GetFromJsonAsync<JsonElement>("/api/v1/messages");
        return inbox.GetProperty("messages").EnumerateArray().Count(message => IsAddressedTo(message, recipient));
    }

    public async Task<JsonElement> ExchangeCodeAsync(string code, string codeVerifier)
    {
        using var http = new HttpClient { BaseAddress = new Uri(KeycloakUrl) };
        var response = await http.PostAsync($"realms/{RealmName}/protocol/openid-connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = KeycloakFixture.BffClientId,
            ["client_secret"] = KeycloakFixture.BffClientSecret,
            ["redirect_uri"] = KeycloakFixture.BffRedirectUri,
            ["code"] = code,
            ["code_verifier"] = codeVerifier
        }));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var payload = body.GetProperty("access_token").GetString()!.Split('.')[1];

        return JsonSerializer.Deserialize<JsonElement>(Base64Url.DecodeFromChars(payload));
    }

    private async Task<string?> FindActionLinkAsync(string recipient)
    {
        var inbox = await _mail.GetFromJsonAsync<JsonElement>("/api/v1/messages");

        foreach (var message in inbox.GetProperty("messages").EnumerateArray().Where(message => IsAddressedTo(message, recipient)))
        {
            var detail = await _mail.GetFromJsonAsync<JsonElement>($"/api/v1/message/{message.GetProperty("ID").GetString()}");
            var match = ActionLink().Match(detail.GetProperty("Text").GetString()!);
            if (match.Success)
                return match.Value;
        }

        return null;
    }

    private static bool IsAddressedTo(JsonElement message, string recipient) =>
        message.GetProperty("To").EnumerateArray().Any(to => string.Equals(to.GetProperty("Address").GetString(), recipient, StringComparison.OrdinalIgnoreCase));

    private async Task StubAsync(string method, string path, string body, string contentType)
    {
        var mapping = new
        {
            request = new { method, urlPath = path },
            response = new { status = 200, body, headers = new Dictionary<string, string> { ["Content-Type"] = contentType } }
        };

        (await _wireMock.PostAsJsonAsync("/__admin/mappings", mapping)).EnsureSuccessStatusCode();
    }

    private async Task StubTokenEndpointAsync()
    {
        var mapping = new
        {
            request = new
            {
                method = "POST",
                urlPath = "/login/oauth/access_token",
                bodyPatterns = new object[]
                {
                    new { contains = $"client_id={FakeClientId}" },
                    new { contains = $"client_secret={FakeClientSecret}" }
                }
            },
            response = new
            {
                status = 200,
                body = "access_token=fake-token&token_type=bearer",
                headers = new Dictionary<string, string> { ["Content-Type"] = "application/x-www-form-urlencoded" }
            }
        };

        (await _wireMock.PostAsJsonAsync("/__admin/mappings", mapping)).EnsureSuccessStatusCode();
    }

    private string WriteRealmPointingToFakeGitHub()
    {
        var realm = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Identity", "user-manager-realm.json")))!;
        var gitHub = realm["identityProviders"]!.AsArray().Single(provider => provider!["alias"]!.GetValue<string>() == "github")!;
        gitHub["config"]!["baseUrl"] = FakeGitHubUrl;
        gitHub["config"]!["apiUrl"] = FakeGitHubUrl;

        var path = Path.Combine(_realmDirectory, "user-manager-realm.json");
        File.WriteAllText(path, realm.ToJsonString());

        return path;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var response = await _admin.PostAsync("realms/master/protocol/openid-connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = KeycloakBuilder.DefaultUsername,
            ["password"] = KeycloakBuilder.DefaultPassword
        }));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("access_token").GetString()!;
    }

    [GeneratedRegex(@"https?://\S+/login-actions/action-token\?key=[^\s\)""]+")]
    private static partial Regex ActionLink();
}
