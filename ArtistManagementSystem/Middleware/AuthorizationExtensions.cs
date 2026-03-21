using System.Security.Claims;

namespace ArtistManagementSystem.Middleware;

public static class AuthorizationExtensions
{
    public static RouteHandlerBuilder RequireRole(this RouteHandlerBuilder builder, string role)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var roleClaim = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrEmpty(roleClaim))
            {
                return Results.Json(new { error = "Unauthorized" }, statusCode: 401);
            }

            if (roleClaim != role)
            {
                return Results.Json(new { error = "Forbidden - Insufficient permissions" }, statusCode: 403);
            }

            return await next(context);
        });
    }

    public static RouteHandlerBuilder RequireAnyRole(this RouteHandlerBuilder builder, params string[] roles)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var roleClaim = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrEmpty(roleClaim))
            {
                return Results.Json(new { error = "Unauthorized" }, statusCode: 401);
            }

            if (!roles.Contains(roleClaim))
            {
                return Results.Json(new { error = "Forbidden - Insufficient permissions" }, statusCode: 403);
            }

            return await next(context);
        });
    }

    public static RouteHandlerBuilder RequireAuthenticated(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Json(new { error = "Unauthorized" }, statusCode: 401);
            }

            return await next(context);
        });
    }
}
