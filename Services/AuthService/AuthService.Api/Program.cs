using AuthService.Api.Converters;
using AuthService.Api.Extensions;
using AuthService.Api.Middlewares;
using AuthService.Domain.Enums;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddPersistence(builder.Configuration);
builder.Services
    .AddInfrastructure()
    .AddDomainServices()
    .AddSecurity()
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
