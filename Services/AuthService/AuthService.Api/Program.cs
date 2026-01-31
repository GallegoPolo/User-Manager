using AuthService.Api.Converters;
using AuthService.Api.Extensions;
using AuthService.Api.Middlewares;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerContext();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddInfrastructure(connectionString, builder.Configuration);

builder.Services
    .AddDomainServices()
    .AddApplication()
    .AddBootstrap();

builder.Services.AddJwtAuthentication(builder.Configuration);

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
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthService API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();