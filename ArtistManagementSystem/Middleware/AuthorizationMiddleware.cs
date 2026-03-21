using System.Security.Claims;

namespace ArtistManagementSystem.Middleware;

public class AuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value?.ToLower();

        if (path == null || path.StartsWith("/api/auth") || path == "/")
        {
            await next(httpContext);
            return;
        }

        await next(httpContext);
    }
}

public static class AuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthorizationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizationMiddleware>();
    }
}
