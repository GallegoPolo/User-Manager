using AuthService.Api.Converters;
using AuthService.Api.Extensions;
using AuthService.Api.Middlewares;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddInfrastructure(connectionString);

builder.Services
    .AddDomainServices()
    .AddApplication()
    .AddBootstrap();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(
        new EnumDescriptionJsonConverter<EApiKeyStatus>()
    );
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();