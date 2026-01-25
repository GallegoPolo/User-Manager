namespace AuthService.Api.Extensions;

public static class SwaggerExtension
{
    public static IServiceCollection AddSwaggerContext(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "AuthService", Version = "v1" });

            var securityScheme = new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.ParameterLocation.Header,
                Description = "Insira o token JWT: Bearer {seu_token}"
            };

            c.AddSecurityDefinition("Bearer", securityScheme);

            var securityReference = new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", null);

            c.AddSecurityRequirement(doc => new Microsoft.OpenApi.OpenApiSecurityRequirement
            {
                { securityReference, new List<string>() }
            });
        });

        return services;
    }
}