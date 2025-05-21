// Path: LeafLoop.Tests.Unit/Services/JwtTokenServiceTests.cs

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using LeafLoop.Models; // For User
using LeafLoop.Services; // For JwtTokenService
// using LeafLoop.Services.Interfaces; // Not strictly needed to test concrete class if IJwtTokenService isn't used here
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
// using Microsoft.Extensions.Logging.Abstractions; // NullLogger can be an alternative for simpler tests
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace LeafLoop.Tests.Unit.Services
{
    public class JwtTokenServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ILogger<JwtTokenService>> _mockLogger; // Mocked logger for general use / verification
        private readonly JwtTokenService _jwtTokenService; // Main service instance using the above mocks

        // Test constants for configuration values
        private const string TestSecretKey = "THIS_IS_A_TEST_SECRET_KEY_THAT_IS_REALLY_REALLY_LONG_ENOUGH_FOR_HS256_SECURITY"; // Min 32 bytes for HS256
        private const string TestIssuer = "TestIssuer";
        private const string TestAudience = "TestAudience";
        private const string TestExpiryInMinutes = "60";

        public JwtTokenServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup IConfiguration for indexer access used by the service.
            // These are the default "good path" configurations.
            _mockConfiguration.Setup(c => c["JwtSettings:Key"]).Returns(TestSecretKey);
            _mockConfiguration.Setup(c => c["JwtSettings:Issuer"]).Returns(TestIssuer);
            _mockConfiguration.Setup(c => c["JwtSettings:Audience"]).Returns(TestAudience);
            _mockConfiguration.Setup(c => c["JwtSettings:ExpiryInMinutes"]).Returns(TestExpiryInMinutes);

            // UserManager requires IUserStore, so we mock that minimally.
            var mockUserStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            _mockLogger = new Mock<ILogger<JwtTokenService>>(); 

            _jwtTokenService = new JwtTokenService(
                _mockConfiguration.Object,
                _mockUserManager.Object,
                _mockLogger.Object 
            );
        }

        // Helper to create a test user instance.
        private User CreateTestUser(int id = 1, string userName = "testuser", string email = "test@example.com")
        {
            return new User { Id = id, UserName = userName, Email = email };
        }

        // --- Tests for GenerateTokenAsync ---

        [Fact]
        public async Task GenerateTokenAsync_ValidUser_ShouldReturnValidJwtToken()
        {
            // Arrange: Prepare user, mock UserManager to return claims and roles.
            var user = CreateTestUser();
            var userClaims = new List<Claim> { new Claim("custom_claim", "value") };
            var userRoles = new List<string> { "User", "Editor" };

            _mockUserManager.Setup(um => um.GetClaimsAsync(user)).ReturnsAsync(userClaims);
            _mockUserManager.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(userRoles);

            // Act: Generate the token.
            var tokenString = await _jwtTokenService.GenerateTokenAsync(user);

            // Assert: Token is generated, not empty, and decodable with expected claims.
            Assert.NotNull(tokenString);
            Assert.NotEmpty(tokenString);

            var handler = new JwtSecurityTokenHandler();
            var decodedToken = handler.ReadJwtToken(tokenString);

            Assert.Equal(TestIssuer, decodedToken.Issuer);
            Assert.Contains(TestAudience, decodedToken.Audiences);
            Assert.Equal(user.Id.ToString(), decodedToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal(user.UserName, decodedToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value);
            Assert.Equal(user.Email, decodedToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value);
            Assert.Contains(decodedToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "User");
            Assert.Contains(decodedToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Editor");
            Assert.Contains(decodedToken.Claims, c => c.Type == "custom_claim" && c.Value == "value");
            Assert.True(decodedToken.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Jti)); // Check JTI presence

            // Verify expiry time (allowing for slight clock skew/processing delay).
            // Ensure service uses CultureInfo.InvariantCulture for parsing ExpiryInMinutes.
            double expiryMinutes = double.Parse(TestExpiryInMinutes, System.Globalization.CultureInfo.InvariantCulture);
            DateTime expectedExpiryUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);
            
            Assert.True(decodedToken.ValidTo > DateTime.UtcNow.AddMinutes(expiryMinutes - 1), "Token expiry is earlier than expected.");
            Assert.True(decodedToken.ValidTo <= expectedExpiryUtc.AddSeconds(10), "Token expiry is much later than expected.");
        }

        [Fact]
        public async Task GenerateTokenAsync_NullUser_ShouldReturnNullAndLogError()
        {
            // Arrange: Use a specific logger mock for this test to verify logging.
            var localMockLogger = new Mock<ILogger<JwtTokenService>>();
            var serviceInstance = new JwtTokenService(_mockConfiguration.Object, _mockUserManager.Object, localMockLogger.Object);

            // Act: Call with null user.
            var tokenString = await serviceInstance.GenerateTokenAsync(null);

            // Assert: Expect null token and error log.
            Assert.Null(tokenString);
            localMockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error, // Service logs this as Error
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Cannot generate token for null user")),
                    null, // The exception is null in this specific log call from the service
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GenerateTokenAsync_MissingJwtKeyConfiguration_ShouldReturnNullAndLogError()
        {
            // Arrange: Create a dedicated IConfiguration mock that returns null for the JWT key.
            var mockConfigMissingKey = new Mock<IConfiguration>();
            mockConfigMissingKey.Setup(c => c["JwtSettings:Key"]).Returns((string)null); // Key is null
            mockConfigMissingKey.Setup(c => c["JwtSettings:Issuer"]).Returns(TestIssuer);
            mockConfigMissingKey.Setup(c => c["JwtSettings:Audience"]).Returns(TestAudience);
            mockConfigMissingKey.Setup(c => c["JwtSettings:ExpiryInMinutes"]).Returns(TestExpiryInMinutes);

            var localMockLogger = new Mock<ILogger<JwtTokenService>>();
            var serviceWithBadConfig = new JwtTokenService(mockConfigMissingKey.Object, _mockUserManager.Object, localMockLogger.Object);
            
            var user = CreateTestUser();
            _mockUserManager.Setup(um => um.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            _mockUserManager.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            // Act: Attempt to generate token with service using bad config.
            var resultToken = await serviceWithBadConfig.GenerateTokenAsync(user);

            // Assert: Token should be null (service catches ArgumentNullException and returns null).
            Assert.Null(resultToken);
            // Verify an error was logged, with the original ArgumentException.
            localMockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error, // Service logs this as Error
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error generating token")),
                    It.IsAny<ArgumentException>(), // SymmetricSecurityKey throws this for bad key (ArgumentNullException or ArgumentOutOfRangeException)
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // --- Tests for ValidateToken ---

        [Fact]
        public async Task ValidateToken_ValidToken_ShouldReturnClaimsPrincipal()
        {
            // Arrange: Generate a valid token.
            var user = CreateTestUser(id: 5, userName: "validuser", email: "valid@mail.com");
            _mockUserManager.Setup(um => um.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            _mockUserManager.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string> { "ValidRole" });
            
            var tokenString = await _jwtTokenService.GenerateTokenAsync(user);
            Assert.NotNull(tokenString); // Ensure token was generated

            // Act: Validate the token.
            var principal = _jwtTokenService.ValidateToken(tokenString);

            // Assert: Principal is not null, authenticated, and contains expected claims.
            Assert.NotNull(principal);
            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal(user.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
            Assert.Equal(user.UserName, principal.FindFirstValue(ClaimTypes.Name));
            Assert.Equal(user.Email, principal.FindFirstValue(ClaimTypes.Email));
            Assert.True(principal.IsInRole("ValidRole"));
        }

        [Fact]
        public async Task ValidateToken_ExpiredToken_ShouldReturnNullAndLogError()
        {
            // Arrange:
            // 1. Setup a config mock for generating a token with a very short expiry.
            var mockConfigForShortLivedToken = new Mock<IConfiguration>();
            mockConfigForShortLivedToken.Setup(c => c["JwtSettings:Key"]).Returns(TestSecretKey);
            mockConfigForShortLivedToken.Setup(c => c["JwtSettings:Issuer"]).Returns(TestIssuer);
            mockConfigForShortLivedToken.Setup(c => c["JwtSettings:Audience"]).Returns(TestAudience);
            // Ensure your JwtTokenService.GenerateTokenAsync uses CultureInfo.InvariantCulture for parsing this.
            mockConfigForShortLivedToken.Setup(c => c["JwtSettings:ExpiryInMinutes"]).Returns("0.001"); 

            // 2. Create a temporary service instance with this short-expiry config to generate the token.
            var tokenGeneratorService = new JwtTokenService(
                mockConfigForShortLivedToken.Object, 
                _mockUserManager.Object, 
                NullLogger<JwtTokenService>.Instance // Logging for this generator instance is not under test here.
            );
            
            var user = CreateTestUser();
            _mockUserManager.Setup(um => um.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            _mockUserManager.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string>());
            
            var tokenString = await tokenGeneratorService.GenerateTokenAsync(user);
            Assert.NotNull(tokenString); // This assert now passes due to service-side parsing fix for ExpiryInMinutes

            await Task.Delay(200); // Wait > 60ms to ensure token expires.

            // Act: Validate the token using the main service instance (which uses _mockLogger for verification).
            var principal = _jwtTokenService.ValidateToken(tokenString);

            // Assert: Principal should be null. Log should indicate SecurityTokenExpiredException at LogLevel.Warning.
            Assert.Null(principal);
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Warning, // CORRECTED: Service logs SecurityTokenExpiredException as Warning
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Token validation failed: Token expired.")), // CORRECTED: Match service log
                    It.IsAny<SecurityTokenExpiredException>(), 
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ValidateToken_InvalidSignature_ShouldReturnNullAndLogError()
        {
            // Arrange: Generate token with one key.
            var user = CreateTestUser();
            _mockUserManager.Setup(um => um.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            _mockUserManager.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string>());
            var tokenString = await _jwtTokenService.GenerateTokenAsync(user); // Generated with TestSecretKey
            Assert.NotNull(tokenString);

            // Create a service instance configured with a DIFFERENT key for validation.
            var mockConfigInvalidKey = new Mock<IConfiguration>();
            mockConfigInvalidKey.Setup(c => c["JwtSettings:Key"]).Returns("ANOTHER_DIFFERENT_VERY_LONG_SECRET_KEY_FOR_VALIDATION_TEST_HS256");
            mockConfigInvalidKey.Setup(c => c["JwtSettings:Issuer"]).Returns(TestIssuer); 
            mockConfigInvalidKey.Setup(c => c["JwtSettings:Audience"]).Returns(TestAudience);
            
            var localMockLogger = new Mock<ILogger<JwtTokenService>>();
            var serviceWithInvalidKeyForValidation = new JwtTokenService(
                mockConfigInvalidKey.Object, 
                _mockUserManager.Object, 
                localMockLogger.Object);

            // Act: Validate with the service configured with the wrong key.
            var principal = serviceWithInvalidKeyForValidation.ValidateToken(tokenString);

            // Assert: Principal should be null. Log should indicate a signature-related exception.
            // Based on Moq output, the service logs LogLevel.Warning and the specific exception caught might be
            // SecurityTokenSignatureKeyNotFoundException when the message is "Token validation failed: Invalid signature."
            // This happens if the catch (SecurityTokenInvalidSignatureException ex) block is hit.
            Assert.Null(principal);
            localMockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Warning, // CORRECTED: As per service code for SecurityTokenInvalidSignatureException
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Token validation failed: Invalid signature.")), // CORRECTED
                    // The actual exception thrown by ValidateToken and caught by the service might be more specific like
                    // SecurityTokenSignatureKeyNotFoundException or a general SecurityTokenInvalidSignatureException.
                    // Let's use a broader exception type that covers signature issues if the exact one is tricky.
                    // However, if the service code is `catch (SecurityTokenInvalidSignatureException ex)`, then this is the type.
                    // If the Moq log showed SecurityTokenSignatureKeyNotFoundException inside this catch block,
                    // it implies SecurityTokenSignatureKeyNotFoundException can be caught as SecurityTokenInvalidSignatureException.
                    // For robustness and matching what was actually caught by that specific catch block:
                    It.IsAny<SecurityTokenInvalidSignatureException>(), // Or It.IsAny<SecurityTokenSignatureKeyNotFoundException>() if that's what the service block truly catches and logs
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void ValidateToken_MalformedToken_ShouldReturnNullAndLogError()
        {
            // Arrange
            var malformedToken = "this.is.not.a.valid.jwt.token";
            _mockLogger.Reset(); // Reset before verification if using shared _mockLogger

            // Act
            var principal = _jwtTokenService.ValidateToken(malformedToken);

            // Assert
            Assert.Null(principal);
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error, // Generic catch logs as Error
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error validating token (general exception).")),
                    It.IsAny<ArgumentException>(), // JwtSecurityTokenHandler often throws ArgumentException for malformed tokens
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void ValidateToken_NullOrEmptyOrWhitespaceToken_ShouldReturnNullAndLogError()
        {
            // Test null token
            _mockLogger.Reset();
            Assert.Null(_jwtTokenService.ValidateToken(null));
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error, // Generic catch
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error validating token (general exception).")),
                    It.IsAny<ArgumentNullException>(), // token is null for ValidateToken method
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            
            // Test empty token
            _mockLogger.Reset();
            Assert.Null(_jwtTokenService.ValidateToken(""));
             _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error, // Generic catch
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error validating token (general exception).")),
                    It.IsAny<ArgumentException>(), // Empty string usually leads to ArgumentException in handler
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            // Test whitespace token
            _mockLogger.Reset();
            Assert.Null(_jwtTokenService.ValidateToken("   "));
            _mockLogger.Verify(
                logger => logger.Log(
                    LogLevel.Error, // Generic catch
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error validating token (general exception).")),
                    It.IsAny<ArgumentException>(), // Whitespace string also usually leads to ArgumentException
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}