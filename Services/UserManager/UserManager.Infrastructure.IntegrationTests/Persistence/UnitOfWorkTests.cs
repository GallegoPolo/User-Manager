using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserManager.Application.Common;
using UserManager.Application.UseCases.Users.Handlers;
using UserManager.Domain.Entities;
using UserManager.Domain.Interfaces;
using UserManager.Domain.ValueObjects;
using UserManager.Infrastructure.IntegrationTests.Fixtures;
using UserManager.Infrastructure.IntegrationTests.TestDoubles;
using UserManager.Infrastructure.Messaging;
using UserManager.Infrastructure.Persistence;
using UserManager.Infrastructure.Repositories;

namespace UserManager.Infrastructure.IntegrationTests.Persistence;

public sealed class UnitOfWorkTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public UnitOfWorkTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [Fact]
    public async Task SaveChanges_TwoUsersWithSameIdentityProviderId_ThrowsUniqueConstraintViolation()
    {
        // Arrange
        var sub = $"kc-{Guid.NewGuid():N}";
        await SaveAsync(NewUser(sub));

        // Act
        var save = () => SaveAsync(NewUser(sub));

        // Assert
        await Assert.ThrowsAsync<UniqueConstraintViolationException>(save);
    }

    [Fact]
    public async Task SaveChanges_AfterUniqueViolation_LeavesNoPendingChanges()
    {
        // Arrange
        var sub = $"kc-{Guid.NewGuid():N}";
        await SaveAsync(NewUser(sub));
        await using var context = _postgres.CreateContext();
        var unitOfWork = new UnitOfWork(context, new NoOpPublisher());
        await new UserRepository(context).AddAsync(NewUser(sub));

        // Act
        await Assert.ThrowsAsync<UniqueConstraintViolationException>(() => unitOfWork.SaveChangesAsync());

        // Assert
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task SaveChanges_AfterUniqueViolation_NextSaveInSameScopeStillPublishesToOutbox()
    {
        // Arrange
        var sub = $"kc-{Guid.NewGuid():N}";
        await SaveAsync(NewUser(sub));
        await using var services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<UserManagerDbContext>(options => options.UseNpgsql(_postgres.ConnectionString))
            .AddIntegrationMessaging(new RabbitMqSettings())
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProvisionCurrentUserHandler).Assembly))
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<IUserRepository, UserRepository>()
            .AddScoped<ICurrentUser>(_ => new FixedCurrentUser($"kc-{Guid.NewGuid():N}", "ana@example.com", "Ana"))
            .BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await users.AddAsync(NewUser(sub));
        await Assert.ThrowsAsync<UniqueConstraintViolationException>(() => unitOfWork.SaveChangesAsync());
        var next = NewUser($"kc-{Guid.NewGuid():N}");
        await users.AddAsync(next);

        // Act
        var exception = await Record.ExceptionAsync(() => unitOfWork.SaveChangesAsync());

        // Assert
        Assert.Null(exception);
        await using var context = _postgres.CreateContext();
        Assert.True(await context.Users.AnyAsync(u => u.Id == next.Id));
    }

    [Fact]
    public async Task SaveChanges_TwoUsersWithSameEmail_IsAllowed()
    {
        // Arrange
        var email = $"{Guid.NewGuid():N}@example.com";
        await SaveAsync(NewUser($"kc-{Guid.NewGuid():N}", email));

        // Act
        var exception = await Record.ExceptionAsync(() => SaveAsync(NewUser($"kc-{Guid.NewGuid():N}", email)));

        // Assert
        Assert.Null(exception);
    }

    private static User NewUser(string sub, string? email = null) =>
        User.Provision(sub, "Ana", Email.Create(email ?? $"{Guid.NewGuid():N}@example.com"));

    private async Task SaveAsync(User user)
    {
        await using var context = _postgres.CreateContext();
        await new UserRepository(context).AddAsync(user);
        await new UnitOfWork(context, new NoOpPublisher()).SaveChangesAsync();
    }

    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
