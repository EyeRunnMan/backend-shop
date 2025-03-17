using System;

namespace BackendShop.Core.Models;

/// <summary>
/// Represents the result of an authentication operation
/// </summary>
public class AuthResult
{
    /// <summary>
    /// The ID token (JWT) for API authentication
    /// </summary>
    public string IdToken { get; set; } = string.Empty;

    /// <summary>
    /// The refresh token for obtaining new ID tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in seconds from issue time
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Unique identifier for the authenticated user
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email of the authenticated user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Calculated expiration time based on ExpiresIn
    /// </summary>
    public DateTime? ExpirationTime => ExpiresIn > 0
        ? DateTime.UtcNow.AddSeconds(ExpiresIn)
        : null;
}