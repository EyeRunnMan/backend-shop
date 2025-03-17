using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System;

namespace BackendShop.API.Models.DTOs;

/// <summary>
/// Request DTO for user login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for user registration
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for token refresh
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for authentication operations
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// ID token (JWT) for API authentication
    /// </summary>
    public string IdToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new ID tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Calculated token expiration date (UTC)
    /// </summary>
    public DateTime ExpiresAt => DateTime.UtcNow.AddSeconds(ExpiresIn);

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Creates a new successful authentication response
    /// </summary>
    public static AuthResponse FromSuccess(string idToken, string refreshToken, int expiresIn)
    {
        return new AuthResponse
        {
            IdToken = idToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            Success = true
        };
    }

    /// <summary>
    /// Creates a new failed authentication response
    /// </summary>
    public static AuthResponse FromError(string message)
    {
        return new AuthResponse
        {
            Success = false,
            Message = message
        };
    }
}