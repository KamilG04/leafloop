using System;
using System.Text.Json;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class UserPreferencesService : IUserPreferencesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserPreferencesService> _logger;

        public UserPreferencesService(
            IUnitOfWork unitOfWork,
            ILogger<UserPreferencesService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PreferencesData> GetUserPreferencesAsync(int userId)
        {
            try
            {
                // First check if user exists
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                // Find or create user preferences
                var userPreferences = await _unitOfWork.SingleOrDefaultEntityAsync<UserPreferences>(up => up.UserId == userId);
                
                // If no preferences exist yet, return default preferences
                if (userPreferences == null)
                {
                    return new PreferencesData();
                }

                // Deserialize preferences JSON
                if (string.IsNullOrEmpty(userPreferences.PreferencesJson))
                {
                    return new PreferencesData();
                }

                try
                {
                    return JsonSerializer.Deserialize<PreferencesData>(userPreferences.PreferencesJson);
                }
                catch (JsonException)
                {
                    _logger.LogWarning("Invalid preferences JSON format for user {UserId}", userId);
                    return new PreferencesData();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user preferences for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UpdateUserPreferencesAsync(int userId, PreferencesData preferences)
        {
            try
            {
                // First check if user exists
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new KeyNotFoundException($"User with ID {userId} not found");
                }

                // Find or create user preferences
                var userPreferences = await _unitOfWork.SingleOrDefaultEntityAsync<UserPreferences>(up => up.UserId == userId);
                
                if (userPreferences == null)
                {
                    // Create new preferences record
                    userPreferences = new UserPreferences
                    {
                        UserId = userId,
                        PreferencesJson = JsonSerializer.Serialize(preferences),
                        LastUpdated = DateTime.UtcNow
                    };
                    
                    await _unitOfWork.AddEntityAsync(userPreferences);
                }
                else
                {
                    // Update existing preferences
                    userPreferences.PreferencesJson = JsonSerializer.Serialize(preferences);
                    userPreferences.LastUpdated = DateTime.UtcNow;
                    
                    _unitOfWork.UpdateEntity(userPreferences);
                }
                
                await _unitOfWork.CompleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user preferences for user ID: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateUserThemeAsync(int userId, string theme)
        {
            try
            {
                var preferences = await GetUserPreferencesAsync(userId);
                preferences.Theme = theme;
                return await UpdateUserPreferencesAsync(userId, preferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user theme for user ID: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateEmailNotificationsAsync(int userId, bool enabled)
        {
            try
            {
                var preferences = await GetUserPreferencesAsync(userId);
                preferences.EmailNotifications = enabled;
                return await UpdateUserPreferencesAsync(userId, preferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating email notifications for user ID: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateLanguagePreferenceAsync(int userId, string language)
        {
            try
            {
                var preferences = await GetUserPreferencesAsync(userId);
                preferences.Language = language;
                return await UpdateUserPreferencesAsync(userId, preferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating language preference for user ID: {UserId}", userId);
                return false;
            }
        }

        public async Task<string> GetUserThemeAsync(int userId)
        {
            try
            {
                var preferences = await GetUserPreferencesAsync(userId);
                return preferences.Theme;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user theme for user ID: {UserId}", userId);
                return "light"; // Default theme
            }
        }
    }
}