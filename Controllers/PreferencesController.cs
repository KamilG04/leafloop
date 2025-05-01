using System;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.Interfaces;
using LeafLoop.Services.DTOs.Preferences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Api
{
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
            _preferencesService = preferencesService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/preferences
        [HttpGet]
        public async Task<ActionResult<PreferencesData>> GetUserPreferences()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var preferences = await _preferencesService.GetUserPreferencesAsync(user.Id);
                return Ok(preferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user preferences");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving preferences");
            }
        }

        // PUT: api/preferences
        [HttpPut]
        public async Task<IActionResult> UpdateUserPreferences([FromBody] PreferencesData preferences)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var success = await _preferencesService.UpdateUserPreferencesAsync(user.Id, preferences);
                if (!success)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update preferences");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating preferences");
            }
        }

        // PUT: api/preferences/theme
        [HttpPut("theme")]
        public async Task<IActionResult> UpdateTheme([FromBody] ThemeUpdateDto request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var success = await _preferencesService.UpdateUserThemeAsync(user.Id, request.Theme);
                if (!success)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update theme");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user theme");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating theme");
            }
        }

        // PUT: api/preferences/email-notifications
        [HttpPut("email-notifications")]
        public async Task<IActionResult> UpdateEmailNotifications([FromBody] EmailNotificationsUpdateDto request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var success = await _preferencesService.UpdateEmailNotificationsAsync(user.Id, request.Enabled);
                if (!success)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update email notification settings");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email notification settings");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating email notification settings");
            }
        }

        // PUT: api/preferences/language
        [HttpPut("language")]
        public async Task<IActionResult> UpdateLanguage([FromBody] LanguageUpdateDto request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var success = await _preferencesService.UpdateLanguagePreferenceAsync(user.Id, request.Language);
                if (!success)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update language preference");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating language preference");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating language preference");
            }
        }

        // GET: api/preferences/theme
        [HttpGet("theme")]
        public async Task<ActionResult<ThemeResponseDto>> GetTheme()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var theme = await _preferencesService.GetUserThemeAsync(user.Id);
                return Ok(new ThemeResponseDto { Theme = theme });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user theme");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving theme");
            }
        }
    }
}