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
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<MessageService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MessageDto> GetMessageByIdAsync(int id)
        {
            try
            {
                var message = await _unitOfWork.Messages.GetByIdAsync(id);
                return _mapper.Map<MessageDto>(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting message with ID: {MessageId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesByUserAsync(int userId, bool asSender = false)
        {
            try
            {
                var messages = await _unitOfWork.Messages.GetMessagesByUserAsync(userId, asSender);
                return _mapper.Map<IEnumerable<MessageDto>>(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting messages for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<MessageDto>> GetConversationAsync(int user1Id, int user2Id)
        {
            try
            {
                var messages = await _unitOfWork.Messages.GetConversationAsync(user1Id, user2Id);
                return _mapper.Map<IEnumerable<MessageDto>>(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting conversation between users: {User1Id} and {User2Id}", user1Id, user2Id);
                throw;
            }
        }

        public async Task<IEnumerable<MessageDto>> GetTransactionMessagesAsync(int transactionId)
        {
            try
            {
                var messages = await _unitOfWork.Messages.GetTransactionMessagesAsync(transactionId);
                return _mapper.Map<IEnumerable<MessageDto>>(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting messages for transaction: {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<int> SendMessageAsync(MessageCreateDto messageDto)
        {
            try
            {
                var message = _mapper.Map<Message>(messageDto);
                message.SentDate = DateTime.UtcNow;
                message.IsRead = false;
                
                await _unitOfWork.Messages.AddAsync(message);
                await _unitOfWork.CompleteAsync();
                
                // If this is a transaction message, update the transaction LastActivity
                if (message.TransactionId.HasValue)
                {
                    var transaction = await _unitOfWork.Transactions.GetByIdAsync(message.TransactionId.Value);
                    if (transaction != null)
                    {
                        // You might want to add a LastActivity property to Transaction class
                        // transaction.LastActivity = DateTime.UtcNow;
                        // _unitOfWork.Transactions.Update(transaction);
                        // await _unitOfWork.CompleteAsync();
                    }
                }
                
                return message.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending message");
                throw;
            }
        }

        public async Task MarkMessageAsReadAsync(int id, int userId)
        {
            try
            {
                var message = await _unitOfWork.Messages.GetByIdAsync(id);
                
                if (message == null)
                {
                    throw new KeyNotFoundException($"Message with ID {id} not found");
                }
                
                // Verify that the user is the recipient of the message
                if (message.ReceiverId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to mark this message as read");
                }
                
                message.IsRead = true;
                
                _unitOfWork.Messages.Update(message);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while marking message as read: {MessageId}", id);
                throw;
            }
        }

        public async Task MarkAllMessagesAsReadAsync(int receiverId, int senderId)
        {
            try
            {
                await _unitOfWork.Messages.MarkMessagesAsReadAsync(receiverId, senderId);
                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while marking all messages as read between sender {SenderId} and receiver {ReceiverId}", senderId, receiverId);
                throw;
            }
        }

        public async Task<int> GetUnreadMessagesCountAsync(int userId)
        {
            try
            {
                return await _unitOfWork.Messages.GetUnreadMessagesCountAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting unread messages count for user: {UserId}", userId);
                throw;
            }
        }
    }
}