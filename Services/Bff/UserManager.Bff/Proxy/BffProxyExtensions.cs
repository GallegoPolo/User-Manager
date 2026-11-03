using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using UserManager.Bff.Security;
using Yarp.ReverseProxy.Transforms;

namespace UserManager.Bff.Proxy;

public static class BffProxyExtensions
{
    public static IServiceCollection AddBffProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms(context =>
            {
                context.AddRequestHeaderRemove("Cookie");
                context.AddRequestHeaderRemove(CsrfHeaderMiddleware.HeaderName);
                context.AddRequestTransform(async transform =>
                {
                    var accessToken = await transform.HttpContext.GetTokenAsync("access_token");

                    if (accessToken is not null)
                        transform.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                });
            });

        return services;
    }
}
