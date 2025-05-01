using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]  // All notification endpoints require authentication
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
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/notifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var notifications = await _notificationService.GetUserNotificationsAsync(user.Id);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user notifications");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving notifications");
            }
        }

        // GET: api/notifications/unread-count
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadNotificationsCount()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var count = await _notificationService.GetUnreadNotificationsCountAsync(user.Id);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread notifications count");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving notifications count");
            }
        }

        // PUT: api/notifications/5/mark-read
        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                await _notificationService.MarkNotificationAsReadAsync(id, user.Id);
                
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Notification with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read. NotificationId: {NotificationId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating notification");
            }
        }

        // PUT: api/notifications/mark-all-read
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                
                await _notificationService.MarkAllNotificationsAsReadAsync(user.Id);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating notifications");
            }
        }

        // For admin/system use only
        // POST: api/notifications/system
        [HttpPost("system")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSystemNotification([FromBody] SystemNotificationDto notificationDto)
        {
            try
            {
                await _notificationService.CreateSystemNotificationAsync(
                    notificationDto.Type,
                    notificationDto.Content,
                    notificationDto.UserIds);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating system notification");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating notification");
            }
        }
    }

    // Helper class for system notifications
    public class SystemNotificationDto
    {
        public string Type { get; set; }
        public string Content { get; set; }
        public IEnumerable<int> UserIds { get; set; }
    }
}