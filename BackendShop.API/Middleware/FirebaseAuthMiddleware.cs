using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using BackendShop.Core.Interfaces;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace BackendShop.API.Middleware;

/// <summary>
/// Middleware for validating Firebase JWT tokens and adding claims to the user
/// </summary>
public class FirebaseAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FirebaseAuthMiddleware> _logger;
    private readonly HashSet<string> _publicPaths;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the Firebase authentication middleware
    /// </summary>
    public FirebaseAuthMiddleware(RequestDelegate next, ILogger<FirebaseAuthMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Define paths that don't require authentication
        _publicPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/login",
            "/api/auth/register",
            "/api/auth/refresh",
            "/health",
            "/swagger"
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip authentication for public paths
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // Check for token in Authorization header
        if (!context.Request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader) ||
            string.IsNullOrWhiteSpace(authHeader))
        {
            await RespondWithUnauthorizedAsync(context, "No authorization token provided");
            return;
        }

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await RespondWithUnauthorizedAsync(context, "Authorization scheme must be Bearer");
            return;
        }

        var token = headerValue.Substring("Bearer ".Length).Trim();

        try
        {
            var isValid = await authService.VerifyIdTokenAsync(token);
            if (!isValid)
            {
                await RespondWithUnauthorizedAsync(context, "Invalid or expired token");
                return;
            }

            // For enhanced security, decode the token and populate user claims
            try
            {
                var firebaseToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                // Create claims identity with Firebase claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, firebaseToken.Uid),
                    new Claim(ClaimTypes.Email, firebaseToken.Claims.TryGetValue("email", out var email)
                        ? email.ToString()
                        : string.Empty)
                };

                // Add custom claims if any
                if (firebaseToken.Claims.TryGetValue("role", out var role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                }

                // Create and set the user principal
                var identity = new ClaimsIdentity(claims, "Firebase");
                context.User = new ClaimsPrincipal(identity);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing token claims");
                // Continue anyway since we've already verified the token
            }

            await _next(context);
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning(ex, "Firebase token validation failed");
            await RespondWithUnauthorizedAsync(context, "Token validation failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            await RespondWithUnauthorizedAsync(context, "Authentication error");
        }
    }

    /// <summary>
    /// Determines if the request path is a public path that doesn't require authentication
    /// </summary>
    private bool IsPublicPath(string path)
    {
        // Check exact matches first
        if (_publicPaths.Contains(path))
        {
            return true;
        }

        // Check if path starts with any public path prefix
        return _publicPaths.Any(p =>
            (p.EndsWith('/') && path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ||
            path.StartsWith(p + '/', StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Responds with a 401 Unauthorized status and error message
    /// </summary>
    private async Task RespondWithUnauthorizedAsync(HttpContext context, string message)
    {
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            Status = StatusCodes.Status401Unauthorized,
            Message = message
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
    }
}