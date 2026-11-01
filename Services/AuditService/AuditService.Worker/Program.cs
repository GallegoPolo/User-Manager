using AuditService.Domain.Interfaces.Repositories;
using AuditService.Infrastructure.Messagings;
using AuditService.Infrastructure.Persistences;
using AuditService.Infrastructure.Repositories;
using AuditService.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

var rabbitMqSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

builder.Services.AddAuditEventConsumer(rabbitMqSettings);

builder.AddObservability();

var host = builder.Build();
host.Run();
