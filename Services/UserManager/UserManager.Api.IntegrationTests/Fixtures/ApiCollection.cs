namespace UserManager.Api.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<UserManagerApiFactory>
{
    public const string Name = "UserManager.Api";
}
