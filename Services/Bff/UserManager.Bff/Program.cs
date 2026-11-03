using UserManager.Bff.Authentication;
using UserManager.Bff.Endpoints;
using UserManager.Bff.Observability;
using UserManager.Bff.Proxy;
using UserManager.Bff.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBffAuthentication(builder.Configuration);
builder.Services.AddBffProxy(builder.Configuration);
builder.AddObservability();

var app = builder.Build();

app.UseMiddleware<CsrfHeaderMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapBffEndpoints();
app.MapReverseProxy();
app.Run();

public partial class Program;
