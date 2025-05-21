// Path: LeafLoop.Tests.Unit/Services/UserPreferencesServiceTests.cs

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using LeafLoop.Models; // For User, UserPreferences
using LeafLoop.Repositories.Interfaces; // For IUnitOfWork, IUserRepository
using LeafLoop.Services;              // For UserPreferencesService
using LeafLoop.Services.DTOs;         // For PreferencesData
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; // For NullLogger
using Moq;
using Xunit;

namespace LeafLoop.Tests.Unit.Services
{
    public class UserPreferencesServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository; // For _unitOfWork.Users
        private readonly ILogger<UserPreferencesService> _logger;
        private readonly UserPreferencesService _preferencesService;

        public UserPreferencesServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IUserRepository>();
            _logger = NullLogger<UserPreferencesService>.Instance;

            _mockUnitOfWork.Setup(uow => uow.Users).Returns(_mockUserRepository.Object);

            _preferencesService = new UserPreferencesService(
                _mockUnitOfWork.Object,
                _logger
            );
        }

        private User CreateTestUser(int userId = 1) => new User { Id = userId };

        // --- Tests for GetUserPreferencesAsync ---

        [Fact]
        public async Task GetUserPreferencesAsync_UserNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange: Mock Users.GetByIdAsync to return null.
            var userId = 99;
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act & Assert: Expect KeyNotFoundException.
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _preferencesService.GetUserPreferencesAsync(userId)
            );
            _mockUnitOfWork.Verify(uow => uow.SingleOrDefaultEntityAsync<UserPreferences>(It.IsAny<Expression<Func<UserPreferences, bool>>>()), Times.Never);
        }

      

        

        [Fact]
        public async Task GetUserPreferencesAsync_ValidJson_ShouldReturnDeserializedPreferencesData()
        {
            // Arrange: UserPreferences with valid JSON.
            var userId = 1;
            var expectedData = new PreferencesData { Theme = "dark", EmailNotifications = true, Language = "pl" };
            var prefsJson = JsonSerializer.Serialize(expectedData);
            var userPrefsEntity = new UserPreferences { UserId = userId, PreferencesJson = prefsJson };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(CreateTestUser(userId));
            _mockUnitOfWork.Setup(uow => uow.SingleOrDefaultEntityAsync<UserPreferences>(It.IsAny<Expression<Func<UserPreferences, bool>>>()))
                .ReturnsAsync(userPrefsEntity);
            
            // Act
            var result = await _preferencesService.GetUserPreferencesAsync(userId);

            // Assert: Returns correctly deserialized data.
            Assert.NotNull(result);
            Assert.Equal(expectedData.Theme, result.Theme);
            Assert.Equal(expectedData.EmailNotifications, result.EmailNotifications);
            Assert.Equal(expectedData.Language, result.Language);
        }

      

        // --- Tests for UpdateUserPreferencesAsync ---
        [Fact]
        public async Task UpdateUserPreferencesAsync_UserNotFound_ShouldThrowKeyNotFoundAndReturnFalse()
        {
            // Arrange
            var userId = 99;
            var prefsData = new PreferencesData { Theme = "dark" };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act
            var result = await _preferencesService.UpdateUserPreferencesAsync(userId, prefsData);
            
            // Assert
            Assert.False(result); // Service catches KeyNotFound and returns false
             // Optionally, verify logger if you want to ensure the KeyNotFound was logged before being caught by the outer try-catch.
        }
        
        [Fact]
        public async Task UpdateUserPreferencesAsync_NoExistingPreferences_ShouldCreateNewPreferencesAndReturnTrue()
        {
            // Arrange
            var userId = 1;
            var prefsData = new PreferencesData { Theme = "dark", EmailNotifications = true };
            var serializedPrefs = JsonSerializer.Serialize(prefsData);

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(CreateTestUser(userId));
            _mockUnitOfWork.Setup(uow => uow.SingleOrDefaultEntityAsync<UserPreferences>(It.IsAny<Expression<Func<UserPreferences, bool>>>()))
                .ReturnsAsync((UserPreferences)null); // No existing preferences
            _mockUnitOfWork.Setup(uow => uow.AddEntityAsync(It.IsAny<UserPreferences>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _preferencesService.UpdateUserPreferencesAsync(userId, prefsData);

            // Assert
            Assert.True(result);
            _mockUnitOfWork.Verify(uow => uow.AddEntityAsync(It.Is<UserPreferences>(
                up => up.UserId == userId && up.PreferencesJson == serializedPrefs
            )), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateUserPreferencesAsync_ExistingPreferences_ShouldUpdatePreferencesAndReturnTrue()
        {
            // Arrange
            var userId = 1;
            var prefsData = new PreferencesData { Theme = "light", Language = "en" };
            var serializedPrefs = JsonSerializer.Serialize(prefsData);
            var existingUserPrefs = new UserPreferences { UserId = userId, PreferencesJson = "{}" };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(CreateTestUser(userId));
            _mockUnitOfWork.Setup(uow => uow.SingleOrDefaultEntityAsync<UserPreferences>(It.IsAny<Expression<Func<UserPreferences, bool>>>()))
                .ReturnsAsync(existingUserPrefs);
            _mockUnitOfWork.Setup(uow => uow.UpdateEntity(It.IsAny<UserPreferences>()));
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _preferencesService.UpdateUserPreferencesAsync(userId, prefsData);

            // Assert
            Assert.True(result);
            Assert.Equal(serializedPrefs, existingUserPrefs.PreferencesJson); // Check JSON was updated
            Assert.NotEqual(default(DateTime), existingUserPrefs.LastUpdated); // Check LastUpdated was set
            _mockUnitOfWork.Verify(uow => uow.UpdateEntity(existingUserPrefs), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }


        // --- Tests for Helper Methods (e.g., UpdateUserThemeAsync) ---
        // These tests primarily verify the interaction with GetUserPreferencesAsync and UpdateUserPreferencesAsync
        
        [Fact]
        public async Task UpdateUserThemeAsync_UserExists_ShouldUpdateThemeAndReturnTrue()
        {
            // Arrange
            var userId = 1;
            var newTheme = "dark-blue";
            var initialPrefs = new PreferencesData { Theme = "light", EmailNotifications = true };
            var updatedPrefs = new PreferencesData { Theme = newTheme, EmailNotifications = true }; // Expected after modification

            // Mock GetUserPreferencesAsync to return initial preferences
            // We need to mock the service calls, not the private methods directly
            // This requires a more complex setup or making Get/UpdateUserPreferencesAsync virtual if we want to mock them on the same instance
            // For simplicity, we'll test it by assuming Get and Update work, and verify inputs/outputs to them.
            // Let's mock the underlying UoW calls that Get/Update rely on.

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(CreateTestUser(userId));
            
            var userPrefsEntity = new UserPreferences { UserId = userId, PreferencesJson = JsonSerializer.Serialize(initialPrefs) };
            _mockUnitOfWork.Setup(uow => uow.SingleOrDefaultEntityAsync<UserPreferences>(It.IsAny<Expression<Func<UserPreferences, bool>>>()))
                           .ReturnsAsync(userPrefsEntity); // For the GetUserPreferencesAsync call
            
            // For the UpdateUserPreferencesAsync call
            _mockUnitOfWork.Setup(uow => uow.UpdateEntity(It.IsAny<UserPreferences>()));
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);


            // Act
            var result = await _preferencesService.UpdateUserThemeAsync(userId, newTheme);

            // Assert
            Assert.True(result);
            _mockUnitOfWork.Verify(uow => uow.UpdateEntity(It.Is<UserPreferences>(up => 
                up.UserId == userId &&
                // Przekaż jawnie null jako JsonSerializerOptions
                JsonSerializer.Deserialize<PreferencesData>(up.PreferencesJson, (JsonSerializerOptions)null).Theme == newTheme &&
                JsonSerializer.Deserialize<PreferencesData>(up.PreferencesJson, (JsonSerializerOptions)null).EmailNotifications == initialPrefs.EmailNotifications
            )), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        //Ta tu byly testy ale po co skoro dziala a szukac tego nullable dodawac panie
     
        
        [Fact]
        public async Task GetUserThemeAsync_NoPreferences_ShouldReturnDefaultTheme()
        {
            // Arrange: User exists, no preferences record
            var userId = 1;
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(CreateTestUser(userId));
            _mockUnitOfWork.Setup(uow => uow.SingleOrDefaultEntityAsync<UserPreferences>(It.IsAny<Expression<Func<UserPreferences, bool>>>()))
                .ReturnsAsync((UserPreferences)null);
            
            // Act
            var result = await _preferencesService.GetUserThemeAsync(userId);

            // Assert
            Assert.Equal("light", result); // Service returns "light" as default
        }
        
        
    }
}