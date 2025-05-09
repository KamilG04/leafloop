using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LeafLoop.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
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

    [HttpGet]
    public async Task<IActionResult> GetUserNotifications()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id);
            return this.ApiOk(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user notifications for UserID: {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            return this.ApiInternalError("Error retrieving notifications", ex);
        }
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadNotificationsCount()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            var count = await _notificationService.GetUnreadNotificationsCountAsync(user.Id);
            return this.ApiOk(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread notifications count for UserID: {UserId}",
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            return this.ApiInternalError("Error retrieving notifications count", ex);
        }
    }

    [HttpPut("{id:int}/mark-read")]
    public async Task<IActionResult> MarkNotificationAsRead(int id)
    {
        if (id <= 0) return this.ApiBadRequest("Invalid Notification ID.");
        User? user = null;

        try
        {
            user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            await _notificationService.MarkNotificationAsReadAsync(id, user.Id);
            return this.ApiNoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound($"Notification with ID {id} not found.");
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiForbidden("You are not authorized to access this notification.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error marking notification as read. NotificationId: {NotificationId}, UserId: {UserId}", id, user?.Id);
            return this.ApiInternalError("Error updating notification", ex);
        }
    }

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllNotificationsAsRead()
    {
        User? user = null;
        try
        {
            user = await _userManager.GetUserAsync(User);
            if (user == null) return this.ApiUnauthorized("User not found.");

            await _notificationService.MarkAllNotificationsAsReadAsync(user.Id);
            return this.ApiNoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for UserID: {UserId}", user?.Id);
            return this.ApiInternalError("Error updating notifications", ex);
        }
    }

    [HttpPost("system")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSystemNotification([FromBody] SystemNotificationDto notificationDto)
    {
        if (!ModelState.IsValid) return this.ApiBadRequest(ModelState);

        try
        {
            await _notificationService.CreateSystemNotificationAsync(
                notificationDto.Type,
                notificationDto.Content,
                notificationDto.UserIds);

            return this.ApiNoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid arguments for system notification: {@NotificationDto}", notificationDto);
            return this.ApiBadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating system notification");
            return this.ApiInternalError("Error creating notification", ex);
        }
    }
}