using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.Commands;
using UserManager.Application.UseCases.Users.Handlers;
using UserManager.Domain.Interfaces;
using UserManager.Infrastructure.IntegrationTests.Fixtures;
using UserManager.Infrastructure.IntegrationTests.TestDoubles;
using UserManager.Infrastructure.Messaging;
using UserManager.Infrastructure.Persistence;
using UserManager.Infrastructure.Repositories;

namespace UserManager.Infrastructure.IntegrationTests.Identity;

public sealed class ProvisionCurrentUserConcurrencyTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public ProvisionCurrentUserConcurrencyTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task Provision_TwoFirstRequestsAtTheSameTime_CreateOneUserAndOneUserCreatedMessage()
    {
        // Arrange
        var sub = $"kc-{Guid.NewGuid():N}";
        using var barrier = new Barrier(2);
        await using var services = BuildServices(new FixedCurrentUser(sub, $"{Guid.NewGuid():N}@example.com", "Ana"), barrier);

        // Act
        var results = await Task.WhenAll(
            Task.Run(() => ProvisionAsync(services)),
            Task.Run(() => ProvisionAsync(services)));

        // Assert
        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(results[0].Value, results[1].Value);

        await using var context = _postgres.CreateContext();
        Assert.Equal(1, await context.Users.CountAsync(u => u.IdentityProviderId == sub));
        var userCreatedMessages = await context.Database
            .SqlQuery<int>($"""SELECT count(*)::int AS "Value" FROM "OutboxMessage" WHERE "MessageType" LIKE '%UserCreated%' AND "Body" LIKE {"%" + results[0].Value + "%"}""")
            .SingleAsync();
        Assert.Equal(1, userCreatedMessages);
    }

    private static async Task<Result<Guid>> ProvisionAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new ProvisionCurrentUserCommand());
    }

    private ServiceProvider BuildServices(ICurrentUser currentUser, Barrier barrier) =>
        new ServiceCollection()
            .AddLogging()
            .AddDbContext<UserManagerDbContext>(options => options.UseNpgsql(_postgres.ConnectionString))
            .AddIntegrationMessaging(new RabbitMqSettings())
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProvisionCurrentUserHandler).Assembly))
            .AddScoped(_ => currentUser)
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<IUserRepository>(provider => new RendezvousUserRepository(new UserRepository(provider.GetRequiredService<UserManagerDbContext>()), barrier))
            .BuildServiceProvider();
}
