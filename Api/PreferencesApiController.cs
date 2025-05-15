using LeafLoop.Models;
using LeafLoop.Models.API; // For ApiResponse
using LeafLoop.Services.DTOs.Preferences; // For Preferences DTOs
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // For StatusCodes
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq; // For ModelState error logging
using System.Security.Claims; // For ClaimTypes
using System.Threading.Tasks; // For Task

namespace LeafLoop.Api
{
    /// <summary>
    /// Manages user preferences. All endpoints require authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ApiAuthPolicy")] // Apply API-specific auth policy
    [Produces("application/json")]
    public class PreferencesController : ControllerBase
    {
        private readonly IUserPreferencesService _preferencesService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<PreferencesController> _logger;

        public PreferencesController(
            IUserPreferencesService preferencesService,
            UserManager<User> userManager,
            ILogger<PreferencesController> logger)
        {
            _preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves the preferences for the authenticated user.
        /// </summary>
        /// <returns>The user's preferences. Returns default preferences if none are set.</returns>
        /// <response code="200">Returns the user's preferences.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PreferencesData>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserPreferences()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("API GetUserPreferences START for UserID Claim: {UserIdClaim}", userIdClaim ?? "N/A");
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API GetUserPreferences UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}", userIdClaim ?? "N/A");
                    return this.ApiUnauthorized("User not found.");
                }

