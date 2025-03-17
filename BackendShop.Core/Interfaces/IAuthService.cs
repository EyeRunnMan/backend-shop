using System.Threading.Tasks;
using BackendShop.Core.Models;

namespace BackendShop.Core.Interfaces;

/// <summary>
/// Service interface for authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user with email and password
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <returns>Authentication result with tokens</returns>
    Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password);

    /// <summary>
    /// Registers a new user with email and password
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <returns>Authentication result with tokens</returns>
    Task<AuthResult> SignUpWithEmailPasswordAsync(string email, string password);

    /// <summary>
    /// Verifies the validity of an ID token
    /// </summary>
    /// <param name="token">The ID token to verify</param>
    /// <returns>True if token is valid, false otherwise</returns>
    Task<bool> VerifyIdTokenAsync(string token);

    /// <summary>
    /// Refreshes an expired ID token using a refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token</param>
    /// <returns>Authentication result with new tokens</returns>
    Task<AuthResult> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes a user's refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke</param>
    /// <returns>True if successfully revoked</returns>
    Task<bool> RevokeRefreshTokenAsync(string refreshToken);
}