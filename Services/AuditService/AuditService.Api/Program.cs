using AuditService.Api.Extensions;
using AuditService.Api.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "AuditService API",
        Version = "v1",
        Description = "API de consulta de logs de auditoria"
    });
});

builder.Services.AddAuditServices(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck<MongoDbHealthCheck>("mongodb", tags: new[] { "ready" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AuditService API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();