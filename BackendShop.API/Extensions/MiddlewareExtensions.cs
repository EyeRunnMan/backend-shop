using BackendShop.API.Middleware;
using Microsoft.AspNetCore.Builder;

namespace BackendShop.API.Extensions;

/// <summary>
/// Extensions for registering custom middleware
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds Firebase authentication middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseFirebaseAuth(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FirebaseAuthMiddleware>();
    }

    /// <summary>
    /// Adds global error handling middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseGlobalErrorHandler(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }

    /// <summary>
    /// Adds security headers middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder builder)
    {
        return builder.Use(async (context, next) =>
        {
            // Security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Add("Content-Security-Policy",
                "default-src 'self'; img-src 'self' data:; script-src 'self'; style-src 'self'; font-src 'self'; object-src 'none'");

            await next();
        });
    }
}