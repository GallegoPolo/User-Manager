using AuditService.Domain.Interfaces.Repositories;
using AuditService.Infrastructure.Messagings;
using AuditService.Infrastructure.Persistences;
using AuditService.Infrastructure.Repositories;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

builder.Services.AddHostedService<AuditEventConsumer>();

var host = builder.Build();
host.Run();