using LeafLoop.Models;
using LeafLoop.Services.DTOs.Preferences;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace LeafLoop.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
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

    [HttpGet]
    public async Task<IActionResult> GetUserPreferences()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            var preferences = await _preferencesService.GetUserPreferencesAsync(user.Id);
            if (preferences == null) return this.ApiOk(new PreferencesData());

            return this.ApiOk(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user preferences for UserID: {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            return this.ApiInternalError("Error retrieving preferences", ex);
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateUserPreferences([FromBody] PreferencesData preferences)
    {
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            var success = await _preferencesService.UpdateUserPreferencesAsync(user.Id, preferences);
            if (!success) return this.ApiInternalError("Failed to update preferences.");

            return this.ApiNoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user preferences for UserID: {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            return this.ApiInternalError("Error updating preferences", ex);
        }
    }

    [HttpPut("theme")]
    public async Task<IActionResult> UpdateTheme([FromBody] ThemeUpdateDto request)
    {
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            var success = await _preferencesService.UpdateUserThemeAsync(user.Id, request.Theme);
            if (!success) return this.ApiInternalError("Failed to update theme.");

            return this.ApiNoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user theme for UserID: {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            return this.ApiInternalError("Error updating user theme", ex);
        }
    }

    [HttpPut("email-notifications")]
    public async Task<IActionResult> UpdateEmailNotifications([FromBody] EmailNotificationsUpdateDto request)
    {
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            var success = await _preferencesService.UpdateEmailNotificationsAsync(user.Id, request.Enabled);
            if (!success) return this.ApiInternalError("Failed to update email notification settings.");

            return this.ApiNoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email notification settings for UserID: {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            return this.ApiInternalError("Error updating email notification settings", ex);
        }
    }

    [HttpPut("language")]
    public async Task<IActionResult> UpdateLanguage([FromBody] LanguageUpdateDto request)
    {
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            var success = await _preferencesService.UpdateLanguagePreferenceAsync(user.Id, request.Language);
            if (!success) return this.ApiInternalError("Failed to update language preference.");

            return this.ApiNoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating language preference for UserID: {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            return this.ApiInternalError("Error updating language preference", ex);
        }
    }

    [HttpGet("theme")]
    public async Task<IActionResult> GetTheme()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            var theme = await _preferencesService.GetUserThemeAsync(user.Id);
            return this.ApiOk(new ThemeResponseDto { Theme = theme ?? "light" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user theme for UserID: {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            return this.ApiInternalError("Error retrieving theme", ex);
        }
    }
}