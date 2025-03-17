using System;
using System.Threading.Tasks;
using BackendShop.API.Models.DTOs;
using BackendShop.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BackendShop.API.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the authentication controller
    /// </summary>
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Authenticates a user with email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication result with tokens</returns>
    /// <response code="200">Successfully authenticated</response>
    /// <response code="400">Invalid request or credentials</response>
    /// <response code="429">Too many login attempts</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for {Email}", request.Email);

        try
        {
            var result = await _authService.SignInWithEmailPasswordAsync(request.Email, request.Password);

            if (!result.Success)
            {
                return BadRequest(AuthResponse.FromError(result.ErrorMessage ?? "Authentication failed"));
            }

            return Ok(AuthResponse.FromSuccess(result.IdToken, result.RefreshToken, result.ExpiresIn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login attempt for {Email}", request.Email);
            return BadRequest(AuthResponse.FromError("Authentication failed"));
        }
    }

    /// <summary>
    /// Registers a new user with email and password
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Authentication result with tokens for the new user</returns>
    /// <response code="201">Successfully registered</response>
    /// <response code="400">Invalid request or email already in use</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("Registration attempt for {Email}", request.Email);

        try
        {
            var result = await _authService.SignUpWithEmailPasswordAsync(request.Email, request.Password);

            if (!result.Success)
            {
                return BadRequest(AuthResponse.FromError(result.ErrorMessage ?? "Registration failed"));
            }

            return Created(
                $"/api/auth/users/{result.UserId}",
                AuthResponse.FromSuccess(result.IdToken, result.RefreshToken, result.ExpiresIn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration attempt for {Email}", request.Email);
            return BadRequest(AuthResponse.FromError("Registration failed"));
        }
    }

    /// <summary>
    /// Refreshes an expired ID token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New authentication tokens</returns>
    /// <response code="200">Successfully refreshed token</response>
    /// <response code="400">Invalid refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (!result.Success)
            {
                return BadRequest(AuthResponse.FromError(result.ErrorMessage ?? "Failed to refresh token"));
            }

            return Ok(AuthResponse.FromSuccess(result.IdToken, result.RefreshToken, result.ExpiresIn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return BadRequest(AuthResponse.FromError("Failed to refresh token"));
        }
    }

    /// <summary>
    /// Logs out a user by revoking their refresh token
    /// </summary>
    /// <param name="request">Refresh token to revoke</param>
    /// <returns>Success indicator</returns>
    /// <response code="200">Successfully logged out</response>
    /// <response code="400">Failed to log out</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var success = await _authService.RevokeRefreshTokenAsync(request.RefreshToken);

            if (!success)
            {
                return BadRequest(new { message = "Failed to log out" });
            }

            return Ok(new { message = "Successfully logged out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return BadRequest(new { message = "Failed to log out" });
        }
    }
}