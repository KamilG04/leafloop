using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<NotificationDto> GetNotificationByIdAsync(int id)
        {
            try
            {
                var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
                return _mapper.Map<NotificationDto>(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting notification with ID: {NotificationId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId)
        {
            try
            {
                var notifications = await _unitOfWork.Notifications.GetUserNotificationsAsync(userId);
                return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting notifications for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUnreadNotificationsCountAsync(int userId)
        {
            try
            {
                return await _unitOfWork.Notifications.GetUnreadNotificationsCountAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting unread notifications count for user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<int> CreateNotificationAsync(NotificationCreateDto notificationDto)
        {
            try
            {
                var notification = _mapper.Map<Notification>(notificationDto);
                notification.SentDate = DateTime.UtcNow;
                notification.IsRead = false;
                
                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.CompleteAsync();
                
                return notification.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating notification");
                throw;
            }
        }

        public async Task CreateSystemNotificationAsync(string type, string content, IEnumerable<int> userIds)
        {
            try
            {
                // Begin a transaction for creating multiple notifications
                await _unitOfWork.BeginTransactionAsync();
                
                foreach (var userId in userIds)
                {
                    var notification = new Notification
                    {
                        Type = type,
                        Content = content,
                        UserId = userId,
                        SentDate = DateTime.UtcNow,
                        IsRead = false
                    };
                    
                    await _unitOfWork.Notifications.AddAsync(notification);
                }
                
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error occurred while creating system notifications");
                throw;
            }
        }

        public async Task MarkNotificationAsReadAsync(int id, int userId)
        {
            try
            {
                var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
                
                if (notification == null)
                {
                    throw new KeyNotFoundException($"Notification with ID {id} not found");
                }
                
                // Verify the notification belongs to the user
                if (notification.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to mark this notification as read");
                }
                
                notification.IsRead = true;
                _unitOfWork.Notifications.Update(notification);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while marking notification as read. NotificationId: {NotificationId}", id);
                throw;
            }
        }

        public async Task MarkAllNotificationsAsReadAsync(int userId)
        {
            try
            {
                await _unitOfWork.Notifications.MarkNotificationsAsReadAsync(userId);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while marking all notifications as read for user ID: {UserId}", userId);
                throw;
            }
        }
    }
}