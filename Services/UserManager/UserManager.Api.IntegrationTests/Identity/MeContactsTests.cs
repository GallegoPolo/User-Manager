using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using UserManager.Api.IntegrationTests.Fixtures;
using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;
using UserManager.Domain.ValueObjects;
using UserManager.Infrastructure.Persistence;
using UserManager.Infrastructure.Repositories;

namespace UserManager.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class MeContactsTests
{
    private readonly UserManagerApiFactory _api;

    public MeContactsTests(UserManagerApiFactory api)
    {
        _api = api;
    }

    [Fact]
    public async Task CreateContact_IsOwnedByCurrentUser()
    {
        // Arrange
        var ana = _api.ClientFor(TestPerson.New("Ana"));

        // Act
        var create = await ana.PostAsJsonAsync("/api/me/contacts", new { name = "Bruno", email = "bruno@example.com" });
        var list = await ana.GetFromJsonAsync<JsonElement>("/api/me/contacts");

        // Assert
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Contains(list.EnumerateArray(), c => c.GetProperty("name").GetString() == "Bruno");
    }

    [Fact]
    public async Task ContactOfAnotherPerson_IsInvisibleEvenToOperator()
    {
        // Arrange
        var ana = _api.ClientFor(TestPerson.New("Ana"));
        var contactId = await CreateContactAsync(ana, "Bruno");
        var otto = _api.ClientFor(TestPerson.New("Otto", "operator"));

        // Act
        var get = await otto.GetAsync($"/api/me/contacts/{contactId}");
        var put = await otto.PutAsJsonAsync($"/api/me/contacts/{contactId}", new { name = "Invasor", email = "x@example.com" });
        var delete = await otto.DeleteAsync($"/api/me/contacts/{contactId}");
        var list = await otto.GetFromJsonAsync<JsonElement>("/api/me/contacts");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.DoesNotContain(list.EnumerateArray(), c => c.GetProperty("id").GetGuid() == contactId);
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync($"/api/me/contacts/{contactId}")).StatusCode);
    }

    [Fact]
    public async Task CreateContact_WhenDuplicateEscapesTheCheck_Returns409()
    {
        // Arrange
        var api = _api.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<IContactRepository>(provider => new EmailCheckAlwaysFreeContactRepository(provider.GetRequiredService<UserManagerDbContext>()))));
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", _api.TokenFor(TestPerson.New("Ana")));
        await client.PostAsJsonAsync("/api/me/contacts", new { name = "Bruno", email = "bruno@example.com" });

        // Act
        var duplicate = await client.PostAsJsonAsync("/api/me/contacts", new { name = "Bruno 2", email = "bruno@example.com" });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    private static async Task<Guid> CreateContactAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/me/contacts", new { name, email = $"{Guid.NewGuid():N}@example.com" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("contactId").GetGuid();
    }

    private sealed class EmailCheckAlwaysFreeContactRepository : IContactRepository
    {
        private readonly ContactRepository _inner;

        public EmailCheckAlwaysFreeContactRepository(UserManagerDbContext context)
        {
            _inner = new ContactRepository(context);
        }

        public Task<bool> EmailExistsAsync(Guid ownerId, Email email, Guid? exceptContactId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddAsync(Contact contact, CancellationToken cancellationToken) => _inner.AddAsync(contact, cancellationToken);
        public Task<Contact?> GetByIdAsync(Guid ownerId, Guid contactId, CancellationToken cancellationToken) => _inner.GetByIdAsync(ownerId, contactId, cancellationToken);
        public Task<IReadOnlyList<Contact>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken) => _inner.GetByOwnerAsync(ownerId, cancellationToken);
        public void Remove(Contact contact) => _inner.Remove(contact);
    }
}
