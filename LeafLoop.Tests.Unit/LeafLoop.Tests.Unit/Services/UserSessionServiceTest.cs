
using System.Linq.Expressions;
using System.Net; 
using System.Security.Claims;
using System.Threading.Tasks;
using LeafLoop.Models; 
using LeafLoop.Repositories.Interfaces; 
using LeafLoop.Services;             
using LeafLoop.Services.DTOs;         
using Microsoft.AspNetCore.Http;      
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; 
using Moq;
using Xunit;

namespace LeafLoop.Tests.Unit.Services
{
    public class UserSessionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly ILogger<UserSessionService> _logger;
        private readonly UserSessionService _sessionService;

        public UserSessionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _logger = NullLogger<UserSessionService>.Instance;

            // Mock ExecuteInTransactionAsync to simply execute the passed function
            _mockUnitOfWork.Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(async (func) => await func());

            _sessionService = new UserSessionService(
                _mockUnitOfWork.Object
               
            );
        }

        private User CreateTestUser(int userId = 1) => new User { Id = userId, UserName = $"testuser{userId}" };

        private Mock<HttpContext> CreateMockHttpContext(string userAgent = "TestAgent/1.0", string ipAddress = "127.0.0.1", bool isAuthenticated = false, int userId = 0, string token = null)
        {
            var mockHttpContext = new Mock<HttpContext>();
            var mockHttpRequest = new Mock<HttpRequest>();
            var mockConnectionInfo = new Mock<ConnectionInfo>();
            var headers = new HeaderDictionary();
            
            if (!string.IsNullOrEmpty(userAgent))
            {
                headers["User-Agent"] = userAgent;
            }
            if (!string.IsNullOrEmpty(token))
            {
                headers["Authorization"] = $"Bearer {token}";
            }

            mockHttpRequest.Setup(req => req.Headers).Returns(headers);
            
            if (!string.IsNullOrEmpty(ipAddress))
            {
                mockConnectionInfo.Setup(conn => conn.RemoteIpAddress).Returns(IPAddress.Parse(ipAddress));
            }
            else
            {
                mockConnectionInfo.Setup(conn => conn.RemoteIpAddress).Returns((IPAddress)null);
            }
            
            mockHttpContext.Setup(ctx => ctx.Request).Returns(mockHttpRequest.Object);
            mockHttpContext.Setup(ctx => ctx.Connection).Returns(mockConnectionInfo.Object);

            var claims = new List<Claim>();
            if (isAuthenticated && userId > 0)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
            }
            var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuthType" : null);
            var claimsPrincipal = new ClaimsPrincipal(identity);
            mockHttpContext.Setup(ctx => ctx.User).Returns(claimsPrincipal);

            return mockHttpContext;
        }


        // --- Tests for CreateSessionAsync ---
        [Fact]
        public async Task CreateSessionAsync_ValidInput_ShouldCreateAndReturnSession()
        {
            // Arrange
            var user = CreateTestUser();
            var token = "test-jwt-token";
            var refreshToken = "test-refresh-token";
            var mockHttpContext = CreateMockHttpContext("Test UserAgent", "192.168.1.1");
            
            UserSession capturedSession = null;
            _mockUnitOfWork.Setup(uow => uow.AddEntityAsync(It.IsAny<UserSession>()))
                .Callback<UserSession>(s => capturedSession = s) // Capture the session being added
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sessionService.CreateSessionAsync(user, token, refreshToken, mockHttpContext.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(token, result.Token);
            Assert.Equal(refreshToken, result.RefreshToken);
            Assert.Equal("Test UserAgent", result.UserAgent);
            Assert.Equal("192.168.1.1", result.IpAddress);
            Assert.True(result.IsActive);
            Assert.True(result.LoginTime > DateTime.UtcNow.AddMinutes(-1));
            Assert.True(result.LastActivity > DateTime.UtcNow.AddMinutes(-1));

            _mockUnitOfWork.Verify(uow => uow.AddEntityAsync(It.IsAny<UserSession>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once); // Inside ExecuteInTransactionAsync
        }

        // --- Tests for GetActiveSessionsForUserAsync ---
        [Fact]
        public async Task GetActiveSessionsForUserAsync_UserHasActiveSessions_ShouldReturnMappedDtos()
        {
            // Arrange
            var userId = 1;
            var sessionEntities = new List<UserSession>
            {
                new UserSession { Id = 1, UserId = userId, UserAgent = "Agent1", IpAddress = "1.1.1.1", LoginTime = DateTime.UtcNow.AddHours(-1), LastActivity = DateTime.UtcNow, IsActive = true },
                new UserSession { Id = 2, UserId = userId, UserAgent = "Agent2", IpAddress = "2.2.2.2", LoginTime = DateTime.UtcNow.AddHours(-2), LastActivity = DateTime.UtcNow.AddMinutes(-30), IsActive = true }
            };
            _mockUnitOfWork.Setup(uow => uow.FindEntitiesAsync<UserSession>(It.IsAny<Expression<Func<UserSession, bool>>>()))
                .ReturnsAsync(sessionEntities);

            // Act
            var result = await _sessionService.GetActiveSessionsForUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            var firstResult = result.First();
            Assert.Equal(sessionEntities.First().UserAgent, firstResult.UserAgent);
            _mockUnitOfWork.Verify(uow => uow.FindEntitiesAsync<UserSession>(It.IsAny<Expression<Func<UserSession, bool>>>()), Times.Once);
        }

        // --- Tests for TerminateSessionAsync ---
        [Fact]
        public async Task TerminateSessionAsync_SessionExistsAndBelongsToUser_ShouldTerminateAndReturnTrue()
        {
            // Arrange
            var sessionId = 1;
            var userId = 1;
            var sessionEntity = new UserSession { Id = sessionId, UserId = userId, IsActive = true };
            _mockUnitOfWork.Setup(uow => uow.SingleOrDefaultEntityAsync<UserSession>(It.IsAny<Expression<Func<UserSession, bool>>>()))
                .ReturnsAsync(sessionEntity);
            _mockUnitOfWork.Setup(uow => uow.UpdateEntity(sessionEntity));
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _sessionService.TerminateSessionAsync(sessionId, userId);

            // Assert
            Assert.True(result);
            Assert.False(sessionEntity.IsActive);
            Assert.NotNull(sessionEntity.LogoutTime);
            _mockUnitOfWork.Verify(uow => uow.UpdateEntity(sessionEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task TerminateSessionAsync_SessionNotFound_ShouldReturnFalse()
        {
            // Arrange
            _mockUnitOfWork.Setup(uow => uow.SingleOrDefaultEntityAsync<UserSession>(It.IsAny<Expression<Func<UserSession, bool>>>()))
                .ReturnsAsync((UserSession)null);

            // Act
            var result = await _sessionService.TerminateSessionAsync(99, 1);

            // Assert
            Assert.False(result);
            _mockUnitOfWork.Verify(uow => uow.UpdateEntity(It.IsAny<UserSession>()), Times.Never);
        }

        // --- Tests for UpdateSessionActivityAsync ---
        [Fact]
        public async Task UpdateSessionActivityAsync_AuthenticatedUserWithActiveSession_ShouldUpdateLastActivity()
        {
            // Arrange
            var userId = 1;
            var token = "active-token";
            var mockHttpContext = CreateMockHttpContext(isAuthenticated: true, userId: userId, token: token);
            var sessionEntity = new UserSession { UserId = userId, Token = token, IsActive = true, LastActivity = DateTime.UtcNow.AddHours(-1) };
            
            _mockUnitOfWork.Setup(uow => uow.SingleOrDefaultEntityAsync<UserSession>(It.IsAny<Expression<Func<UserSession, bool>>>()))
                .ReturnsAsync(sessionEntity);
            _mockUnitOfWork.Setup(uow => uow.UpdateEntity(sessionEntity));
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);
            
            var initialLastActivity = sessionEntity.LastActivity;

            // Act
            await _sessionService.UpdateSessionActivityAsync(mockHttpContext.Object);

            // Assert
            Assert.True(sessionEntity.LastActivity > initialLastActivity);
            _mockUnitOfWork.Verify(uow => uow.UpdateEntity(sessionEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateSessionActivityAsync_UserNotAuthenticated_ShouldDoNothing()
        {
            // Arrange
            var mockHttpContext = CreateMockHttpContext(isAuthenticated: false); // Not authenticated

            // Act
            await _sessionService.UpdateSessionActivityAsync(mockHttpContext.Object);

            // Assert: No DB calls should be made
            _mockUnitOfWork.Verify(uow => uow.SingleOrDefaultEntityAsync<UserSession>(It.IsAny<Expression<Func<UserSession, bool>>>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never);
        }
        
        [Fact]
        public async Task UpdateSessionActivityAsync_SessionNotFound_ShouldDoNothing()
        {
            // Arrange
            var userId = 1;
            var token = "non-existent-token";
            var mockHttpContext = CreateMockHttpContext(isAuthenticated: true, userId: userId, token: token);
            _mockUnitOfWork.Setup(uow => uow.SingleOrDefaultEntityAsync<UserSession>(It.IsAny<Expression<Func<UserSession, bool>>>()))
                .ReturnsAsync((UserSession)null); // Session not found

            // Act
            await _sessionService.UpdateSessionActivityAsync(mockHttpContext.Object);

            // Assert
            _mockUnitOfWork.Verify(uow => uow.UpdateEntity(It.IsAny<UserSession>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        // --- Tests for ValidateSessionAsync ---
        [Fact]
        public async Task ValidateSessionAsync_ValidAndActiveToken_ShouldReturnTrue()
        {
            // Arrange
            var token = "valid-active-token";
            var sessionEntity = new UserSession { Token = token, IsActive = true };
            _mockUnitOfWork.Setup(uow => uow.SingleOrDefaultEntityAsync<UserSession>(It.IsAny<Expression<Func<UserSession, bool>>>()))
                .ReturnsAsync(sessionEntity);

            // Act
            var result = await _sessionService.ValidateSessionAsync(token);

            // Assert
            Assert.True(result);
        }
       
    }
}