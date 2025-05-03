using System;
using System.Threading.Tasks;
using LeafLoop.Models;          // Dla User
using LeafLoop.Models.API;      // Dla ApiResponse<T> i ApiResponse
using LeafLoop.Services.DTOs.Preferences; // Dla DTOs preferencji
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;      // Dla StatusCodes
using Microsoft.AspNetCore.Identity;  // Dla UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeafLoop.Api;             // <<<=== DODAJ TEN USING dla ApiControllerExtensions

namespace LeafLoop.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Wszystkie akcje wymagają autoryzacji
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

        // GET: api/preferences
        [HttpGet]
        public async Task<IActionResult> GetUserPreferences() // Zmieniono sygnaturę na IActionResult
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return this.ApiUnauthorized("User not found."); // Użyj ApiUnauthorized
                }

                var preferences = await _preferencesService.GetUserPreferencesAsync(user.Id); // Zakładam, że zwraca PreferencesData lub null
                if (preferences == null)
                {
                    // Można zwrócić Not Found lub pusty obiekt preferencji, zależnie od logiki
                    // return this.ApiNotFound("Preferences not found for this user.");
                    // Lub zwróć domyślne/puste preferencje:
                     return this.ApiOk(new PreferencesData()); // Zakładając, że pusty obiekt jest akceptowalny
                }

                return this.ApiOk(preferences); // Użyj ApiOk<T>
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user preferences for UserID: {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return this.ApiInternalError("Error retrieving preferences", ex); // Użyj ApiInternalError
            }
        }

        // PUT: api/preferences
        [HttpPut]
        public async Task<IActionResult> UpdateUserPreferences([FromBody] PreferencesData preferences) // Dodano FromBody
        {
            // Dodaj walidację modelu, jeśli PreferencesData ma atrybuty
            if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return this.ApiUnauthorized("User not found."); // Użyj ApiUnauthorized
                }

                // Zakładamy, że UpdateUserPreferencesAsync zwraca bool lub rzuca wyjątki
                var success = await _preferencesService.UpdateUserPreferencesAsync(user.Id, preferences);
                if (!success)
                {
                    // Zwróć błąd serwera lub BadRequest, jeśli problemem były dane
                    return this.ApiInternalError("Failed to update preferences.");
                }

                return this.ApiNoContent(); // Użyj ApiNoContent
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences for UserID: {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return this.ApiInternalError("Error updating preferences", ex); // Użyj ApiInternalError
            }
        }

        // PUT: api/preferences/theme
        [HttpPut("theme")]
        public async Task<IActionResult> UpdateTheme([FromBody] ThemeUpdateDto request) // Dodano FromBody
        {
             if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return this.ApiUnauthorized("User not found."); // Użyj ApiUnauthorized
                }

                var success = await _preferencesService.UpdateUserThemeAsync(user.Id, request.Theme);
                if (!success)
                {
                    return this.ApiInternalError("Failed to update theme."); // Użyj ApiInternalError
                }

                return this.ApiNoContent(); // Użyj ApiNoContent
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user theme for UserID: {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return this.ApiInternalError("Error updating user theme", ex); // Użyj ApiInternalError
            }
        }

        // PUT: api/preferences/email-notifications
        [HttpPut("email-notifications")]
        public async Task<IActionResult> UpdateEmailNotifications([FromBody] EmailNotificationsUpdateDto request) // Dodano FromBody
        {
             if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return this.ApiUnauthorized("User not found."); // Użyj ApiUnauthorized
                }

                var success = await _preferencesService.UpdateEmailNotificationsAsync(user.Id, request.Enabled);
                if (!success)
                {
                    return this.ApiInternalError("Failed to update email notification settings."); // Użyj ApiInternalError
                }

                return this.ApiNoContent(); // Użyj ApiNoContent
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email notification settings for UserID: {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return this.ApiInternalError("Error updating email notification settings", ex); // Użyj ApiInternalError
            }
        }

        // PUT: api/preferences/language
        [HttpPut("language")]
        public async Task<IActionResult> UpdateLanguage([FromBody] LanguageUpdateDto request) // Dodano FromBody
        {
             if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return this.ApiUnauthorized("User not found."); // Użyj ApiUnauthorized
                }

                var success = await _preferencesService.UpdateLanguagePreferenceAsync(user.Id, request.Language);
                if (!success)
                {
                    return this.ApiInternalError("Failed to update language preference."); // Użyj ApiInternalError
                }

                return this.ApiNoContent(); // Użyj ApiNoContent
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating language preference for UserID: {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return this.ApiInternalError("Error updating language preference", ex); // Użyj ApiInternalError
            }
        }

        // GET: api/preferences/theme
        [HttpGet("theme")]
        public async Task<IActionResult> GetTheme() // Zmieniono sygnaturę na IActionResult
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return this.ApiUnauthorized("User not found."); // Użyj ApiUnauthorized
                }

                var theme = await _preferencesService.GetUserThemeAsync(user.Id); // Zakładam, że zwraca string lub null

                // Zwróć obiekt DTO opakowany w ApiOk<T>
                return this.ApiOk(new ThemeResponseDto { Theme = theme ?? "light" }); // Zwróć domyślny, jeśli null
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user theme for UserID: {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return this.ApiInternalError("Error retrieving theme", ex); // Użyj ApiInternalError
            }
        }
    }

    // Definicje DTO powinny być w plikach DTO (np. Services/DTOs/Preferences/)
    /*
    using System.ComponentModel.DataAnnotations;

    namespace LeafLoop.Services.DTOs.Preferences
    {
        // Główne DTO preferencji (przykład)
        public class PreferencesData
        {
            public string Theme { get; set; } = "light";
            public bool EmailNotificationsEnabled { get; set; } = true;
            public string Language { get; set; } = "en";
            // Dodaj inne preferencje
        }

        // DTO do aktualizacji motywu
        public class ThemeUpdateDto
        {
            [Required]
            [RegularExpression("^(light|dark)$", ErrorMessage = "Theme must be 'light' or 'dark'")]
            public string Theme { get; set; } = null!;
        }

         // DTO do odpowiedzi z motywem
        public class ThemeResponseDto
        {
            public string Theme { get; set; } = "light";
        }

        // DTO do aktualizacji powiadomień email
        public class EmailNotificationsUpdateDto
        {
            [Required]
            public bool Enabled { get; set; }
        }

        // DTO do aktualizacji języka
        public class LanguageUpdateDto
        {
            [Required]
            [StringLength(10)] // Przykładowy limit
            public string Language { get; set; } = null!;
        }
    }
    */
}