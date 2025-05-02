using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Models.API;
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
    [Authorize] // All transaction endpoints require authentication
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IMessageService _messageService;
        private readonly IRatingService _ratingService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(
            ITransactionService transactionService,
            IMessageService messageService,
            IRatingService ratingService,
            UserManager<User> userManager,
            ILogger<TransactionsController> logger)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/transactions
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<TransactionDto>>>> GetUserTransactions(
            [FromQuery] bool asSeller = false)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var transactions = await _transactionService.GetTransactionsByUserAsync(user.Id, asSeller);
                return this.ApiOk(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user transactions");
                return this.ApiError<IEnumerable<TransactionDto>>(
                    StatusCodes.Status500InternalServerError, "Error retrieving transactions");
            }
        }

        // GET: api/transactions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<TransactionWithDetailsDto>>> GetTransaction(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Check if user can access this transaction
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return Forbid();
                }

                var transaction = await _transactionService.GetTransactionWithDetailsAsync(id);

                if (transaction == null)
                {
                    return this.ApiNotFound<TransactionWithDetailsDto>($"Transaction with ID {id} not found");
                }

                return this.ApiOk(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction details. TransactionId: {TransactionId}", id);
                return this.ApiError<TransactionWithDetailsDto>(
                    StatusCodes.Status500InternalServerError, "Error retrieving transaction details");
            }
        }

        // POST: api/transactions
        [HttpPost]
        public async Task<ActionResult<ApiResponse<int>>> InitiateTransaction(TransactionCreateDto transactionDto)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Set the buyer ID to current user
                transactionDto.BuyerId = user.Id;

                var transactionId = await _transactionService.InitiateTransactionAsync(transactionDto);

                // Get the created transaction for the response
                var transaction = await _transactionService.GetTransactionByIdAsync(transactionId);
                if (transaction == null)
                {
                    return this.ApiError<int>(
                        StatusCodes.Status500InternalServerError, "Transaction created but could not be retrieved");
                }

                return Created($"/api/transactions/{transactionId}", 
                    ApiResponse<int>.SuccessResponse(transactionId, "Transaction initiated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating transaction");
                return this.ApiError<int>(
                    StatusCodes.Status500InternalServerError, "Error initiating transaction");
            }
        }

        // PUT: api/transactions/5/status
        [HttpPut("{id}/status")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateTransactionStatus(
            int id, [FromBody] TransactionStatusUpdateDto statusUpdateDto)
        {
            if (id != statusUpdateDto.TransactionId)
            {
                return this.ApiBadRequest<object>("Transaction ID mismatch");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);

                await _transactionService.UpdateTransactionStatusAsync(id, statusUpdateDto.Status, user.Id);

                return this.ApiOk<object>(null, $"Transaction status updated to {statusUpdateDto.Status}");
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound<object>($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction status. TransactionId: {TransactionId}", id);
                return this.ApiError<object>(
                    StatusCodes.Status500InternalServerError, "Error updating transaction status");
            }
        }

        // POST: api/transactions/5/complete
        [HttpPost("{id}/complete")]
        public async Task<ActionResult<ApiResponse<object>>> CompleteTransaction(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                await _transactionService.CompleteTransactionAsync(id, user.Id);

                return this.ApiOk<object>(null, "Transaction completed successfully");
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound<object>($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing transaction. TransactionId: {TransactionId}", id);
                return this.ApiError<object>(
                    StatusCodes.Status500InternalServerError, "Error completing transaction");
            }
        }

        // POST: api/transactions/5/cancel
        [HttpPost("{id}/cancel")]
        public async Task<ActionResult<ApiResponse<object>>> CancelTransaction(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                await _transactionService.CancelTransactionAsync(id, user.Id);

                return this.ApiOk<object>(null, "Transaction cancelled successfully");
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound<object>($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling transaction. TransactionId: {TransactionId}", id);
                return this.ApiError<object>(
                    StatusCodes.Status500InternalServerError, "Error cancelling transaction");
            }
        }

        // GET: api/transactions/5/messages
        [HttpGet("{id}/messages")]
        public async Task<ActionResult<ApiResponse<IEnumerable<MessageDto>>>> GetTransactionMessages(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Check if user can access this transaction
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return Forbid();
                }

                var messages = await _messageService.GetTransactionMessagesAsync(id);
                return this.ApiOk(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction messages. TransactionId: {TransactionId}", id);
                return this.ApiError<IEnumerable<MessageDto>>(
                    StatusCodes.Status500InternalServerError, "Error retrieving transaction messages");
            }
        }

        // POST: api/transactions/5/messages
        [HttpPost("{id}/messages")]
        public async Task<ActionResult<ApiResponse<int>>> SendTransactionMessage(
            int id, [FromBody] TransactionMessageDto messageDto)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Check if user can access this transaction
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return Forbid();
                }

                // Get transaction to determine sender and receiver
                var transaction = await _transactionService.GetTransactionByIdAsync(id);

                var messageCreateDto = new MessageCreateDto
                {
                    Content = messageDto.Content,
                    SenderId = user.Id,
                    // Set receiver to the other party in the transaction
                    ReceiverId = user.Id == transaction.SellerId ? transaction.BuyerId : transaction.SellerId,
                    TransactionId = id
                };

                var messageId = await _messageService.SendMessageAsync(messageCreateDto);

                return this.ApiOk(messageId, "Message sent successfully");
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound<int>($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending transaction message. TransactionId: {TransactionId}", id);
                return this.ApiError<int>(
                    StatusCodes.Status500InternalServerError, "Error sending message");
            }
        }

        // POST: api/transactions/5/ratings
        [HttpPost("{id}/ratings")]
        public async Task<ActionResult<ApiResponse<int>>> RateTransaction(
            int id, [FromBody] TransactionRatingDto ratingDto)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Check if user can access this transaction
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return Forbid();
                }

                // Get transaction to determine who's being rated
                var transaction = await _transactionService.GetTransactionByIdAsync(id);

                // Create rating
                var ratingCreateDto = new RatingCreateDto
                {
                    Value = ratingDto.Value,
                    Comment = ratingDto.Comment,
                    RaterId = user.Id,
                    // Set rated entity to the other party in the transaction
                    RatedEntityId = user.Id == transaction.SellerId ? transaction.BuyerId : transaction.SellerId,
                    RatedEntityType = RatedEntityType.User,
                    TransactionId = id
                };

                var ratingId = await _ratingService.AddRatingAsync(ratingCreateDto);

                return this.ApiOk(ratingId, "Rating submitted successfully");
            }
            catch (KeyNotFoundException)
            {
                return this.ApiNotFound<int>($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rating transaction. TransactionId: {TransactionId}", id);
                return this.ApiError<int>(
                    StatusCodes.Status500InternalServerError, "Error rating transaction");
            }
        }

        // GET: api/transactions/5/ratings
        [HttpGet("{id}/ratings")]
        public async Task<ActionResult<ApiResponse<IEnumerable<RatingDto>>>> GetTransactionRatings(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Check if user can access this transaction
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return Forbid();
                }

                var ratings = await _ratingService.GetRatingsByTransactionAsync(id);
                return this.ApiOk(ratings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction ratings. TransactionId: {TransactionId}", id);
                return this.ApiError<IEnumerable<RatingDto>>(
                    StatusCodes.Status500InternalServerError, "Error retrieving transaction ratings");
            }
        }
    }

    // Helper class for transaction status update
    public class TransactionStatusUpdateDto
    {
        public int TransactionId { get; set; }
        public TransactionStatus Status { get; set; }
    }

    // Helper class for transaction message
    public class TransactionMessageDto
    {
        public string Content { get; set; }
    }

    // Helper class for transaction rating
    public class TransactionRatingDto
    {
        public int Value { get; set; }
        public string Comment { get; set; }
    }
}