                var preferences = await _preferencesService.GetUserPreferencesAsync(user.Id);
                if (preferences == null)
                {
                    _logger.LogInformation("API GetUserPreferences: No preferences found for UserID {UserId}, returning default.", user.Id);
                    return this.ApiOk(new PreferencesData());
                }
                _logger.LogInformation("API GetUserPreferences SUCCESS for UserID: {UserId}", user.Id);
                return this.ApiOk(preferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetUserPreferences ERROR for UserID Claim: {UserIdClaim}", userIdClaim ?? "N/A");
                return this.ApiInternalError("Error retrieving preferences.", ex);
            }
        }

        /// <summary>
        /// Updates the preferences for the authenticated user.
        /// </summary>
        /// <param name="preferencesDto">The new preferences data.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">Preferences updated successfully.</response>
        /// <response code="400">If the preferences data is invalid.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs or update fails.</response>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserPreferences([FromBody] PreferencesData preferencesDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("API UpdateUserPreferences START for UserID Claim: {UserIdClaim}. Preferences: {@PreferencesDto}", userIdClaim ?? "N/A", preferencesDto);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API UpdateUserPreferences BAD_REQUEST: Invalid model state for UserID Claim: {UserIdClaim}. Errors: {@ModelStateErrors}", 
                    userIdClaim ?? "N/A", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }
            
            User? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API UpdateUserPreferences UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}", userIdClaim ?? "N/A");
                    return this.ApiUnauthorized("User not found.");
                }

                var success = await _preferencesService.UpdateUserPreferencesAsync(user.Id, preferencesDto);
                if (!success)
                {
                    _logger.LogError("API UpdateUserPreferences ERROR: UpdateUserPreferencesAsync returned false for UserID: {UserId}", user.Id);
                    return this.ApiInternalError("Failed to update preferences. Please try again.");
                }
                _logger.LogInformation("API UpdateUserPreferences SUCCESS for UserID: {UserId}", user.Id);
                return this.ApiNoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UpdateUserPreferences EXCEPTION for UserID: {UserId}", user?.Id.ToString() ?? userIdClaim ?? "N/A");
                return this.ApiInternalError("Error updating preferences.", ex);
            }
        }

        /// <summary>
        /// Updates the theme preference for the authenticated user.
        /// </summary>
        /// <param name="themeUpdateDto">The DTO containing the new theme preference.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">Theme updated successfully.</response>
        /// <response code="400">If the theme data is invalid.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs or update fails.</response>
        [HttpPut("theme")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTheme([FromBody] ThemeUpdateDto themeUpdateDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("API UpdateTheme START for UserID Claim: {UserIdClaim}. Theme: {Theme}", userIdClaim ?? "N/A", themeUpdateDto?.Theme);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API UpdateTheme BAD_REQUEST: Invalid model state for UserID Claim: {UserIdClaim}. Errors: {@ModelStateErrors}", 
                    userIdClaim ?? "N/A", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }
            
            User? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                     _logger.LogWarning("API UpdateTheme UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}", userIdClaim ?? "N/A");
                    return this.ApiUnauthorized("User not found.");
                }

                var success = await _preferencesService.UpdateUserThemeAsync(user.Id, themeUpdateDto.Theme);
                if (!success)
                {
                    _logger.LogError("API UpdateTheme ERROR: UpdateUserThemeAsync returned false for UserID: {UserId}", user.Id);
                    return this.ApiInternalError("Failed to update theme.");
                }
                _logger.LogInformation("API UpdateTheme SUCCESS for UserID: {UserId}. New theme: {Theme}", user.Id, themeUpdateDto.Theme);
                return this.ApiNoContent();
            }
            catch (ArgumentException ex) 
            {
                _logger.LogWarning(ex, "API UpdateTheme BAD_REQUEST: Invalid theme value for UserID: {UserId}. Theme: {Theme}", 
                    user?.Id.ToString() ?? userIdClaim ?? "N/A", themeUpdateDto.Theme);
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UpdateTheme EXCEPTION for UserID: {UserId}", user?.Id.ToString() ?? userIdClaim ?? "N/A");
                return this.ApiInternalError("Error updating user theme.", ex);
            }
        }

        /// <summary>
        /// Updates the email notification preference for the authenticated user.
        /// </summary>
        /// <param name="emailNotificationsUpdateDto">The DTO containing the new email notification preference.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">Email notification setting updated successfully.</response>
        /// <response code="400">If the request data is invalid.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs or update fails.</response>
        [HttpPut("email-notifications")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmailNotifications([FromBody] EmailNotificationsUpdateDto emailNotificationsUpdateDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("API UpdateEmailNotifications START for UserID Claim: {UserIdClaim}. Enabled: {Enabled}", 
                userIdClaim ?? "N/A", emailNotificationsUpdateDto?.Enabled);

            if (!ModelState.IsValid)
            {
                 _logger.LogWarning("API UpdateEmailNotifications BAD_REQUEST: Invalid model state for UserID Claim: {UserIdClaim}. Errors: {@ModelStateErrors}", 
                    userIdClaim ?? "N/A", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }
            
            User? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API UpdateEmailNotifications UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}", userIdClaim ?? "N/A");
                    return this.ApiUnauthorized("User not found.");
                }

                var success = await _preferencesService.UpdateEmailNotificationsAsync(user.Id, emailNotificationsUpdateDto.Enabled);
                if (!success)
                {
                    _logger.LogError("API UpdateEmailNotifications ERROR: UpdateEmailNotificationsAsync returned false for UserID: {UserId}", user.Id);
                    return this.ApiInternalError("Failed to update email notification settings.");
                }
                _logger.LogInformation("API UpdateEmailNotifications SUCCESS for UserID: {UserId}. Enabled: {Enabled}", user.Id, emailNotificationsUpdateDto.Enabled);
                return this.ApiNoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UpdateEmailNotifications EXCEPTION for UserID: {UserId}", user?.Id.ToString() ?? userIdClaim ?? "N/A");
                return this.ApiInternalError("Error updating email notification settings.", ex);
            }
        }

        /// <summary>
        /// Updates the language preference for the authenticated user.
        /// </summary>
        /// <param name="languageUpdateDto">The DTO containing the new language preference.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">Language preference updated successfully.</response>
        /// <response code="400">If the language data is invalid.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs or update fails.</response>
        [HttpPut("language")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateLanguage([FromBody] LanguageUpdateDto languageUpdateDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("API UpdateLanguage START for UserID Claim: {UserIdClaim}. Language: {Language}", 
                userIdClaim ?? "N/A", languageUpdateDto?.Language);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API UpdateLanguage BAD_REQUEST: Invalid model state for UserID Claim: {UserIdClaim}. Errors: {@ModelStateErrors}", 
                    userIdClaim ?? "N/A", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }
            
            User? user = null;
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API UpdateLanguage UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}", userIdClaim ?? "N/A");
                    return this.ApiUnauthorized("User not found.");
                }

                var success = await _preferencesService.UpdateLanguagePreferenceAsync(user.Id, languageUpdateDto.Language);
                if (!success)
                {
                    _logger.LogError("API UpdateLanguage ERROR: UpdateLanguagePreferenceAsync returned false for UserID: {UserId}", user.Id);
                    return this.ApiInternalError("Failed to update language preference.");
                }
                _logger.LogInformation("API UpdateLanguage SUCCESS for UserID: {UserId}. New language: {Language}", user.Id, languageUpdateDto.Language);
                return this.ApiNoContent();
            }
            catch (ArgumentException ex) 
            {
                _logger.LogWarning(ex, "API UpdateLanguage BAD_REQUEST: Invalid language value for UserID: {UserId}. Language: {Language}", 
                    user?.Id.ToString() ?? userIdClaim ?? "N/A", languageUpdateDto.Language);
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UpdateLanguage EXCEPTION for UserID: {UserId}", user?.Id.ToString() ?? userIdClaim ?? "N/A");
                return this.ApiInternalError("Error updating language preference.", ex);
            }
        }

        /// <summary>
        /// Retrieves the theme preference for the authenticated user.
        /// </summary>
        /// <returns>The user's current theme preference.</returns>
        /// <response code="200">Returns the user's theme preference.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("theme")]
        [ProducesResponseType(typeof(ApiResponse<ThemeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTheme()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("API GetTheme START for UserID Claim: {UserIdClaim}", userIdClaim ?? "N/A");
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API GetTheme UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}", userIdClaim ?? "N/A");
                    return this.ApiUnauthorized("User not found.");
                }

                var theme = await _preferencesService.GetUserThemeAsync(user.Id);
                 _logger.LogInformation("API GetTheme SUCCESS for UserID: {UserId}. Theme: {Theme}", user.Id, theme);
                return this.ApiOk(new ThemeResponseDto { Theme = theme ?? "light" }); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetTheme ERROR for UserID Claim: {UserIdClaim}", userIdClaim ?? "N/A");
                return this.ApiInternalError("Error retrieving theme.", ex);
            }
        }
    }
}
