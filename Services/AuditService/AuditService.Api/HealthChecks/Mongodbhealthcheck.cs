using AuditService.Infrastructure.Persistences;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace AuditService.Api.HealthChecks;

public class MongoDbHealthCheck : IHealthCheck
{
    private readonly MongoDbContext _context;
    private readonly ILogger<MongoDbHealthCheck> _logger;

    public MongoDbHealthCheck(MongoDbContext context, ILogger<MongoDbHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.AuditLogs.Database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}", cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("MongoDB está acessível");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no health check do MongoDB");
            return HealthCheckResult.Unhealthy("MongoDB não está acessível", ex);
        }
    }
}