namespace UserManager.Api.IntegrationTests.Fixtures;

public sealed record TestPerson(string Sub, string? Email, string Name, string[] Roles)
{
    public static TestPerson New(string name, params string[] roles)
    {
        var id = Guid.NewGuid().ToString("N");
        return new TestPerson($"kc-{id}", $"{name.ToLowerInvariant()}-{id}@example.com", name, roles);
    }
}
