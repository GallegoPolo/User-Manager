using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Testcontainers.Keycloak;

namespace UserManager.Infrastructure.IntegrationTests.Fixtures;

public sealed class KeycloakFixture : IAsyncLifetime
{
    public const string RealmName = "user-manager";
    public const string BffClientId = "user-manager-bff";
    public const string Image = "quay.io/keycloak/keycloak:26.8.1";
    public const string BffClientSecret = "user-manager-bff-dev-secret";
    public const string BffRedirectUri = "http://localhost:5173/signin-oidc";

    private readonly KeycloakContainer _container = new KeycloakBuilder(Image)
        .WithRealm(Path.Combine(AppContext.BaseDirectory, "Identity", "user-manager-realm.json"))
        .Build();

    private HttpClient _http = null!;
    private HttpClient _admin = null!;

    public JsonElement Realm { get; private set; }

    public JsonElement BffClientAsImported { get; private set; }

    public string KeycloakUrl => _container.GetBaseAddress().TrimEnd('/');

    public Task<JsonElement> GetIdentityProviderAsync(string alias) =>
        _admin.GetFromJsonAsync<JsonElement>($"admin/realms/{RealmName}/identity-provider/instances/{alias}");

    public async Task<IReadOnlyList<string>> GetTopLevelFlowAliasesAsync()
    {
        var flows = await _admin.GetFromJsonAsync<JsonElement>($"admin/realms/{RealmName}/authentication/flows");
        return flows.EnumerateArray().Select(flow => flow.GetProperty("alias").GetString()!).ToList();
    }

    public async Task<IReadOnlyList<string>> GetAuthenticatorsOfFlowAsync(string alias)
    {
        var executions = await _admin.GetFromJsonAsync<JsonElement>($"admin/realms/{RealmName}/authentication/flows/{Uri.EscapeDataString(alias)}/executions");
        return executions.EnumerateArray()
            .Where(execution => execution.TryGetProperty("providerId", out _))
            .Select(execution => execution.GetProperty("providerId").GetString()!)
            .ToList();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _http = new HttpClient { BaseAddress = new Uri(_container.GetBaseAddress()) };
        _admin = new HttpClient { BaseAddress = _http.BaseAddress };
        _admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync());

        Realm = await _admin.GetFromJsonAsync<JsonElement>($"admin/realms/{RealmName}");

        var clients = await _admin.GetFromJsonAsync<JsonElement>($"admin/realms/{RealmName}/clients?clientId={BffClientId}");
        if (clients.GetArrayLength() == 0)
            return;

        BffClientAsImported = clients[0];
        await EnablePasswordGrantForTestsAsync(BffClientAsImported.GetProperty("id").GetString()!);
    }

    public async Task DisposeAsync()
    {
        _http?.Dispose();
        _admin?.Dispose();
        await _container.DisposeAsync();
    }

    public async Task<JsonElement> GetAccessTokenPayloadAsync(string username, string password)
    {
        var response = await _http.PostAsync($"realms/{RealmName}/protocol/openid-connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = BffClientId,
            ["client_secret"] = BffClientSecret,
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid"
        }));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var payload = body.GetProperty("access_token").GetString()!.Split('.')[1];

        return JsonSerializer.Deserialize<JsonElement>(Base64Url.DecodeFromChars(payload));
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var response = await _http.PostAsync("realms/master/protocol/openid-connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
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

    private async Task EnablePasswordGrantForTestsAsync(string clientUuid)
    {
        var client = await _admin.GetFromJsonAsync<JsonObject>($"admin/realms/{RealmName}/clients/{clientUuid}");
        client!["directAccessGrantsEnabled"] = true;

        var response = await _admin.PutAsJsonAsync($"admin/realms/{RealmName}/clients/{clientUuid}", client);
        response.EnsureSuccessStatusCode();
    }
}
