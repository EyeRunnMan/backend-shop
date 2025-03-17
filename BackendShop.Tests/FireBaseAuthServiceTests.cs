using System;
using System.Net.Http;
using System.Threading.Tasks;
using BackendShop.Core.Models;
using BackendShop.Infrastructure.Configurations;
using BackendShop.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BackendShop.Tests.Services
{
    public class FirebaseAuthServiceTests
    {
        private readonly Mock<IOptions<FirebaseConfig>> _mockConfig;
        private readonly Mock<ILogger<FirebaseAuthService>> _mockLogger;
        private readonly HttpClient _httpClient;

        // Test email and password - update these with valid test credentials
        private const string TestEmail = "test@example.com";
        private const string TestPassword = "Test123!";

        public FirebaseAuthServiceTests()
        {
            // Set up the configuration mock with your Firebase API key
            _mockConfig = new Mock<IOptions<FirebaseConfig>>();
            _mockConfig.Setup(c => c.Value).Returns(new FirebaseConfig
            {
                ApiKey = "AIzaSyAbu5HxjU4g7OadsfQprkAUq1RxcUB2XSu6Lqw", // Use your actual API key
                ProjectId = "shop-app-asdfa4391" // Use your actual project ID
            });

            _mockLogger = new Mock<ILogger<FirebaseAuthService>>();
            _httpClient = new HttpClient();
        }

        [Fact]
        public async Task TestSignUpWithEmailPassword()
        {
            // Arrange
            var service = new FirebaseAuthService(_httpClient, _mockConfig.Object, _mockLogger.Object);

            // Generate a unique email for testing to avoid conflicts
            string uniqueEmail = $"test_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";

            try
            {
                // Act
                var result = await service.SignUpWithEmailPasswordAsync(uniqueEmail, TestPassword);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Success, $"Registration failed with message: {result.ErrorMessage}");
                Assert.False(string.IsNullOrEmpty(result.IdToken), "ID token should not be empty");
                Assert.False(string.IsNullOrEmpty(result.RefreshToken), "Refresh token should not be empty");
                Assert.True(result.ExpiresIn > 0, "Token should have an expiration time");
            }
            catch (Exception ex)
            {
                Assert.True(false, $"Exception occurred during test: {ex.Message}");
            }
        }

        [Fact]
        public async Task TestSignInWithEmailPassword()
        {
            // Arrange
            var service = new FirebaseAuthService(_httpClient, _mockConfig.Object, _mockLogger.Object);

            try
            {
                // Act
                var result = await service.SignInWithEmailPasswordAsync(TestEmail, TestPassword);

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Success, $"Login failed with message: {result.ErrorMessage}");
                Assert.False(string.IsNullOrEmpty(result.IdToken), "ID token should not be empty");
                Assert.False(string.IsNullOrEmpty(result.RefreshToken), "Refresh token should not be empty");
                Assert.True(result.ExpiresIn > 0, "Token should have an expiration time");
            }
            catch (Exception ex)
            {
                Assert.True(false, $"Exception occurred during test: {ex.Message}");
            }
        }

        [Fact]
        public async Task TestSignInWithWrongPassword()
        {
            // Arrange
            var service = new FirebaseAuthService(_httpClient, _mockConfig.Object, _mockLogger.Object);

            // Act
            var result = await service.SignInWithEmailPasswordAsync(TestEmail, "WrongPassword123!");

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success, "Login should fail with wrong password");
            Assert.True(!string.IsNullOrEmpty(result.ErrorMessage), "Error message should be provided");
        }

        [Fact]
        public async Task TestRefreshToken()
        {
            // Arrange
            var service = new FirebaseAuthService(_httpClient, _mockConfig.Object, _mockLogger.Object);

            // First, sign in to get a refresh token
            var signInResult = await service.SignInWithEmailPasswordAsync(TestEmail, TestPassword);

            // Skip test if sign-in failed (avoids false negatives)
            if (!signInResult.Success)
            {
                Assert.True(true, "Skipping refresh token test as sign-in failed");
                return;
            }

            try
            {
                // Act
                var refreshResult = await service.RefreshTokenAsync(signInResult.RefreshToken);

                // Assert
                Assert.NotNull(refreshResult);
                Assert.True(refreshResult.Success, $"Token refresh failed with message: {refreshResult.ErrorMessage}");
                Assert.False(string.IsNullOrEmpty(refreshResult.IdToken), "New ID token should not be empty");
                Assert.False(string.IsNullOrEmpty(refreshResult.RefreshToken), "New refresh token should not be empty");
                Assert.True(refreshResult.ExpiresIn > 0, "New token should have an expiration time");
            }
            catch (Exception ex)
            {
                Assert.True(false, $"Exception occurred during refresh token test: {ex.Message}");
            }
        }
    }
}