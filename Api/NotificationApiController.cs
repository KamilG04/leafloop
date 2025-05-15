using LeafLoop.Models;
using LeafLoop.Models.API; // For ApiResponse
using LeafLoop.Services.DTOs; // For SystemNotificationDto and NotificationDto
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // For StatusCodes
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // For IEnumerable
using System.Linq; // For Count()
using System.Security.Claims; // For ClaimTypes
using System.Threading.Tasks; // For Task

namespace LeafLoop.Api
{
    /// <summary>
    /// Manages user notifications. All endpoints require authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All actions in this controller require authentication by default
    [Produces("application/json")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            UserManager<User> userManager,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves all notifications for the authenticated user.
        /// </summary>
        /// <returns>A list of notifications for the user.</returns>
        /// <response code="200">Returns the list of notifications.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserNotifications()
        {
            var userIdForLogging = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "N/A";
            _logger.LogInformation("API GetUserNotifications START for UserID: {UserId}", userIdForLogging);
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API GetUserNotifications UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}", userIdForLogging);
                    return this.ApiUnauthorized("User not found.");
                }

                var notifications = await _notificationService.GetUserNotificationsAsync(user.Id);
                _logger.LogInformation("API GetUserNotifications SUCCESS for UserID: {UserId}. Count: {Count}", user.Id, notifications?.Count() ?? 0);
                return this.ApiOk(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetUserNotifications ERROR for UserID: {UserId}", userIdForLogging);
                return this.ApiInternalError("Error retrieving notifications.", ex);
            }
        }

        /// <summary>
        /// Retrieves the count of unread notifications for the authenticated user.
        /// </summary>
        /// <returns>The count of unread notifications.</returns>
        /// <response code="200">Returns the count of unread notifications.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)] // Changed from ApiResponse<int> to ApiResponse<object> to match 'new { count = count }'
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUnreadNotificationsCount()
        {
            var userIdForLogging = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "N/A";
            _logger.LogInformation("API GetUnreadNotificationsCount START for UserID: {UserId}", userIdForLogging);
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API GetUnreadNotificationsCount UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}", userIdForLogging);
                    return this.ApiUnauthorized("User not found.");
                }

                var count = await _notificationService.GetUnreadNotificationsCountAsync(user.Id);
                _logger.LogInformation("API GetUnreadNotificationsCount SUCCESS for UserID: {UserId}. Count: {Count}", user.Id, count);
                return this.ApiOk(new { count = count }); // Wrap in an object for consistent ApiResponse<T>
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetUnreadNotificationsCount ERROR for UserID: {UserId}", userIdForLogging);
                return this.ApiInternalError("Error retrieving notifications count.", ex);
            }
        }

        /// <summary>
        /// Marks a specific notification as read for the authenticated user.
        /// </summary>
        /// <param name="id">The ID of the notification to mark as read.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">Notification marked as read successfully.</response>
        /// <response code="400">If the notification ID is invalid.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="403">If the user is not authorized to access this notification.</response>
        /// <response code="404">If the notification with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPut("{id:int}/mark-read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var userIdForLogging = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "N/A";
            _logger.LogInformation("API MarkNotificationAsRead START for NotificationID: {NotificationId}, UserID: {UserId}", id, userIdForLogging);

            if (id <= 0)
            {
                _logger.LogWarning("API MarkNotificationAsRead BAD_REQUEST: Invalid Notification ID: {NotificationId}", id);
                return this.ApiBadRequest("Invalid Notification ID.");
            }
            
            User? user = null; // For logging in catch block
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API MarkNotificationAsRead UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}", userIdForLogging);
                    return this.ApiUnauthorized("User not found.");
                }

                await _notificationService.MarkNotificationAsReadAsync(id, user.Id);
                _logger.LogInformation("API MarkNotificationAsRead SUCCESS for NotificationID: {NotificationId}, UserID: {UserId}", id, user.Id);
                return this.ApiNoContent();
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("API MarkNotificationAsRead NOT_FOUND: Notification with ID {NotificationId} not found for UserID: {UserId}.", id, user?.Id);
                return this.ApiNotFound($"Notification with ID {id} not found.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("API MarkNotificationAsRead FORBIDDEN: UserID {UserId} not authorized for NotificationID {NotificationId}.", user?.Id, id);
                return this.ApiForbidden("You are not authorized to access this notification.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API MarkNotificationAsRead ERROR. NotificationId: {NotificationId}, UserId: {UserId}", id, user?.Id);
                return this.ApiInternalError("Error updating notification.", ex);
            }
        }

        /// <summary>
        /// Marks all notifications as read for the authenticated user.
        /// </summary>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">All notifications marked as read successfully.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPut("mark-all-read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var userIdForLogging = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "N/A";
            _logger.LogInformation("API MarkAllNotificationsAsRead START for UserID: {UserId}", userIdForLogging);
            User? user = null; // For logging in catch block
            try
            {
                user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API MarkAllNotificationsAsRead UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}", userIdForLogging);
                    return this.ApiUnauthorized("User not found.");
                }

                await _notificationService.MarkAllNotificationsAsReadAsync(user.Id);
                _logger.LogInformation("API MarkAllNotificationsAsRead SUCCESS for UserID: {UserId}", user.Id);
                return this.ApiNoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API MarkAllNotificationsAsRead ERROR for UserID: {UserId}", user?.Id);
                return this.ApiInternalError("Error updating notifications.", ex);
            }
        }

        /// <summary>
        /// Creates a system-wide notification or a notification for specific users. Requires Admin role.
        /// </summary>
        /// <param name="notificationDto">The system notification data, including type, content, and optional user IDs.</param>
        /// <returns>A 204 No Content response if successful.</returns>
        /// <response code="204">System notification created successfully.</response>
        /// <response code="400">If the notification data is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not an Admin.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("system")]
        [Authorize(Roles = "Admin")] // Only Admins can create system notifications
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)] 
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]     
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateSystemNotification([FromBody] SystemNotificationDto notificationDto) // Corrected DTO name
        {
            var adminUserIdForLogging = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "N/A";
            _logger.LogInformation("API CreateSystemNotification START by AdminID: {AdminId}. TargetUserIds: {TargetUserIdsCount}", adminUserIdForLogging, notificationDto?.UserIds?.Count() ?? 0);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API CreateSystemNotification BAD_REQUEST: Invalid model state by AdminID: {AdminId}. Errors: {@ModelStateErrors}", adminUserIdForLogging, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                await _notificationService.CreateSystemNotificationAsync(
                    notificationDto.Type,
                    notificationDto.Content,
                    notificationDto.UserIds 
                );
                _logger.LogInformation("API CreateSystemNotification SUCCESS by AdminID: {AdminId}", adminUserIdForLogging);
                return this.ApiNoContent(); 
            }
            catch (ArgumentException ex) 
            {
                // Corrected logging call: ensure notificationDto is passed as an object for structured logging if needed, or just use its properties.
                _logger.LogWarning(ex, "API CreateSystemNotification BAD_REQUEST: Invalid arguments for system notification by AdminID: {AdminId}. Type: {NotificationType}, Content: {NotificationContent}", adminUserIdForLogging, notificationDto.Type, notificationDto.Content);
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API CreateSystemNotification ERROR by AdminID: {AdminId}. DTO Type: {NotificationType}", adminUserIdForLogging, notificationDto?.Type);
                return this.ApiInternalError("Error creating system notification.", ex);
            }
        }
    }
}
