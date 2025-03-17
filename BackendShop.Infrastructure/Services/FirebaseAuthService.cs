using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BackendShop.Core.Interfaces;
using BackendShop.Core.Models;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BackendShop.Infrastructure.Configurations;
using System.Collections.Generic;
using System.Net;
using Polly;
using System.Net.Http.Headers;

namespace BackendShop.Infrastructure.Services;

/// <summary>
/// Firebase implementation of the authentication service
/// </summary>
public class FirebaseAuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly FirebaseConfig _config;
    private readonly ILogger<FirebaseAuthService> _logger;
    private readonly string _identityEndpoint = "https://identitytoolkit.googleapis.com/v1/accounts:";
    private readonly string _secureTokenEndpoint = "https://securetoken.googleapis.com/v1/token";
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the Firebase Authentication Service
    /// </summary>
    public FirebaseAuthService(
        HttpClient httpClient,
        IOptions<FirebaseConfig> config,
        ILogger<FirebaseAuthService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrEmpty(_config.ApiKey))
        {
            throw new ArgumentException("Firebase API Key is required", nameof(config));
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public async Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password)
    {
        _logger.LogInformation("Attempting to sign in user with email: {Email}", email);

        try
        {
            var requestData = new Dictionary<string, object>
            {
                ["email"] = email,
                ["password"] = password,
                ["returnSecureToken"] = true
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_identityEndpoint}signInWithPassword?key={_config.ApiKey}",
                requestData,
                _jsonOptions);

            return await HandleAuthResponseAsync(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during sign in for {Email}", email);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = $"Authentication service unavailable: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during sign in for {Email}", email);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred during authentication"
            };
        }
    }

    /// <inheritdoc />
    public async Task<AuthResult> SignUpWithEmailPasswordAsync(string email, string password)
    {
        _logger.LogInformation("Attempting to register new user with email: {Email}", email);

        try
        {
            var requestData = new Dictionary<string, object>
            {
                ["email"] = email,
                ["password"] = password,
                ["returnSecureToken"] = true
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_identityEndpoint}signUp?key={_config.ApiKey}",
                requestData,
                _jsonOptions);

            return await HandleAuthResponseAsync(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during registration for {Email}", email);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = $"Registration service unavailable: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for {Email}", email);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred during registration"
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> VerifyIdTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Attempt to verify null or empty token");
            return false;
        }

        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
            return decodedToken != null;
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning(ex, "Token verification failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token verification");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        _logger.LogInformation("Attempting to refresh token");

        try
        {
            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            };

            var content = new FormUrlEncodedContent(requestData);

            var response = await _httpClient.PostAsync(
                $"{_secureTokenEndpoint}?key={_config.ApiKey}",
                content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed with status: {StatusCode}", response.StatusCode);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Failed to refresh token"
                };
            }

            var responseText = await response.Content.ReadAsStringAsync();
            var responseData = JsonDocument.Parse(responseText);
            var root = responseData.RootElement;

            return new AuthResult
            {
                IdToken = root.GetProperty("id_token").GetString() ?? string.Empty,
                RefreshToken = root.GetProperty("refresh_token").GetString() ?? string.Empty,
                ExpiresIn = root.GetProperty("expires_in").ValueKind == JsonValueKind.Number
    ? root.GetProperty("expires_in").GetInt32()
    : int.Parse(root.GetProperty("expires_in").GetString() ?? "3600"),
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Failed to refresh token due to an error"
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        _logger.LogInformation("Attempting to revoke refresh token");

        // Note: Firebase REST API doesn't have a direct endpoint for revoking refresh tokens.
        // This would typically be done by updating user's tokens in Firebase Auth or
        // maintaining a blocked tokens list on your server.

        // For proper implementation, you might:
        // 1. Use the Admin SDK to disable the user temporarily
        // 2. Maintain a denylist of revoked tokens in Redis/database
        // 3. Use Firebase Auth custom claims to mark tokens as revoked

        try
        {
            // This is a placeholder - in a real implementation you would use one of the above approaches
            // For example, using a user ID from the token to disable that user:

            // Get user ID from refresh token (would require additional API call to Firebase)
            // var userId = await GetUserIdFromRefreshToken(refreshToken);
            // await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
            // {
            //     Uid = userId,
            //     Disabled = true
            // });

            _logger.LogWarning("Token revocation not fully implemented - implement token blacklisting for production");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return false;
        }
    }

    /// <summary>
    /// Handles the Firebase authentication response and converts it to AuthResult
    /// </summary>
    private async Task<AuthResult> HandleAuthResponseAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Auth operation failed with status: {StatusCode}, Response: {Response}",
                response.StatusCode, errorResponse);

            string errorMessage = "Authentication failed";

            try
            {
                var errorJson = JsonDocument.Parse(errorResponse);
                var errorRoot = errorJson.RootElement;

                if (errorRoot.TryGetProperty("error", out var errorObj) &&
                    errorObj.TryGetProperty("message", out var messageElement))
                {
                    string firebaseError = messageElement.GetString() ?? string.Empty;
                    errorMessage = MapFirebaseErrorToUserFriendlyMessage(firebaseError);
                }
            }
            catch (JsonException)
            {
                // Fall back to default message
            }

            return new AuthResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var responseData = JsonDocument.Parse(responseText);
        var root = responseData.RootElement;

        return new AuthResult
        {
            IdToken = root.GetProperty("idToken").GetString() ?? string.Empty,
            RefreshToken = root.GetProperty("refreshToken").GetString() ?? string.Empty,
            ExpiresIn = root.GetProperty("expiresIn").ValueKind == JsonValueKind.Number
            ? root.GetProperty("expiresIn").GetInt32()
            : int.Parse(root.GetProperty("expiresIn").GetString() ?? "3600"),
            UserId = root.GetProperty("localId").GetString() ?? string.Empty,
            Email = root.GetProperty("email").GetString() ?? string.Empty,
            Success = true
        };
    }

    /// <summary>
    /// Maps Firebase error codes to user-friendly messages
    /// </summary>
    private string MapFirebaseErrorToUserFriendlyMessage(string firebaseError)
    {
        return firebaseError switch
        {
            "EMAIL_EXISTS" => "The email address is already in use by another account.",
            "OPERATION_NOT_ALLOWED" => "Password sign-in is disabled for this project.",
            "TOO_MANY_ATTEMPTS_TRY_LATER" => "We have blocked all requests from this device due to unusual activity. Try again later.",
            "EMAIL_NOT_FOUND" => "Invalid email or password.",
            "INVALID_PASSWORD" => "Invalid email or password.",
            "USER_DISABLED" => "The user account has been disabled by an administrator.",
            "INVALID_ID_TOKEN" => "The user's credential is no longer valid. The user must sign in again.",
            "TOKEN_EXPIRED" => "The user's credential has expired. The user must sign in again.",
            _ => "An error occurred during authentication."
        };
    }
}